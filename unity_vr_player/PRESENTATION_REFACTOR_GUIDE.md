# Presentation 层重构指南

## 📋 概述

本文档说明如何将现有代码从旧架构迁移到新架构。

---

## 🔄 VideoBrowserUI 迁移指南

### 重构对比

#### 旧架构（VideoBrowserUI.cs）

```csharp
public class VideoBrowserUI : MonoBehaviour
{
    private VRVideoPlayer vrVideoPlayer;
    private LocalFileManager localFileManager;
    private IPlaybackService playbackService;

    private void Start()
    {
        vrVideoPlayer = FindObjectOfType<VRVideoPlayer>();
        localFileManager = FindObjectOfType<LocalFileManager>();
        playbackService = vrVideoPlayer.GetPlaybackService();

        // 直接订阅事件
        localFileManager.LocalVideoLibraryChanged += OnLocalVideoLibraryChanged;
        playbackService.StateChanged += OnPlaybackStateChanged;
        playbackService.PlaybackUpdated += OnPlaybackUpdated;
    }
}
```

**问题**:
- ❌ 紧耦合：直接依赖具体实现
- ❌ 难以测试：无法 Mock 依赖
- ❌ 事件订阅复杂：需要管理多个事件源
- ❌ 职责不清晰：混合了 UI 创建和业务逻辑

#### 新架构（VideoBrowserUI_Refactored.cs）

```csharp
public class VideoBrowserUI : MonoBehaviour
{
    [SerializeField] private PlaybackOrchestrator playbackOrchestrator;
    [SerializeField] private LibraryManager libraryManager;
    private ILogger logger;

    private void Start()
    {
        logger = LoggerManager.For("VideoBrowserUI");

        // 依赖注入
        if (playbackOrchestrator == null)
        {
            playbackOrchestrator = FindObjectOfType<PlaybackOrchestrator>();
        }

        if (libraryManager == null)
        {
            libraryManager = FindObjectOfType<LibraryManager>();
        }

        // 订阅事件总线
        SubscribeToEventBus();
    }

    private void SubscribeToEventBus()
    {
        EventBus.Instance.Subscribe<LibraryScanStartedEvent>(OnLibraryScanStarted);
        EventBus.Instance.Subscribe<LibraryScanCompletedEvent>(OnLibraryScanCompleted);
        EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);
        EventBus.Instance.Subscribe<PlaybackStoppedEvent>(OnPlaybackStopped);
    }
}
```

**优势**:
- ✅ 松耦合：使用接口和依赖注入
- ✅ 易于测试：可以 Mock 依赖
- ✅ 统一事件：通过 EventBus 管理所有事件
- ✅ 职责清晰：UI 只负责显示和用户交互

---

## 📝 迁移步骤

### 步骤 1: 备份现有文件

```bash
# 重命名旧文件
mv Assets/Scripts/VideoBrowserUI.cs Assets/Scripts/VideoBrowserUI_Legacy.cs
```

### 步骤 2: 更新场景配置

#### 在场景中添加必需的组件

```
场景结构：
├── LocalFileScanner (GameObject)
│   └── LocalFileScanner Component
│       - Max Collected Videos: 200
│       - Scan Depth: 0
│       - Include Common Media Folders: ✓
│
├── FileCacheManager (GameObject)
│   └── FileCacheManager Component
│
├── AndroidPermissionManager (GameObject)
│   └── AndroidPermissionManager Component
│
├── AndroidStorageAccess (GameObject)
│   └── AndroidStorageAccess Component
│
├── LibraryManager (GameObject)
│   └── LibraryManager Component
│       - File Scanner: [拖入 LocalFileScanner]
│       - Cache Manager: [拖入 FileCacheManager]
│       - Permission Manager: [拖入 AndroidPermissionManager]
│       - Storage Access: [拖入 AndroidStorageAccess]
│       - Enable Auto Scan: ✗
│       - Scan On Startup: ✗
│
├── PlaybackOrchestrator (GameObject)
│   └── PlaybackOrchestrator Component
│       - Video Player: [拖入 VRVideoPlayer]
│       - Cache Manager: [拖入 FileCacheManager]
│       - Enable Auto Cache: ✓
│       - Auto Prepare: ✓
│
└── VideoBrowserUI (GameObject)
    └── VideoBrowserUI Refactored Component
        - Playback Orchestrator: [拖入 PlaybackOrchestrator]
        - Library Manager: [拖入 LibraryManager]
        - Auto Initialize: ✓
```

### 步骤 3: 更新代码引用

#### 旧代码（需要修改的地方）

