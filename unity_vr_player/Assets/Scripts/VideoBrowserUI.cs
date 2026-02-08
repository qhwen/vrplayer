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

    private VRVideoPlayer videoPlayer;
    private LocalFileManager localFileManager;

    private Canvas canvas;
    private RectTransform listContent;

    private Text statusText;
    private Text currentVideoText;
    private Text hintText;

    private Button refreshButton;
    private Button grantPermissionButton;
    private Button openSettingsButton;
    private Button pauseResumeButton;
    private Button stopButton;

    private readonly List<Button> generatedButtons = new List<Button>();

    private string idleStatusMessage = "Ready";

    private void Start()
    {
        videoPlayer = FindObjectOfType<VRVideoPlayer>();
        localFileManager = FindObjectOfType<LocalFileManager>();

        if (videoPlayer == null || localFileManager == null)
        {
            Debug.LogError("VideoBrowserUI initialization failed: missing dependencies.");
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
        StartCoroutine(InitializeAndRefresh());
    }

    private IEnumerator InitializeAndRefresh()
    {
        yield return null;

        if (localFileManager != null && !localFileManager.HasReadableMediaPermission())
        {
            localFileManager.RequestReadableMediaPermission();
            yield return WaitForPermissionRequest(4f);
        }

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

    private void Update()
    {
        if (videoPlayer == null)
        {
            return;
        }

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
        panelRect.anchorMax = new Vector2(0.97f, 0.52f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelBackground = panelObject.AddComponent<Image>();
        panelBackground.color = new Color(0f, 0f, 0f, 0.72f);

        CreateAnchoredLabel(panelObject.transform, "Title", "Local Video Library", font, 38, TextAnchor.UpperCenter,
            new Vector2(0.02f, 0.88f), new Vector2(0.98f, 1f));

        statusText = CreateAnchoredLabel(panelObject.transform, "Status", "Loading videos...", font, 28, TextAnchor.UpperCenter,
            new Vector2(0.02f, 0.78f), new Vector2(0.98f, 0.88f));

        currentVideoText = CreateAnchoredLabel(panelObject.transform, "CurrentVideo", "No video selected", font, 24, TextAnchor.MiddleLeft,
            new Vector2(0.02f, 0.70f), new Vector2(0.98f, 0.78f));

        GameObject controlRow = new GameObject("ControlRow");
        controlRow.transform.SetParent(panelObject.transform, false);

        RectTransform controlRowRect = controlRow.AddComponent<RectTransform>();
        controlRowRect.anchorMin = new Vector2(0.02f, 0.60f);
        controlRowRect.anchorMax = new Vector2(0.98f, 0.68f);
        controlRowRect.offsetMin = Vector2.zero;
        controlRowRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup controlLayout = controlRow.AddComponent<HorizontalLayoutGroup>();
        controlLayout.spacing = 14f;
        controlLayout.childControlHeight = true;
        controlLayout.childControlWidth = true;
        controlLayout.childForceExpandWidth = true;

        refreshButton = CreateActionButton(controlRow.transform, "RefreshButton", "Refresh", font);
        pauseResumeButton = CreateActionButton(controlRow.transform, "PauseResumeButton", "Pause", font);
        stopButton = CreateActionButton(controlRow.transform, "StopButton", "Stop", font);

        refreshButton.onClick.AddListener(RefreshVideoList);
        pauseResumeButton.onClick.AddListener(OnPauseResumeClicked);
        stopButton.onClick.AddListener(OnStopClicked);

        GameObject permissionRow = new GameObject("PermissionRow");
        permissionRow.transform.SetParent(panelObject.transform, false);

        RectTransform permissionRowRect = permissionRow.AddComponent<RectTransform>();
        permissionRowRect.anchorMin = new Vector2(0.02f, 0.52f);
        permissionRowRect.anchorMax = new Vector2(0.98f, 0.60f);
        permissionRowRect.offsetMin = Vector2.zero;
        permissionRowRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup permissionLayout = permissionRow.AddComponent<HorizontalLayoutGroup>();
        permissionLayout.spacing = 14f;
        permissionLayout.childControlHeight = true;
        permissionLayout.childControlWidth = true;
        permissionLayout.childForceExpandWidth = true;

        grantPermissionButton = CreateActionButton(permissionRow.transform, "GrantPermissionButton", "Grant Permission", font);
        openSettingsButton = CreateActionButton(permissionRow.transform, "OpenSettingsButton", "Open Settings", font);

        grantPermissionButton.onClick.AddListener(OnGrantPermissionClicked);
        openSettingsButton.onClick.AddListener(OnOpenSettingsClicked);

        hintText = CreateAnchoredLabel(panelObject.transform, "Hint", "", font, 20, TextAnchor.UpperLeft,
            new Vector2(0.02f, 0.45f), new Vector2(0.98f, 0.52f));

        GameObject scrollObject = new GameObject("VideoScroll");
        scrollObject.transform.SetParent(panelObject.transform, false);

        RectTransform scrollRect = scrollObject.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.02f, 0.04f);
        scrollRect.anchorMax = new Vector2(0.98f, 0.45f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = Vector2.zero;

        Image scrollBackground = scrollObject.AddComponent<Image>();
        scrollBackground.color = new Color(0.08f, 0.08f, 0.08f, 0.96f);

        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;

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

        VerticalLayoutGroup listLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 10f;
        listLayout.padding = new RectOffset(10, 10, 10, 10);
        listLayout.childAlignment = TextAnchor.UpperCenter;
        listLayout.childControlHeight = true;
        listLayout.childControlWidth = true;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = listContent;
    }

    private void RefreshVideoList()
    {
        if (localFileManager == null)
        {
            return;
        }

        idleStatusMessage = "Scanning local storage...";
        List<VideoFile> videos = localFileManager.GetLocalVideos();
        RebuildVideoList(videos);

        bool hasPermission = localFileManager.HasReadableMediaPermission();
        SetPermissionButtonsVisible(!hasPermission);

        if (videos.Count == 0)
        {
            if (!hasPermission)
            {
                idleStatusMessage = "Storage permission required";
                hintText.text = "Tap Grant Permission. If no popup appears, tap Open Settings and allow Photos and videos.";
            }
            else
            {
                idleStatusMessage = "No playable video found";
                hintText.text = "Put MP4 files in /storage/emulated/0/Movies or /storage/emulated/0/Download, then tap Refresh.";
            }

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
            itemButton.onClick.AddListener(() => PlayVideo(selectedFile));

            generatedButtons.Add(itemButton);
        }
    }

    private void PlayVideo(VideoFile file)
    {
        if (file == null || videoPlayer == null)
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

        videoPlayer.PlayVideo(path);
    }

    private void OnGrantPermissionClicked()
    {
        StartCoroutine(RequestPermissionAndRefresh());
    }

    private IEnumerator RequestPermissionAndRefresh()
    {
        if (localFileManager == null)
        {
            yield break;
        }

        localFileManager.RequestReadableMediaPermission();
        yield return WaitForPermissionRequest(6f);
        RefreshVideoList();
    }

    private void OnOpenSettingsClicked()
    {
        if (localFileManager == null)
        {
            return;
        }

        localFileManager.OpenAppPermissionSettings();
    }

    private void OnPauseResumeClicked()
    {
        if (videoPlayer == null)
        {
            return;
        }

        if (videoPlayer.GetIsPlaying())
        {
            videoPlayer.PauseVideo();
        }
        else
        {
            videoPlayer.ResumeVideo();
        }
    }

    private void OnStopClicked()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.StopVideo();
        idleStatusMessage = "Stopped";
    }

    private void UpdateControlButtons()
    {
        if (pauseResumeButton == null || videoPlayer == null)
        {
            return;
        }

        Text buttonLabel = pauseResumeButton.GetComponentInChildren<Text>();
        if (buttonLabel == null)
        {
            return;
        }

        buttonLabel.text = videoPlayer.GetIsPlaying() ? "Pause" : "Resume";
    }

    private void UpdateRuntimeStatus()
    {
        if (statusText == null || currentVideoText == null || videoPlayer == null)
        {
            return;
        }

        string currentUrl = videoPlayer.GetCurrentVideoUrl();
        if (string.IsNullOrWhiteSpace(currentUrl))
        {
            currentVideoText.text = "No video selected";
        }
        else
        {
            currentVideoText.text = "Source: " + Shorten(currentUrl, 90);
        }

        if (localFileManager != null && localFileManager.IsPermissionRequestInFlight())
        {
            statusText.text = "Requesting permission...";
            return;
        }

        if (!videoPlayer.GetHasVideoSource())
        {
            statusText.text = idleStatusMessage;
            return;
        }

        string lastError = videoPlayer.GetLastErrorMessage();
        if (!string.IsNullOrWhiteSpace(lastError))
        {
            statusText.text = "Playback failed: " + lastError;
            return;
        }

        if (videoPlayer.GetIsPreparing())
        {
            statusText.text = "Loading video...";
            return;
        }

        if (videoPlayer.GetIsPlaying())
        {
            statusText.text = "Playing";
            return;
        }

        if (videoPlayer.GetIsInitialized())
        {
            statusText.text = "Paused";
            return;
        }

        statusText.text = idleStatusMessage;
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

    private static string Shorten(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength - 3) + "...";
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

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 66f;
        layoutElement.preferredHeight = 66f;

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonObject.transform, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = 24;
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

    private static Button CreateListItemButton(Transform parent, string name, string label, Font font)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.14f, 0.14f, 0.14f, 0.96f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 74f;
        layoutElement.preferredHeight = 74f;

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
