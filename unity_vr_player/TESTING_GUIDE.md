# 新架构测试指南

## 🧪 快速测试步骤

### 步骤 1：创建测试场景

1. 在 Unity 中打开项目
2. 创建新场景：`File > New Scene`
3. 保存场景为 `ArchitectureTest.unity`（放在 `Assets/Scenes/` 目录）

### 步骤 2：添加组件到场景

#### 2.1 添加 LibraryManager
1. 在 Hierarchy 中右键：`Create Empty`
2. 重命名为 `LibraryManager`
3. 添加组件：`Add Component > Library Manager`
4. 在 Inspector 中配置：
   - `Enable Auto Scan`: 取消勾选（避免自动扫描干扰测试）
   - `Scan On Startup`: 取消勾选

#### 2.2 添加 PlaybackOrchestrator
1. 在 Hierarchy 中右键：`Create Empty`
2. 重命名为 `PlaybackOrchestrator`
3. 添加组件：`Add Component > Playback Orchestrator`
4. 在 Inspector 中配置：
   - `Enable Auto Cache`: 勾选（测试缓存功能）
   - `Auto Prepare`: 勾选（测试自动准备功能）

#### 2.3 添加 ArchitectureTest
1. 在 Hierarchy 中右键：`Create Empty`
2. 重命名为 `ArchitectureTest`
3. 添加组件：`Add Component > Architecture Test`
4. 在 Inspector 中配置测试选项：
   - `Auto Start`: 勾选（自动运行测试）
   - `Test EventBus`: 勾选
   - `Test Logger`: 勾选
   - `Test Config`: 勾选
   - `Test PlaybackOrchestrator`: 勾选
   - `Test LibraryManager`: 勾选

#### 2.4 添加 Directional Light（可选）
1. `GameObject > Light > Directional Light`
2. 用于场景照明

### 步骤 3：运行测试

1. 点击 Play 按钮 ▶️
2. 查看 Console 窗口的输出
3. 你应该看到以下测试输出：

```
========================================
开始新架构功能测试
========================================

[测试1] 测试 Logger 系统
----------------------------------------
[ArchitectureTest] [Info] 这是一条 Info 日志
[ArchitectureTest] [Debug] 这是一条 Debug 日志
[ArchitectureTest] [Warning] 这是一条 Warning 日志
[ArchitectureTest] [Error] 捕获到测试异常
✅ Logger 测试完成

[测试2] 测试 EventBus 系统
----------------------------------------
收到 LibraryScanStartedEvent: RequestId=test_request_001, CallCount=1
收到 LibraryScanStartedEvent: RequestId=test_request_001, CallCount=2
✅ EventBus 测试完成 (收到 2 次事件)

[测试3] 测试 Config 系统
----------------------------------------
当前准备超时: 30s
当前最大视频大小: 5GB
已修改准备超时为: 60s
重新读取的准备超时: 60s
已恢复原配置值
✅ Config 测试完成

[测试4] 测试 LibraryManager
----------------------------------------
当前视频库中有 X 个视频
  - video1.mp4 (1.2 GB)
  - video2.mp4 (800 MB)
...
✅ LibraryManager 测试完成

[测试5] 测试 PlaybackOrchestrator
----------------------------------------
当前播放状态: Idle
当前没有播放视频
✅ PlaybackOrchestrator 测试完成

========================================
所有测试完成！
========================================
```

---

## 📊 测试说明

### 测试 1：Logger 系统
- 测试不同日志级别（Info、Debug、Warning、Error）
- 测试异常日志记录
- 测试结构化日志

**预期结果**：所有日志正确输出到 Console，格式统一

### 测试 2：EventBus 系统
- 测试事件发布和订阅
- 测试多次发布事件
- 测试取消订阅后不再收到事件

**预期结果**：收到 2 次事件，取消订阅后不再收到

### 测试 3：Config 系统
- 测试配置读取
- 测试配置修改
- 测试配置持久化

**预期结果**：配置成功修改并保存，重新读取正确

### 测试 4：LibraryManager
- 测试获取视频库
- 测试搜索功能
- 测试筛选功能

**预期结果**：正确显示视频库信息

### 测试 5：PlaybackOrchestrator
- 测试获取播放状态
- 测试获取当前视频
- 测试事件订阅

**预期结果**：正确显示当前播放状态

---

## 🎯 手动测试场景

### 场景 1：播放视频流程

```csharp
// 在 Inspector 中取消勾选 Auto Start
// 在 Start 方法中手动调用：

public void ManualPlaybackTest()
{
    // 1. 刷新视频库
    var libraryManager = FindObjectOfType<LibraryManager>();
    libraryManager.RefreshLibraryAsync();
    
    // 2. 等待库更新完成（监听 LibraryScanCompletedEvent）
    
    // 3. 获取第一个视频
    var videos = libraryManager.GetVideos();
    if (videos.Count > 0)
    {
        var firstVideo = videos[0];
        
        // 4. 播放视频
        var orchestrator = FindObjectOfType<PlaybackOrchestrator>();
        orchestrator.PlayVideoAsync(firstVideo);
    }
}
```

### 场景 2：文件选择和播放

```csharp
public void FilePickerTest()
{
    // 1. 打开文件选择器
    var libraryManager = FindObjectOfType<LibraryManager>();
    libraryManager.OpenFilePicker();
    
    // 2. 等待用户选择（监听 VideoAddedEvent）
    
    // 3. 播放选择的视频
    // 通过 VideoAddedEvent 获取新添加的视频
}
```

### 场景 3：错误处理测试

