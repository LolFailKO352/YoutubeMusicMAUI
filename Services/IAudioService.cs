using System;
using System.Threading;
using System.Threading.Tasks;

namespace Melodium.Services;

public interface IAudioService
{
    bool IsPlaying { get; }
    float Volume { get; set; }

    event Action<TimeSpan, TimeSpan>? PositionChanged;
    event Action? MediaEnded;
    event Action<bool>? PlaybackStateChanged;
    event Action<string>? PlaybackError;

    Task PlayFromUrlAsync(string streamUrl, CancellationToken cancellationToken, Action<string>? statusCallback = null);
    void Play();
    void Pause();
    void Stop();
    void SeekTo(TimeSpan position);
}