```csharp
// 1. 获取本地视频
var videos = localFileManager.GetLocalVideos();

// 2. 刷新视频库
localFileManager.RefreshLocalVideos();

// 3. 打开文件选择器
localFileManager.OpenFilePicker();

// 4. 请求权限
localFileManager.RequestReadableMediaPermission();

// 5. 播放视频
playbackService.Open(path);
playbackService.Play();

// 6. 暂停/恢复
playbackService.Pause();
playbackService.Play();

// 7. 停止播放
playbackService.Stop();
```

#### 新代码（修改后的版本）

```csharp
// 1. 获取本地视频
var videos = libraryManager.GetVideos();

// 2. 刷新视频库
await libraryManager.RefreshLibraryAsync();

// 3. 打开文件选择器
libraryManager.OpenFilePicker();

// 4. 请求权限
var permissionManager = FindObjectOfType<AndroidPermissionManager>();
permissionManager.RequestReadableMediaPermission();

// 5. 播放视频
await playbackOrchestrator.PlayVideoAsync(video);

// 6. 暂停/恢复
playbackOrchestrator.PausePlayback();
playbackOrchestrator.ResumePlayback();

// 7. 停止播放
playbackOrchestrator.StopPlayback();
```

### 步骤 4: 更新事件处理

#### 旧事件处理

```csharp
private void Start()
{
    localFileManager.LocalVideoLibraryChanged += OnLocalVideoLibraryChanged;
    playbackService.StateChanged += OnPlaybackStateChanged;
    playbackService.PlaybackUpdated += OnPlaybackUpdated;
    playbackService.ErrorOccurred += OnPlaybackError;
}

private void OnDestroy()
{
    localFileManager.LocalVideoLibraryChanged -= OnLocalVideoLibraryChanged;
    playbackService.StateChanged -= OnPlaybackStateChanged;
    playbackService.PlaybackUpdated -= OnPlaybackUpdated;
    playbackService.ErrorOccurred -= OnPlaybackError;
}
```

#### 新事件处理

```csharp
private void SubscribeToEventBus()
{
    // 库事件
    EventBus.Instance.Subscribe<LibraryScanStartedEvent>(OnLibraryScanStarted);
    EventBus.Instance.Subscribe<LibraryScanCompletedEvent>(OnLibraryScanCompleted);
    EventBus.Instance.Subscribe<VideoAddedEvent>(OnVideoAdded);
    EventBus.Instance.Subscribe<VideoRemovedEvent>(OnVideoRemoved);

    // 播放事件
    EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);
    EventBus.Instance.Subscribe<PlaybackStoppedEvent>(OnPlaybackStopped);
    EventBus.Instance.Subscribe<PlaybackPausedEvent>(OnPlaybackPaused);
    EventBus.Instance.Subscribe<PlaybackResumedEvent>(OnPlaybackResumed);
    EventBus.Instance.Subscribe<PlaybackErrorEvent>(OnPlaybackError);
}

private void UnsubscribeFromEventBus()
{
    // 库事件
    EventBus.Instance.Unsubscribe<LibraryScanStartedEvent>(OnLibraryScanStarted);
    EventBus.Instance.Unsubscribe<LibraryScanCompletedEvent>(OnLibraryScanCompleted);
    EventBus.Instance.Unsubscribe<VideoAddedEvent>(OnVideoAdded);
    EventBus.Instance.Unsubscribe<VideoRemovedEvent>(OnVideoRemoved);

    // 播放事件
    EventBus.Instance.Unsubscribe<PlaybackStartedEvent>(OnPlaybackStarted);
    EventBus.Instance.Unsubscribe<PlaybackStoppedEvent>(OnPlaybackStopped);
    EventBus.Instance.Unsubscribe<PlaybackPausedEvent>(OnPlaybackPaused);
    EventBus.Instance.Unsubscribe<PlaybackResumedEvent>(OnPlaybackResumed);
    EventBus.Instance.Unsubscribe<PlaybackErrorEvent>(OnPlaybackError);
}
```

### 步骤 5: 更新日志记录

#### 旧代码

```csharp
Debug.Log("Video library refreshed");
Debug.LogWarning("Permission denied");
Debug.LogError("Playback failed: " + error);
```

#### 新代码

```csharp
logger.Info("Video library refreshed");
logger.Warning("Permission denied");
logger.Error("Playback failed: " + error, exception);
```

---

## 🎯 关键变化总结

### 1. 依赖管理

| 旧方式 | 新方式 |
|--------|--------|
| `FindObjectOfType<LocalFileManager>()` | `FindObjectOfType<LibraryManager>()` |
| `vrVideoPlayer.GetPlaybackService()` | `FindObjectOfType<PlaybackOrchestrator>()` |
| 直接访问 | 依赖注入 |

### 2. 事件系统

