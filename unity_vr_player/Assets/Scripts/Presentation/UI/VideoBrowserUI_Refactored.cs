using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRPlayer.Application.Library;
using VRPlayer.Application.Playback;
using VRPlayer.Core.EventBus;
using VRPlayer.Core.Logging;

namespace VRPlayer.Presentation.UI
{
    /// <summary>
    /// 移动端友好的播放和本地视频浏览 UI（使用新架构重构版本）
    /// </summary>
    public class VideoBrowserUI : MonoBehaviour
    {
        private const int MaxVisibleItems = 60;
        private const float ListItemHeight = 80f;
        private const float ListItemSpacing = 8f;
        private const float ListPadding = 10f;

        [Header("依赖注入")]
        [SerializeField] private PlaybackOrchestrator playbackOrchestrator;
        [SerializeField] private LibraryManager libraryManager;

        [Header("UI 配置")]
        [SerializeField] private bool autoInitialize = true;

        private Canvas canvas;
        private RectTransform listContent;
        private ScrollRect listScrollRect;

        private Text statusText;
        private Text currentVideoText;
        private Text hintText;
        private Text playbackTimeText;

        private Button refreshButton;
        private Button grantPermissionButton;
        private Button openSettingsButton;
        private Button pauseResumeButton;
        private Button stopButton;
        private Button backToListButton;

        private Slider progressSlider;

        private readonly List<Button> generatedButtons = new List<Button>();

        private string idleStatusMessage = "Ready";
        private bool suppressSeekCallback;
        private bool refreshAfterSettingsReturn;
        private Coroutine permissionFlowCoroutine;

        private PlaybackSnapshot lastSnapshot = PlaybackSnapshot.CreateDefault();
        private ILogger logger;

        private void Awake()
        {
            logger = LoggerManager.For("VideoBrowserUI");

            if (!autoInitialize) return;

            // 自动查找依赖
            if (playbackOrchestrator == null)
            {
                playbackOrchestrator = FindObjectOfType<PlaybackOrchestrator>();
            }

            if (libraryManager == null)
            {
                libraryManager = FindObjectOfType<LibraryManager>();
            }

            if (playbackOrchestrator == null || libraryManager == null)
            {
                logger.Error("初始化失败: 缺少依赖项");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            if (playbackOrchestrator == null || libraryManager == null)
            {
                logger.Error("启动失败: 依赖项未初始化");
                return;
            }

            // 订阅事件总线
            SubscribeToEventBus();

            // 创建 UI
            EnsureEventSystemExists();
            canvas = EnsureOverlayCanvas();
            BuildPanel();

            // 注册播放事件
            RegisterPlaybackEvents();

            // 初始化并刷新
            StartCoroutine(InitializeAndRefresh());
        }

        private void OnDestroy()
        {
            // 取消订阅事件总线
            UnsubscribeFromEventBus();

            // 清理进度条回调
            if (progressSlider != null)
            {
                progressSlider.onValueChanged.RemoveListener(OnProgressChanged);
            }

            // 停止协程
            if (permissionFlowCoroutine != null)
            {
                StopCoroutine(permissionFlowCoroutine);
                permissionFlowCoroutine = null;
            }
        }

        #region 事件总线订阅

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

        #endregion

        #region 事件处理器

        private void OnLibraryScanStarted(object sender, LibraryScanStartedEvent e)
        {
            idleStatusMessage = "扫描视频库...";
            UpdateStatusText();
        }

        private void OnLibraryScanCompleted(object sender, LibraryScanCompletedEvent e)
        {
            logger.Info($"库扫描完成: 发现 {e.NewVideosCount} 个新视频，耗时 {e.Duration.TotalSeconds:F2}s");
            RefreshVideoList();
        }

        private void OnVideoAdded(object sender, VideoAddedEvent e)
        {
            logger.Info($"视频已添加: {e.VideoFile.name}");
            RefreshVideoList();
        }

        private void OnVideoRemoved(object sender, VideoRemovedEvent e)
        {
            logger.Info($"视频已移除: {e.VideoFile.name}");
            RefreshVideoList();
        }

        private void OnPlaybackStarted(object sender, PlaybackStartedEvent e)
        {
            idleStatusMessage = "正在播放: " + e.VideoFile.name;
            UpdateStatusText();
        }

        private void OnPlaybackStopped(object sender, PlaybackStoppedEvent e)
        {
            idleStatusMessage = "已停止";
            UpdateStatusText();
            ResetPlaybackUI();
        }

        private void OnPlaybackPaused(object sender, PlaybackPausedEvent e)
        {
            idleStatusMessage = "已暂停";
            UpdateStatusText();
        }

        private void OnPlaybackResumed(object sender, PlaybackResumedEvent e)
        {
            idleStatusMessage = "正在播放";
            UpdateStatusText();
        }

        private void OnPlaybackError(object sender, PlaybackErrorEvent e)
        {
            logger.Error($"播放错误: {e.ErrorMessage}");
            if (statusText != null)
            {
                statusText.text = "播放失败: " + e.ErrorMessage;
            }
        }

        #endregion

        #region 播放控制

        private void RegisterPlaybackEvents()
        {
            // 注意: PlaybackOrchestrator 已经通过 EventBus 发布事件
            // 这里不需要直接订阅它的内部事件
        }

        private IEnumerator InitializeAndRefresh()
        {
            yield return null;
            RefreshVideoList();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus || !refreshAfterSettingsReturn)
            {
                return;
            }

            refreshAfterSettingsReturn = false;
            RefreshVideoList();
        }

