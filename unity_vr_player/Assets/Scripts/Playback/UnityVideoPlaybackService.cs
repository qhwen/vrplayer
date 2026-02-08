using System;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Unity VideoPlayer based playback service with explicit state machine.
/// </summary>
public class UnityVideoPlaybackService : MonoBehaviour, IPlaybackService
{
    [Header("Playback Settings")]
    [SerializeField, Range(5f, 60f)] private float prepareTimeoutSeconds = 20f;
    [SerializeField] private bool autoPlayOnOpen;
    [SerializeField] private bool loopPlayback;
    [SerializeField, Range(0f, 1f)] private float initialVolume = 1f;

    private static readonly string[] SupportedExtensions = { ".mp4", ".mkv", ".mov" };

    private VideoPlayer videoPlayer;

    private PlaybackState state = PlaybackState.Idle;
    private PlaybackSnapshot snapshot = PlaybackSnapshot.CreateDefault();
    private PlaybackError lastError = PlaybackError.None;

    private bool hasSource;
    private string currentSource = string.Empty;
    private bool pendingPlay;
    private float prepareStartTime;

    private float lastEmittedPosition = -1f;
    private float lastEmittedDuration = -1f;
    private bool lastEmittedBuffering;

    public PlaybackState State => state;
    public PlaybackSnapshot Snapshot => snapshot;
    public PlaybackError LastError => lastError;
    public bool HasSource => hasSource;
    public string CurrentSource => currentSource;
    public Texture CurrentTexture => videoPlayer != null ? videoPlayer.texture : null;

    public event Action<PlaybackState> StateChanged;
    public event Action<PlaybackSnapshot> PlaybackUpdated;
    public event Action<PlaybackError> ErrorOccurred;

    private void Awake()
    {
        EnsureVideoPlayer();
        UpdateSnapshot(true);
    }

    private void Update()
    {
        CheckPrepareTimeout();
        UpdateSnapshot(false);
    }

    public VideoPlayer GetNativePlayer()
    {
        return videoPlayer;
    }

    public bool Open(string source)
    {
        string normalized = NormalizeVideoPath(source);

        if (!ValidateSource(source, normalized, out PlaybackErrorCode errorCode, out string errorMessage))
        {
            SetError(errorCode, errorMessage, normalized);
            return false;
        }

        ClearError();

        if (videoPlayer == null)
        {
            SetError(PlaybackErrorCode.Unknown, "Video player is not initialized.", normalized);
            return false;
        }

        pendingPlay = autoPlayOnOpen;
        hasSource = true;
        currentSource = normalized;

        videoPlayer.Stop();
        videoPlayer.url = normalized;

        prepareStartTime = Time.unscaledTime;
        SetState(PlaybackState.Preparing);
        videoPlayer.Prepare();

        UpdateSnapshot(true);
        return true;
    }

    public void Play()
    {
        if (!hasSource || string.IsNullOrWhiteSpace(currentSource))
        {
            SetError(PlaybackErrorCode.InvalidSource, "No media source has been opened.", currentSource);
            return;
        }

        if (state == PlaybackState.Preparing)
        {
            pendingPlay = true;
            return;
        }

        if (state == PlaybackState.Error)
        {
            if (Open(currentSource))
            {
                pendingPlay = true;
            }

            return;
        }

        if (!videoPlayer.isPrepared)
        {
            pendingPlay = true;
            prepareStartTime = Time.unscaledTime;
            SetState(PlaybackState.Preparing);
            videoPlayer.Prepare();
            return;
        }

        videoPlayer.Play();
        SetState(PlaybackState.Playing);
        UpdateSnapshot(true);
    }

    public void Pause()
    {
        if (videoPlayer == null || state != PlaybackState.Playing)
        {
            return;
        }

        videoPlayer.Pause();
        SetState(PlaybackState.Paused);
        UpdateSnapshot(true);
    }

    public void Stop()
    {
        if (videoPlayer == null)
        {
            return;
        }

        pendingPlay = false;
        videoPlayer.Stop();

        if (hasSource)
        {
            SetState(PlaybackState.Ready);
        }
        else
        {
            SetState(PlaybackState.Idle);
        }

        snapshot.positionSeconds = 0f;
        snapshot.normalizedProgress = 0f;
        UpdateSnapshot(true);
    }

