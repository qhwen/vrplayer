using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mobile-friendly playback and local video browser UI.
/// </summary>
public class VideoBrowserUI : MonoBehaviour
{
    private const int MaxVisibleItems = 60;
    private const float ListItemHeight = 80f;
    private const float ListItemSpacing = 8f;
    private const float ListPadding = 10f;

    private VRVideoPlayer vrVideoPlayer;
    private LocalFileManager localFileManager;
    private IPlaybackService playbackService;

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

    private PlaybackSnapshot lastSnapshot = PlaybackSnapshot.CreateDefault();

    private void Start()
    {
        vrVideoPlayer = FindObjectOfType<VRVideoPlayer>();
        localFileManager = FindObjectOfType<LocalFileManager>();

        if (vrVideoPlayer == null || localFileManager == null)
        {
            Debug.LogError("VideoBrowserUI initialization failed: missing dependencies.");
            enabled = false;
            return;
        }

        playbackService = vrVideoPlayer.GetPlaybackService();
        if (playbackService == null)
        {
            Debug.LogError("VideoBrowserUI initialization failed: playback service is missing.");
            enabled = false;
            return;
        }

        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("UICanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.6f;

            canvasObject.AddComponent<GraphicRaycaster>();
        }

        BuildPanel();
        RegisterPlaybackEvents();
        StartCoroutine(InitializeAndRefresh());
    }

    private void OnDestroy()
    {
        if (playbackService != null)
        {
            playbackService.StateChanged -= OnPlaybackStateChanged;
            playbackService.PlaybackUpdated -= OnPlaybackUpdated;
            playbackService.ErrorOccurred -= OnPlaybackError;
        }

        if (progressSlider != null)
        {
            progressSlider.onValueChanged.RemoveListener(OnProgressChanged);
        }
    }

    private void RegisterPlaybackEvents()
    {
        playbackService.StateChanged += OnPlaybackStateChanged;
        playbackService.PlaybackUpdated += OnPlaybackUpdated;
        playbackService.ErrorOccurred += OnPlaybackError;

        OnPlaybackUpdated(playbackService.Snapshot);
        UpdateRuntimeStatus();
    }

    private IEnumerator InitializeAndRefresh()
    {
        yield return null;
        RefreshVideoList();
    }