        private void Update()
        {
            UpdateControlButtons();
            UpdateRuntimeStatus();
        }

        private void UpdateStatusText()
        {
            if (statusText != null)
            {
                statusText.text = idleStatusMessage;
            }
        }

        #endregion

        #region UI 创建

        private static void EnsureEventSystemExists()
        {
            EventSystem current = FindObjectOfType<EventSystem>();
            if (current != null)
            {
                if (current.GetComponent<BaseInputModule>() == null)
                {
                    current.gameObject.AddComponent<StandaloneInputModule>();
                }

                return;
            }

            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static Canvas EnsureOverlayCanvas()
        {
            Canvas existing = GameObject.Find("VideoBrowserCanvas")?.GetComponent<Canvas>();
            if (existing != null)
            {
                if (existing.GetComponent<GraphicRaycaster>() == null)
                {
                    existing.gameObject.AddComponent<GraphicRaycaster>();
                }

                return existing;
            }

            GameObject canvasObject = new GameObject("VideoBrowserCanvas");
            Canvas created = canvasObject.AddComponent<Canvas>();
            created.renderMode = RenderMode.ScreenSpaceOverlay;
            created.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.6f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return created;
        }

        private void BuildPanel()
        {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject panelObject = new GameObject("VideoBrowserPanel");
            panelObject.transform.SetParent(canvas.transform, false);

            RectTransform panelRect = panelObject.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.03f, 0.02f);
            panelRect.anchorMax = new Vector2(0.97f, 0.56f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBackground = panelObject.AddComponent<Image>();
            panelBackground.color = new Color(0f, 0f, 0f, 0.72f);

            CreateAnchoredLabel(panelObject.transform, "Title", "Local Video Library", font, 38, TextAnchor.UpperCenter,
                new Vector2(0.02f, 0.90f), new Vector2(0.98f, 1f));

            statusText = CreateAnchoredLabel(panelObject.transform, "Status", "Loading videos...", font, 28, TextAnchor.UpperCenter,
                new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.90f));

            currentVideoText = CreateAnchoredLabel(panelObject.transform, "CurrentVideo", "No video selected", font, 22, TextAnchor.MiddleLeft,
                new Vector2(0.02f, 0.74f), new Vector2(0.98f, 0.82f));

            GameObject controlRow = CreateHorizontalRow(panelObject.transform, "ControlRow", new Vector2(0.02f, 0.65f), new Vector2(0.98f, 0.74f));
            refreshButton = CreateActionButton(controlRow.transform, "RefreshButton", "Refresh", font);
            pauseResumeButton = CreateActionButton(controlRow.transform, "PauseResumeButton", "Pause", font);
            stopButton = CreateActionButton(controlRow.transform, "StopButton", "Stop", font);
            backToListButton = CreateActionButton(controlRow.transform, "BackButton", "Back", font);

            refreshButton.onClick.AddListener(OnRefreshClicked);
            pauseResumeButton.onClick.AddListener(OnPauseResumeClicked);
            stopButton.onClick.AddListener(OnStopClicked);
            backToListButton.onClick.AddListener(OnBackToListClicked);

            GameObject progressRow = CreateHorizontalRow(panelObject.transform, "ProgressRow", new Vector2(0.02f, 0.57f), new Vector2(0.98f, 0.65f));
            progressSlider = CreateProgressSlider(progressRow.transform, "ProgressSlider");
            progressSlider.onValueChanged.AddListener(OnProgressChanged);
            playbackTimeText = CreateFixedLabel(progressRow.transform, "TimeLabel", "00:00 / 00:00", font, 20, 220f);

            GameObject permissionRow = CreateHorizontalRow(panelObject.transform, "PermissionRow", new Vector2(0.02f, 0.49f), new Vector2(0.98f, 0.57f));
            grantPermissionButton = CreateActionButton(permissionRow.transform, "SelectVideosButton", "Select Videos", font);
            openSettingsButton = CreateActionButton(permissionRow.transform, "OpenSettingsButton", "Scan Settings", font);

            grantPermissionButton.onClick.AddListener(OnSelectVideosClicked);
            openSettingsButton.onClick.AddListener(OnOpenSettingsClicked);

            hintText = CreateAnchoredLabel(panelObject.transform, "Hint", "", font, 19, TextAnchor.UpperLeft,
                new Vector2(0.02f, 0.42f), new Vector2(0.98f, 0.49f));

            GameObject scrollObject = new GameObject("VideoScroll");
            scrollObject.transform.SetParent(panelObject.transform, false);

            RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.02f, 0.04f);
            scrollRect.anchorMax = new Vector2(0.98f, 0.42f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            Image scrollBackground = scrollObject.AddComponent<Image>();
            scrollBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);