| 旧方式 | 新方式 |
|--------|--------|
| `localFileManager.LocalVideoLibraryChanged += ...` | `EventBus.Instance.Subscribe<LibraryScanCompletedEvent>(...)` |
| `playbackService.StateChanged += ...` | `EventBus.Instance.Subscribe<PlaybackStartedEvent>(...)` |
| 手动管理订阅 | 统一通过 EventBus |

### 3. 播放控制

| 旧方式 | 新方式 |
|--------|--------|
| `playbackService.Open(path)` | `await playbackOrchestrator.PlayVideoAsync(video)` |
| `playbackService.Play()` | `playbackOrchestrator.ResumePlayback()` |
| `playbackService.Pause()` | `playbackOrchestrator.PausePlayback()` |
| `playbackService.Stop()` | `playbackOrchestrator.StopPlayback()` |

### 4. 日志系统

| 旧方式 | 新方式 |
|--------|--------|
| `Debug.Log(...)` | `logger.Info(...)` |
| `Debug.LogWarning(...)` | `logger.Warning(...)` |
| `Debug.LogError(...)` | `logger.Error(...)` |

---

## ✅ 迁移检查清单

### 前置条件

- [ ] 已创建所有必需的 GameObject 和组件
- [ ] 已配置依赖注入关系
- [ ] 已测试 LibraryManager 和 PlaybackOrchestrator

### 代码更新

- [ ] 已更新所有 `LocalFileManager` 引用为 `LibraryManager`
- [ ] 已更新所有 `IPlaybackService` 引用为 `PlaybackOrchestrator`
- [ ] 已更新所有事件订阅为 EventBus
- [ ] 已更新所有日志记录为 Logger

### 测试验证

- [ ] 视频列表正确显示
- [ ] 视频播放正常
- [ ] 暂停/恢复功能正常
- [ ] 停止播放功能正常
- [ ] 进度条拖动正常
- [ ] 刷新视频库正常
- [ ] 文件选择器正常
- [ ] 权限请求正常

---

## 🐛 常见问题

### Q1: "FindObjectOfType 返回 null"

**原因**: 场景中没有对应的 GameObject

**解决**: 确保在场景中添加了所有必需的组件

### Q2: 事件没有触发

**原因**: 没有正确订阅或取消订阅

**解决**:
1. 确保在 `Start()` 中订阅
2. 确保在 `OnDestroy()` 中取消订阅
3. 检查事件参数是否正确

### Q3: 播放失败

**原因**: PlaybackOrchestrator 没有正确配置 VRVideoPlayer

**解决**: 在 Inspector 中将 VRVideoPlayer 拖入 PlaybackOrchestrator 的 Video Player 字段

### Q4: 权限请求不工作

**原因**: AndroidPermissionManager 未添加到场景

**解决**: 在场景中添加 GameObject 并附加 AndroidPermissionManager 组件

---

## 🚀 最佳实践

### 1. 使用依赖注入

```csharp
// ✅ 推荐
[SerializeField] private PlaybackOrchestrator playbackOrchestrator;

// ❌ 不推荐
private PlaybackOrchestrator playbackOrchestrator;
private void Start() => playbackOrchestrator = FindObjectOfType<PlaybackOrchestrator>();
```

### 2. 使用事件总线

```csharp
// ✅ 推荐
EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);

// ❌ 不推荐
playbackService.StateChanged += OnPlaybackStateChanged;
```

### 3. 使用结构化日志

```csharp
// ✅ 推荐
logger.Info("Playback started", new { videoName = video.name, duration = video.duration });

// ❌ 不推荐
Debug.Log($"Playback started: {video.name}");
```

### 4. 使用 async/await

```csharp
// ✅ 推荐
await playbackOrchestrator.PlayVideoAsync(video);

// ❌ 不推荐
playbackService.Open(path);
playbackService.Play();
```

---

## 📚 相关文档

- `ARCHITECTURE_REFACTOR_V2.md` - 完整的架构设计文档
- `NEW_ARCHITECTURE_USAGE_GUIDE.md` - 新架构使用指南
- `FINAL_COMPLETION_REPORT.md` - 最终完成报告
- `TESTING_GUIDE.md` - 测试指南

---

## 🎓 下一步

迁移完成后，您可以：

1. **测试新架构**
   - 运行 `ArchitectureTest.cs`
   - 验证所有功能正常

2. **重构其他 UI 组件**
   - VRVideoPlayer
   - 其他自定义 UI

3. **编写单元测试**
   - 为 UI 组件编写测试
   - 为 Application 层编写测试

4. **性能优化**
   - 对比旧新架构的性能
   - 优化关键路径

---

**迁移完成后，您的代码将更加清晰、可维护、可测试！** 🎉
