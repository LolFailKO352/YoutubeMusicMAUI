using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using YoutubeMusic.Models;
using YoutubeMusic.Services;
using System.Globalization;

namespace YoutubeMusic.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly YouTubeMusicService _ytService;
    private readonly IAudioService _audioService;
    private readonly TranslationService _translationService;
    private System.Threading.CancellationTokenSource? _downloadCts;
    private readonly List<SongModel> _originalQueue = new();

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; } = "Připraveno. Zvol sekci a hraj.";

    // Navigace a přihlášení
    [ObservableProperty]
    public partial string CurrentView { get; set; } = "Home"; // Home, Search, Library, Login

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoggedIn))]
    public partial bool IsLoggedIn { get; set; }

    public bool IsNotLoggedIn => !IsLoggedIn;

    [ObservableProperty]
    public partial string UserProfileName { get; set; } = "Nepřihlášen";

    // --- Lokalizace ---
    public ObservableCollection<CultureInfo> Languages { get; } = new();

    [ObservableProperty]
    public partial CultureInfo? SelectedLanguage { get; set; }

    partial void OnSelectedLanguageChanged(CultureInfo? value)
    {
        if (value != null)
        {
            Preferences.Default.Set("AppLanguage", value.Name);
            _ = UpdateLocalizedStringsAsync(value);
            LanguageChanged?.Invoke(this, value);
        }
    }
    
    public static event EventHandler<CultureInfo>? LanguageChanged;

    [ObservableProperty] public partial string TextHome { get; set; } = "Domů";
    [ObservableProperty] public partial string TextSearch { get; set; } = "Hledat";
    [ObservableProperty] public partial string TextLibrary { get; set; } = "Knihovna";
    [ObservableProperty] public partial string TextQueue { get; set; } = "Fronta";
    [ObservableProperty] public partial string TextAccount { get; set; } = "Účet";
    [ObservableProperty] public partial string TextLogout { get; set; } = "Odhlásit";
    [ObservableProperty] public partial string TextSettings { get; set; } = "Nastavení";
    [ObservableProperty] public partial string TextLanguageSelection { get; set; } = "Výběr jazyka";
    
    [ObservableProperty] public partial string TextSearchPlaceholder { get; set; } = "Vyhledat aplikace, hry a další (nebo hudbu!)";
    [ObservableProperty] public partial string TextLanguageDescription { get; set; } = "Vyberte preferovaný jazyk aplikace. Seznam obsahuje všechny dostupné světové jazyky.";
    [ObservableProperty] public partial string TextHeroSubtitle { get; set; } = "Poslouchej hudbu bez omezení a bez reklam";
    [ObservableProperty] public partial string TextStartListening { get; set; } = "Začít poslouchat";
    [ObservableProperty] public partial string TextRecommendedMusic { get; set; } = "Doporučená hudba";
    [ObservableProperty] public partial string TextLoadingRecommendations { get; set; } = "Načítám doporučení...";
    [ObservableProperty] public partial string TextPersonalizedSongs { get; set; } = "Doporučené skladby přímo pro vás";
    [ObservableProperty] public partial string TextLockedLibrary { get; set; } = "Uzamčená Knihovna";
    [ObservableProperty] public partial string TextLockedLibraryDesc { get; set; } = "Chcete-li zobrazit své skladby, playlisty a alba z YouTube Music, musíte se přihlásit.";
    [ObservableProperty] public partial string TextGoToLogin { get; set; } = "Přejít k přihlášení";
    [ObservableProperty] public partial string TextMusic { get; set; } = "Hudba";
    [ObservableProperty] public partial string TextSongs { get; set; } = "Skladby";
    [ObservableProperty] public partial string TextAlbums { get; set; } = "Alba";
    [ObservableProperty] public partial string TextArtists { get; set; } = "Interpreti";
    [ObservableProperty] public partial string TextAddFolder { get; set; } = "Přidat složku";
    [ObservableProperty] public partial string TextShuffleAndPlay { get; set; } = "Zamíchat a přehrát";
    [ObservableProperty] public partial string TextSortBy { get; set; } = "Řadit dle: Názvu";
    [ObservableProperty] public partial string TextUnknownGenre { get; set; } = "Neznámý žánr";
    [ObservableProperty] public partial string TextPlay { get; set; } = "Přehrát";
    [ObservableProperty] public partial string TextLoginInstructions { get; set; } = "Instrukce pro přihlášení";
    [ObservableProperty] public partial string TextLoginInstruction1 { get; set; } = "1. Přihlaste se ke svému Google / YouTube účtu přímo v okně níže.";
    [ObservableProperty] public partial string TextLoginInstruction2 { get; set; } = "2. Po úspěšném přihlášení a načtení hlavní stránky YouTube Music vás aplikace automaticky připojí a stáhne vaši osobní knihovnu.";
    [ObservableProperty] public partial string TextPlaybackQueue { get; set; } = "Fronta přehrávání";
    [ObservableProperty] public partial string TextClearQueue { get; set; } = "Vyčistit frontu";
    [ObservableProperty] public partial string TextEmptyQueue { get; set; } = "Fronta je prázdná";
    [ObservableProperty] public partial string TextEmptyQueueDesc { get; set; } = "Najděte nějaké skladby a spusťte přehrávání.";
    [ObservableProperty] public partial string TextSongsCountLabel { get; set; } = "Skladeb:";
    [ObservableProperty] public partial string TextReleaseYearLabel { get; set; } = "Rok:";
    [ObservableProperty] public partial string TextSubscribersLabel { get; set; } = "Odběratelé:";
    [ObservableProperty] public partial string TextSongsInQueueLabel { get; set; } = "Skladeb ve frontě:";
    [ObservableProperty] public partial string TextStatusReady { get; set; } = "Připraveno. Zvol sekci a hraj.";
    [ObservableProperty] public partial string TextStatusLibraryEmpty { get; set; } = "Knihovna skladeb je prázdná, míchám z doporučené hudby...";
    [ObservableProperty] public partial string TextStatusShuffleFailed { get; set; } = "Nelze zahájit míchání - žádné dostupné skladby.";
    [ObservableProperty] public partial string TextStatusLibraryLoaded { get; set; } = "Vaše knihovna a doporučení byly úspěšně načteny.";

    private readonly Dictionary<string, string> _baseTexts = new()
    {
        { nameof(TextHome), "Domů" },
        { nameof(TextSearch), "Hledat" },
        { nameof(TextLibrary), "Knihovna" },
        { nameof(TextQueue), "Fronta" },
        { nameof(TextAccount), "Účet" },
        { nameof(TextLogout), "Odhlásit" },
        { nameof(TextSettings), "Nastavení" },
        { nameof(TextLanguageSelection), "Výběr jazyka" },
        { nameof(TextSearchPlaceholder), "Vyhledat aplikace, hry a další (nebo hudbu!)" },
        { nameof(TextLanguageDescription), "Vyberte preferovaný jazyk aplikace. Seznam obsahuje všechny dostupné světové jazyky." },
        { nameof(TextHeroSubtitle), "Poslouchej hudbu bez omezení a bez reklam" },
        { nameof(TextStartListening), "Začít poslouchat" },
        { nameof(TextRecommendedMusic), "Doporučená hudba >" },
        { nameof(TextLoadingRecommendations), "Načítám doporučení..." },
        { nameof(TextPersonalizedSongs), "Doporučené skladby přímo pro vás" },
        { nameof(TextLockedLibrary), "Uzamčená Knihovna" },
        { nameof(TextLockedLibraryDesc), "Chcete-li zobrazit své skladby, playlisty a alba z YouTube Music, musíte se přihlásit." },
        { nameof(TextGoToLogin), "Přejít k přihlášení" },
        { nameof(TextMusic), "Hudba" },
        { nameof(TextSongs), "Skladby" },
        { nameof(TextAlbums), "Alba" },
        { nameof(TextArtists), "Interpreti" },
        { nameof(TextAddFolder), "Přidat složku" },
        { nameof(TextShuffleAndPlay), "Zamíchat a přehrát" },
        { nameof(TextSortBy), "Řadit dle: Názvu" },
        { nameof(TextUnknownGenre), "Neznámý žánr" },
        { nameof(TextPlay), "Přehrát" },
        { nameof(TextLoginInstructions), "Instrukce pro přihlášení" },
        { nameof(TextLoginInstruction1), "1. Přihlaste se ke svému Google / YouTube účtu přímo v okně níže." },
        { nameof(TextLoginInstruction2), "2. Po úspěšném přihlášení a načtení hlavní stránky YouTube Music vás aplikace automaticky připojí a stáhne vaši osobní knihovnu." },
        { nameof(TextPlaybackQueue), "Fronta přehrávání" },
        { nameof(TextClearQueue), "Vyčistit frontu" },
        { nameof(TextEmptyQueue), "Fronta je prázdná" },
        { nameof(TextEmptyQueueDesc), "Najděte nějaké skladby a spusťte přehrávání." },
        { nameof(TextSongsCountLabel), "Skladeb:" },
        { nameof(TextReleaseYearLabel), "Rok:" },
        { nameof(TextSubscribersLabel), "Odběratelé:" },
        { nameof(TextSongsInQueueLabel), "Skladeb ve frontě:" },
        { nameof(TextStatusReady), "Připraveno. Zvol sekci a hraj." },
        { nameof(TextStatusLibraryEmpty), "Knihovna skladeb je prázdná, míchám z doporučené hudby..." },
        { nameof(TextStatusShuffleFailed), "Nelze zahájit míchání - žádné dostupné skladby." },
        { nameof(TextStatusLibraryLoaded), "Vaše knihovna a doporučení byly úspěšně načteny." }
    };

    private async Task UpdateLocalizedStringsAsync(CultureInfo culture)
    {
        string targetLang = culture.TwoLetterISOLanguageName;
        
        foreach (var kvp in _baseTexts)
        {
            string translated = await _translationService.TranslateAsync(kvp.Value, targetLang);
            var prop = this.GetType().GetProperty(kvp.Key);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(this, translated);
            }
        }
    }
    // --- Konec lokalizace ---

    // Přehrávač
    [ObservableProperty]
    public partial bool IsFullScreenPlayerVisible { get; set; }

    [RelayCommand]
    private void ToggleFullScreenPlayer()
    {
        IsFullScreenPlayerVisible = !IsFullScreenPlayerVisible;
    }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCurrentSongNotNull))]
    [NotifyPropertyChangedFor(nameof(IsCurrentSongNull))]
    public partial SongModel? CurrentSong { get; set; }

    public bool IsCurrentSongNotNull => CurrentSong != null;
    public bool IsCurrentSongNull => CurrentSong == null;

    [ObservableProperty]
    public partial SongModel? SelectedSong { get; set; }

    partial void OnSelectedSongChanged(SongModel? value)
    {
        if (value != null)
        {
            _ = PlaySongAsync(value);
            SelectedSong = null;
        }
    }

    [ObservableProperty]
    public partial string PlayPauseIcon { get; set; } = "▶";

    partial void OnIsPlayingChanged(bool value)
    {
        PlayPauseIcon = value ? "⏸" : "▶";
    }

    [ObservableProperty]
    public partial string LibraryTab { get; set; } = "Songs"; // Songs, Playlists, Albums, Artists

    [RelayCommand]
    private void SetLibraryTab(string tabName)
    {
        LibraryTab = tabName;
    }

    [ObservableProperty]
    public partial double Volume { get; set; } = 0.5;

    partial void OnVolumeChanged(double value)
    {
        _audioService.Volume = (float)value;
    }

    [ObservableProperty]
    public partial string PositionText { get; set; } = "0:00";

    [ObservableProperty]
    public partial string DurationText { get; set; } = "0:00";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressRatio))]
    public partial double PositionSeconds { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressRatio))]
    public partial double DurationSeconds { get; set; }

    public double ProgressRatio => DurationSeconds > 0 ? PositionSeconds / DurationSeconds : 0;

    // Kolekce
    public ObservableCollection<SongModel> SearchResults { get; } = new();
    public ObservableCollection<SongModel> HomeRecommendations { get; } = new();
    public ObservableCollection<SongModel> LibrarySongs { get; } = new();
    public ObservableCollection<PlaylistModel> LibraryPlaylists { get; } = new();
    public ObservableCollection<AlbumModel> LibraryAlbums { get; } = new();
    public ObservableCollection<ArtistModel> LibraryArtists { get; } = new();
    public ObservableCollection<SongModel> PlaybackQueue { get; } = new();
    public ObservableCollection<SongModel> LibraryBasedRecommendations { get; } = new();

    [ObservableProperty]
    public partial bool HasPersonalizedRecommendations { get; set; }

    [ObservableProperty]
    public partial int CurrentQueueIndex { get; set; } = -1;

    [ObservableProperty]
    public partial bool IsShuffle { get; set; }

    [ObservableProperty]
    public partial int RepeatMode { get; set; } // 0 = Off, 1 = Repeat Queue, 2 = Repeat Song

    [ObservableProperty]
    public partial SongModel? SelectedQueueSong { get; set; }

    partial void OnSelectedQueueSongChanged(SongModel? value)
    {
        if (value != null && value != CurrentSong)
        {
            _ = PlayFromQueueAsync(value);
        }
    }

    [RelayCommand]
    private async Task NotImplemented(string? featureName)
    {
        StatusMessage = $"{await _translationService.TranslateAsync("Zatím nepodporováno:", SelectedLanguage?.TwoLetterISOLanguageName ?? "cs")} {featureName ?? "tato funkce"}";
    }

    [RelayCommand]
    public async Task ShuffleAndPlayLibraryAsync()
    {
        if (LibrarySongs.Count == 0)
        {
            StatusMessage = TextStatusLibraryEmpty;
            PlaybackQueue.Clear();
            _originalQueue.Clear();
            
            // Fallback: Use HomeRecommendations if LibrarySongs is empty
            var fallbackSongs = HomeRecommendations.Count > 0 ? HomeRecommendations.ToList() : SearchResults.ToList();
            
            if (fallbackSongs.Count == 0)
            {
                StatusMessage = TextStatusShuffleFailed;
                return;
            }

            foreach (var song in fallbackSongs)
            {
                PlaybackQueue.Add(song);
                _originalQueue.Add(song);
            }
        }
        else
        {
            PlaybackQueue.Clear();
            _originalQueue.Clear();
            
            foreach (var song in LibrarySongs)
            {
                PlaybackQueue.Add(song);
                _originalQueue.Add(song);
            }
        }

        CurrentQueueIndex = 0;
        IsShuffle = true;
        ApplyShuffle();
        
        await PlayQueueCurrentSongAsync();
        IsBusy = false;
    }

    public MainViewModel(YouTubeMusicService ytService, IAudioService audioService, TranslationService translationService)
    {
        _ytService = ytService;
        _audioService = audioService;
        _translationService = translationService;
        _audioService.Volume = (float)Volume;

        // Hook up audio service events
        _audioService.PositionChanged += OnAudioPositionChanged;
        _audioService.MediaEnded += OnAudioMediaEnded;
        _audioService.PlaybackStateChanged += OnAudioPlaybackStateChanged;
        _audioService.PlaybackError += OnAudioPlaybackError;

        // Načtení jazyků
        var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures)
            .OrderBy(c => c.NativeName)
            .ToList();
        foreach (var c in cultures)
        {
            if (!string.IsNullOrEmpty(c.NativeName))
            {
                Languages.Add(c);
            }
        }

        var savedLang = Preferences.Default.Get("AppLanguage", "cs");
        SelectedLanguage = Languages.FirstOrDefault(c => c.Name == savedLang) ?? Languages.FirstOrDefault(c => c.TwoLetterISOLanguageName == "cs");

        // Načteme domovskou obrazovku nezávisle na přihlášení
        _ = Task.Run(LoadHomeRecommendationsAsync);

        // Načteme uložené přihlášení při startu
        _ = Task.Run(LoadSavedSessionAsync);
    }

    private void OnAudioPositionChanged(TimeSpan position, TimeSpan duration)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PositionSeconds = position.TotalSeconds;
            DurationSeconds = duration.TotalSeconds;
            PositionText = position.ToString(@"m\:ss");
            DurationText = duration.TotalSeconds > 0 ? duration.ToString(@"m\:ss") : "0:00";
        });
    }

    private void OnAudioMediaEnded()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (RepeatMode == 2) // Repeat Song
            {
                _ = PlayQueueCurrentSongAsync();
            }
            else
            {
                _ = PlayNextSongAsync();
            }
        });
    }

    private void OnAudioPlaybackStateChanged(bool playing)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsPlaying = playing;
        });
    }

    private void OnAudioPlaybackError(string errorMessage)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            StatusMessage = $"{await _translationService.TranslateAsync("CHYBA:", SelectedLanguage?.TwoLetterISOLanguageName ?? "cs")} {errorMessage}";
        });
    }

    [RelayCommand]
    private void Navigate(string viewName)
    {
        CurrentView = viewName;
    }

    [RelayCommand]
    private async Task PerformSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsBusy = true;
        SearchResults.Clear();
        StatusMessage = $"Vyhledávám: {SearchQuery}...";

        await _ytService.EnsureInitializedAsync();
        var songs = await _ytService.SearchSongsAsync(SearchQuery);

        foreach (var song in songs)
        {
            SearchResults.Add(song);
        }

        StatusMessage = $"Hledání dokončeno. Nalezeno {songs.Count} skladeb.";
        IsBusy = false;
    }

    [RelayCommand]
    public async Task PlaySongAsync(SongModel? song)
    {
        if (song == null) return;

        PlaybackQueue.Clear();
        PlaybackQueue.Add(song);
        CurrentQueueIndex = 0;

        _originalQueue.Clear();
        _originalQueue.Add(song);
        IsShuffle = false;

        await PlayQueueCurrentSongAsync();
    }

    [RelayCommand]
    public async Task PlayPlaylistAsync(PlaylistModel? playlist)
    {
        if (playlist == null) return;

        IsBusy = true;
        StatusMessage = $"Načítám skladby z playlistu: {playlist.Title}...";

        var songs = await _ytService.GetPlaylistSongsAsync(playlist.Id);
        if (songs.Count > 0)
        {
            PlaybackQueue.Clear();
            _originalQueue.Clear();
            foreach (var song in songs)
            {
                PlaybackQueue.Add(song);
                _originalQueue.Add(song);
            }
            CurrentQueueIndex = 0;
            if (IsShuffle)
            {
                ApplyShuffle();
            }
            await PlayQueueCurrentSongAsync();
        }
        else
        {
            StatusMessage = "Playlist neobsahuje žádné skladby.";
        }
        IsBusy = false;
    }

    [RelayCommand]
    public async Task PlayAlbumAsync(AlbumModel? album)
    {
        if (album == null) return;

        IsBusy = true;
        StatusMessage = $"Načítám skladby z alba: {album.Title}...";

        var songs = await _ytService.GetAlbumSongsAsync(album.Id);
        if (songs.Count > 0)
        {
            PlaybackQueue.Clear();
            _originalQueue.Clear();
            foreach (var song in songs)
            {
                PlaybackQueue.Add(song);
                _originalQueue.Add(song);
            }
            CurrentQueueIndex = 0;
            if (IsShuffle)
            {
                ApplyShuffle();
            }
            await PlayQueueCurrentSongAsync();
        }
        else
        {
            StatusMessage = "Album neobsahuje žádné skladby.";
        }
        IsBusy = false;
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (CurrentSong == null) return;

        if (IsPlaying)
        {
            _audioService.Pause();
        }
        else
        {
            _audioService.Play();
        }
    }

    [RelayCommand]
    public async Task PlayNextSongAsync()
    {
        if (PlaybackQueue.Count == 0) return;

        if (CurrentQueueIndex + 1 < PlaybackQueue.Count)
        {
            CurrentQueueIndex++;
            await PlayQueueCurrentSongAsync();
        }
        else if (RepeatMode == 1) // Repeat Queue
        {
            CurrentQueueIndex = 0;
            await PlayQueueCurrentSongAsync();
        }
        else
        {
            StatusMessage = "Konec fronty přehrávání.";
        }
    }

    [RelayCommand]
    public async Task PlayPreviousSongAsync()
    {
        if (PlaybackQueue.Count == 0) return;

        if (PositionSeconds > 3)
        {
            await PlayQueueCurrentSongAsync();
            return;
        }

        if (CurrentQueueIndex - 1 >= 0)
        {
            CurrentQueueIndex--;
            await PlayQueueCurrentSongAsync();
        }
        else if (RepeatMode == 1) // Repeat Queue
        {
            CurrentQueueIndex = PlaybackQueue.Count - 1;
            await PlayQueueCurrentSongAsync();
        }
        else
        {
            await PlayQueueCurrentSongAsync();
        }
    }

    private async Task PlayQueueCurrentSongAsync()
    {
        if (CurrentQueueIndex < 0 || CurrentQueueIndex >= PlaybackQueue.Count) return;

        // Stop current playback
        _audioService.Stop();

        // Cancel previous download
        _downloadCts?.Cancel();
        _downloadCts = new System.Threading.CancellationTokenSource();
        var token = _downloadCts.Token;

        var song = PlaybackQueue[CurrentQueueIndex];
        CurrentSong = song;
        SelectedQueueSong = song;
        IsBusy = true;
        StatusMessage = $"Příprava: {song.Title}...";

        // Reset position display
        PositionSeconds = 0;
        DurationSeconds = 0;
        PositionText = "0:00";
        DurationText = "0:00";

        try
        {
            StatusMessage = $"[1/4] Získávám stream URL pro: {song.VideoId}...";
            VmLog($"Getting stream URL for videoId={song.VideoId}");

            await _ytService.EnsureInitializedAsync();
            var streamUrl = await _ytService.GetAudioStreamUrlAsync(song.VideoId);
            token.ThrowIfCancellationRequested();

            // Pokud jsme právě začali přehrávat písničku a fronta nemá další, natáhneme Rádio
            if (CurrentQueueIndex == PlaybackQueue.Count - 1 && PlaybackQueue.Count < 20)
            {
                _ = Task.Run(async () =>
                {
                    var upNext = await _ytService.GetUpNextRadioAsync(song.VideoId);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var nextSong in upNext)
                        {
                            PlaybackQueue.Add(nextSong);
                            _originalQueue.Add(nextSong);
                        }
                    });
                });
            }

            VmLog($"Stream URL result: {(streamUrl == null ? "NULL" : $"length={streamUrl.Length}, first100={streamUrl[..Math.Min(100, streamUrl.Length)]}")}");
            VmLog($"Stream URL has pot={streamUrl?.Contains("&pot=") == true}, has sig={streamUrl?.Contains("&sig=") == true}");
            VmLog($"FULL URL: {streamUrl}");
            if (!string.IsNullOrEmpty(streamUrl))
            {
                StatusMessage = $"[2/4] Stream URL získáno. Stahuji: {song.Title}...";

                await _audioService.PlayFromUrlAsync(streamUrl, token, msg =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        StatusMessage = $"{song.Title} — {msg}";
                    });
                });

                VmLog("PlayFromUrlAsync returned (přehrávání je event-driven).");

                // Stav přehrávání teď řídí události MediaElementu (OnAudioPlaybackStateChanged /
                // OnAudioPlaybackError), tady jen nastavíme úvodní hlášku.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusMessage = $"▶ {song.Title}";
                });
            }
            else
            {
                StatusMessage = "❌ GetAudioStreamUrlAsync vrátilo NULL — nepodařilo se získat stream.";
                VmLog("ERROR: streamUrl is null");
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Přehrávání přeskočeno.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Výjimka: {ex.GetType().Name}: {ex.Message}";
            VmLog($"EXCEPTION: {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static void VmLog(string message)
    {
        try
        {
            var msg = $"[{DateTime.Now:HH:mm:ss.fff}] [ViewModel] {message}\n";
            var logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ytm_naudio_log.txt");
            System.IO.File.AppendAllText(logPath, msg);
            System.Diagnostics.Debug.WriteLine(msg);
        }
        catch { }
    }

    public void SeekTo(TimeSpan position)
    {
        _audioService.SeekTo(position);
    }

    [RelayCommand]
    private void ToggleShuffle()
    {
        IsShuffle = !IsShuffle;
        if (IsShuffle)
        {
            ApplyShuffle();
        }
        else
        {
            RemoveShuffle();
        }
    }

    [RelayCommand]
    private void ToggleRepeat()
    {
        RepeatMode = (RepeatMode + 1) % 3;
    }

    [RelayCommand]
    public async Task PlayFromQueueAsync(SongModel? song)
    {
        if (song == null) return;
        int index = PlaybackQueue.IndexOf(song);
        if (index >= 0)
        {
            CurrentQueueIndex = index;
            await PlayQueueCurrentSongAsync();
        }
    }

    [RelayCommand]
    private void ClearQueue()
    {
        PlaybackQueue.Clear();
        _originalQueue.Clear();
        CurrentQueueIndex = -1;
        CurrentSong = null;
        SelectedQueueSong = null;
        _audioService.Stop();
        IsPlaying = false;
        StatusMessage = "Fronta byla vymazána.";
    }

    private void ApplyShuffle()
    {
        if (PlaybackQueue.Count <= 1) return;

        var current = CurrentSong;
        var otherSongs = PlaybackQueue.Where(s => s != current).ToList();

        var rng = new Random();
        int n = otherSongs.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var value = otherSongs[k];
            otherSongs[k] = otherSongs[n];
            otherSongs[n] = value;
        }

        PlaybackQueue.Clear();
        if (current != null)
        {
            PlaybackQueue.Add(current);
        }
        foreach (var s in otherSongs)
        {
            PlaybackQueue.Add(s);
        }
        CurrentQueueIndex = current != null ? 0 : -1;
    }

    private void RemoveShuffle()
    {
        if (_originalQueue.Count == 0) return;

        var current = CurrentSong;
        PlaybackQueue.Clear();
        foreach (var s in _originalQueue)
        {
            PlaybackQueue.Add(s);
        }

        if (current != null)
        {
            CurrentQueueIndex = PlaybackQueue.IndexOf(current);
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _audioService.Stop();

        SecureStorage.Default.Remove("ytm_cookies");
        _ytService.InitializeClient(null);
        IsLoggedIn = false;
        UserProfileName = "Nepřihlášen";
        LibrarySongs.Clear();
        LibraryPlaylists.Clear();
        LibraryAlbums.Clear();
        LibraryArtists.Clear();
        PlaybackQueue.Clear();
        CurrentSong = null;
        IsPlaying = false;

        StatusMessage = "Uživatel byl odhlášen.";
        CurrentView = "Home";
    }

    public async Task LoadSavedSessionAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync("ytm_cookies");
            if (!string.IsNullOrEmpty(json))
            {
                var dtos = System.Text.Json.JsonSerializer.Deserialize<List<CookieDto>>(json);
                if (dtos != null && dtos.Count > 0)
                {
                    var cookies = dtos.Select(d => new Cookie(d.Name, d.Value, d.Path, d.Domain)).ToList();
                    _ytService.InitializeClient(cookies);
                    IsLoggedIn = true;
                    UserProfileName = "Můj účet";
                    StatusMessage = "Relace obnovena.";
                    
                    _ = Task.Run(LoadLibraryAsync);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Nepodařilo se načíst relaci: {ex.Message}";
        }
    }

    public async Task SaveSessionAsync(List<Cookie> cookies)
    {
        try
        {
            var dtos = cookies.Select(c => new CookieDto { Name = c.Name, Value = c.Value, Domain = c.Domain, Path = c.Path }).ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(dtos);
            await SecureStorage.Default.SetAsync("ytm_cookies", json);
            
            _ytService.InitializeClient(cookies);
            IsLoggedIn = true;
            UserProfileName = "Můj účet";
            StatusMessage = "Přihlášení uloženo!";
            
            _ = Task.Run(LoadLibraryAsync);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Chyba při ukládání přihlášení: {ex.Message}";
        }
    }

    public async Task LoadHomeRecommendationsAsync()
    {
        try
        {
            await _ytService.EnsureInitializedAsync();
            var homeSongs = await _ytService.GetHomeRecommendationsAsync();
            
            // Pokud Youtube vrátí prázdný seznam (např. u nepřihlášených účtů), uděláme fallback search
            if (homeSongs == null || homeSongs.Count == 0)
            {
                homeSongs = await _ytService.SearchSongsAsync("Top 100 hitů");
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                HomeRecommendations.Clear();
                foreach (var h in homeSongs) HomeRecommendations.Add(h);
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Chyba při stahování doporučení: {ex.Message}";
        }
    }

    public async Task LoadLibraryAsync()
    {
        IsBusy = true;
        StatusMessage = "Načítám domovskou obrazovku a knihovnu z YouTube Music...";
        try
        {
            await _ytService.EnsureInitializedAsync();
            
            _ = Task.Run(LoadHomeRecommendationsAsync);
            
            var songs = await _ytService.GetLibrarySongsAsync();
            var playlists = await _ytService.GetLibraryPlaylistsAsync();
            var albums = await _ytService.GetLibraryAlbumsAsync();
            var artists = await _ytService.GetLibraryArtistsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {

                LibrarySongs.Clear();
                foreach (var s in songs) LibrarySongs.Add(s);

                LibraryPlaylists.Clear();
                foreach (var p in playlists) LibraryPlaylists.Add(p);

                LibraryAlbums.Clear();
                foreach (var a in albums) LibraryAlbums.Add(a);

                LibraryArtists.Clear();
                foreach (var art in artists) LibraryArtists.Add(art);

                StatusMessage = TextStatusLibraryLoaded;
            });

            _ = Task.Run(() => GenerateLibraryRecommendationsAsync(songs.ToList(), artists.ToList(), albums.ToList(), playlists.ToList()));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Chyba při stahování knihovny: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task GenerateLibraryRecommendationsAsync(List<SongModel> userSongs, List<ArtistModel> userArtists, List<AlbumModel> userAlbums, List<PlaylistModel> userPlaylists)
    {
        // Původní kontrola byla: if (userSongs == null || userSongs.Count == 0) return;
        // Ale uživatel může mít knihovnu interpretů a alb místo jednotlivých skladeb.
        if ((userSongs == null || userSongs.Count == 0) && 
            (userArtists == null || userArtists.Count == 0) &&
            (userAlbums == null || userAlbums.Count == 0) &&
            (userPlaylists == null || userPlaylists.Count == 0)) 
            return;

        try
        {
            // 1. Najít nejoblíbenější interprety nebo přímo odebírané interprety
            var seedArtists = new HashSet<string>();
            
            if (userSongs != null && userSongs.Count > 0)
            {
                var topFromSongs = userSongs
                    .GroupBy(s => s.Artist)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key);
                foreach (var a in topFromSongs) seedArtists.Add(a);
            }

            if (userArtists != null && userArtists.Count > 0)
            {
                var topFromArtists = userArtists.Take(5).Select(a => a.Name);
                foreach (var a in topFromArtists) seedArtists.Add(a);
            }

            if (userAlbums != null && userAlbums.Count > 0)
            {
                var topFromAlbums = userAlbums.Take(5).Select(a => a.ArtistName);
                foreach (var a in topFromAlbums) { if (!string.IsNullOrEmpty(a) && a != "Neznámý interpret") seedArtists.Add(a); }
            }

            if (seedArtists.Count == 0) return;

            // 2. Získat náhodné songy od těchto interpretů a z nich stáhnout UpNext
            var rng = new Random();
            var allRecommendedSongs = new List<SongModel>();
            
            var artistsList = seedArtists.OrderBy(x => rng.Next()).Take(3).ToList();

            foreach (var artist in artistsList)
            {
                // Najít seed písničku (buď tu co uživatel má, nebo prohledat daného interpreta)
                string seedVideoId = null;
                var artistSongs = userSongs?.Where(s => s.Artist == artist).ToList();
                
                if (artistSongs != null && artistSongs.Count > 0)
                {
                    seedVideoId = artistSongs[rng.Next(artistSongs.Count)].VideoId;
                }
                else
                {
                    // Pokud uživatel má jen interpreta v knihovně, najdeme jeho hit
                    var searchRes = await _ytService.SearchSongsAsync(artist);
                    if (searchRes.Count > 0)
                    {
                        seedVideoId = searchRes[0].VideoId;
                    }
                }

                if (!string.IsNullOrEmpty(seedVideoId))
                {
                    var radioSongs = await _ytService.GetUpNextRadioAsync(seedVideoId);
                    allRecommendedSongs.AddRange(radioSongs);
                }
            }

            // 3. Odstranit duplicity a skladby, které už uživatel má v knihovně
            var libraryVideoIds = new HashSet<string>(userSongs?.Select(s => s.VideoId) ?? Enumerable.Empty<string>());
            
            var finalSongs = allRecommendedSongs
                .Where(s => !libraryVideoIds.Contains(s.VideoId))
                .GroupBy(s => s.VideoId)
                .Select(g => g.First())
                .OrderBy(x => rng.Next()) // Zamíchat
                .Take(20)
                .ToList();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                LibraryBasedRecommendations.Clear();
                foreach (var s in finalSongs) LibraryBasedRecommendations.Add(s);

                HasPersonalizedRecommendations = LibraryBasedRecommendations.Count > 0;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Chyba při generování doporučení: {ex.Message}");
        }
    }
}
