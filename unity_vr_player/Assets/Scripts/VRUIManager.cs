using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// VR 播放器 UI 管理器。
/// </summary>
public class VRUIManager : MonoBehaviour
{
    [Header("UI设置")]
    [SerializeField] private Font uiFont;
    [SerializeField] private GameObject controlPanel;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text timeDisplay;
    [SerializeField] private Button volumeUpButton;
    [SerializeField] private Button volumeDownButton;

    private VRVideoPlayer videoPlayer;
    private Canvas canvas;
    private bool isControlPanelVisible = true;
    private float currentVolume = 1f;
    private bool isUpdatingProgress;

    private void Start()
    {
        videoPlayer = FindObjectOfType<VRVideoPlayer>();
        canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("UICanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (controlPanel == null)
        {
            CreateDefaultControlPanel();
        }

        SetupButtonEvents();

        if (progressBar != null)
        {
            progressBar.wholeNumbers = false;
            progressBar.onValueChanged.AddListener(OnProgressChanged);
        }

        UpdatePlayPauseButton(false);
        UpdateTimeDisplay("00:00", "00:00");
    }

    private void CreateDefaultControlPanel()
    {
        controlPanel = new GameObject("ControlPanel");
        controlPanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = controlPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(32f, 32f);
        panelRect.sizeDelta = new Vector2(360f, 240f);

        Image panelBackground = controlPanel.AddComponent<Image>();
        panelBackground.color = new Color(0f, 0f, 0f, 0.7f);

        playPauseButton = CreateButton("PlayPauseButton", "▶", new Vector2(60f, 180f));
        stopButton = CreateButton("StopButton", "■", new Vector2(140f, 180f));
        volumeUpButton = CreateButton("VolumeUpButton", "+", new Vector2(220f, 180f));
        volumeDownButton = CreateButton("VolumeDownButton", "-", new Vector2(300f, 180f));

        progressBar = CreateSlider("ProgressBar", new Vector2(180f, 120f), new Vector2(300f, 24f));
        timeDisplay = CreateText("TimeDisplay", "00:00 / 00:00", new Vector2(180f, 80f), new Vector2(280f, 28f));
    }

    private Button CreateButton(string name, string label, Vector2 anchoredPosition)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(controlPanel.transform, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(64f, 44f);
        rect.anchoredPosition = anchoredPosition;

        GameObject textObject = new GameObject("Label");
        textObject.transform.SetParent(buttonObject.transform, false);

        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = 24;
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    private Slider CreateSlider(string name, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(controlPanel.transform, false);

        RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
        sliderRect.sizeDelta = size;
        sliderRect.anchoredPosition = anchoredPosition;

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(sliderObject.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.25f);
        backgroundRect.anchorMax = new Vector2(1f, 0.75f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        GameObject fillAreaObject = new GameObject("Fill Area");
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(6f, 6f);
        fillAreaRect.offsetMax = new Vector2(-6f, -6f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(fillAreaObject.transform, false);
        Image fillImage = fillObject.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.7f, 0.2f, 1f);

        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.fillRect = fillRect;
        slider.targetGraphic = fillImage;

        return slider;
    }

    private Text CreateText(string name, string content, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(controlPanel.transform, false);

        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = 18;
        text.text = content;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        return text;
    }

    private void SetupButtonEvents()
    {
        if (playPauseButton != null)
        {
            playPauseButton.onClick.AddListener(OnPlayPauseClick);
        }

        if (stopButton != null)
        {
            stopButton.onClick.AddListener(OnStopClick);
        }

        if (volumeUpButton != null)
        {
            volumeUpButton.onClick.AddListener(OnVolumeUpClick);
        }

        if (volumeDownButton != null)
        {
            volumeDownButton.onClick.AddListener(OnVolumeDownClick);
        }
    }

    private void OnPlayPauseClick()
    {
        if (videoPlayer == null)
        {
            return;
        }

        if (videoPlayer.GetIsPlaying())
        {
            videoPlayer.PauseVideo();
            UpdatePlayPauseButton(false);
        }
        else
        {
            videoPlayer.ResumeVideo();
            UpdatePlayPauseButton(true);
        }
    }

    private void OnStopClick()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.StopVideo();
        UpdatePlayPauseButton(false);

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }

        UpdateTimeDisplay("00:00", "00:00");
    }

    private void OnVolumeUpClick()
    {
        currentVolume = Mathf.Clamp01(currentVolume + 0.1f);
        if (videoPlayer != null)
        {
            videoPlayer.SetVolume(currentVolume);
        }
    }

    private void OnVolumeDownClick()
    {
        currentVolume = Mathf.Clamp01(currentVolume - 0.1f);
        if (videoPlayer != null)
        {
            videoPlayer.SetVolume(currentVolume);
        }
    }

    private void OnProgressChanged(float value)
    {
        if (isUpdatingProgress || videoPlayer == null || !videoPlayer.GetIsInitialized())
        {
            return;
        }

        UnityEngine.Video.VideoPlayer nativePlayer = videoPlayer.GetVideoPlayer();
        if (nativePlayer == null)
        {
            return;
        }

        double length = nativePlayer.length;
        if (length > 0)
        {
            videoPlayer.SeekTo((float)(value * length));
        }
    }

    private void Update()
    {
        if (videoPlayer == null)
        {
            return;
        }

        UnityEngine.Video.VideoPlayer nativePlayer = videoPlayer.GetVideoPlayer();
        if (nativePlayer == null)
        {
            return;
        }

        if (progressBar != null && videoPlayer.GetIsInitialized())
        {
            double length = nativePlayer.length;
            double time = nativePlayer.time;

            if (length > 0)
            {
                isUpdatingProgress = true;
                progressBar.value = Mathf.Clamp01((float)(time / length));
                isUpdatingProgress = false;
            }

            UpdateTimeDisplay(FormatTime((float)time), FormatTime((float)length));
        }

        UpdatePlayPauseButton(videoPlayer.GetIsPlaying());
    }

    public void ToggleControlPanel()
    {
        isControlPanelVisible = !isControlPanelVisible;

        if (controlPanel != null)
        {
            controlPanel.SetActive(isControlPanelVisible);
        }
    }

    public void UpdateTimeDisplay(string currentTime, string totalTime)
    {
        if (timeDisplay == null)
        {
            return;
        }

        timeDisplay.text = currentTime + " / " + totalTime;
    }

    public void UpdatePlayPauseButton(bool isPlaying)
    {
        if (playPauseButton == null)
        {
            return;
        }

        Text text = playPauseButton.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = isPlaying ? "⏸" : "▶";
        }
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }

    private void OnDestroy()
    {
        if (progressBar != null)
        {
            progressBar.onValueChanged.RemoveListener(OnProgressChanged);
        }
    }
}