    private IEnumerator WaitForPermissionRequest(float timeoutSeconds)
    {
        float left = timeoutSeconds;

        while (localFileManager != null && localFileManager.IsPermissionRequestInFlight() && left > 0f)
        {
            left -= Time.unscaledDeltaTime;
            yield return null;
        }
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

        refreshButton.onClick.AddListener(RefreshVideoList);
        pauseResumeButton.onClick.AddListener(OnPauseResumeClicked);
        stopButton.onClick.AddListener(OnStopClicked);
        backToListButton.onClick.AddListener(OnBackToListClicked);

        GameObject progressRow = CreateHorizontalRow(panelObject.transform, "ProgressRow", new Vector2(0.02f, 0.57f), new Vector2(0.98f, 0.65f));
        progressSlider = CreateProgressSlider(progressRow.transform, "ProgressSlider");
        progressSlider.onValueChanged.AddListener(OnProgressChanged);
        playbackTimeText = CreateFixedLabel(progressRow.transform, "TimeLabel", "00:00 / 00:00", font, 20, 220f);

        GameObject permissionRow = CreateHorizontalRow(panelObject.transform, "PermissionRow", new Vector2(0.02f, 0.49f), new Vector2(0.98f, 0.57f));
        grantPermissionButton = CreateActionButton(permissionRow.transform, "GrantPermissionButton", "Grant Permission", font);
        openSettingsButton = CreateActionButton(permissionRow.transform, "OpenSettingsButton", "Open Settings", font);

        grantPermissionButton.onClick.AddListener(OnGrantPermissionClicked);
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

    private void RefreshVideoList()
    {
        if (localFileManager == null)
        {
            return;
        }

        idleStatusMessage = "Scanning Movies folder...";

        bool hasPermission = localFileManager.HasReadableMediaPermission();
        if (!hasPermission)
        {
            SetPermissionButtonsVisible(true);
            RebuildVideoList(new List<VideoFile>());

            idleStatusMessage = "Photos and videos permission required";
            if (localFileManager.WasLastPermissionRequestDeniedAndDontAskAgain())
            {
                hintText.text = "Permission was denied with Don't ask again. Tap Open Settings and choose Allow all photos and videos.";
            }
            else
            {
                hintText.text = "Tap Grant Permission and choose Allow all photos and videos.";
            }

            return;
        }

        List<VideoFile> videos = localFileManager.GetLocalVideos();
        RebuildVideoList(videos);
        SetPermissionButtonsVisible(false);

        if (videos.Count == 0)
        {
            idleStatusMessage = "No playable video found";
            hintText.text = "Put MP4 files in /storage/emulated/0/Movies, then tap Refresh.";
            return;
        }

        idleStatusMessage = "Found " + videos.Count + " videos. Tap one to play.";
        hintText.text = "Touch and drag outside the panel to rotate the VR view.";
    }

    private void SetPermissionButtonsVisible(bool visible)
    {
        if (grantPermissionButton != null)
        {
            grantPermissionButton.gameObject.SetActive(visible);
        }

        if (openSettingsButton != null)
        {
            openSettingsButton.gameObject.SetActive(visible);
        }
    }

    private void RebuildVideoList(List<VideoFile> videos)
    {
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

    private void PlayVideo(VideoFile file)
    {
        if (file == null || playbackService == null)
        {
            return;
        }

        string path = file.localPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            path = file.path;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            idleStatusMessage = "Cannot play: invalid file path";
            return;
        }

        idleStatusMessage = "Preparing: " + file.name;
        currentVideoText.text = "Selected: " + file.name;

        if (!playbackService.Open(path))
        {
            PlaybackError error = playbackService.LastError;
            statusText.text = "Playback failed [" + error.code + "]: " + error.message;
            return;
        }

        playbackService.Play();
    }

    private void OnGrantPermissionClicked()
    {
        if (localFileManager == null || localFileManager.IsPermissionRequestInFlight())
        {
            return;
        }

        StartCoroutine(RequestPermissionAndRefresh());
    }

    private IEnumerator RequestPermissionAndRefresh()
    {
        if (localFileManager == null)
        {
            yield break;
        }

        idleStatusMessage = "Requesting permission...";
        localFileManager.RequestReadableMediaPermission();
        yield return WaitForPermissionRequest(10f);
        RefreshVideoList();
    }

    private void OnOpenSettingsClicked()
    {
        refreshAfterSettingsReturn = true;
        idleStatusMessage = "Open Settings and choose Allow all photos and videos.";
        localFileManager?.OpenAppPermissionSettings();
    }

    private void OnPauseResumeClicked()
    {
        if (playbackService == null)
        {
            return;
        }

        if (playbackService.State == PlaybackState.Playing)
        {
            playbackService.Pause();
            return;
        }

        playbackService.Play();
    }

    private void OnStopClicked()
    {
        playbackService?.Stop();
        idleStatusMessage = "Stopped";
    }

    private void OnBackToListClicked()
    {
        playbackService?.Stop();

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

        idleStatusMessage = "Back to list";
    }

    private void OnProgressChanged(float value)
    {
        if (suppressSeekCallback || playbackService == null || !playbackService.HasSource)
        {
            return;
        }

        float duration = Mathf.Max(0f, lastSnapshot.durationSeconds);
        if (duration <= 0.01f)
        {
            return;
        }

        playbackService.Seek(value * duration);
    }

    private void OnPlaybackStateChanged(PlaybackState state)
    {
        UpdateRuntimeStatus();
    }

    private void OnPlaybackUpdated(PlaybackSnapshot snapshot)
    {
        lastSnapshot = snapshot;

        if (progressSlider != null)
        {
            suppressSeekCallback = true;
            progressSlider.value = snapshot.normalizedProgress;
            suppressSeekCallback = false;
        }

        if (playbackTimeText != null)
        {
            playbackTimeText.text = FormatTime(snapshot.positionSeconds) + " / " + FormatTime(snapshot.durationSeconds);
        }

        UpdateRuntimeStatus();
    }

    private void OnPlaybackError(PlaybackError error)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = "Playback failed [" + error.code + "]: " + error.message;
    }

    private void UpdateControlButtons()
    {
        if (pauseResumeButton == null || playbackService == null)
        {
            return;
        }

        Text buttonLabel = pauseResumeButton.GetComponentInChildren<Text>();
        if (buttonLabel != null)
        {
            buttonLabel.text = playbackService.State == PlaybackState.Playing ? "Pause" : "Resume";
        }

        if (progressSlider != null)
        {
            progressSlider.interactable = playbackService.HasSource && lastSnapshot.durationSeconds > 0.01f;
        }
    }

    private void UpdateRuntimeStatus()
    {
        if (statusText == null || currentVideoText == null || playbackService == null)
        {
            return;
        }

        string currentUrl = playbackService.CurrentSource;
        if (string.IsNullOrWhiteSpace(currentUrl))
        {
            currentVideoText.text = "No video selected";
        }
        else if (!currentVideoText.text.StartsWith("Selected:"))
        {
            currentVideoText.text = "Source: " + Shorten(currentUrl, 90);
        }

        if (localFileManager != null && localFileManager.IsPermissionRequestInFlight())
        {
            statusText.text = "Requesting permission...";
            return;
        }

        if (!playbackService.HasSource)
        {
            statusText.text = idleStatusMessage;
            return;
        }

        if (playbackService.LastError.HasError)
        {
            PlaybackError error = playbackService.LastError;
            statusText.text = "Playback failed [" + error.code + "]: " + error.message;
            return;
        }

        switch (playbackService.State)
        {
            case PlaybackState.Preparing:
                statusText.text = "Loading video...";
                break;
            case PlaybackState.Playing:
                statusText.text = lastSnapshot.isBuffering ? "Buffering..." : "Playing";
                break;
            case PlaybackState.Paused:
                statusText.text = "Paused";
                break;
            case PlaybackState.Ready:
                statusText.text = "Ready";
                break;
            case PlaybackState.Error:
                statusText.text = "Playback failed";
                break;
            default:
                statusText.text = idleStatusMessage;
                break;
        }
    }

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

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }

    private static string Shorten(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength - 3) + "...";
    }

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
}
