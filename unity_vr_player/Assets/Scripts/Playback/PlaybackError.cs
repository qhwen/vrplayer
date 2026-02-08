using System;

[Serializable]
public struct PlaybackError
{
    public PlaybackErrorCode code;
    public string message;
    public string source;

    public static PlaybackError None => new PlaybackError
    {
        code = PlaybackErrorCode.None,
        message = string.Empty,
        source = string.Empty
    };

    public bool HasError => code != PlaybackErrorCode.None;

    public static PlaybackError Create(PlaybackErrorCode errorCode, string errorMessage, string errorSource)
    {
        return new PlaybackError
        {
            code = errorCode,
            message = errorMessage ?? string.Empty,
            source = errorSource ?? string.Empty
        };
    }
}
