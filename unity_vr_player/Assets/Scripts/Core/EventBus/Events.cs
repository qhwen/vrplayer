using VRPlayer.Domain.Entities;

namespace VRPlayer.Core.EventBus
{
    #region 播放相关事件

    /// <summary>
    /// 视频选中事件
    /// </summary>
    public class VideoSelectedEvent
    {
        public string VideoPath { get; set; }
        public VideoFile VideoFile { get; set; }
    }

    /// <summary>
    /// 播放开始事件
    /// </summary>
    public class PlaybackStartedEvent
    {
        public VideoFile VideoFile { get; set; }
    }

    /// <summary>
    /// 播放状态改变事件
    /// </summary>
    public class PlaybackStateChangedEvent
    {
        public Domain.Playback.PlaybackState OldState { get; set; }
        public Domain.Playback.PlaybackState NewState { get; set; }
        public string VideoPath { get; set; }
    }

    /// <summary>
    /// 播放进度更新事件
    /// </summary>
    public class PlaybackProgressEvent
    {
        public float Position { get; set; }
        public float Duration { get; set; }
        public float NormalizedProgress { get; set; }
        public bool IsPlaying { get; set; }
    }

    /// <summary>
    /// 播放错误事件
    /// </summary>
    public class PlaybackErrorEvent
    {
        public Domain.Playback.PlaybackError Error { get; set; }
        public string VideoPath { get; set; }
    }

    /// <summary>
    /// 播放停止事件
    /// </summary>
    public class PlaybackStoppedEvent
    {
        public string VideoPath { get; set; }
        public bool WasCompleted { get; set; }
    }

    #endregion

    #region 库管理相关事件

    /// <summary>
    /// 库更新事件
    /// </summary>
    public class LibraryUpdatedEvent
    {
        public int VideoCount { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    /// <summary>
    /// 视频添加到库事件
    /// </summary>
    public class VideoAddedEvent
    {
        public VideoFile VideoFile { get; set; }
        public DateTime AddedTime { get; set; }
    }

    /// <summary>
    /// 视频从库移除事件
    /// </summary>
    public class VideoRemovedEvent
    {
        public string VideoPath { get; set; }
        public DateTime RemovedTime { get; set; }
    }

    #endregion

    #region 缓存相关事件

    /// <summary>
    /// 下载开始事件
    /// </summary>
    public class DownloadStartedEvent
    {
        public VideoFile VideoFile { get; set; }
        public string DownloadPath { get; set; }
    }

    /// <summary>
    /// 下载进度事件
    /// </summary>
    public class DownloadProgressEvent
    {
        public VideoFile VideoFile { get; set; }
        public float Progress { get; set; } // 0.0 - 1.0
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public string Speed { get; set; }
    }

    /// <summary>
    /// 下载完成事件
    /// </summary>
    public class DownloadCompletedEvent
    {
        public VideoFile VideoFile { get; set; }
        public string CachePath { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
    }

    /// <summary>
    /// 缓存清理事件
    /// </summary>
    public class CacheCleanupEvent
    {
        public long BytesFreed { get; set; }
        public int FilesRemoved { get; set; }
        public long NewTotalSize { get; set; }
    }

    #endregion

    #region 权限相关事件

    /// <summary>
    /// 权限改变事件
    /// </summary>
    public class PermissionChangedEvent
    {
        public Domain.Storage.PermissionType PermissionType { get; set; }
        public Domain.Storage.PermissionStatus OldStatus { get; set; }
        public Domain.Storage.PermissionStatus NewStatus { get; set; }
    }

    /// <summary>
    /// 权限请求结果事件
    /// </summary>
    public class PermissionRequestResultEvent
    {
        public Domain.Storage.PermissionType PermissionType { get; set; }
        public Domain.Storage.PermissionRequestResult Result { get; set; }
    }

    #endregion

    #region 文件选择相关事件

    /// <summary>
    /// 文件选择结果事件
    /// </summary>
    public class FileSelectionEvent
    {
        public bool Success { get; set; }
        public string[] SelectedPaths { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion

    #region UI相关事件

    /// <summary>
    /// UI状态改变事件
    /// </summary>
    public class UIStateChangedEvent
    {
        public string UIComponentName { get; set; }
        public string OldState { get; set; }
        public string NewState { get; set; }
    }

    /// <summary>
    /// UI导航事件
    /// </summary>
    public class UINavigationEvent
    {
        public string FromView { get; set; }
        public string ToView { get; set; }
        public object NavigationData { get; set; }
    }

    #endregion

    #region VR相关事件

    /// <summary>
    /// VR头部旋转事件
    /// </summary>
    public class VRHeadRotationEvent
    {
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
    }

    /// <summary>
    /// VR手势事件
    /// </summary>
    public class VRGestureEvent
    {
        public GestureType GestureType { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 Delta { get; set; }
    }

    public enum GestureType
    {
        Tap,
        DoubleTap,
        LongPress,
        Drag,
        Pinch,
        Rotate
    }

    #endregion

    #region 系统相关事件

    /// <summary>
    /// 应用暂停事件
    /// </summary>
    public class AppPausedEvent
    {
        public DateTime PauseTime { get; set; }
    }

    /// <summary>
    /// 应用恢复事件
    /// </summary>
    public class AppResumedEvent
    {
        public DateTime ResumeTime { get; set; }
        public TimeSpan PausedDuration { get; set; }
    }

    /// <summary>
    /// 应用退出事件
    /// </summary>
    public class AppQuitEvent
    {
        public DateTime QuitTime { get; set; }
    }

    #endregion

    #region 辅助类

    /// <summary>
    /// 2D向量辅助类
    /// </summary>
    [Serializable]
    public class Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2 zero => new Vector2(0f, 0f);
        public static Vector2 one => new Vector2(1f, 1f);

        public override string ToString() => $"({x}, {y})";
    }

    #endregion
}
