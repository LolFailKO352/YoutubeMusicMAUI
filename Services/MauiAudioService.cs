using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;

namespace Melodium.Services;

public class MauiAudioService : IAudioService, IDisposable
{
    private MediaElement? _mediaElement;
    private string? _currentTempFile;
    private string? _currentPlayFile;
    private bool _disposed;
    private float _volume = 0.5f;

    public event Action<TimeSpan, TimeSpan>? PositionChanged;
    public event Action? MediaEnded;
    public event Action<bool>? PlaybackStateChanged;
    public event Action<string>? PlaybackError;

    public void Initialize(MediaElement mediaElement)
    {
        _mediaElement = mediaElement;
        
        // Remove existing handlers to avoid memory leaks if initialized multiple times
        _mediaElement.MediaEnded -= OnMediaEnded;
        _mediaElement.PositionChanged -= OnPositionChanged;
        _mediaElement.MediaFailed -= OnMediaFailed;
        _mediaElement.StateChanged -= OnStateChanged;

        _mediaElement.MediaEnded += OnMediaEnded;
        _mediaElement.PositionChanged += OnPositionChanged;
        _mediaElement.MediaFailed += OnMediaFailed;
        _mediaElement.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, MediaStateChangedEventArgs e)
    {
        PlaybackStateChanged?.Invoke(IsPlaying);
    }

    private void OnMediaFailed(object? sender, MediaFailedEventArgs e)
    {
        Log($"MediaElement Failed: {e.ErrorMessage}");
        PlaybackError?.Invoke($"Chyba přehrávače: {e.ErrorMessage}");
        PlaybackStateChanged?.Invoke(false);
    }

    private void OnPositionChanged(object? sender, MediaPositionChangedEventArgs e)
    {
        if (_mediaElement != null)
        {
            PositionChanged?.Invoke(e.Position, _mediaElement.Duration);
        }
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        PlaybackStateChanged?.Invoke(false);
        MediaEnded?.Invoke();
    }

    public bool IsPlaying => _mediaElement?.CurrentState == MediaElementState.Playing;

