using System;

[Serializable]
public struct PlaybackSnapshot
{
    public PlaybackState state;
    public float positionSeconds;
    public float durationSeconds;
    public float normalizedProgress;
    public bool isBuffering;
    public string source;

    public static PlaybackSnapshot CreateDefault()
    {
        return new PlaybackSnapshot
        {
            state = PlaybackState.Idle,
            positionSeconds = 0f,
            durationSeconds = 0f,
            normalizedProgress = 0f,
            isBuffering = false,
            source = string.Empty
        };
    }
}
