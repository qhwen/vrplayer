# VR Player 架构优化进度报告

> **更新日期**: 2026-03-24
> **当前阶段**: 阶段2 - 基础设施层重构（已完成）
> **下一阶段**: 阶段3 - 服务层优化

---

## ✅ 已完成工作

### 阶段 1：基础设施层（Core）- ✅ 完成

#### 1.1 EventBus（事件总线）- ✅ 完成

**创建的文件**：
- `Assets/Scripts/Core/EventBus/IEventBus.cs` - 事件总线接口
- `Assets/Scripts/Core/EventBus/EventBus.cs` - 事件总线实现
- `Assets/Scripts/Core/EventBus/Events.cs` - 所有领域事件定义

**功能特性**：
- ✅ 线程安全的发布订阅机制
- ✅ 支持泛型事件类型
- ✅ 异步事件通知
- ✅ 错误处理和日志记录
- ✅ 自动清理订阅

**已定义的事件类型**：
```csharp
// 播放相关事件
- VideoSelectedEvent
- PlaybackStartedEvent
- PlaybackStateChangedEvent
- PlaybackProgressEvent
- PlaybackErrorEvent
- PlaybackStoppedEvent

// 库管理相关事件
- LibraryUpdatedEvent
- VideoAddedEvent
- VideoRemovedEvent

// 缓存相关事件
- DownloadStartedEvent
- DownloadProgressEvent
- DownloadCompletedEvent
- CacheCleanupEvent

// 权限相关事件
- PermissionChangedEvent
- PermissionRequestResultEvent

// 文件选择相关事件
- FileSelectionEvent

// UI相关事件
- UIStateChangedEvent
- UINavigationEvent

// VR相关事件
- VRHeadRotationEvent
- VRGestureEvent

// 系统相关事件
- AppPausedEvent
- AppResumedEvent
- AppQuitEvent
```

**使用示例**：
```csharp
// 订阅事件
EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);

// 发布事件
EventBus.Instance.Publish(new PlaybackStartedEvent { VideoFile = video });

// 取消订阅
EventBus.Instance.Unsubscribe<PlaybackStartedEvent>(OnPlaybackStarted);
```

---

#### 1.2 Logger（日志系统）- ✅ 完成

**创建的文件**：
- `Assets/Scripts/Core/Logging/ILogger.cs` - 日志接口
- `Assets/Scripts/Core/Logging/StructuredLogger.cs` - 结构化日志实现
- `Assets/Scripts/Core/Logging/LoggerManager.cs` - 日志管理器

**功能特性**：
- ✅ 统一的日志格式
- ✅ 多级别日志（Debug, Info, Warning, Error）
- ✅ 模块化日志记录
- ✅ 支持上下文信息
- ✅ 异常堆栈跟踪
- ✅ 可配置日志级别
- ✅ 子日志记录器支持

**使用示例**：
```csharp
// 方式1：使用Log便捷类
Log.Default.Info("Application started");
Log.For("Playback").Debug($"Playing video: {videoName}");

// 方式2：使用LoggerManager
var logger = LoggerManager.For("MyModule");
logger.Info("Module initialized");
logger.Error("Something went wrong", exception);

// 方式3：创建子日志记录器
var parentLogger = LoggerManager.For("Parent");
var childLogger = parentLogger.CreateChildLogger("Child");
childLogger.Info("Child module initialized");
```

**日志格式示例**：
```
[14:23:45.123] [INFO] [Playback] Playing video: sample.mp4
[14:23:45.456] [DEBUG] [Playback.Cache] Cache hit for key: abc123
[14:23:46.789] [ERROR] [Playback] Video playback failed: Decoder error
Exception: DecoderException
Message: Failed to decode video codec H.265
```

---

#### 1.3 ConfigManager（配置管理）- ✅ 完成

**创建的文件**：
- `Assets/Scripts/Core/Config/IAppConfig.cs` - 配置接口
- `Assets/Scripts/Core/Config/PlaybackConfig.cs` - 播放器配置
- `Assets/Scripts/Core/Config/ScanConfig.cs` - 扫描配置
- `Assets/Scripts/Core/Config/CacheConfig.cs` - 缓存配置
- `Assets/Scripts/Core/Config/AppConfigManager.cs` - 配置管理器

