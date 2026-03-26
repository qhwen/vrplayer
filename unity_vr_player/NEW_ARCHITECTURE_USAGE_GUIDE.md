# 新架构使用指南

> **版本**: 1.0.0
> **更新日期**: 2026-03-24
> **适用对象**: 开发者

---

## 目录

- [快速开始](#快速开始)
- [事件总线使用](#事件总线使用)
- [日志系统使用](#日志系统使用)
- [配置管理使用](#配置管理使用)
- [播放编排器使用](#播放编排器使用)
- [库管理器使用](#库管理器使用)
- [完整示例](#完整示例)

---

## 快速开始

### 1. 初始化新架构

```csharp
using VRPlayer.Core.EventBus;
using VRPlayer.Core.Logging;
using VRPlayer.Core.Config;

// 方式1：自动初始化（推荐）
// EventBus、LoggerManager、AppConfigManager 都是单例，
// 首次访问会自动初始化

// 方式2：手动初始化（可选）
var eventBus = EventBus.Instance;
var logger = LoggerManager.For("MyModule");
var config = AppConfigManager.Instance;
```

### 2. 创建基础场景

```csharp
using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // 初始化日志系统
        Log.SetLevel(LogLevel.Info);
        Log.Default.Info("Application initialized");

        // 加载配置
        var config = Config.Playback;
        Debug.Log($"Prepare timeout: {config.prepareTimeoutSeconds}秒");
    }
}
```

---

## 事件总线使用

### 基本使用

```csharp
using VRPlayer.Core.EventBus;

public class MyComponent : MonoBehaviour
{
    private void Start()
    {
        // 订阅事件
        EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);
        EventBus.Instance.Subscribe<PlaybackErrorEvent>(OnPlaybackError);
    }

    private void OnDestroy()
    {
        // 取消订阅
        EventBus.Instance.Unsubscribe<PlaybackStartedEvent>(OnPlaybackStarted);
        EventBus.Instance.Unsubscribe<PlaybackErrorEvent>(OnPlaybackError);
    }

    private void OnPlaybackStarted(PlaybackStartedEvent e)
    {
        Debug.Log($"Started playing: {e.VideoFile.name}");
    }

    private void OnPlaybackError(PlaybackErrorEvent e)
    {
        Debug.LogError($"Playback error: {e.Error.message}");
    }
}
```

### 发布事件

```csharp
// 发布视频选中事件
EventBus.Instance.Publish(new VideoSelectedEvent
{
    VideoFile = myVideoFile
});

// 发布播放进度事件
EventBus.Instance.Publish(new PlaybackProgressEvent
{
    Position = 120.5f,
    Duration = 360.0f,
    NormalizedProgress = 0.335f,
    IsPlaying = true
});

// 发布库更新事件
EventBus.Instance.Publish(new LibraryUpdatedEvent
{
    VideoCount = 50,
    UpdateTime = DateTime.Now
});
```

### 所有可用事件类型

| 事件类 | 说明 | 使用场景 |
|--------|------|----------|
| `VideoSelectedEvent` | 视频被选中 | 用户点击视频列表项 |
| `PlaybackStartedEvent` | 播放开始 | 开始播放新视频 |
| `PlaybackStateChangedEvent` | 播放状态改变 | 播放/暂停/停止 |
| `PlaybackProgressEvent` | 播放进度更新 | 更新进度条 |
| `PlaybackErrorEvent` | 播放错误 | 显示错误提示 |
| `PlaybackStoppedEvent` | 播放停止 | 清理资源 |
| `LibraryUpdatedEvent` | 库更新 | 刷新UI |
| `VideoAddedEvent` | 视频添加 | 显示通知 |
| `VideoRemovedEvent` | 视频移除 | 移除列表项 |
| `DownloadStartedEvent` | 下载开始 | 显示下载指示器 |
| `DownloadProgressEvent` | 下载进度 | 更新下载进度条 |
| `DownloadCompletedEvent` | 下载完成 | 隐藏下载指示器 |
| `PermissionChangedEvent` | 权限改变 | 更新UI状态 |
| `FileSelectionEvent` | 文件选择完成 | 处理选中的文件 |

---

## 日志系统使用

### 基本使用

```csharp
using VRPlayer.Core.Logging;

public class MyComponent : MonoBehaviour
{
    private readonly ILogger logger = LoggerManager.For("MyComponent");

    private void Start()
    {
        logger.Info("Component started");

        try
        {
            DoSomething();
            logger.Debug("Operation completed successfully");
        }
        catch (Exception ex)
        {
            logger.Error("Operation failed", ex);
        }
    }
}
```

### 日志级别

```csharp
// 设置全局最低日志级别
Log.SetLevel(LogLevel.Debug);

// 在配置中设置
Config.Set("log_level", "Debug");

// 级别说明：
// LogLevel.Debug   - 详细的调试信息
// LogLevel.Info    - 一般信息
// LogLevel.Warning - 警告信息
// LogLevel.Error   - 错误信息
// LogLevel.None    - 不输出日志
```

### 子日志记录器

```csharp
public class VideoManager : MonoBehaviour
{
    private ILogger logger;
    private ILogger cacheLogger;
    private ILogger playbackLogger;

    private void Start()
    {
        logger = LoggerManager.For("VideoManager");
        cacheLogger = logger.CreateChildLogger("Cache");
        playbackLogger = logger.CreateChildLogger("Playback");

        logger.Info("VideoManager initialized");
    }

    public void CacheVideo()
    {
        cacheLogger.Info("Caching video...");
    }

    public void PlayVideo()
    {
        playbackLogger.Info("Playing video...");
    }
}
```

### 日志格式

所有日志都遵循统一格式：

```
[14:23:45.123] [INFO] [VideoManager] VideoManager initialized
[14:23:46.456] [DEBUG] [VideoManager.Cache] Caching video...
[14:23:47.789] [ERROR] [VideoManager.Playback] Decoder failed
Exception: DecoderException
Message: Failed to decode video codec H.265
```

---

## 配置管理使用

### 基本使用

```csharp
using VRPlayer.Core.Config;

public class MyComponent : MonoBehaviour
{
    private void Start()
    {
        // 读取配置
        var playbackConfig = Config.Playback;
        float timeout = playbackConfig.prepareTimeoutSeconds;

        Debug.Log($"Prepare timeout: {timeout}秒");

        // 修改配置
        playbackConfig.prepareTimeoutSeconds = 45f;
        playbackConfig.autoPlayOnOpen = true;

        // 保存配置
        Config.SavePlaybackConfig(playbackConfig);

        // 或直接保存
        Config.Save();
    }
}
```

### 访问单个配置项

```csharp
// 读取配置
int scanDepth = Config.Get<int>("scan_depth", 2);
bool enableVR = Config.Get<bool>("enable_vr", true);

// 修改配置
Config.Set("scan_depth", 3);
Config.Set("enable_vr", false);

// 保存所有配置
Config.Save();
```

### 使用预定义配置类

#### PlaybackConfig（播放器配置）

```csharp
var config = Config.Playback;

// 修改配置
config.prepareTimeoutSeconds = 30f;
config.autoPlayOnOpen = false;
config.loopPlayback = true;
config.initialVolume = 0.8f;

config.renderTextureWidth = 1920;
config.renderTextureHeight = 1080;

config.enableHeadTracking = true;
config.rotationSensitivity = 0.5f;
config.smoothingFactor = 0.1f;

// 保存
Config.SavePlaybackConfig(config);
```

#### ScanConfig（扫描配置）

```csharp
var config = Config.Scan;

// 修改配置
config.scanDepth = 2;
config.maxVideos = 200;
config.scanDirectories = new[] { "Movies", "Downloads" };
config.includeSubdirectories = true;

config.supportedExtensions = new[] { ".mp4", ".mkv", ".mov" };
config.minFileSizeBytes = 1024 * 1024; // 1MB
config.maxFileSizeBytes = 10L * 1024 * 1024 * 1024; // 10GB

// Android特定
config.androidMoviesDirectory = "/storage/emulated/0/Movies";
config.includeMoviesSubdirectories = true;

// 保存
Config.SaveScanConfig(config);
```

#### CacheConfig（缓存配置）

```csharp
var config = Config.Cache;

// 修改配置
config.maxCacheSizeBytes = 2L * 1024 * 1024 * 1024; // 2GB
config.cleanupThreshold = 0.8f; // 80%时触发清理
config.cleanupStrategy = CacheConfig.CleanupStrategy.LRU;

config.maxCacheAgeDays = 30;
config.autoCleanupOnAppStart = true;

config.maxConcurrentDownloads = 3;
config.downloadTimeoutSeconds = 60;

// 保存
Config.SaveCacheConfig(config);

// 获取格式化的缓存大小限制
Debug.Log($"Max cache size: {config.GetFormattedMaxCacheSize()}");
```

---

## 播放编排器使用

### 基本使用

```csharp
using VRPlayer.Application.Playback;
using VRPlayer.Domain.Entities;

public class PlaybackController : MonoBehaviour
{
    public PlaybackOrchestrator orchestrator;

    private void Start()
    {
        // PlaybackOrchestrator 应该添加到场景中
        orchestrator = FindObjectOfType<PlaybackOrchestrator>();
    }

    public void PlayVideo(VideoFile video)
    {
        // 播放视频（会自动处理缓存、下载等）
        orchestrator.PlayVideoAsync(video);
    }

    public void Pause()
    {
        orchestrator.PausePlayback();
    }

    public void Resume()
    {
        orchestrator.ResumePlayback();
    }

    public void Stop()
    {
        orchestrator.StopPlayback();
    }

    public void SeekTo(float seconds)
    {
        orchestrator.SeekTo(seconds);
    }

    public void SetVolume(float volume)
    {
        orchestrator.SetVolume(volume);
    }

    public void CancelDownload()
    {
        orchestrator.CancelDownload();
    }
}
```

### 订阅播放事件

```csharp
using VRPlayer.Core.EventBus;

public class PlaybackUI : MonoBehaviour
{
    private void Start()
    {
        // 订阅播放开始事件
        EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);

        // 订阅播放进度事件
        EventBus.Instance.Subscribe<PlaybackProgressEvent>(OnPlaybackProgress);

        // 订阅播放错误事件
        EventBus.Instance.Subscribe<PlaybackErrorEvent>(OnPlaybackError);

        // 订阅下载进度事件
        EventBus.Instance.Subscribe<DownloadProgressEvent>(OnDownloadProgress);
    }

    private void OnDestroy()
    {
        // 取消订阅
        EventBus.Instance.Unsubscribe<PlaybackStartedEvent>(OnPlaybackStarted);
        EventBus.Instance.Unsubscribe<PlaybackProgressEvent>(OnPlaybackProgress);
        EventBus.Instance.Unsubscribe<PlaybackErrorEvent>(OnPlaybackError);
        EventBus.Instance.Unsubscribe<DownloadProgressEvent>(OnDownloadProgress);
    }

    private void OnPlaybackStarted(PlaybackStartedEvent e)
    {
        Debug.Log($"Playing: {e.VideoFile.name}");
        UpdateUI(e.VideoFile);
    }

    private void OnPlaybackProgress(PlaybackProgressEvent e)
    {
        Debug.Log($"Progress: {e.NormalizedProgress * 100:F1}%");
        UpdateProgressBar(e.NormalizedProgress);
        UpdateTimeDisplay(e.Position, e.Duration);
    }

    private void OnPlaybackError(PlaybackErrorEvent e)
    {
        Debug.LogError($"Error: {e.Error.message}");
        ShowError(e.Error.message);
    }

    private void OnDownloadProgress(DownloadProgressEvent e)
    {
        Debug.Log($"Download progress: {e.Progress * 100:F1}%");
        UpdateDownloadProgress(e.Progress);
    }
}
```

### 检查播放状态

```csharp
public class PlaybackMonitor : MonoBehaviour
{
    public PlaybackOrchestrator orchestrator;

    private void Update()
    {
        // 检查播放状态
        if (orchestrator.IsPlaying)
        {
            Debug.Log("Currently playing");
        }

        if (orchestrator.IsPaused)
        {
            Debug.Log("Currently paused");
        }

        if (orchestrator.IsDownloading)
        {
            Debug.Log("Currently downloading");
        }

        // 获取当前视频
        var currentVideo = orchestrator.CurrentVideo;
        if (currentVideo != null)
        {
            Debug.Log($"Current video: {currentVideo.name}");
        }

        // 获取播放状态
        var state = orchestrator.PlaybackState;
        Debug.Log($"Playback state: {state}");

        // 获取播放快照
        var snapshot = orchestrator.PlaybackSnapshot;
        Debug.Log($"Position: {snapshot.positionSeconds}s, Duration: {snapshot.durationSeconds}s");
    }
}
```

---

## 库管理器使用

### 基本使用

```csharp
using VRPlayer.Application.Library;
using VRPlayer.Domain.Entities;

public class LibraryController : MonoBehaviour
{
    public LibraryManager libraryManager;

    private void Start()
    {
        // LibraryManager 应该添加到场景中
        libraryManager = FindObjectOfType<LibraryManager>();
    }

    public void RefreshLibrary()
    {
        // 刷新视频库
        libraryManager.RefreshLibraryAsync();
    }

    public void OpenFilePicker()
    {
        // 打开文件选择器（多选）
        libraryManager.OpenFilePicker();
    }

    public void RequestPermissions()
    {
        // 请求权限
        libraryManager.RequestPermissionAsync(
            Domain.Storage.PermissionType.ReadMediaVideo
        );
    }

    public void OpenSettings()
    {
        // 打开应用设置
        libraryManager.OpenAppSettings();
    }
}
```

### 访问视频库

```csharp
public class LibraryUI : MonoBehaviour
{
    public LibraryManager libraryManager;

    private void Start()
    {
        // 获取所有视频
        var library = libraryManager.GetLibrary();
        Debug.Log($"Total videos: {library.Count}");

        // 获取视频数量
        int count = libraryManager.GetVideoCount();
        Debug.Log($"Video count: {count}");

        // 获取本地视频
        var localVideos = libraryManager.GetLocalVideos();
        Debug.Log($"Local videos: {localVideos.Count}");

        // 获取远程视频
        var remoteVideos = libraryManager.GetRemoteVideos();
        Debug.Log($"Remote videos: {remoteVideos.Count}");

        // 获取已缓存视频
        var cachedVideos = libraryManager.GetCachedVideos();
        Debug.Log($"Cached videos: {cachedVideos.Count}");
    }
}
```

### 搜索和筛选

```csharp
public class LibrarySearch : MonoBehaviour
{
    public LibraryManager libraryManager;

    public void Search(string query)
    {
        // 搜索视频
        var results = libraryManager.SearchVideos(query);
        Debug.Log($"Found {results.Count} videos for '{query}'");

        DisplayResults(results);
    }

    public void FilterBySource(VideoFile.VideoSourceType sourceType)
    {
        // 按来源筛选
        var videos = libraryManager.FilterBySource(sourceType);
        Debug.Log($"Found {videos.Count} videos from {sourceType}");

        DisplayResults(videos);
    }
}
```

### 添加和移除视频

```csharp
public class LibraryEditor : MonoBehaviour
{
    public LibraryManager libraryManager;

    public void AddVideo(VideoFile video)
    {
        // 添加单个视频
        libraryManager.AddVideoToLibrary(video);
    }

    public void AddVideos(List<VideoFile> videos)
    {
        // 批量添加视频
        libraryManager.AddVideosToLibrary(videos);
    }

    public void RemoveVideo(string videoPath)
    {
        // 移除视频
        libraryManager.RemoveVideoFromLibrary(videoPath);
    }

    public void ClearLibrary()
    {
        // 清空库
        libraryManager.ClearLibrary();
    }
}
```

### 订阅库事件

```csharp
using VRPlayer.Core.EventBus;

public class LibraryUI : MonoBehaviour
{
    private void Start()
    {
        // 订阅库更新事件
        EventBus.Instance.Subscribe<LibraryUpdatedEvent>(OnLibraryUpdated);

        // 订阅视频添加事件
        EventBus.Instance.Subscribe<VideoAddedEvent>(OnVideoAdded);

        // 订阅视频移除事件
        EventBus.Instance.Subscribe<VideoRemovedEvent>(OnVideoRemoved);

        // 订阅文件选择事件
        EventBus.Instance.Subscribe<FileSelectionEvent>(OnFileSelection);
    }

    private void OnDestroy()
    {
        // 取消订阅
        EventBus.Instance.Unsubscribe<LibraryUpdatedEvent>(OnLibraryUpdated);
        EventBus.Instance.Unsubscribe<VideoAddedEvent>(OnVideoAdded);
        EventBus.Instance.Unsubscribe<VideoRemovedEvent>(OnVideoRemoved);
        EventBus.Instance.Unsubscribe<FileSelectionEvent>(OnFileSelection);
    }

    private void OnLibraryUpdated(LibraryUpdatedEvent e)
    {
        Debug.Log($"Library updated: {e.VideoCount} videos");
        RefreshVideoList();
    }

    private void OnVideoAdded(VideoAddedEvent e)
    {
        Debug.Log($"Video added: {e.VideoFile.name}");
        AddVideoToList(e.VideoFile);
    }

    private void OnVideoRemoved(VideoRemovedEvent e)
    {
        Debug.Log($"Video removed: {e.VideoPath}");
        RemoveVideoFromList(e.VideoPath);
    }

    private void OnFileSelection(FileSelectionEvent e)
    {
        if (e.Success && e.SelectedPaths != null)
        {
            Debug.Log($"Selected {e.SelectedPaths.Length} files");
            foreach (var path in e.SelectedPaths)
            {
                Debug.Log($"  - {path}");
            }
        }
    }
}
```

---

## 完整示例

### 示例 1：创建简单的视频播放器

```csharp
using UnityEngine;
using UnityEngine.UI;

using VRPlayer.Application.Playback;
using VRPlayer.Application.Library;
using VRPlayer.Core.EventBus;
using VRPlayer.Core.Logging;
using VRPlayer.Domain.Entities;

public class SimpleVideoPlayer : MonoBehaviour
{
    public PlaybackOrchestrator playbackOrchestrator;
    public LibraryManager libraryManager;

    public Button playButton;
    public Button pauseButton;
    public Button stopButton;
    public Slider progressSlider;
    public Text statusText;

    private readonly ILogger logger = LoggerManager.For("SimpleVideoPlayer");

    private void Start()
    {
        logger.Info("SimpleVideoPlayer initialized");

        // 查找组件
        playbackOrchestrator = FindObjectOfType<PlaybackOrchestrator>();
        libraryManager = FindObjectOfType<LibraryManager>();

        if (playbackOrchestrator == null || libraryManager == null)
        {
            logger.Error("Required components not found");
            return;
        }

        // 订阅事件
        SubscribeToEvents();

        // 按钮事件
        playButton.onClick.AddListener(Play);
        pauseButton.onClick.AddListener(Pause);
        stopButton.onClick.AddListener(Stop);

        // 进度条事件
        progressSlider.onValueChanged.AddListener(OnSeek);
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);
        EventBus.Instance.Subscribe<PlaybackProgressEvent>(OnPlaybackProgress);
        EventBus.Instance.Subscribe<PlaybackErrorEvent>(OnPlaybackError);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Instance.Unsubscribe<PlaybackStartedEvent>(OnPlaybackStarted);
        EventBus.Instance.Unsubscribe<PlaybackProgressEvent>(OnPlaybackProgress);
        EventBus.Instance.Unsubscribe<PlaybackErrorEvent>(OnPlaybackError);
    }

    public void Play()
    {
        playbackOrchestrator.ResumePlayback();
    }

    public void Pause()
    {
        playbackOrchestrator.PausePlayback();
    }

    public void Stop()
    {
        playbackOrchestrator.StopPlayback();
    }

    public void OnSeek(float value)
    {
        var snapshot = playbackOrchestrator.PlaybackSnapshot;
        if (snapshot.durationSeconds > 0)
        {
            float targetTime = value * snapshot.durationSeconds;
            playbackOrchestrator.SeekTo(targetTime);
        }
    }

    public void PlayVideo(VideoFile video)
    {
        logger.Info($"Playing video: {video.name}");
        playbackOrchestrator.PlayVideoAsync(video);
    }

    private void OnPlaybackStarted(PlaybackStartedEvent e)
    {
        statusText.text = $"Playing: {e.VideoFile.name}";
        logger.Info($"Started playing: {e.VideoFile.name}");
    }

    private void OnPlaybackProgress(PlaybackProgressEvent e)
    {
        progressSlider.value = e.NormalizedProgress;
        statusText.text = $"{FormatTime(e.Position)} / {FormatTime(e.Duration)}";
    }

    private void OnPlaybackError(PlaybackErrorEvent e)
    {
        statusText.text = $"Error: {e.Error.message}";
        logger.Error($"Playback error: {e.Error.message}");
    }

    private string FormatTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        return $"{time.Minutes:D2}:{time.Seconds:D2}";
    }
}
```

### 示例 2：创建视频浏览器

```csharp
using UnityEngine;
using UnityEngine.UI;

using VRPlayer.Application.Library;
using VRPlayer.Core.EventBus;
using VRPlayer.Core.Logging;
using VRPlayer.Domain.Entities;

public class VideoBrowser : MonoBehaviour
{
    public LibraryManager libraryManager;
    public Transform videoListContent;
    public GameObject videoItemPrefab;
    public Button refreshButton;
    public Button selectVideosButton;
    public InputField searchInput;
    public Text videoCountText;

    private readonly ILogger logger = LoggerManager.For("VideoBrowser");
    private List<GameObject> videoItems = new List<GameObject>();

    private void Start()
    {
        logger.Info("VideoBrowser initialized");

        // 查找组件
        libraryManager = FindObjectOfType<LibraryManager>();
        if (libraryManager == null)
        {
            logger.Error("LibraryManager not found");
            return;
        }

        // 订阅事件
        SubscribeToEvents();

        // 按钮事件
        refreshButton.onClick.AddListener(OnRefresh);
        selectVideosButton.onClick.AddListener(OnSelectVideos);
        searchInput.onEndEdit.AddListener(OnSearch);

        // 初始化视频列表
        RefreshVideoList();
    }

    private void OnDestroy()
    {
        // 清理视频列表项
        foreach (var item in videoItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        videoItems.Clear();

        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        EventBus.Instance.Subscribe<LibraryUpdatedEvent>(OnLibraryUpdated);
        EventBus.Instance.Subscribe<VideoAddedEvent>(OnVideoAdded);
        EventBus.Instance.Subscribe<VideoRemovedEvent>(OnVideoRemoved);
        EventBus.Instance.Subscribe<FileSelectionEvent>(OnFileSelection);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Instance.Unsubscribe<LibraryUpdatedEvent>(OnLibraryUpdated);
        EventBus.Instance.Unsubscribe<VideoAddedEvent>(OnVideoAdded);
        EventBus.Instance.Unsubscribe<VideoRemovedEvent>(OnVideoRemoved);
        EventBus.Instance.Unsubscribe<FileSelectionEvent>(OnFileSelection);
    }

    private void OnRefresh()
    {
        logger.Info("Refreshing library");
        libraryManager.RefreshLibraryAsync();
    }

    private void OnSelectVideos()
    {
        logger.Info("Opening file picker");
        libraryManager.OpenFilePicker();
    }

    private void OnSearch(string query)
    {
        logger.Info($"Searching for: {query}");
        var results = libraryManager.SearchVideos(query);
        UpdateVideoList(results);
    }

    private void OnLibraryUpdated(LibraryUpdatedEvent e)
    {
        logger.Info($"Library updated: {e.VideoCount} videos");
        RefreshVideoList();
    }

    private void OnVideoAdded(VideoAddedEvent e)
    {
        logger.Info($"Video added: {e.VideoFile.name}");
        AddVideoItem(e.VideoFile);
        UpdateVideoCount();
    }

    private void OnVideoRemoved(VideoRemovedEvent e)
    {
        logger.Info($"Video removed: {e.VideoPath}");
        RefreshVideoList();
    }

    private void OnFileSelection(FileSelectionEvent e)
    {
        if (e.Success && e.SelectedPaths != null && e.SelectedPaths.Length > 0)
        {
            logger.Info($"Selected {e.SelectedPaths.Length} files");
            foreach (var path in e.SelectedPaths)
            {
                logger.Debug($"  - {path}");
            }
        }
    }

    private void RefreshVideoList()
    {
        var library = libraryManager.GetLibrary();
        UpdateVideoList(library);
    }

    private void UpdateVideoList(System.Collections.Generic.IReadOnlyList<VideoFile> videos)
    {
        // 清理现有项
        foreach (var item in videoItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        videoItems.Clear();

        // 创建新项
        foreach (var video in videos)
        {
            AddVideoItem(video);
        }

        UpdateVideoCount();
    }

    private void AddVideoItem(VideoFile video)
    {
        var item = Instantiate(videoItemPrefab, videoListContent);
        var videoItem = item.GetComponent<VideoItem>();

        if (videoItem != null)
        {
            videoItem.Initialize(video);
            videoItem.OnVideoClicked += OnVideoClicked;
        }

        videoItems.Add(item);
    }

    private void OnVideoClicked(VideoFile video)
    {
        logger.Info($"Video clicked: {video.name}");
        EventBus.Instance.Publish(new VideoSelectedEvent { VideoFile = video });
    }

    private void UpdateVideoCount()
    {
        videoCountText.text = $"Videos: {libraryManager.GetVideoCount()}";
    }
}
```

### 示例 3：配置管理UI

```csharp
using UnityEngine;
using UnityEngine.UI;

using VRPlayer.Core.Config;
using VRPlayer.Core.Logging;

public class SettingsUI : MonoBehaviour
{
    public Slider timeoutSlider;
    public Text timeoutText;
    public Toggle autoPlayToggle;
    public Toggle loopToggle;
    public Slider volumeSlider;
    public Text volumeText;
    public Button saveButton;
    public Button resetButton;

    private readonly ILogger logger = LoggerManager.For("SettingsUI");

    private void Start()
    {
        logger.Info("SettingsUI initialized");

        // 加载配置
        LoadSettings();

        // 按钮事件
        saveButton.onClick.AddListener(SaveSettings);
        resetButton.onClick.AddListener(ResetSettings);

        // 滑块事件
        timeoutSlider.onValueChanged.AddListener(OnTimeoutChanged);
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void LoadSettings()
    {
        var config = Config.Playback;

        timeoutSlider.value = config.prepareTimeoutSeconds;
        timeoutText.text = $"{config.prepareTimeoutSeconds}秒";

        autoPlayToggle.isOn = config.autoPlayOnOpen;
        loopToggle.isOn = config.loopPlayback;

        volumeSlider.value = config.initialVolume;
        volumeText.text = $"{config.initialVolume * 100:F0}%";
    }

    private void SaveSettings()
    {
        logger.Info("Saving settings");

        var config = Config.Playback;
        config.prepareTimeoutSeconds = timeoutSlider.value;
        config.autoPlayOnOpen = autoPlayToggle.isOn;
        config.loopPlayback = loopToggle.isOn;
        config.initialVolume = volumeSlider.value;

        Config.SavePlaybackConfig(config);

        logger.Info("Settings saved");
    }

    private void ResetSettings()
    {
        logger.Info("Resetting settings to defaults");

        // 重置为默认值
        var defaultConfig = new PlaybackConfig();
        Config.SavePlaybackConfig(defaultConfig);

        // 重新加载
        LoadSettings();
    }

    private void OnTimeoutChanged(float value)
    {
        timeoutText.text = $"{value:F0}秒";
    }

    private void OnVolumeChanged(float value)
    {
        volumeText.text = $"{value * 100:F0}%";
    }
}
```

---

## 总结

### 核心概念

1. **事件驱动**：使用 `EventBus` 实现模块间松耦合通信
2. **日志系统**：使用 `LoggerManager` 记录调试信息
3. **配置管理**：使用 `AppConfigManager` 管理应用配置
4. **播放编排**：使用 `PlaybackOrchestrator` 协调播放流程
5. **库管理**：使用 `LibraryManager` 管理视频库

### 最佳实践

1. **单例模式**：EventBus、LoggerManager、AppConfigManager 都是单例
2. **事件订阅**：在 `OnDestroy` 中取消订阅，避免内存泄漏
3. **异步操作**：使用 `async/await` 处理耗时操作
4. **错误处理**：使用 try-catch 捕获异常，记录日志
5. **依赖查找**：使用 `FindObjectOfType` 从场景中获取组件

### 下一步

- 查看 `ARCHITECTURE_REFACTOR_V2.md` 了解完整的架构设计
- 查看 `REFACTOR_PROGRESS.md` 了解当前进度
- 根据需求扩展或修改现有组件

---

**文档版本**: 1.0.0
**最后更新**: 2026-03-24
**维护者**: VR Player Team
