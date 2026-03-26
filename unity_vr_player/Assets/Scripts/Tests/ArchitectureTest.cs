using UnityEngine;
using VRPlayer.Core.EventBus;
using VRPlayer.Core.Logging;
using VRPlayer.Application.Playback;
using VRPlayer.Application.Library;
using VRPlayer.Domain.Entities;
using VRPlayer.Core.Config;

namespace VRPlayer.Tests
{
    /// <summary>
    /// 新架构功能测试脚本
    /// 演示如何使用EventBus、Logger、Config、PlaybackOrchestrator和LibraryManager
    /// </summary>
    public class ArchitectureTest : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool testEventBus = true;
        [SerializeField] private bool testLogger = true;
        [SerializeField] private bool testConfig = true;
        [SerializeField] private bool testPlaybackOrchestrator = true;
        [SerializeField] private bool testLibraryManager = true;

        private PlaybackOrchestrator playbackOrchestrator;
        private LibraryManager libraryManager;
        private ILogger logger;

        private void Start()
        {
            if (!autoStart)
            {
                Debug.Log("[ArchitectureTest] 自动测试已禁用。手动调用 RunTests() 开始测试。");
                return;
            }

            RunTests();
        }

        public async void RunTests()
        {
            Debug.Log("========================================");
            Debug.Log("开始新架构功能测试");
            Debug.Log("========================================");

            // 测试1: Logger
            if (testLogger)
            {
                await TestLogger();
            }

            // 测试2: EventBus
            if (testEventBus)
            {
                await TestEventBus();
            }

            // 测试3: Config
            if (testConfig)
            {
                await TestConfig();
            }

            // 测试4: LibraryManager
            if (testLibraryManager)
            {
                await TestLibraryManager();
            }

            // 测试5: PlaybackOrchestrator
            if (testPlaybackOrchestrator)
            {
                await TestPlaybackOrchestrator();
            }

            Debug.Log("========================================");
            Debug.Log("所有测试完成！");
            Debug.Log("========================================");
        }