            listScrollRect = scrollObject.AddComponent<ScrollRect>();
            listScrollRect.horizontal = false;
            listScrollRect.movementType = ScrollRect.MovementType.Clamped;
            listScrollRect.scrollSensitivity = 40f;

            GameObject viewportObject = new GameObject("Viewport");
            viewportObject.transform.SetParent(scrollObject.transform, false);

            RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            Mask mask = viewportObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewportObject.transform, false);

            listContent = contentObject.AddComponent<RectTransform>();
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.anchoredPosition = Vector2.zero;
            listContent.sizeDelta = new Vector2(0f, 0f);

            listScrollRect.viewport = viewportRect;
            listScrollRect.content = listContent;
        }

        #endregion

        #region 视频列表

        private void RefreshVideoList()
        {
            if (libraryManager == null)
            {
                return;
            }

            var videos = libraryManager.GetVideos();
            RebuildVideoList(videos);
            UpdateHintText(videos.Count);
        }

        private void UpdateHintText(int videoCount)
        {
            if (libraryManager == null || hintText == null)
            {
                return;
            }

            var permissionManager = FindObjectOfType<Infrastructure.Platform.AndroidPermissionManager>();
            bool hasReadablePermission = permissionManager?.HasReadableMediaPermission() ?? false;
            bool hasMoviesPermission = permissionManager?.HasMoviesScanPermission() ?? false;

            if (videoCount == 0)
            {
                idleStatusMessage = "没有选择视频";

                if (hasMoviesPermission)
                {
                    hintText.text = "点击 Select Videos，或在 /storage/emulated/0/Movies 放置 MP4 文件后点击 Refresh。";
                }
                else if (hasReadablePermission)
                {
                    hintText.text = "点击 Select Videos 添加选中的视频。Movies 自动扫描仍需要完整视频权限。";
                }
                else
                {
                    hintText.text = "点击 Select Videos 直接播放。点击 Scan Settings 请求 Movies 扫描权限。";
                }

                return;
            }

            if (hasMoviesPermission)
            {
                idleStatusMessage = $"找到 {videoCount} 个视频。点击其中一个播放。";
                hintText.text = "在面板外触摸并拖动以旋转 VR 视图。";
                return;
            }

            idleStatusMessage = $"找到 {videoCount} 个选中的视频。点击其中一个播放。";
            hintText.text = "Select Videos 无需 Movies 权限。仅对 /Movies 自动扫描使用 Scan Settings。";
        }

        private void RebuildVideoList(List<VideoFile> videos)
        {
            // 清空现有按钮
            for (int i = 0; i < generatedButtons.Count; i++)
            {
                if (generatedButtons[i] != null)
                {
                    Destroy(generatedButtons[i].gameObject);
                }
            }

            generatedButtons.Clear();

            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            int count = Mathf.Min(videos.Count, MaxVisibleItems);

            // 创建新按钮
            for (int i = 0; i < count; i++)
            {
                VideoFile selectedFile = videos[i];
                Button itemButton = CreateListItemButton(listContent, "Video_" + i, BuildButtonText(selectedFile), font);

                RectTransform itemRect = itemButton.GetComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0f, 1f);
                itemRect.anchorMax = new Vector2(1f, 1f);
                itemRect.pivot = new Vector2(0.5f, 1f);
                itemRect.anchoredPosition = new Vector2(0f, -ListPadding - i * (ListItemHeight + ListItemSpacing));
                itemRect.sizeDelta = new Vector2(-ListPadding * 2f, ListItemHeight);

                itemButton.onClick.AddListener(() => PlayVideo(selectedFile));
                generatedButtons.Add(itemButton);
            }

            // 更新内容高度
            float contentHeight = ListPadding * 2f;
            if (count > 0)
            {
                contentHeight += count * ListItemHeight + Mathf.Max(0, count - 1) * ListItemSpacing;
            }

            listContent.sizeDelta = new Vector2(0f, contentHeight);

            if (listScrollRect != null)
            {
                listScrollRect.StopMovement();
                listScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        #endregion

        #region 播放控制

        private async void PlayVideo(VideoFile file)
        {
            if (playbackOrchestrator == null)
            {
                logger.Error("PlaybackOrchestrator 未初始化");
                return;
            }

            idleStatusMessage = "准备中: " + file.name;
            currentVideoText.text = "Selected: " + file.name;

            // 使用 PlaybackOrchestrator 播放视频
            await playbackOrchestrator.PlayVideoAsync(file);
        }

        private void OnPauseResumeClicked()
        {
            if (playbackOrchestrator == null)
            {
                return;
            }

            var state = playbackOrchestrator.GetCurrentState();
            if (state == PlaybackState.Playing)
            {
                playbackOrchestrator.PausePlayback();
            }
            else
            {
                playbackOrchestrator.ResumePlayback();
            }
        }

        private void OnStopClicked()
        {
            playbackOrchestrator?.StopPlayback();
            idleStatusMessage = "已停止";
        }

        private void OnBackToListClicked()
        {
            playbackOrchestrator?.StopPlayback();
            ResetPlaybackUI();
            idleStatusMessage = "返回列表";
        }

        private void ResetPlaybackUI()
        {
            if (currentVideoText != null)
            {
                currentVideoText.text = "No video selected";
            }

            if (progressSlider != null)
            {
                suppressSeekCallback = true;
                progressSlider.value = 0f;
                suppressSeekCallback = false;
            }

            if (playbackTimeText != null)
            {
                playbackTimeText.text = "00:00 / 00:00";
            }
        }

        #endregion

        #region 权限和文件选择

        private void OnRefreshClicked()
        {
            if (libraryManager == null)
            {
                return;
            }

            idleStatusMessage = "刷新视频库...";
            StartCoroutine(libraryManager.RefreshLibraryAsync());
        }

        private void OnSelectVideosClicked()
        {
            if (libraryManager == null)
            {
                return;
            }

            idleStatusMessage = "打开系统选择器...";
            libraryManager.OpenFilePicker();
        }

        private void OnOpenSettingsClicked()
        {
            if (permissionFlowCoroutine != null)
            {
                StopCoroutine(permissionFlowCoroutine);
            }

            permissionFlowCoroutine = StartCoroutine(RequestMoviesPermissionFlow());
        }

        private IEnumerator RequestMoviesPermissionFlow()
        {
            var permissionManager = FindObjectOfType<Infrastructure.Platform.AndroidPermissionManager>();
            if (permissionManager == null)
            {
                logger.Error("AndroidPermissionManager 未找到");
                permissionFlowCoroutine = null;
                yield break;
            }

            if (permissionManager.HasMoviesScanPermission())
            {
                idleStatusMessage = "Movies 扫描权限已授予。";
                RefreshVideoList();
                permissionFlowCoroutine = null;
                yield break;
            }

            idleStatusMessage = "请求 Movies 权限...";
            permissionManager.RequestReadableMediaPermission();

            while (permissionManager.IsPermissionRequestInFlight())
            {
                yield return null;
            }

            if (permissionManager.HasMoviesScanPermission())
            {
                idleStatusMessage = "Movies 扫描权限已授予。";
                RefreshVideoList();
                permissionFlowCoroutine = null;
                yield break;
            }

            if (permissionManager.WasLastPermissionRequestDeniedAndDontAskAgain())
            {
                refreshAfterSettingsReturn = true;
                idleStatusMessage = "权限被阻止。打开应用设置...";
                permissionManager.OpenAppPermissionSettings();
                permissionFlowCoroutine = null;
                yield break;
            }

            idleStatusMessage = "Movies 权限被拒绝。您可以继续使用 Select Videos。";
            RefreshVideoList();
            permissionFlowCoroutine = null;
        }

        #endregion

        #region UI 更新

        private void UpdateControlButtons()
        {
            if (pauseResumeButton == null || playbackOrchestrator == null)
            {
                return;
            }

            Text buttonLabel = pauseResumeButton.GetComponentInChildren<Text>();
            if (buttonLabel != null)
            {
                var state = playbackOrchestrator.GetCurrentState();
                buttonLabel.text = state == PlaybackState.Playing ? "Pause" : "Resume";
            }

            if (progressSlider != null)
            {
                progressSlider.interactable = lastSnapshot.durationSeconds > 0.01f;
            }
        }

        private void UpdateRuntimeStatus()
        {
            if (statusText == null || currentVideoText == null || playbackOrchestrator == null)
            {
                return;
            }

            var state = playbackOrchestrator.GetCurrentState();

            // 更新当前视频文本
            var currentVideo = playbackOrchestrator.GetCurrentVideo();
            if (currentVideo == null)
            {
                currentVideoText.text = "No video selected";
            }
            else if (!currentVideoText.text.StartsWith("Selected:"))
            {
                currentVideoText.text = "Selected: " + currentVideo.name;
            }

            // 检查权限请求状态
            var permissionManager = FindObjectOfType<Infrastructure.Platform.AndroidPermissionManager>();
            if (permissionManager != null && permissionManager.IsPermissionRequestInFlight())
            {
                statusText.text = "请求权限中...";
                return;
            }

            // 如果没有视频，显示空闲状态
            if (currentVideo == null)
            {
                statusText.text = idleStatusMessage;
                return;
            }

            // 根据状态更新状态文本
            switch (state)
            {
                case PlaybackState.Preparing:
                    statusText.text = "加载视频中...";
                    break;
                case PlaybackState.Playing:
                    statusText.text = lastSnapshot.isBuffering ? "缓冲中..." : "正在播放";
                    break;
                case PlaybackState.Paused:
                    statusText.text = "已暂停";
                    break;
                case PlaybackState.Ready:
                    statusText.text = "就绪";
                    break;
                case PlaybackState.Error:
                    statusText.text = "播放失败";
                    break;
                default:
                    statusText.text = idleStatusMessage;
                    break;
            }
        }

        #endregion

        #region 辅助方法

        private static string BuildButtonText(VideoFile file)
        {
            if (file == null)
            {
                return "Unknown file";
            }

            string name = string.IsNullOrWhiteSpace(file.name) ? "Unnamed video" : file.name;
            string size = FormatSize(file.size);
            return name + "\n" + size;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 B";
            }

            double value = bytes;
            string[] units = { "B", "KB", "MB", "GB" };
            int unitIndex = 0;

            while (value >= 1024d && unitIndex < units.Length - 1)
            {
                value /= 1024d;
                unitIndex++;
            }

            return value.ToString("F1") + " " + units[unitIndex];
        }

        #endregion

        #region UI 创建辅助方法

        private static GameObject CreateHorizontalRow(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject row = new GameObject(name);
            row.transform.SetParent(parent, false);

            RectTransform rect = row.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            return row;
        }

        private static Text CreateAnchoredLabel(Transform parent, string name, string content, Font font, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = labelObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Text text = labelObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.text = content;

            return text;
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Font font)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.24f, 0.28f, 0.98f);
            image.raycastTarget = true;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 62f;
            layoutElement.preferredHeight = 62f;

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 23;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private static Slider CreateProgressSlider(Transform parent, string name)
        {
            GameObject sliderObject = new GameObject(name);
            sliderObject.transform.SetParent(parent, false);

            LayoutElement layout = sliderObject.AddComponent<LayoutElement>();
            layout.minHeight = 52f;
            layout.preferredHeight = 52f;
            layout.flexibleWidth = 1f;

            RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(sliderObject.transform, false);
            Image background = backgroundObject.AddComponent<Image>();
            background.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.30f);
            backgroundRect.anchorMax = new Vector2(1f, 0.70f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fillAreaObject = new GameObject("Fill Area");
            fillAreaObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0f);
            fillAreaRect.anchorMax = new Vector2(1f, 1f);
            fillAreaRect.offsetMin = new Vector2(8f, 8f);
            fillAreaRect.offsetMax = new Vector2(-8f, -8f);

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(fillAreaObject.transform, false);
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.72f, 0.2f, 1f);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(sliderObject.transform, false);
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(0.92f, 0.92f, 0.92f, 1f);

            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 42f);

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            sliderRect.sizeDelta = new Vector2(0f, 52f);
            return slider;
        }

        private static Text CreateFixedLabel(Transform parent, string name, string content, Font font, int fontSize, float preferredWidth)
        {
            GameObject labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);

            LayoutElement layout = labelObject.AddComponent<LayoutElement>();
            layout.minWidth = preferredWidth;
            layout.preferredWidth = preferredWidth;
            layout.flexibleWidth = 0f;

            Text text = labelObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleRight;
            text.color = Color.white;
            text.text = content;

            return text;
        }

        private static Button CreateListItemButton(Transform parent, string name, string label, Font font)
        {
            GameObject buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.14f, 0.14f, 0.14f, 0.96f);
            image.raycastTarget = true;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            GameObject textObject = new GameObject("Label");
            textObject.transform.SetParent(buttonObject.transform, false);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = "  " + label;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 0f);
            textRect.offsetMax = new Vector2(-12f, 0f);

            return button;
        }

        #endregion
    }
}
