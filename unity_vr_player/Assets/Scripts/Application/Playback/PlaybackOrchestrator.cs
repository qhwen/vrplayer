using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using VRPlayer.Core.Config;
using VRPlayer.Core.EventBus;
using VRPlayer.Core.Logging;
using VRPlayer.Domain.Entities;
using VRPlayer.Domain.Playback;
using VRPlayer.Domain.Storage;

namespace VRPlayer.Application.Playback
{
    /// <summary>
    /// 播放编排器 - 协调播放相关的所有操作
    /// 职责：
    /// 1. 视频播放流程编排
    /// 2. 缓存检查和下载
    /// 3. 播放状态管理
    /// 4. 事件发布
    /// </summary>
    public class PlaybackOrchestrator : MonoBehaviour
    {
        #region 私有字段

        private IPlaybackService playbackService;
        private ICacheManager cacheManager;
        private IEventBus eventBus;
        private ILogger logger;

        private VideoFile currentVideo;
        private CancellationTokenSource downloadCancellationToken;
        private bool isDownloading;

        private float lastEmittedPosition = -1f;
        private float lastEmittedDuration = -1f;

        #endregion

        #region 生命周期

        private void Awake()
        {
            logger = LoggerManager.For("PlaybackOrchestrator");
            logger.Info("PlaybackOrchestrator initialized");
        }

        private void Start()
        {
            // 从场景或依赖注入容器中获取服务
            InitializeServices();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CancelDownload();
            StopPlayback();
        }

        #endregion

        #region 初始化

        private void InitializeServices()
        {
            // 尝试从场景中获取服务
            playbackService = FindObjectOfType<IPlaybackService>();
            cacheManager = FindObjectOfType<ICacheManager>();

            // 如果没有找到，尝试创建默认实现
            if (playbackService == null)
            {
                logger.Warning("IPlaybackService not found in scene, attempting to find component");
                var playerComponent = GetComponent<UnityVideoPlaybackService>();
                if (playerComponent != null)
                {
                    playbackService = playerComponent;
                }
            }

            // 获取事件总线
            eventBus = EventBus.Instance;

            logger.Info($"Services initialized - PlaybackService: {playbackService != null}, CacheManager: {cacheManager != null}");
        }

        private void SubscribeToEvents()
        {
            if (playbackService != null)
            {
                playbackService.StateChanged += OnPlaybackStateChanged;
                playbackService.PlaybackUpdated += OnPlaybackUpdated;
                playbackService.ErrorOccurred += OnPlaybackError;
            }

            // 订阅视频选中事件
            eventBus.Subscribe<VideoSelectedEvent>(OnVideoSelected);
        }

