using System;
using UnityEngine;
using UnityEngine.Video;

public interface IPlaybackService
{
    PlaybackState State { get; }
    PlaybackSnapshot Snapshot { get; }
    PlaybackError LastError { get; }
    bool HasSource { get; }
    string CurrentSource { get; }
    Texture CurrentTexture { get; }

    bool Open(string source);
    void Play();
    void Pause();
    void Stop();
    void Seek(float seconds);
    void SetVolume(float volume);

    event Action<PlaybackState> StateChanged;
    event Action<PlaybackSnapshot> PlaybackUpdated;
    event Action<PlaybackError> ErrorOccurred;

    VideoPlayer GetNativePlayer();
}