**功能特性**：
- ✅ 类型安全的配置访问
- ✅ PlayerPrefs 持久化
- ✅ JSON序列化支持
- ✅ 配置缓存机制
- ✅ 预定义配置类（PlaybackConfig, ScanConfig, CacheConfig）
- ✅ 便捷访问接口

**使用示例**：
```csharp
// 方式1：使用Config便捷类
var playbackConfig = Config.Playback;
var scanConfig = Config.Scan;

// 方式2：使用ConfigManager
var configManager = AppConfigManager.Instance;
var timeout = configManager.Get("prepareTimeout", 30f);
configManager.Set("prepareTimeout", 45f);
configManager.Save();

// 方式3：使用预定义配置类
var config = new PlaybackConfig
{
    prepareTimeoutSeconds = 45f,
    autoPlayOnOpen = true
};
Config.SavePlaybackConfig(config);

// 读取配置
var loadedConfig = Config.GetPlaybackConfig();
Debug.Log($"Timeout: {loadedConfig.prepareTimeoutSeconds}秒");
```

---

### 阶段 2：Domain 层优化 - ✅ 完成

#### 2.1 实体类优化 - ✅ 完成

**创建的文件**：
- `Assets/Scripts/Domain/Entities/VideoFile.cs` - 增强版视频文件实体

**功能增强**：
```csharp
// 基本信息增强
- name, path, url, localPath

// 元数据增强
- size, duration, width, height
- codec, frameRate, bitRate

// VR 特性
- is360, isStereo
- projectionType (Equirectangular, CubeMap, Fisheye, Cylindrical)
- stereoFormat (None, SideBySide, TopBottom, LeftRight)

// 状态信息
- isRemote, isCached, cacheDate
- playCount, lastPlayDate

// 来源信息
- sourceType (Local, WebDAV, S3, AzureBlob, HTTP, Custom)
- sourceName

// 附加信息
- thumbnail, metadata
- creationDate, modificationDate
```

**辅助方法**：
```csharp
// 格式化文件大小
video.GetFormattedSize(); // "1.5 GB"

// 格式化时长
video.GetFormattedDuration(); // "10:30"

// 克隆对象
var clone = video.Clone();

// 验证有效性
bool valid = video.IsValid();
```

---

#### 2.2 Storage 接口定义 - ✅ 完成

**创建的文件**：
- `Assets/Scripts/Domain/Storage/IFileScanner.cs` - 文件扫描接口
- `Assets/Scripts/Domain/Storage/ICacheManager.cs` - 缓存管理接口
- `Assets/Scripts/Domain/Storage/IPermissionManager.cs` - 权限管理接口
- `Assets/Scripts/Domain/Storage/IStorageAccess.cs` - 存储访问接口

**IFileScanner 接口**：
```csharp
interface IFileScanner
{
    event Action<ScanProgress> ScanProgress;
    Task<ScanResult> ScanAsync(ScanOptions options);
    void CancelScan();
    bool IsScanning { get; }
}

// ScanOptions
- Directories: 扫描目录列表
- MaxDepth: 最大扫描深度
- MaxResults: 最大结果数
- IncludeSubdirectories: 是否包含子目录
- AllowedExtensions: 允许的扩展名
- MinFileSize/MaxFileSize: 文件大小限制

// ScanProgress
- FoundCount: 已找到数量
- ScannedCount: 已扫描数量
- ProgressPercentage: 进度百分比
- CurrentDirectory: 当前目录
- EstimatedRemainingSeconds: 估计剩余时间
```