        private void UnsubscribeFromEvents()
        {
            if (playbackService != null)
            {
                playbackService.StateChanged -= OnPlaybackStateChanged;
                playbackService.PlaybackUpdated -= OnPlaybackUpdated;
                playbackService.ErrorOccurred -= OnPlaybackError;
            }

            eventBus.Unsubscribe<VideoSelectedEvent>(OnVideoSelected);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 异步播放视频（包含缓存检查和下载）
        /// </summary>
        /// <param name="video">视频文件</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task PlayVideoAsync(VideoFile video, CancellationToken cancellationToken = default)
        {
            if (video == null)
            {
                logger.Error("Cannot play null video");
                return;
            }

            logger.Info($"Starting playback: {video.name}");

            currentVideo = video;

            try
            {
                // 步骤 1：检查是否需要下载
                string playPath = await PrepareVideoForPlaybackAsync(video, cancellationToken);

                if (string.IsNullOrEmpty(playPath))
                {
                    logger.Error("Failed to prepare video for playback");
                    eventBus.Publish(new PlaybackErrorEvent
                    {
                        Error = new PlaybackError
                        {
                            code = PlaybackErrorCode.FileNotFound,
                            message = "Failed to prepare video for playback",
                            source = video.path
                        },
                        VideoPath = video.path
                    });
                    return;
                }

                // 步骤 2：停止当前播放
                StopPlayback();

                // 步骤 3：开始新播放
                if (playbackService != null)
                {
                    // 更新视频的播放统计
                    UpdatePlaybackStats(video);

                    // 打开并播放
                    if (playbackService.Open(playPath))
                    {
                        playbackService.Play();

                        // 发布播放开始事件
                        eventBus.Publish(new PlaybackStartedEvent
                        {
                            VideoFile = video
                        });

                        logger.Info($"Playback started: {video.name}");
                    }
                    else
                    {
                        logger.Error($"Failed to open video: {playPath}");

                        // 发布播放错误事件
                        eventBus.Publish(new PlaybackErrorEvent
                        {
                            Error = playbackService.LastError,
                            VideoPath = playPath
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.Warning($"Playback cancelled for video: {video.name}");
            }
            catch (Exception ex)
            {
                logger.Error($"Error playing video: {video.name}", ex);

                // 发布播放错误事件
                eventBus.Publish(new PlaybackErrorEvent
                {
                    Error = new PlaybackError
                    {
                        code = PlaybackErrorCode.Unknown,
                        message = ex.Message,
                        source = video.path
                    },
                    VideoPath = video.path
                });
            }
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void PausePlayback()
        {
            if (playbackService == null || playbackService.State != PlaybackState.Playing)
            {
                return;
            }

            logger.Info("Pausing playback");
            playbackService.Pause();
        }

        /// <summary>
        /// 恢复播放
        /// </summary>
        public void ResumePlayback()
        {
            if (playbackService == null || playbackService.State != PlaybackState.Paused)
            {
                return;
            }

            logger.Info("Resuming playback");
            playbackService.Play();
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void StopPlayback()
        {
            if (playbackService == null)
            {
                return;
            }

            if (playbackService.State == PlaybackState.Idle)
            {
                return;
            }

            logger.Info("Stopping playback");
            playbackService.Stop();

            // 发布播放停止事件
            eventBus.Publish(new PlaybackStoppedEvent
            {
                VideoPath = currentVideo?.path,
                WasCompleted = false
            });
        }

        /// <summary>
        /// 跳转到指定时间
        /// </summary>
        /// <param name="seconds">秒数</param>
        public void SeekTo(float seconds)
        {
            if (playbackService == null || currentVideo == null)
            {
                return;
            }

            logger.Debug($"Seeking to {seconds} seconds");
            playbackService.Seek(seconds);
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="volume">音量 (0.0 - 1.0)</param>
        public void SetVolume(float volume)
        {
            if (playbackService == null)
            {
                return;
            }

            float clampedVolume = Mathf.Clamp01(volume);
            logger.Debug($"Setting volume to {clampedVolume}");
            playbackService.SetVolume(clampedVolume);
        }

        /// <summary>
        /// 取消当前下载
        /// </summary>
        public void CancelDownload()
        {
            if (downloadCancellationToken != null && !downloadCancellationToken.IsCancellationRequested)
            {
                logger.Info("Cancelling download");
                downloadCancellationToken.Cancel();
                downloadCancellationToken.Dispose();
                downloadCancellationToken = null;
                isDownloading = false;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 准备视频播放（检查缓存，必要时下载）
        /// </summary>
        private async Task<string> PrepareVideoForPlaybackAsync(VideoFile video, CancellationToken cancellationToken)
        {
            // 如果是本地文件，直接返回
            if (!video.isRemote)
            {
                logger.Debug("Video is local, no download needed");
                return video.path;
            }

            // 如果没有缓存管理器，直接使用远程URL
            if (cacheManager == null)
            {
                logger.Warning("Cache manager not available, playing directly from remote");
                return video.url;
            }

            // 检查是否已缓存
            string cacheKey = GenerateCacheKey(video);
            string cachedPath = cacheManager.GetCachePath(cacheKey, GetFileExtension(video.path));

            if (cacheManager.Exists(cacheKey, GetFileExtension(video.path)))
            {
                logger.Info($"Using cached version: {cachedPath}");
                video.isCached = true;
                video.cacheDate = DateTime.Now;
                video.localPath = cachedPath;
                return cachedPath;
            }

            // 需要下载
            logger.Info($"Video not cached, starting download from: {video.url}");

            // 取消之前的下载
            CancelDownload();

            // 创建新的取消令牌
            downloadCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            isDownloading = true;

            try
            {
                // 发布下载开始事件
                eventBus.Publish(new DownloadStartedEvent
                {
                    VideoFile = video,
                    DownloadPath = cachedPath
                });

                // 下载到缓存
                bool downloadSuccess = await cacheManager.StoreAsync(
                    cacheKey,
                    video.url,
                    GetFileExtension(video.path),
                    progress =>
                    {
                        // 发布下载进度事件
                        eventBus.Publish(new DownloadProgressEvent
                        {
                            VideoFile = video,
                            Progress = progress
                        });
                    },
                    downloadCancellationToken.Token
                );

                if (downloadSuccess)
                {
                    // 更新视频缓存状态
                    video.isCached = true;
                    video.cacheDate = DateTime.Now;
                    video.localPath = cachedPath;

                    // 发布下载完成事件
                    eventBus.Publish(new DownloadCompletedEvent
                    {
                        VideoFile = video,
                        CachePath = cachedPath,
                        Success = true
                    });

                    logger.Info($"Download completed: {cachedPath}");
                    return cachedPath;
                }
                else
                {
                    logger.Error("Download failed");

                    // 发布下载失败事件
                    eventBus.Publish(new DownloadCompletedEvent
                    {
                        VideoFile = video,
                        CachePath = cachedPath,
                        Success = false,
                        ErrorMessage = "Download failed"
                    });

                    return string.Empty;
                }
            }
            catch (OperationCanceledException)
            {
                logger.Warning("Download cancelled");

                // 发布下载取消事件
                eventBus.Publish(new DownloadCompletedEvent
                {
                    VideoFile = video,
                    CachePath = cachedPath,
                    Success = false,
                    ErrorMessage = "Download cancelled"
                });

                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error($"Error downloading video: {video.url}", ex);

                // 发布下载失败事件
                eventBus.Publish(new DownloadCompletedEvent
                {
                    VideoFile = video,
                    CachePath = cachedPath,
                    Success = false,
                    ErrorMessage = ex.Message
                });

                return string.Empty;
            }
            finally
            {
                isDownloading = false;
                if (downloadCancellationToken != null)
                {
                    downloadCancellationToken.Dispose();
                    downloadCancellationToken = null;
                }
            }
        }

        /// <summary>
        /// 更新播放统计
        /// </summary>
        private void UpdatePlaybackStats(VideoFile video)
        {
            if (video == null) return;

            video.playCount++;
            video.lastPlayDate = DateTime.Now;

            logger.Debug($"Updated playback stats for {video.name}: play count = {video.playCount}");
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        private string GenerateCacheKey(VideoFile video)
        {
            if (video == null) return "";

            // 使用URL作为缓存键
            return video.path ?? video.url ?? "";
        }

        /// <summary>
        /// 获取文件扩展名
        /// </summary>
        private string GetFileExtension(string path)
        {
            if (string.IsNullOrEmpty(path)) return ".mp4";

            string ext = System.IO.Path.GetExtension(path);
            return string.IsNullOrEmpty(ext) ? ".mp4" : ext.ToLowerInvariant();
        }

        #endregion

        #region 事件处理器

        /// <summary>
        /// 处理视频选中事件
        /// </summary>
        private void OnVideoSelected(VideoSelectedEvent e)
        {
            if (e == null || e.VideoFile == null) return;

            logger.Info($"Video selected: {e.VideoFile.name}");

            // 自动播放选中的视频
            PlayVideoAsync(e.VideoFile).ConfigureAwait(false);
        }

        /// <summary>
        /// 处理播放状态改变事件
        /// </summary>
        private void OnPlaybackStateChanged(PlaybackState newState)
        {
            logger.Debug($"Playback state changed to: {newState}");

            // 发布播放状态改变事件
            eventBus.Publish(new PlaybackStateChangedEvent
            {
                NewState = newState,
                VideoPath = currentVideo?.path
            });
        }

        /// <summary>
        /// 处理播放更新事件
        /// </summary>
        private void OnPlaybackUpdated(PlaybackSnapshot snapshot)
        {
            // 限制事件发布频率（每0.15秒发布一次）
            float positionDiff = Mathf.Abs(snapshot.positionSeconds - lastEmittedPosition);
            float durationDiff = Mathf.Abs(snapshot.durationSeconds - lastEmittedDuration);

            bool shouldPublish = positionDiff >= 0.15f || durationDiff >= 0.15f;

            if (shouldPublish)
            {
                lastEmittedPosition = snapshot.positionSeconds;
                lastEmittedDuration = snapshot.durationSeconds;

                // 发布播放进度事件
                eventBus.Publish(new PlaybackProgressEvent
                {
                    Position = snapshot.positionSeconds,
                    Duration = snapshot.durationSeconds,
                    NormalizedProgress = snapshot.normalizedProgress,
                    IsPlaying = snapshot.state == PlaybackState.Playing
                });
            }
        }

        /// <summary>
        /// 处理播放错误事件
        /// </summary>
        private void OnPlaybackError(PlaybackError error)
        {
            logger.Error($"Playback error: {error.message}");

            // 发布播放错误事件
            eventBus.Publish(new PlaybackErrorEvent
            {
                Error = error,
                VideoPath = error.source
            });
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 当前播放的视频
        /// </summary>
        public VideoFile CurrentVideo => currentVideo;

        /// <summary>
        /// 是否正在播放
        /// </summary>
        public bool IsPlaying => playbackService?.State == PlaybackState.Playing;

        /// <summary>
        /// 是否正在下载
        /// </summary>
        public bool IsDownloading => isDownloading;

        /// <summary>
        /// 当前播放状态
        /// </summary>
        public PlaybackState PlaybackState => playbackService?.State ?? PlaybackState.Idle;

        /// <summary>
        /// 当前播放快照
        /// </summary>
        public PlaybackSnapshot PlaybackSnapshot => playbackService?.Snapshot ?? PlaybackSnapshot.CreateDefault();

        #endregion
    }
}