```csharp
public void ErrorHandlingTest()
{
    // 1. 尝试播放不存在的视频
    var invalidVideo = new VideoFile
    {
        name = "nonexistent.mp4",
        localPath = "/invalid/path/nonexistent.mp4"
    };
    
    var orchestrator = FindObjectOfType<PlaybackOrchestrator>();
    orchestrator.PlayVideoAsync(invalidVideo);
    
    // 2. 监听 PlaybackErrorEvent
    // 预期：应该收到错误事件
}
```

---

## 🔍 调试技巧

### 1. 查看日志输出

所有日志都会输出到 Unity Console：
- Logger 日志格式：`[ModuleName] [Level] Message`
- 事件日志格式：`[事件] EventName: Details`

### 2. 监控事件流

在 `ArchitectureTest.cs` 中添加更多事件订阅：

```csharp
private void SubscribeToAllEvents()
{
    // 订阅所有库相关事件
    EventBus.Instance.Subscribe<LibraryScanStartedEvent>(OnEvent);
    EventBus.Instance.Subscribe<LibraryScanCompletedEvent>(OnEvent);
    EventBus.Instance.Subscribe<VideoAddedEvent>(OnEvent);
    EventBus.Instance.Subscribe<VideoRemovedEvent>(OnEvent);
    
    // 订阅所有播放相关事件
    EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnEvent);
    EventBus.Instance.Subscribe<PlaybackStoppedEvent>(OnEvent);
    EventBus.Instance.Subscribe<PlaybackPausedEvent>(OnEvent);
    EventBus.Instance.Subscribe<PlaybackResumedEvent>(OnEvent);
    EventBus.Instance.Subscribe<PlaybackStateChangedEvent>(OnEvent);
    EventBus.Instance.Subscribe<PlaybackErrorEvent>(OnEvent);
    
    // 订阅缓存事件
    EventBus.Instance.Subscribe<CacheHitEvent>(OnEvent);
    EventBus.Instance.Subscribe<CacheMissEvent>(OnEvent);
    EventBus.Instance.Subscribe<CacheDownloadStartedEvent>(OnEvent);
    EventBus.Instance.Subscribe<CacheDownloadCompletedEvent>(OnEvent);
    EventBus.Instance.Subscribe<CacheDownloadFailedEvent>(OnEvent);
}

private void OnEvent(object sender, object e)
{
    Debug.Log($"[事件] {e.GetType().Name}");
}
```

### 3. 检查配置持久化

```csharp
public void CheckConfigPersistence()
{
    // 修改配置
    var config = Config.Playback;
    config.prepareTimeoutSeconds = 123.45f;
    Config.SavePlaybackConfig(config);
    
    // 重新打开应用
    // 重新运行测试
    
    // 验证配置是否持久化
    var reloaded = Config.Playback;
    Debug.Log($"持久化的配置值: {reloaded.prepareTimeoutSeconds}");
    // 应该输出: 123.45
}
```

---

## ✅ 验证检查清单

测试完成后，确认以下功能都正常：

- [ ] Logger 能正确输出不同级别的日志
- [ ] EventBus 能正确发布和订阅事件
- [ ] EventBus 的取消订阅功能正常
- [ ] Config 能正确读取和修改配置
- [ ] Config 的持久化功能正常
- [ ] LibraryManager 能正确获取视频库
- [ ] LibraryManager 的搜索功能正常
- [ ] LibraryManager 的筛选功能正常
- [ ] PlaybackOrchestrator 能正确获取播放状态
- [ ] 所有事件都正确发布和接收

---

## 🐛 常见问题

### 问题 1：找不到 LibraryManager 或 PlaybackOrchestrator

**原因**：场景中没有添加对应组件

**解决**：按照"步骤 2"添加组件到场景

### 问题 2：测试没有自动运行

**原因**：Auto Start 未勾选

**解决**：在 Inspector 中勾选 Auto Start，或手动调用 `RunTests()`

### 问题 3：配置没有持久化

**原因**：PlayerPrefs 在某些平台可能不支持

**解决**：在 Unity Editor 中测试应该能正常持久化

### 问题 4：事件没有收到

**原因**：订阅时序问题或事件发布时机问题

**解决**：确保在事件发布前已经完成订阅，或在 Start 方法中订阅

---

## 📈 性能测试

### 测试 EventBus 性能

```csharp
public async Task TestEventBusPerformance()
{
    const int eventCount = 10000;
    var receivedCount = 0;
    var sw = new System.Diagnostics.Stopwatch();

    EventBus.Instance.Subscribe<LibraryScanStartedEvent>((s, e) =>
    {
        receivedCount++;
    });

    sw.Start();
    for (int i = 0; i < eventCount; i++)
    {
        EventBus.Instance.Publish(new LibraryScanStartedEvent
        {
            RequestId = $"test_{i}",
            ScanPath = "/test/path"
        });
    }
    sw.Stop();

    Debug.Log($"发布 {eventCount} 个事件耗时: {sw.ElapsedMilliseconds}ms");
    Debug.Log($"收到 {receivedCount} 个事件");
    Debug.Log($"平均每个事件: {(sw.ElapsedMilliseconds / (float)eventCount):F4}ms");
}
```

**预期结果**：10000 个事件应该在 100-500ms 内完成

---

## 🎓 下一步

测试完成后，你可以：

1. **阅读完整文档**：
   - `ARCHITECTURE_REFACTOR_V2.md` - 完整架构设计
   - `NEW_ARCHITECTURE_USAGE_GUIDE.md` - 使用指南
   - `REFACTOR_FINAL_SUMMARY.md` - 总结报告

2. **集成到现有代码**：
   - 替换旧的 LocalFileManager 使用
   - 使用新的事件系统重构 VideoBrowserUI
   - 使用 PlaybackOrchestrator 重构 VRVideoPlayer

3. **继续优化**：
   - 实现 Infrastructure 层的接口
   - 添加单元测试
   - 性能优化

---

祝你测试顺利！🚀