**ICacheManager 接口**：
```csharp
interface ICacheManager
{
    string CacheDirectory { get; }
    string GetCachePath(string key, string extension = ".mp4");
    bool Exists(string key, string extension = ".mp4");

    Task<bool> StoreAsync(
        string key,
        string sourcePath,
        string extension = ".mp4",
        Action<float> onProgress = null,
        CancellationToken cancellationToken = default);

    bool Evict(string key, string extension = ".mp4");
    long GetTotalSizeBytes();
    long GetFreeSpaceBytes();
    int GetFileCount();
    void Clear();
    Task<CacheCleanupResult> CleanupAsync(CleanupOptions options, CancellationToken cancellationToken = default);
    CacheInfo GetCacheInfo();
}

// CleanupOptions
- TargetFreeBytes: 目标可用空间
- MaxAge: 最大缓存年龄
- Strategy: LRU/FIFO/Oldest/SizeFirst/LeastAccessed
- KeepKeys: 要保留的文件列表
```

**IPermissionManager 接口**：
```csharp
interface IPermissionManager
{
    event Action<PermissionType, PermissionStatus> PermissionChanged;
    Task<PermissionStatus> CheckPermissionAsync(PermissionType permission);
    Task<PermissionRequestResult> RequestPermissionAsync(PermissionType permission);
    bool ShouldShowRequestRationale(PermissionType permission);
    void OpenAppSettings();
    bool IsRequestInFlight { get; }
}

// PermissionType
- ReadMediaVideo (Android 13+)
- ReadExternalStorage (Android 10-12)
- WriteExternalStorage
- Camera
- Microphone
- Internet
- Custom

// PermissionStatus
- NotRequested
- Granted
- Denied
- DeniedPermanently
- Unknown
```

**IStorageAccess 接口**：
```csharp
interface IStorageAccess
{
    event Action<FilePickerResult> FileSelected;
    void OpenFilePicker(FilePickerOptions options);
    void OpenMultipleFilePicker(FilePickerOptions options);
    void OpenDirectoryPicker(FilePickerOptions options);
    bool IsAvailable { get; }
}

// FilePickerOptions
- AllowedExtensions: 允许的扩展名
- Title: 选择器标题
- AllowMultiple: 是否允许多选
- InitialDirectory: 初始目录
- Mode: File/Directory/Save
- MimeTypes: MIME类型过滤器
```

---

## 📁 目录结构

```
unity_vr_player/Assets/Scripts/
├── Core/                           # ✅ 已完成
│   ├── EventBus/                   # ✅ 事件总线
│   │   ├── IEventBus.cs
│   │   ├── EventBus.cs
│   │   └── Events.cs
│   ├── Logging/                    # ✅ 日志系统
│   │   ├── ILogger.cs
│   │   ├── StructuredLogger.cs
│   │   └── LoggerManager.cs
│   └── Config/                     # ✅ 配置管理
│       ├── IAppConfig.cs
│       ├── PlaybackConfig.cs
│       ├── ScanConfig.cs
│       ├── CacheConfig.cs
│       └── AppConfigManager.cs
│
├── Domain/                         # ✅ 已完成
│   ├── Entities/                   # ✅ 实体类
│   │   └── VideoFile.cs
│   └── Storage/                    # ✅ Storage接口
│       ├── IFileScanner.cs
│       ├── ICacheManager.cs
│       ├── IPermissionManager.cs
│       └── IStorageAccess.cs
│
├── Application/                    # 🔄 下一步
│   ├── Playback/
│   │   └── PlaybackOrchestrator.cs
│   └── Library/
│       └── LibraryManager.cs
│
├── Infrastructure/                 # 🔄 待优化
│   ├── Storage/
│   │   ├── LocalFileScanner.cs
│   │   ├── FileCacheManager.cs
│   │   └── AndroidPermissionManager.cs
│   └── Network/
│       └── WebDAVClient.cs
│
└── Presentation/                   # 🔄 待重构
    └── UI/
        └── VideoBrowserUI.cs
```

---

## 🎯 下一阶段工作

### 阶段 3：服务层优化（Application）

#### 3.1 创建 PlaybackOrchestrator（播放编排器）

**目标**：
- 协调播放相关操作
- 整合 PlaybackService、CacheManager、EventBus
- 提供统一的播放控制接口