    public void Seek(float seconds)
    {
        if (videoPlayer == null || !hasSource || !videoPlayer.canSetTime)
        {
            return;
        }

        double target = Mathf.Max(0f, seconds);
        if (videoPlayer.length > 0d)
        {
            target = Math.Min(target, videoPlayer.length);
        }

        videoPlayer.time = target;
        UpdateSnapshot(true);
    }

    public void SetVolume(float volume)
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.SetDirectAudioVolume(0, Mathf.Clamp01(volume));
    }

    private void EnsureVideoPlayer()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = loopPlayback;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetDirectAudioVolume(0, Mathf.Clamp01(initialVolume));

        videoPlayer.prepareCompleted += OnPrepareCompleted;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.started += OnVideoStarted;
        videoPlayer.loopPointReached += OnLoopPointReached;
    }

    private void CheckPrepareTimeout()
    {
        if (state != PlaybackState.Preparing)
        {
            return;
        }

        if (Time.unscaledTime - prepareStartTime < prepareTimeoutSeconds)
        {
            return;
        }

        videoPlayer.Stop();
        SetError(PlaybackErrorCode.PrepareTimeout, "Video prepare timeout.", currentSource);
    }

    private void UpdateSnapshot(bool force)
    {
        if (videoPlayer == null)
        {
            return;
        }

        float duration = snapshot.durationSeconds;
        if (videoPlayer.length > 0d)
        {
            duration = (float)videoPlayer.length;
        }

        float position = snapshot.positionSeconds;
        if (hasSource && (videoPlayer.isPlaying || videoPlayer.isPrepared || videoPlayer.canSetTime))
        {
            position = (float)Math.Max(0d, videoPlayer.time);
        }

        bool isBuffering = state == PlaybackState.Preparing
            || (state == PlaybackState.Playing && !videoPlayer.isPlaying && hasSource);

        snapshot.state = state;
        snapshot.positionSeconds = Mathf.Max(0f, position);
        snapshot.durationSeconds = Mathf.Max(0f, duration);
        snapshot.normalizedProgress = snapshot.durationSeconds > 0.01f
            ? Mathf.Clamp01(snapshot.positionSeconds / snapshot.durationSeconds)
            : 0f;
        snapshot.isBuffering = isBuffering;
        snapshot.source = currentSource ?? string.Empty;

        bool changed = force
                       || Mathf.Abs(snapshot.positionSeconds - lastEmittedPosition) >= 0.15f
                       || Mathf.Abs(snapshot.durationSeconds - lastEmittedDuration) >= 0.15f
                       || lastEmittedBuffering != isBuffering;

        if (!changed)
        {
            return;
        }

        lastEmittedPosition = snapshot.positionSeconds;
        lastEmittedDuration = snapshot.durationSeconds;
        lastEmittedBuffering = isBuffering;

        PlaybackUpdated?.Invoke(snapshot);
    }

    private void SetState(PlaybackState newState)
    {
        if (state == newState)
        {
            return;
        }

        state = newState;
        snapshot.state = newState;
        StateChanged?.Invoke(newState);
    }

    private void ClearError()
    {
        lastError = PlaybackError.None;
    }

    private void SetError(PlaybackErrorCode code, string message, string source)
    {
        lastError = PlaybackError.Create(code, message, source);
        SetState(PlaybackState.Error);
        ErrorOccurred?.Invoke(lastError);
        UpdateSnapshot(true);
    }

    private static bool ValidateSource(string rawSource, string normalizedSource, out PlaybackErrorCode errorCode, out string errorMessage)
    {
        errorCode = PlaybackErrorCode.None;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(rawSource))
        {
            errorCode = PlaybackErrorCode.InvalidSource;
            errorMessage = "Video source is empty.";
            return false;
        }

        string extensionCandidate = ExtractFilePathForValidation(rawSource, normalizedSource);
        string extension = Path.GetExtension(extensionCandidate)?.ToLowerInvariant() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(extension) && !IsSupportedExtension(extension))
        {
            errorCode = PlaybackErrorCode.UnsupportedFormat;
            errorMessage = "Unsupported media format: " + extension;
            return false;
        }

        if (LooksLikeLocalPath(rawSource, normalizedSource))
        {
            string localPath = ToLocalPath(rawSource, normalizedSource);
            if (!string.IsNullOrWhiteSpace(localPath) && !File.Exists(localPath))
            {
                errorCode = PlaybackErrorCode.FileNotFound;
                errorMessage = "File does not exist: " + localPath;
                return false;
            }
        }

        return true;
    }

    private static string ExtractFilePathForValidation(string rawSource, string normalizedSource)
    {
        string candidate = !string.IsNullOrWhiteSpace(rawSource) ? rawSource : (normalizedSource ?? string.Empty);

        if (Uri.TryCreate(candidate, UriKind.Absolute, out Uri uri))
        {
            return uri.AbsolutePath;
        }

        return candidate;
    }

    private static bool LooksLikeLocalPath(string rawSource, string normalizedSource)
    {
        if (string.IsNullOrWhiteSpace(rawSource) && string.IsNullOrWhiteSpace(normalizedSource))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(rawSource))
        {
            string lowerRaw = rawSource.ToLowerInvariant();
            if (lowerRaw.StartsWith("http://") || lowerRaw.StartsWith("https://") || lowerRaw.StartsWith("content://") || lowerRaw.StartsWith("jar:file://"))
            {
                return false;
            }

            if (lowerRaw.StartsWith("file://") || Path.IsPathRooted(rawSource) || rawSource.StartsWith("/"))
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedSource))
        {
            string lowerNormalized = normalizedSource.ToLowerInvariant();
            if (lowerNormalized.StartsWith("file://"))
            {
                return true;
            }
        }

        return false;
    }

    private static string ToLocalPath(string rawSource, string normalizedSource)
    {
        if (!string.IsNullOrWhiteSpace(rawSource) && !rawSource.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            return rawSource.Replace("\\", "/");
        }

        if (!string.IsNullOrWhiteSpace(normalizedSource) && normalizedSource.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Uri uri = new Uri(normalizedSource);
                return uri.LocalPath;
            }
            catch
            {
                return normalizedSource.Replace("file://", string.Empty);
            }
        }

        return normalizedSource ?? string.Empty;
    }

    private static bool IsSupportedExtension(string extension)
    {
        for (int i = 0; i < SupportedExtensions.Length; i++)
        {
            if (extension == SupportedExtensions[i])
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeVideoPath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return string.Empty;
        }

        string path = rawPath.Trim();

        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("content://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("jar:file://", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        string normalized = path.Replace("\\", "/");

        if (normalized.StartsWith("/"))
        {
            return "file://" + normalized;
        }

        if (Path.IsPathRooted(path))
        {
            if (normalized.Length > 1 && normalized[1] == ':')
            {
                normalized = "/" + normalized;
            }

            return "file://" + normalized;
        }

        return path;
    }

    private static PlaybackErrorCode MapErrorCode(string message)
    {
        string lower = (message ?? string.Empty).ToLowerInvariant();

        if (lower.Contains("permission") || lower.Contains("denied") || lower.Contains("forbidden"))
        {
            return PlaybackErrorCode.PermissionDenied;
        }

        if (lower.Contains("not found") || lower.Contains("no such file") || lower.Contains("404"))
        {
            return PlaybackErrorCode.FileNotFound;
        }

        if (lower.Contains("unsupported") || lower.Contains("format"))
        {
            return PlaybackErrorCode.UnsupportedFormat;
        }

        if (lower.Contains("decode") || lower.Contains("codec") || lower.Contains("decoder"))
        {
            return PlaybackErrorCode.DecoderFailure;
        }

        return PlaybackErrorCode.Unknown;
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        if (source != videoPlayer)
        {
            return;
        }

        SetState(PlaybackState.Ready);
        UpdateSnapshot(true);

        if (!pendingPlay)
        {
            return;
        }

        pendingPlay = false;
        Play();
    }

    private void OnVideoStarted(VideoPlayer source)
    {
        if (source != videoPlayer)
        {
            return;
        }

        SetState(PlaybackState.Playing);
        UpdateSnapshot(true);
    }

    private void OnLoopPointReached(VideoPlayer source)
    {
        if (source != videoPlayer)
        {
            return;
        }

        if (videoPlayer.isLooping)
        {
            SetState(PlaybackState.Playing);
        }
        else
        {
            SetState(PlaybackState.Paused);
        }

        UpdateSnapshot(true);
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        if (source != videoPlayer)
        {
            return;
        }

        PlaybackErrorCode code = MapErrorCode(message);
        SetError(code, string.IsNullOrWhiteSpace(message) ? "Unknown playback error." : message, currentSource);
    }

    private void OnDestroy()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.prepareCompleted -= OnPrepareCompleted;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.started -= OnVideoStarted;
        videoPlayer.loopPointReached -= OnLoopPointReached;
    }
}