    public float Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0f, 1f);
            if (_mediaElement != null)
            {
                _mediaElement.Volume = _volume;
            }
        }
    }

    private static void Log(string message)
    {
        try
        {
            var msg = $"[{DateTime.Now:HH:mm:ss.fff}] [MauiAudioService] {message}\n";
            var logPath = Path.Combine(Path.GetTempPath(), "ytm_audio_log.txt");
            File.AppendAllText(logPath, msg);
            System.Diagnostics.Debug.WriteLine(msg);
        }
        catch { }
    }

    public async Task PlayFromUrlAsync(string streamUrl, CancellationToken cancellationToken, Action<string>? statusCallback = null)
    {
        if (_mediaElement == null)
        {
            PlaybackError?.Invoke("Přehrávač nebyl inicializován. Chybí MediaElement.");
            return;
        }

        Log($"PlayFromUrlAsync called. URL length: {streamUrl?.Length}");

        Stop();
        CleanupTempFile();

        statusCallback?.Invoke("Stahování audio streamu...");

        var tempPath = await DownloadStreamAsync(streamUrl, cancellationToken, statusCallback);
        if (string.IsNullOrEmpty(tempPath))
        {
            Log("Download returned null/empty path");
            PlaybackError?.Invoke("Nepodařilo se stáhnout audio stream.");
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var fileInfo = new FileInfo(tempPath);
        Log($"Downloaded file: {tempPath}, size: {fileInfo.Length} bytes, exists: {fileInfo.Exists}");

        if (!fileInfo.Exists || fileInfo.Length < 1000)
        {
            Log($"File too small or doesn't exist: {fileInfo.Length} bytes");
            PlaybackError?.Invoke($"Stažený soubor je příliš malý ({fileInfo.Length} B).");
            return;
        }

        try
        {
            statusCallback?.Invoke("Zpracovávám audio...");
            string playPath = tempPath;

            // Media Foundation (Windows) and sometimes ExoPlayer (Android) cannot play 
            // fragmented MP4 (itag 140) directly from a local file without a chunk index.
            // We must convert it to ADTS AAC to ensure maximum compatibility.
            try
            {
                if (tempPath.EndsWith(".m4a"))
                {
                    playPath = FragmentedMp4Demuxer.DemuxToAacFile(tempPath);
                }
            }
            catch (Exception demuxEx)
            {
                Log($"Demux error: {demuxEx}");
                PlaybackError?.Invoke("Chyba při přípravě AAC: " + demuxEx.Message);
                return;
            }

            _currentPlayFile = playPath;
            Log($"Playing from file: {playPath}");
            
            // Must set source on the main thread since MediaElement is a UI element
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _mediaElement.Source = MediaSource.FromFile(playPath);
                _mediaElement.Play();
            });

            statusCallback?.Invoke($"Přehrávám ({fileInfo.Length / (1024.0 * 1024.0):F1} MB)");
        }
        catch (Exception ex)
        {
            Log($"ERROR in playback setup: {ex}");
            PlaybackError?.Invoke($"Chyba přehrávání: {ex.Message}");
        }
    }

    public void Play()
    {
        Log("Play() called");
        if (_mediaElement != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _mediaElement.Play();
            });
        }
    }

    public void Pause()
    {
        Log("Pause() called");
        if (_mediaElement != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _mediaElement.Pause();
            });
        }
    }

    public void Stop()
    {
        Log("Stop() called");
        if (_mediaElement != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _mediaElement.Stop();
            });
        }
    }

    public void SeekTo(TimeSpan position)
    {
        if (_mediaElement != null)
        {
            var clampedPosition = TimeSpan.FromSeconds(
                Math.Clamp(position.TotalSeconds, 0, _mediaElement.Duration.TotalSeconds));
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _mediaElement.SeekTo(clampedPosition);
            });
            Log($"SeekTo: {clampedPosition}");
        }
    }

    private async Task<string?> DownloadStreamAsync(string url, CancellationToken ct, Action<string>? statusCallback)
    {
        try
        {
            string ext = ".m4a";
            if (url.Contains("webm", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("mime=audio%2Fwebm", StringComparison.OrdinalIgnoreCase))
            {
                ext = ".webm";
            }

            var cleanUrl = System.Text.RegularExpressions.Regex.Replace(
                url, @"(?:&|\?)range=\d+(?:-\d+)?", "");

            Log($"Download: ext={ext}, URL length={cleanUrl.Length}");

            var tempPath = Path.Combine(
                Microsoft.Maui.Storage.FileSystem.CacheDirectory,
                $"ytm_media_{Guid.NewGuid()}{ext}");

            Log($"Temp path: {tempPath}");

            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = System.Net.DecompressionMethods.None
            };
            using var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMinutes(5);

            const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Add("Origin", "https://music.youtube.com");
            client.DefaultRequestHeaders.Add("Referer", "https://music.youtube.com/");
            client.DefaultRequestHeaders.Add("Accept", "*/*");

            const long chunkSize = 10 * 1024 * 1024; // 10 MB
            long totalRead = 0;
            long? contentLength = null;

            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    long rangeStart = totalRead;
                    long rangeEnd = totalRead + chunkSize - 1;

                    using var request = new HttpRequestMessage(HttpMethod.Get, cleanUrl);
                    request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
                    request.Headers.TryAddWithoutValidation("Origin", "https://music.youtube.com");
                    request.Headers.TryAddWithoutValidation("Referer", "https://music.youtube.com/");
                    request.Headers.TryAddWithoutValidation("Accept", "*/*");
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(rangeStart, rangeEnd);

                    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

                    if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                        response.StatusCode != System.Net.HttpStatusCode.PartialContent)
                    {
                        response.EnsureSuccessStatusCode();
                    }

                    if (!contentLength.HasValue && response.Content.Headers.ContentRange?.Length.HasValue == true)
                    {
                        contentLength = response.Content.Headers.ContentRange.Length;
                    }
                    if (!contentLength.HasValue && response.Content.Headers.ContentLength.HasValue && response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        contentLength = response.Content.Headers.ContentLength;
                    }

                    using var stream = await response.Content.ReadAsStreamAsync(ct);
                    var buffer = new byte[81920];
                    int bytesRead;
                    long chunkRead = 0;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        await fs.WriteAsync(buffer, 0, bytesRead, ct);
                        totalRead += bytesRead;
                        chunkRead += bytesRead;

                        if (totalRead % (512 * 1024) < 81920)
                        {
                            var mb = totalRead / (1024.0 * 1024.0);
                            if (contentLength.HasValue)
                            {
                                var pct = (totalRead * 100.0) / contentLength.Value;
                                statusCallback?.Invoke($"Staženo: {mb:F1} MB ({pct:F0}%)");
                            }
                            else
                            {
                                statusCallback?.Invoke($"Staženo: {mb:F1} MB...");
                            }
                        }
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.OK) break;
                    if (chunkRead < chunkSize) break;
                    if (contentLength.HasValue && totalRead >= contentLength.Value) break;
                }
            }

            _currentTempFile = tempPath;
            return tempPath;
        }
        catch (OperationCanceledException)
        {
            Log("Download cancelled");
            throw;
        }
        catch (Exception ex)
        {
            Log($"Download ERROR: {ex}");
            statusCallback?.Invoke($"Chyba stahování: {ex.Message}");
            return null;
        }
    }

    private void CleanupTempFile()
    {
        if (!string.IsNullOrEmpty(_currentTempFile) && File.Exists(_currentTempFile))
        {
            try { File.Delete(_currentTempFile); } catch { }
        }
        _currentTempFile = null;

        if (!string.IsNullOrEmpty(_currentPlayFile) && File.Exists(_currentPlayFile) && _currentPlayFile != _currentTempFile)
        {
            try { File.Delete(_currentPlayFile); } catch { }
        }
        _currentPlayFile = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        if (_mediaElement != null)
        {
            _mediaElement.MediaEnded -= OnMediaEnded;
            _mediaElement.PositionChanged -= OnPositionChanged;
            _mediaElement.MediaFailed -= OnMediaFailed;
            _mediaElement.StateChanged -= OnStateChanged;
            _mediaElement.Handler?.DisconnectHandler();
        }
        CleanupTempFile();
    }
}