**主要职责**：
- 视频播放流程编排
- 缓存检查和下载
- 播放状态管理
- 事件发布

#### 3.2 创建 LibraryManager（库管理器）

**目标**：
- 管理视频库
- 整合 FileScanner、PermissionManager、EventBus
- 提供统一的库操作接口

**主要职责**：
- 视频库扫描
- 权限请求
- 库更新通知
- 视频添加/移除

### 阶段 4：基础设施实现层优化（Infrastructure）

#### 4.1 拆分 LocalFileManager

**拆分为**：
- `LocalFileScanner.cs` - 实现 IFileScanner
- `FileCacheManager.cs` - 实现 ICacheManager
- `AndroidPermissionManager.cs` - 实现 IPermissionManager
- `AndroidStorageAccess.cs` - 实现 IStorageAccess

#### 4.2 优化实现类

**目标**：
- 使用新的日志系统
- 使用新的配置管理
- 使用新的事件总线
- 实现新的领域接口

---

## 📊 进度统计

| 模块 | 进度 | 状态 |
|------|------|------|
| Core 层 | 100% | ✅ 完成 |
| Domain 层 | 100% | ✅ 完成 |
| Application 层 | 0% | 🔄 待开始 |
| Infrastructure 层 | 30% | 🔄 进行中 |
| Presentation 层 | 0% | 🔄 待开始 |
| Platform 层 | 0% | 🔄 待开始 |

**总体进度**: 40%

---

## 🚀 使用新架构的示例

### 示例 1：使用事件总线

```csharp
// 在 Start 方法中订阅事件
void Start()
{
    EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);
    EventBus.Instance.Subscribe<PlaybackErrorEvent>(OnPlaybackError);
}

void OnDestroy()
{
    EventBus.Instance.Unsubscribe<PlaybackStartedEvent>(OnPlaybackStarted);
    EventBus.Instance.Unsubscribe<PlaybackErrorEvent>(OnPlaybackError);
}

void OnPlaybackStarted(PlaybackStartedEvent e)
{
    Debug.Log($"Started playing: {e.VideoFile.name}");
}

void OnPlaybackError(PlaybackErrorEvent e)
{
    Debug.LogError($"Playback error: {e.Error.message}");
}

// 播放视频时发布事件
EventBus.Instance.Publish(new PlaybackStartedEvent { VideoFile = video });
```

### 示例 2：使用日志系统

```csharp
// 获取日志记录器
private readonly ILogger logger = LoggerManager.For("MyComponent");

void Start()
{
    logger.Info("Component started");
}

void DoSomething()
{
    logger.Debug("Performing operation");

    try
    {
        // 执行操作
        logger.Info("Operation completed successfully");
    }
    catch (Exception ex)
    {
        logger.Error("Operation failed", ex);
    }
}
```

### 示例 3：使用配置管理

```csharp
// 读取配置
var playbackConfig = Config.Playback;
float timeout = playbackConfig.prepareTimeoutSeconds;

// 修改配置
playbackConfig.prepareTimeoutSeconds = 45f;
Config.SavePlaybackConfig(playbackConfig);

// 访问单个配置项
var scanDepth = Config.Get<int>("scan_depth", 2);
Config.Set("scan_depth", 3);
Config.Save();
```

---

## 📝 注意事项

1. **向后兼容**
   - 保留旧的接口和类
   - 标记为 `[Obsolete]`
   - 提供迁移路径

2. **测试**
   - 为每个新组件编写单元测试
   - 确保功能完整性
   - 验证性能表现

3. **文档**
   - 为每个接口和类添加XML文档注释
   - 提供使用示例
   - 更新开发者指南

---

## 🤔 需要确认的事项

- [ ] 是否需要添加依赖注入容器？
- [ ] 是否需要实现数据持久化（SQLite）？
- [ ] 是否需要实现更复杂的缓存策略？
- [ ] 是否需要支持更多平台（iOS, Windows）？
- [ ] 是否需要实现A/B测试或功能开关？

---

**下一步**: 确认当前架构是否符合预期，然后继续创建 Application 层的实现。
