# VR Player 架构重构设计文档

> **版本**: 2.0.0
> **创建日期**: 2026-03-24
> **Unity 版本**: 2022.3 LTS
> **重构原则**: 渐进式重构,保持Unity与Java原始架构不变

---

## 目录

- [重构目标](#重构目标)
- [架构设计原则](#架构设计原则)
- [新架构概览](#新架构概览)
- [核心模块设计](#核心模块设计)
- [数据交互逻辑](#数据交互逻辑)
- [平台适配策略](#平台适配策略)
- [迁移路线图](#迁移路线图)

---

## 重构目标

### 1. 保持不变的部分

✅ **Unity与Java的原始架构**
- Unity C# 层保持为核心逻辑层
- Android Java 层保持为平台适配层
- Unity-Java 交互方式不变（JNI/AndroidJavaObject）

✅ **核心功能模块**
- VR 视频播放
- 本地文件管理
- WebDAV 集成
- Android 权限适配
- SAF 文件选择器

### 2. 需要优化的部分

🔄 **代码结构优化**
- 职责分离：拆分大类为单一职责的小类
- 依赖注入：降低耦合度
- 接口抽象：提高可测试性

🔄 **模块划分优化**
- 基础设施层：统一日志、配置、事件
- 服务层：业务逻辑封装
- 平台层：平台特定代码隔离

🔄 **数据交互优化**
- 统一数据模型
- 标准化通信协议
- 错误处理机制

---

## 架构设计原则

### 1. SOLID 原则

**S - Single Responsibility（单一职责）**
```csharp
// ❌ 错误：单一类承担过多职责
public class LocalFileManager : MonoBehaviour
{
    // 文件扫描
    // 缓存管理
    // 权限请求
    // SAF 调用
    // 持久化
}

// ✅ 正确：职责分离
public interface IFileScanner { }
public interface ICacheManager { }
public interface IPermissionManager { }
public interface IStorageAccess { }
public interface ISettingsRepository { }
```

**O - Open/Closed（开闭原则）**
```csharp
// ✅ 通过接口扩展，而非修改现有代码
public interface IVideoSource
{
    Task<List<VideoFile>> ListAsync(string path);
}

// 扩展新的视频源无需修改现有代码
public class S3VideoSource : IVideoSource { }
public class AzureBlobVideoSource : IVideoSource { }
```

**L - Liskov Substitution（里氏替换）**
```csharp
// ✅ 所有实现可互换使用
IPlaybackService service = GetComponent<UnityVideoPlaybackService>();
// 或
IPlaybackService service = GetComponent<ExoPlayerPlaybackService>();
```

**I - Interface Segregation（接口隔离）**
```csharp
// ❌ 肥接口
public interface IFileManager
{
    void Scan();
    void Cache();
    void RequestPermission();
    void OpenPicker();
}

// ✅ 拆分为多个小接口
public interface IFileScanner { }
public interface ICacheManager { }
public interface IPermissionManager { }
public interface IStorageAccess { }
```

**D - Dependency Inversion（依赖倒置）**
```csharp
// ✅ 依赖抽象，不依赖具体实现
public class VideoBrowserUI : MonoBehaviour
{
    private readonly IPlaybackService playbackService;
    private readonly IFileScanner fileScanner;

    public VideoBrowserUI(
        IPlaybackService playbackService,
        IFileScanner fileScanner)
    {
        this.playbackService = playbackService;
        this.fileScanner = fileScanner;
    }
}
```

### 2. 分层架构原则

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │  表现层（UI、场景）
├─────────────────────────────────────────┤
│          Application Layer              │  应用层（用例、流程）
├─────────────────────────────────────────┤
│            Domain Layer                 │  领域层（实体、接口）
├─────────────────────────────────────────┤
│         Infrastructure Layer            │  基础设施层（服务实现）
├─────────────────────────────────────────┤
│           Platform Layer                │  平台层（平台适配）
└─────────────────────────────────────────┘
```

**依赖规则**：
- 上层可以依赖下层
- 下层不能依赖上层
- 同层之间通过接口通信

---

## 新架构概览

### 整体架构图

```
┌─────────────────────────────────────────────────────────┐
│                    Unity C# Layer                        │
│                                                          │
│  ┌──────────────────────────────────────────────┐      │
│  │     Presentation (UI & Scene Management)     │      │
│  │  ┌────────────┐  ┌──────────────┐           │      │
│  │  │ VideoUI    │  │ VRPlayerUI  │           │      │
│  │  └────────────┘  └──────────────┘           │      │
│  └──────────────────────────────────────────────┘      │
│                        ↓                                 │
│  ┌──────────────────────────────────────────────┐      │
│  │     Application (Use Cases & Orchestration)  │      │
│  │  ┌──────────────┐  ┌──────────────┐         │      │
│  │  │ PlaybackFlow │  │ LibraryFlow  │         │      │
│  │  └──────────────┘  └──────────────┘         │      │
│  └──────────────────────────────────────────────┘      │
│                        ↓                                 │
│  ┌──────────────────────────────────────────────┐      │
│  │         Domain (Entities & Interfaces)       │      │
│  │  ┌─────────────┐  ┌─────────────┐           │      │
│  │  │ IPlayback   │  │ IVideoSource│           │      │
│  │  │ ICache      │  │ IPermission │           │      │
│  │  └─────────────┘  └─────────────┘           │      │
│  └──────────────────────────────────────────────┘      │
│                        ↓                                 │
│  ┌──────────────────────────────────────────────┐      │
│  │      Infrastructure (Service Implementations) │      │
│  │  ┌─────────────┐  ┌─────────────┐           │      │
│  │  │ VideoPlayer │  │ FileCache   │           │      │
│  │  │ WebDavSrc   │  │ LocalSrc    │           │      │
│  │  └─────────────┘  └─────────────┘           │      │
│  └──────────────────────────────────────────────┘      │
│                        ↓                                 │
│  ┌──────────────────────────────────────────────┐      │
│  │    Platform Abstraction Layer (PAL)          │      │
│  │  ┌──────────────┐  ┌──────────────────┐     │      │
│  │  │ IPlatform    │  │ IAndroidBridge   │     │      │
│  │  │ IIOSBridge   │  │ IWindowsBridge   │     │      │
│  │  └──────────────┘  └──────────────────┘     │      │
│  └──────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│                  Native Platform Layer                  │
│                                                          │
│  ┌────────────────┐  ┌──────────────────┐              │
│  │  Android (Java)│  │ iOS (Objective-C)│              │
│  │  - SAF Picker  │  │  - PHPicker      │              │
│  │  - Permissions │  │  - Photos        │              │
│  └────────────────┘  └──────────────────┘              │
└─────────────────────────────────────────────────────────┘
```

### 目录结构

```
unity_vr_player/Assets/Scripts/
├── Core/                           # 核心基础设施
│   ├── EventBus/                   # 事件总线
│   ├── Logging/                    # 日志系统
│   ├── Config/                     # 配置管理
│   └── DiContainer/                # 依赖注入容器
│
├── Domain/                         # 领域层
│   ├── Entities/                   # 实体
│   │   ├── VideoFile.cs
│   │   ├── PlaybackState.cs
│   │   └── PlaybackError.cs
│   ├── Interfaces/                 # 核心接口
│   │   ├── Playback/
│   │   │   ├── IPlaybackService.cs
│   │   │   ├── IVideoSource.cs
│   │   │   └── ICacheService.cs
│   │   ├── Storage/
│   │   │   ├── IFileScanner.cs
│   │   │   ├── ICacheManager.cs
│   │   │   ├── IPermissionManager.cs
│   │   │   └── IStorageAccess.cs
│   │   └── Platform/
│   │       ├── IPlatformService.cs
│   │       └── IPlatformBridge.cs
│   └── ValueObjects/              # 值对象
│       └── VideoMetadata.cs
│
├── Application/                   # 应用层（用例）
│   ├── Playback/
│   │   ├── PlaybackOrchestrator.cs
│   │   ├── PlaybackController.cs
│   │   └── PlaybackStateManager.cs
│   ├── Library/
│   │   ├── LibraryManager.cs
│   │   ├── LibraryScanner.cs
│   │   └── LibraryCacheManager.cs
│   └── WebDAV/
│       ├── WebDAVOrchestrator.cs
│       └── WebDAVDownloadManager.cs
│
├── Infrastructure/                # 基础设施实现
│   ├── Playback/
│   │   ├── UnityPlaybackService.cs
│   │   └── ExoPlayerBridge.cs      # Android ExoPlayer 集成
│   ├── Storage/
│   │   ├── LocalFileScanner.cs
│   │   ├── FileCacheManager.cs
│   │   └── AndroidPermissionManager.cs
│   ├── Network/
│   │   ├── WebDAVClient.cs
│   │   └── HttpClientWrapper.cs
│   └── Persistence/
│       ├── PlayerPrefsRepository.cs
│       └── FileRepository.cs
│
├── Platform/                      # 平台适配
│   ├── Android/
│   │   ├── AndroidBridge.cs
│   │   ├── SAFPickerBridge.cs
│   │   └── AndroidPermissionBridge.cs
│   ├── iOS/
│   │   ├── iOSBridge.cs
│   │   └── PHPickerBridge.cs
│   └── Windows/
│       └── WindowsBridge.cs
│
├── Presentation/                  # 表现层
│   ├── UI/
│   │   ├── Components/
│   │   │   ├── VideoListItem.cs
│   │   │   ├── ControlPanel.cs
│   │   │   └── ProgressBar.cs
│   │   ├── Views/
│   │   │   ├── VideoBrowserView.cs
│   │   │   ├── PlayerView.cs
│   │   │   └── SettingsView.cs
│   │   └── ViewModels/
│   │       ├── VideoBrowserViewModel.cs
│   │       └── PlayerViewModel.cs
│   ├── VR/
│   │   ├── VRVideoRenderer.cs
│   │   ├── VRInputController.cs
│   │   └── VRCameraController.cs
│   └── Scenes/
│       └── SceneController.cs
│
└── Bootstrap/                     # 启动引导
    ├── AppBootstrap.cs
    ├── ServiceInstaller.cs
    └── RuntimeInitializer.cs
```

---

## 核心模块设计

### 1. 基础设施层（Core）

#### 1.1 事件总线

```csharp
/// <summary>
/// 简单的事件总线，用于模块间解耦通信
/// </summary>
public interface IEventBus
{
    void Subscribe<T>(Action<T> handler) where T : class;
    void Unsubscribe<T>(Action<T> handler) where T : class;
    void Publish<T>(T eventData) where T : class;
}

/// <summary>
/// 领域事件定义
/// </summary>
public class VideoSelectedEvent
{
    public string VideoPath { get; set; }
    public VideoFile VideoFile { get; set; }
}

public class PlaybackStateChangedEvent
{
    public PlaybackState OldState { get; set; }
    public PlaybackState NewState { get; set; }
}

public class LibraryUpdatedEvent
{
    public int VideoCount { get; set; }
    public DateTime UpdateTime { get; set; }
}
```

**优势**：
- 模块间松耦合
- 易于扩展新事件
- 支持多订阅者

#### 1.2 日志系统

```csharp
/// <summary>
/// 统一的日志接口
/// </summary>
public interface ILogger
{
    void Debug(string message, object context = null);
    void Info(string message, object context = null);
    void Warning(string message, object context = null);
    void Error(string message, Exception exception = null, object context = null);
}

/// <summary>
/// 结构化日志记录器
/// </summary>
public class StructuredLogger : ILogger
{
    private readonly string module;
    private readonly LogLevel minLevel;

    public StructuredLogger(string module, LogLevel minLevel = LogLevel.Info)
    {
        this.module = module;
        this.minLevel = minLevel;
    }

    public void Info(string message, object context = null)
    {
        if (minLevel > LogLevel.Info) return;
        Log("INFO", message, context);
    }

    public void Error(string message, Exception exception = null, object context = null)
    {
        Log("ERROR", message, context, exception);
    }

    private void Log(string level, string message, object context = null, Exception exception = null)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string contextStr = context != null ? $"[{context.GetType().Name}] " : "";
        string logMessage = $"[{timestamp}] [{level}] [{module}] {contextStr}{message}";

        if (exception != null)
        {
            logMessage += $"\nException: {exception}";
        }

        Debug.Log(logMessage);
    }
}
```

**优势**：
- 统一日志格式
- 可配置日志级别
- 支持上下文信息

#### 1.3 配置管理

```csharp
/// <summary>
/// 应用配置接口
/// </summary>
public interface IAppConfig
{
    T Get<T>(string key, T defaultValue = default);
    void Set<T>(string key, T value);
    void Save();
}

/// <summary>
/// 播放器配置
/// </summary>
[Serializable]
public class PlaybackConfig
{
    [Range(5f, 120f)]
    public float prepareTimeoutSeconds = 30f;

    public bool autoPlayOnOpen = false;
    public bool loopPlayback = false;
    [Range(0f, 1f)]
    public float initialVolume = 1f;
    public int renderTextureWidth = 1920;
    public int renderTextureHeight = 1080;
}

/// <summary>
/// 扫描配置
/// </summary>
[Serializable]
public class ScanConfig
{
    [Range(0, 5)]
    public int scanDepth = 2;
    [Range(20, 500)]
    public int maxVideos = 200;
    public string[] scanDirectories = { "Movies", "Downloads" };
    public bool includeSubdirectories = true;
}

/// <summary>
/// 配置管理器
/// </summary>
public class ConfigManager : IAppConfig
{
    private Dictionary<string, object> configs = new Dictionary<string, object>();
    private PlaybackConfig playbackConfig;
    private ScanConfig scanConfig;

    public void Load()
    {
        // 从 PlayerPrefs 加载
        string playbackJson = PlayerPrefs.GetString("config_playback");
        string scanJson = PlayerPrefs.GetString("config_scan");

        playbackConfig = JsonUtility.FromJson<PlaybackConfig>(playbackJson) ?? new PlaybackConfig();
        scanConfig = JsonUtility.FromJson<ScanConfig>(scanJson) ?? new ScanConfig();
    }

    public void Save()
    {
        PlayerPrefs.SetString("config_playback", JsonUtility.ToJson(playbackConfig));
        PlayerPrefs.SetString("config_scan", JsonUtility.ToJson(scanConfig));
        PlayerPrefs.Save();
    }

    public T Get<T>(string key, T defaultValue = default)
    {
        // 实现
        return defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        // 实现
    }
}
```

**优势**：
- 类型安全
- 序列化/反序列化
- 统一配置接口

---

### 2. 领域层（Domain）

#### 2.1 播放服务接口

```csharp
/// <summary>
/// 播放服务接口 - 核心抽象
/// </summary>
public interface IPlaybackService
{
    // 状态
    PlaybackState State { get; }
    PlaybackSnapshot Snapshot { get; }
    PlaybackError LastError { get; }
    bool HasSource { get; }
    string CurrentSource { get; }
    Texture CurrentTexture { get; }

    // 控制
    bool Open(string source);
    void Play();
    void Pause();
    void Stop();
    void Seek(float seconds);
    void SetVolume(float volume);

    // 事件
    event Action<PlaybackState> StateChanged;
    event Action<PlaybackSnapshot> PlaybackUpdated;
    event Action<PlaybackError> ErrorOccurred;
}
```

#### 2.2 文件扫描接口

```csharp
/// <summary>
/// 文件扫描接口
/// </summary>
public interface IFileScanner
{
    Task<ScanResult> ScanAsync(ScanOptions options);
    event Action<ScanProgress> ScanProgress;
}

/// <summary>
/// 扫描选项
/// </summary>
public class ScanOptions
{
    public string[] Directories { get; set; }
    public int MaxDepth { get; set; }
    public int MaxResults { get; set; }
    public bool IncludeSubdirectories { get; set; }
    public CancellationToken CancellationToken { get; set; }
}

/// <summary>
/// 扫描结果
/// </summary>
public class ScanResult
{
    public List<VideoFile> Videos { get; set; }
    public int TotalScanned { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}

/// <summary>
/// 扫描进度
/// </summary>
public class ScanProgress
{
    public int FoundCount { get; set; }
    public int ScannedCount { get; set; }
    public float ProgressPercentage { get; set; }
    public string CurrentDirectory { get; set; }
}
```

#### 2.3 缓存管理接口

```csharp
/// <summary>
/// 缓存管理接口
/// </summary>
public interface ICacheManager
{
    string GetCachePath(string key, string extension = ".mp4");
    bool Exists(string key, string extension = ".mp4");
    Task<bool> StoreAsync(string key, string sourcePath, string extension = ".mp4", Action<float> onProgress = null);
    bool Evict(string key, string extension = ".mp4");
    long GetTotalSizeBytes();
    long GetFreeSpaceBytes();
    void Clear();
    Task<CacheCleanupResult> CleanupAsync(CleanupOptions options);
}

/// <summary>
/// 缓存清理选项
/// </summary>
public class CleanupOptions
{
    public long TargetFreeBytes { get; set; }
    public TimeSpan? MaxAge { get; set; }
    public CleanupStrategy Strategy { get; set; } = CleanupStrategy.LRU;
}

public enum CleanupStrategy
{
    LRU,        // 最近最少使用
    FIFO,       // 先进先出
    Oldest      // 最旧优先
}
```

#### 2.4 权限管理接口

```csharp
/// <summary>
/// 权限管理接口
/// </summary>
public interface IPermissionManager
{
    Task<PermissionStatus> CheckPermissionAsync(PermissionType permission);
    Task<PermissionRequestResult> RequestPermissionAsync(PermissionType permission);
    bool ShouldShowRequestRationale(PermissionType permission);
    void OpenAppSettings();
    event Action<PermissionType, PermissionStatus> PermissionChanged;
}

public enum PermissionType
{
    ReadMediaVideo,
    ReadExternalStorage,
    Camera,
    Microphone
}

public enum PermissionStatus
{
    NotRequested,
    Granted,
    Denied,
    DeniedPermanently
}

public class PermissionRequestResult
{
    public PermissionStatus Status { get; set; }
    public bool IsPermanentlyDenied { get; set; }
}
```

#### 2.5 存储访问接口

```csharp
/// <summary>
/// 存储访问接口 - 统一的文件选择器接口
/// </summary>
public interface IStorageAccess
{
    void OpenFilePicker(FilePickerOptions options);
    void OpenMultipleFilePicker(FilePickerOptions options);
    event Action<FilePickerResult> FileSelected;
}

/// <summary>
/// 文件选择器选项
/// </summary>
public class FilePickerOptions
{
    public string[] AllowedExtensions { get; set; } = { ".mp4", ".mkv", ".mov" };
    public string Title { get; set; } = "Select Video";
    public bool AllowMultiple { get; set; } = false;
}

/// <summary>
/// 文件选择结果
/// </summary>
public class FilePickerResult
{
    public bool Success { get; set; }
    public List<string> SelectedPaths { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

### 3. 应用层（Application）

#### 3.1 播放编排器

```csharp
/// <summary>
/// 播放编排器 - 协调播放相关操作
/// </summary>
public class PlaybackOrchestrator : MonoBehaviour
{
    private readonly IPlaybackService playbackService;
    private readonly ICacheManager cacheManager;
    private readonly IEventBus eventBus;
    private readonly ILogger logger;

    public PlaybackOrchestrator(
        IPlaybackService playbackService,
        ICacheManager cacheManager,
        IEventBus eventBus,
        ILogger logger)
    {
        this.playbackService = playbackService;
        this.cacheManager = cacheManager;
        this.eventBus = eventBus;
        this.logger = logger;

        SubscribeToEvents();
    }

    public async Task PlayVideoAsync(VideoFile video)
    {
        logger.Info($"Starting playback: {video.name}");

        // 检查缓存
        string cachedPath = cacheManager.GetCachePath(video.path);
        if (cacheManager.Exists(video.path))
        {
            logger.Info($"Using cached version: {cachedPath}");
            video.localPath = cachedPath;
        }
        else
        {
            // 下载到缓存
            if (video.IsRemote)
            {
                logger.Info($"Downloading video: {video.url}");
                bool downloaded = await cacheManager.StoreAsync(video.path, video.url,
                    progress => eventBus.Publish(new DownloadProgressEvent
                    {
                        VideoFile = video,
                        Progress = progress
                    }));

                if (downloaded)
                {
                    video.localPath = cachedPath;
                }
            }
        }

        // 播放
        if (playbackService.Open(video.localPath))
        {
            playbackService.Play();
            eventBus.Publish(new PlaybackStartedEvent { VideoFile = video });
        }
    }

    private void SubscribeToEvents()
    {
        playbackService.StateChanged += OnStateChanged;
        playbackService.ErrorOccurred += OnErrorOccurred;
    }

    private void OnStateChanged(PlaybackState state)
    {
        eventBus.Publish(new PlaybackStateChangedEvent
        {
            NewState = state
        });
    }

    private void OnErrorOccurred(PlaybackError error)
    {
        logger.Error($"Playback error: {error.message}");
        eventBus.Publish(new PlaybackErrorEvent { Error = error });
    }
}
```

#### 3.2 库管理器

```csharp
/// <summary>
/// 库管理器 - 管理视频库
/// </summary>
public class LibraryManager : MonoBehaviour
{
    private readonly IFileScanner fileScanner;
    private readonly IPermissionManager permissionManager;
    private readonly IEventBus eventBus;
    private readonly ILogger logger;

    private List<VideoFile> library = new List<VideoFile>();

    public IReadOnlyList<VideoFile> Library => library.AsReadOnly();

    public LibraryManager(
        IFileScanner fileScanner,
        IPermissionManager permissionManager,
        IEventBus eventBus,
        ILogger logger)
    {
        this.fileScanner = fileScanner;
        this.permissionManager = permissionManager;
        this.eventBus = eventBus;
        this.logger = logger;
    }

    public async Task RefreshLibraryAsync()
    {
        logger.Info("Refreshing library...");

        // 检查权限
        var permissionStatus = await permissionManager.CheckPermissionAsync(PermissionType.ReadMediaVideo);
        if (permissionStatus != PermissionStatus.Granted)
        {
            var requestResult = await permissionManager.RequestPermissionAsync(PermissionType.ReadMediaVideo);
            if (requestResult.Status != PermissionStatus.Granted)
            {
                logger.Warning("Permission denied, cannot scan library");
                return;
            }
        }

        // 扫描
        var options = new ScanOptions
        {
            Directories = new[] { "Movies", "Downloads" },
            MaxDepth = 2,
            MaxResults = 200,
            IncludeSubdirectories = true
        };

        var result = await fileScanner.ScanAsync(options);

        if (result.Success)
        {
            library = result.Videos;
            logger.Info($"Library refreshed: {library.Count} videos");
            eventBus.Publish(new LibraryUpdatedEvent { VideoCount = library.Count });
        }
        else
        {
            logger.Error($"Library refresh failed: {result.ErrorMessage}");
        }
    }
}
```

---

### 4. 基础设施实现层（Infrastructure）

#### 4.1 本地文件扫描器

```csharp
/// <summary>
/// 本地文件扫描器实现
/// </summary>
public class LocalFileScanner : IFileScanner
{
    private readonly ILogger logger;
    private readonly string[] supportedExtensions = { ".mp4", ".mkv", ".mov" };

    public event Action<ScanProgress> ScanProgress;

    public LocalFileScanner(ILogger logger)
    {
        this.logger = logger;
    }

    public async Task<ScanResult> ScanAsync(ScanOptions options)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var videos = new List<VideoFile>();
        int scannedCount = 0;

        foreach (var dir in options.Directories)
        {
            var fullPath = Path.Combine(Application.persistentDataPath, dir);
            if (!Directory.Exists(fullPath))
            {
                logger.Warning($"Directory not found: {fullPath}");
                continue;
            }

            await ScanDirectoryAsync(fullPath, videos, options, 0, ref scannedCount);
        }

        stopwatch.Stop();

        return new ScanResult
        {
            Videos = videos.Take(options.MaxResults).ToList(),
            TotalScanned = scannedCount,
            Duration = stopwatch.Elapsed,
            Success = true
        };
    }

    private async Task ScanDirectoryAsync(
        string path,
        List<VideoFile> videos,
        ScanOptions options,
        int currentDepth,
        ref int scannedCount)
    {
        if (currentDepth > options.MaxDepth || videos.Count >= options.MaxResults)
        {
            return;
        }

        try
        {
            var files = Directory.GetFiles(path);
            foreach (var file in files)
            {
                scannedCount++;
                var ext = Path.GetExtension(file).ToLower();
                if (supportedExtensions.Contains(ext))
                {
                    var fileInfo = new FileInfo(file);
                    videos.Add(new VideoFile
                    {
                        name = Path.GetFileNameWithoutExtension(file),
                        path = file,
                        size = fileInfo.Length,
                        is360 = false // 可根据文件名判断
                    });

                    ScanProgress?.Invoke(new ScanProgress
                    {
                        FoundCount = videos.Count,
                        ScannedCount = scannedCount,
                        CurrentDirectory = path
                    });
                }
            }

            if (options.IncludeSubdirectories)
            {
                var directories = Directory.GetDirectories(path);
                foreach (var dir in directories)
                {
                    await ScanDirectoryAsync(dir, videos, options, currentDepth + 1, ref scannedCount);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Error scanning directory: {path}", ex);
        }
    }
}
```

#### 4.2 Android 权限管理器

```csharp
#if UNITY_ANDROID
/// <summary>
/// Android 权限管理器
/// </summary>
public class AndroidPermissionManager : IPermissionManager
{
    private readonly ILogger logger;
    private PermissionRequestResult lastRequestResult;

    public event Action<PermissionType, PermissionStatus> PermissionChanged;

    public AndroidPermissionManager(ILogger logger)
    {
        this.logger = logger;
    }

    public Task<PermissionStatus> CheckPermissionAsync(PermissionType permission)
    {
        string androidPermission = GetAndroidPermissionString(permission);
        bool granted = Permission.HasUserAuthorizedPermission(androidPermission);
        return Task.FromResult(granted ? PermissionStatus.Granted : PermissionStatus.NotRequested);
    }

    public async Task<PermissionRequestResult> RequestPermissionAsync(PermissionType permission)
    {
        string androidPermission = GetAndroidPermissionString(permission);

        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += perm => OnPermissionGranted(permission);
        callbacks.PermissionDenied += perm => OnPermissionDenied(permission, false);
        callbacks.PermissionDeniedAndDontAskAgain += perm => OnPermissionDenied(permission, true);

        Permission.RequestUserPermission(androidPermission, callbacks);

        // 等待权限请求完成（简化实现）
        await Task.Delay(100);

        lastRequestResult.IsPermanentlyDenied =
            lastRequestResult.Status == PermissionStatus.Denied &&
            !Permission.HasUserAuthorizedPermission(androidPermission) &&
            !ShouldShowRequestRationale(permission);

        return lastRequestResult;
    }

    public bool ShouldShowRequestRationale(PermissionType permission)
    {
        string androidPermission = GetAndroidPermissionString(permission);
        return !Permission.HasUserAuthorizedPermission(androidPermission);
    }

    public void OpenAppSettings()
    {
        using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = new AndroidJavaObject("android.content.Intent",
            "android.settings.APPLICATION_DETAILS_SETTINGS"))
        {
            string uri = "package:" + currentActivity.Call<string>("getPackageName");
            intent.Call<AndroidJavaObject>("setData", AndroidJavaObject.GetStatic<AndroidJavaObject>("android.net.Uri", "parse", uri));
            currentActivity.Call("startActivity", intent);
        }
    }

    private string GetAndroidPermissionString(PermissionType permission)
    {
        return permission switch
        {
            PermissionType.ReadMediaVideo => "android.permission.READ_MEDIA_VIDEO",
            PermissionType.ReadExternalStorage => "android.permission.READ_EXTERNAL_STORAGE",
            _ => throw new ArgumentException($"Unsupported permission: {permission}")
        };
    }

    private void OnPermissionGranted(PermissionType permission)
    {
        lastRequestResult = new PermissionRequestResult
        {
            Status = PermissionStatus.Granted,
            IsPermanentlyDenied = false
        };
        PermissionChanged?.Invoke(permission, PermissionStatus.Granted);
    }

    private void OnPermissionDenied(PermissionType permission, bool permanent)
    {
        lastRequestResult = new PermissionRequestResult
        {
            Status = PermissionStatus.Denied,
            IsPermanentlyDenied = permanent
        };
        PermissionChanged?.Invoke(permission, permanent ? PermissionStatus.DeniedPermanently : PermissionStatus.Denied);
    }
}
#endif
```

#### 4.3 Android 存储访问

```csharp
#if UNITY_ANDROID
/// <summary>
/// Android 存储访问实现
/// </summary>
public class AndroidStorageAccess : IStorageAccess
{
    private readonly ILogger logger;

    public event Action<FilePickerResult> FileSelected;

    public AndroidStorageAccess(ILogger logger)
    {
        this.logger = logger;
    }

    public void OpenFilePicker(FilePickerOptions options)
    {
        using (var bridge = new AndroidJavaClass("com.vrplayer.saf.SafPickerBridge"))
        {
            var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity");

            bridge.CallStatic("launchVideoPicker", activity, options.AllowMultiple, "OnAndroidVideoPickerResult");
        }
    }

    public void OpenMultipleFilePicker(FilePickerOptions options)
    {
        var multiOptions = new FilePickerOptions
        {
            AllowedExtensions = options.AllowedExtensions,
            Title = options.Title,
            AllowMultiple = true
        };
        OpenFilePicker(multiOptions);
    }

    /// <summary>
    /// Android Java 回调方法
    /// </summary>
    public void OnAndroidVideoPickerResult(string jsonResult)
    {
        try
        {
            var result = JsonUtility.FromJson<AndroidPickerResult>(jsonResult);
            var pickerResult = new FilePickerResult
            {
                Success = result.Success,
                SelectedPaths = result.Paths?.ToList() ?? new List<string>(),
                ErrorMessage = result.ErrorMessage
            };

            FileSelected?.Invoke(pickerResult);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to parse picker result: {jsonResult}", ex);
            FileSelected?.Invoke(new FilePickerResult
            {
                Success = false,
                ErrorMessage = ex.Message
            });
        }
    }

    [Serializable]
    private class AndroidPickerResult
    {
        public bool Success;
        public string[] Paths;
        public string ErrorMessage;
    }
}
#endif
```

---

### 5. 平台抽象层（Platform）

#### 5.1 平台服务接口

```csharp
/// <summary>
/// 平台服务接口 - 提供平台特定功能
/// </summary>
public interface IPlatformService
{
    string PlatformName { get; }
    string AppDataPath { get; }
    string CachePath { get; }
    void OpenUrl(string url);
    void ShareText(string text);
}

#if UNITY_ANDROID
public class AndroidPlatformService : IPlatformService
{
    public string PlatformName => "Android";
    public string AppDataPath => Application.persistentDataPath;
    public string CachePath => Application.temporaryCachePath;

    public void OpenUrl(string url)
    {
        using (var unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityClass.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            using (var uri = AndroidJavaObject.GetStatic<AndroidJavaObject>("android.net.Uri", "parse", url))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW", uri))
            {
                currentActivity.Call("startActivity", intent);
            }
        }
    }

    public void ShareText(string text)
    {
        // Android 分享实现
    }
}
#endif

#if UNITY_IOS
public class iOSPlatformService : IPlatformService
{
    public string PlatformName => "iOS";
    public string AppDataPath => Application.persistentDataPath;
    public string CachePath => Application.temporaryCachePath;

    public void OpenUrl(string url)
    {
        Application.OpenURL(url);
    }

    public void ShareText(string text)
    {
        // iOS 分享实现
    }
}
#endif
```

---

## 数据交互逻辑

### 1. 统一数据模型

```csharp
/// <summary>
/// 视频文件实体
/// </summary>
[Serializable]
public class VideoFile
{
    // 基本信息
    public string name;           // 文件名（不含扩展名）
    public string path;           // 唯一标识路径（远程URL或本地路径）
    public string url;            // 实际访问URL
    public string localPath;     // 本地缓存路径

    // 元数据
    public long size;             // 文件大小（字节）
    public long duration;         // 时长（秒）
    public int width;             // 视频宽度
    public int height;            // 视频高度
    public string codec;          // 编解码器

    // VR 特性
    public bool is360;            // 是否为360°视频
    public bool isStereo;         // 是否为立体视频
    public VrProjectionType projectionType; // 投影类型

    // 状态
    public bool isRemote;         // 是否为远程视频
    public bool isCached;         // 是否已缓存
    public DateTime? cacheDate;   // 缓存时间

    // 来源
    public VideoSourceType sourceType; // 来源类型
    public string sourceName;     // 来源名称

    // 附加信息
    public string thumbnail;      // 缩略图URL或路径
    public string metadata;       // 元数据JSON
}

public enum VideoSourceType
{
    Local,
    WebDAV,
    S3,
    AzureBlob,
    HTTP,
    Custom
}

public enum VrProjectionType
{
    Equirectangular,  // 等距柱状投影
    CubeMap,          // 立方体贴图
    Fisheye,          // 鱼眼投影
    Cylindrical       // 柱面投影
}
```

### 2. 数据流图

```
用户选择视频
    ↓
VideoBrowserUI (Presentation)
    ↓
PlaybackOrchestrator (Application)
    ↓
检查缓存 → ICacheManager.Check()
    ↓ (未缓存)
下载视频 → ICacheManager.StoreAsync()
    ↓ (播放)
IPlaybackService.Open() → Play()
    ↓
UnityVideoPlayer.Render() → VRVideoRenderer
    ↓
Texture Update → UI Update
```

### 3. 事件驱动架构

```csharp
// 事件定义
public class VideoSelectedEvent { }
public class PlaybackStartedEvent { }
public class PlaybackProgressEvent { }
public class PlaybackErrorEvent { }
public class LibraryUpdatedEvent { }
public class CacheProgressEvent { }

// 使用示例
public class VideoBrowserUI : MonoBehaviour
{
    private IEventBus eventBus;

    private void Start()
    {
        eventBus.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);
        eventBus.Subscribe<PlaybackErrorEvent>(OnPlaybackError);
    }

    private void OnPlaybackStarted(PlaybackStartedEvent e)
    {
        UpdateUI(e.VideoFile);
    }

    private void OnVideoClicked(VideoFile video)
    {
        eventBus.Publish(new VideoSelectedEvent { VideoFile = video });
    }
}
```

---

## 平台适配策略

### 1. Unity-Java 交互

**保持不变的交互方式**：
```csharp
// C# 调用 Java
using (var bridge = new AndroidJavaClass("com.vrplayer.saf.SafPickerBridge"))
{
    bridge.CallStatic("launchVideoPicker", activity, true, callbackName);
}

// Java 回调 C#
// Unity 自动调用 MonoBehaviour 上的回调方法
public void OnAndroidVideoPickerResult(string json)
{
    // 处理结果
}
```

### 2. 条件编译

```csharp
// 平台特定代码隔离
#if UNITY_ANDROID
    // Android 特定实现
    var permissionManager = new AndroidPermissionManager(logger);
#elif UNITY_IOS
    // iOS 特定实现
    var permissionManager = new iOSPermissionManager(logger);
#else
    // 其他平台
    var permissionManager = new DefaultPermissionManager(logger);
#endif
```

### 3. 平台抽象接口

```csharp
// 定义接口
public interface IPlatformBridge
{
    void RequestPermissions(string[] permissions, Action<bool> callback);
    void OpenFilePicker(Action<string[]> callback);
}

// 平台特定实现
#if UNITY_ANDROID
public class AndroidBridge : IPlatformBridge { }
#elif UNITY_IOS
public class iOSBridge : IPlatformBridge { }
#endif
```

---

## 迁移路线图

### 阶段 1：基础设施层重构（1-2周）

- [ ] 创建 Core 模块（EventBus, Logger, Config）
- [ ] 重构领域层接口
- [ ] 创建新的目录结构
- [ ] 编写单元测试框架

### 阶段 2：服务层优化（2-3周）

- [ ] 拆分 LocalFileManager
  - [ ] 提取 IFileScanner
  - [ ] 提取 ICacheManager
  - [ ] 提取 IPermissionManager
  - [ ] 提取 IStorageAccess
- [ ] 实现 PlaybackOrchestrator
- [ ] 实现 LibraryManager
- [ ] 重构 VideoBrowserUI

### 阶段 3：平台层增强（1-2周）

- [ ] 统一平台接口
- [ ] 实现 Android 平台服务
- [ ] 实现 iOS 平台服务（可选）
- [ ] 优化 Java 桥接

### 阶段 4：测试与优化（1周）

- [ ] 单元测试
- [ ] 集成测试
- [ ] 性能测试
- [ ] 文档更新

### 阶段 5：迁移与部署（持续）

- [ ] 逐步迁移现有功能
- [ ] 向后兼容性保证
- [ ] CI/CD 更新
- [ ] 用户测试

---

## 向后兼容性

### 1. 保留旧接口

```csharp
// 保留旧接口，标记为过时
[Obsolete("Use IPlaybackService instead")]
public class VRVideoPlayer : MonoBehaviour
{
    private IPlaybackService playbackService;

    // 旧方法
    public void PlayVideo(string path)
    {
        // 委托给新接口
        if (playbackService.Open(path))
        {
            playbackService.Play();
        }
    }
}
```

### 2. 适配器模式

```csharp
/// <summary>
/// 适配器 - 将旧接口适配到新架构
/// </summary>
public class LocalFileManagerAdapter : LocalFileManager
{
    private IFileScanner fileScanner;
    private ICacheManager cacheManager;

    public override List<VideoFile> GetLocalVideos()
    {
        // 使用新的 IFileScanner
        return fileScanner.ScanAsync(new ScanOptions()).Result.Videos;
    }
}
```

---

## 性能优化

### 1. 对象池

```csharp
/// <summary>
/// UI 对象池
/// </summary>
public class UIObjectPool : MonoBehaviour
{
    private Queue<GameObject> pool = new Queue<GameObject>();
    private GameObject prefab;

    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        return Instantiate(prefab, transform);
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

### 2. 异步操作

```csharp
/// <summary>
/// 异步文件扫描
/// </summary>
public async Task<ScanResult> ScanAsync(ScanOptions options)
{
    return await Task.Run(() =>
    {
        // 在后台线程执行
        return ScanInternal(options);
    });
}
```

### 3. 缓存优化

```csharp
/// <summary>
/// 内存缓存
/// </summary>
public class MemoryCache
{
    private Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();
    private int maxCacheSize = 100;

    public bool TryGet<T>(string key, out T value)
    {
        if (cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            value = (T)entry.Value;
            return true;
        }
        value = default;
        return false;
    }
}
```

---

## 总结

### 重构收益

✅ **代码质量提升**
- 职责清晰，易于维护
- 低耦合，高内聚
- 可测试性强

✅ **扩展性增强**
- 插件化架构
- 易于添加新功能
- 支持多平台

✅ **性能优化**
- 异步操作
- 对象池
- 智能缓存

✅ **开发效率**
- 统一的接口
- 完善的文档
- 丰富的工具

### 风险控制

⚠️ **向后兼容**
- 保留旧接口
- 渐进式迁移
- 充分测试

⚠️ **性能影响**
- 性能基准测试
- 优化关键路径
- 监控内存使用

⚠️ **平台差异**
- 统一接口设计
- 平台特定优化
- 充分测试

---

**文档版本**: 2.0.0
**最后更新**: 2026-03-24
**维护者**: VR Player Team