        private System.Threading.Tasks.Task TestLogger()
        {
            Debug.Log("\n[测试1] 测试 Logger 系统");
            Debug.Log("----------------------------------------");

            logger = LoggerManager.For("ArchitectureTest");

            // 测试不同级别的日志
            logger.Info("这是一条 Info 日志");
            logger.Debug("这是一条 Debug 日志");
            logger.Warning("这是一条 Warning 日志");
            
            // 测试带异常的日志
            try
            {
                throw new System.Exception("这是一个测试异常");
            }
            catch (System.Exception ex)
            {
                logger.Error("捕获到测试异常", ex);
            }

            // 测试结构化日志
            logger.Info("测试结构化日志", new
            {
                userId = "test_user_001",
                action = "test",
                timestamp = System.DateTime.Now.ToString()
            });

            Debug.Log("✅ Logger 测试完成");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private System.Threading.Tasks.Task TestEventBus()
        {
            Debug.Log("\n[测试2] 测试 EventBus 系统");
            Debug.Log("----------------------------------------");

            // 创建测试事件
            var testEvent = new LibraryScanStartedEvent
            {
                RequestId = "test_request_001",
                ScanPath = "/test/path"
            };

            // 订阅事件
            int callCount = 0;
            System.EventHandler<LibraryScanStartedEvent> handler = (sender, args) =>
            {
                callCount++;
                Debug.Log($"收到 LibraryScanStartedEvent: RequestId={args.RequestId}, CallCount={callCount}");
            };

            EventBus.Instance.Subscribe<LibraryScanStartedEvent>(handler);

            // 发布事件
            Debug.Log("发布第一个事件...");
            EventBus.Instance.Publish(testEvent);

            Debug.Log("发布第二个事件...");
            EventBus.Instance.Publish(testEvent);

            // 取消订阅
            EventBus.Instance.Unsubscribe<LibraryScanStartedEvent>(handler);

            // 再次发布（不应该收到）
            Debug.Log("取消订阅后发布事件（不应收到）...");
            EventBus.Instance.Publish(testEvent);

            Debug.Log($"✅ EventBus 测试完成 (收到 {callCount} 次事件)");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private System.Threading.Tasks.Task TestConfig()
        {
            Debug.Log("\n[测试3] 测试 Config 系统");
            Debug.Log("----------------------------------------");

            // 读取当前配置
            var playbackConfig = Config.Playback;
            Debug.Log($"当前准备超时: {playbackConfig.prepareTimeoutSeconds}s");
            Debug.Log($"当前最大视频大小: {playbackConfig.maxVideoSizeGB}GB");

            // 修改配置
            float originalTimeout = playbackConfig.prepareTimeoutSeconds;
            playbackConfig.prepareTimeoutSeconds = 60f;
            Config.SavePlaybackConfig(playbackConfig);

            Debug.Log($"已修改准备超时为: {playbackConfig.prepareTimeoutSeconds}s");

            // 重新读取验证
            var newConfig = Config.Playback;
            Debug.Log($"重新读取的准备超时: {newConfig.prepareTimeoutSeconds}s");

            // 恢复原值
            playbackConfig.prepareTimeoutSeconds = originalTimeout;
            Config.SavePlaybackConfig(playbackConfig);

            Debug.Log("已恢复原配置值");
            Debug.Log("✅ Config 测试完成");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private System.Threading.Tasks.Task TestLibraryManager()
        {
            Debug.Log("\n[测试4] 测试 LibraryManager");
            Debug.Log("----------------------------------------");

            // 查找 LibraryManager
            libraryManager = FindObjectOfType<LibraryManager>();
            if (libraryManager == null)
            {
                Debug.LogWarning("⚠️ 场景中没有 LibraryManager 组件，跳过此测试");
                return System.Threading.Tasks.Task.CompletedTask;
            }

            // 订阅库事件
            EventBus.Instance.Subscribe<LibraryScanStartedEvent>(OnLibraryScanStarted);
            EventBus.Instance.Subscribe<LibraryScanCompletedEvent>(OnLibraryScanCompleted);
            EventBus.Instance.Subscribe<VideoAddedEvent>(OnVideoAdded);

            // 测试获取视频库
            var videos = libraryManager.GetVideos();
            Debug.Log($"当前视频库中有 {videos.Count} 个视频");

            foreach (var video in videos.Take(5)) // 只显示前5个
            {
                Debug.Log($"  - {video.name} ({video.sizeFormatted})");
            }

            // 测试搜索
            var searchResults = libraryManager.SearchVideos("");
            Debug.Log($"搜索结果: {searchResults.Count} 个视频");

            // 测试筛选
            var filtered = libraryManager.FilterVideos(vf => vf.isVR360);
            Debug.Log($"VR360 视频: {filtered.Count} 个");

            Debug.Log("✅ LibraryManager 测试完成");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private System.Threading.Tasks.Task TestPlaybackOrchestrator()
        {
            Debug.Log("\n[测试5] 测试 PlaybackOrchestrator");
            Debug.Log("----------------------------------------");

            // 查找 PlaybackOrchestrator
            playbackOrchestrator = FindObjectOfType<PlaybackOrchestrator>();
            if (playbackOrchestrator == null)
            {
                Debug.LogWarning("⚠️ 场景中没有 PlaybackOrchestrator 组件，跳过此测试");
                return System.Threading.Tasks.Task.CompletedTask;
            }

            // 订阅播放事件
            EventBus.Instance.Subscribe<PlaybackStartedEvent>(OnPlaybackStarted);
            EventBus.Instance.Subscribe<PlaybackStateChangedEvent>(OnPlaybackStateChanged);
            EventBus.Instance.Subscribe<PlaybackErrorEvent>(OnPlaybackError);

            // 测试获取当前状态
            var state = playbackOrchestrator.GetCurrentState();
            Debug.Log($"当前播放状态: {state}");

            // 测试获取当前视频
            var currentVideo = playbackOrchestrator.GetCurrentVideo();
            if (currentVideo != null)
            {
                Debug.Log($"当前视频: {currentVideo.name}");
            }
            else
            {
                Debug.Log("当前没有播放视频");
            }

            Debug.Log("✅ PlaybackOrchestrator 测试完成");
            return System.Threading.Tasks.Task.CompletedTask;
        }

        // 事件处理器
        private void OnLibraryScanStarted(object sender, LibraryScanStartedEvent e)
        {
            Debug.Log($"[事件] 库扫描开始: {e.ScanPath}");
        }

        private void OnLibraryScanCompleted(object sender, LibraryScanCompletedEvent e)
        {
            Debug.Log($"[事件] 库扫描完成: 发现 {e.NewVideosCount} 个新视频, 耗时 {e.Duration.TotalSeconds:F2}s");
        }

        private void OnVideoAdded(object sender, VideoAddedEvent e)
        {
            Debug.Log($"[事件] 视频已添加: {e.VideoFile.name}");
        }

        private void OnPlaybackStarted(object sender, PlaybackStartedEvent e)
        {
            Debug.Log($"[事件] 播放开始: {e.VideoFile.name}");
        }

        private void OnPlaybackStateChanged(object sender, PlaybackStateChangedEvent e)
        {
            Debug.Log($"[事件] 播放状态改变: {e.OldState} → {e.NewState}");
        }

        private void OnPlaybackError(object sender, PlaybackErrorEvent e)
        {
            Debug.LogError($"[事件] 播放错误: {e.ErrorMessage}");
        }

        private void OnDestroy()
        {
            // 清理订阅
            EventBus.Instance.UnsubscribeAll(this);
        }
    }
}
