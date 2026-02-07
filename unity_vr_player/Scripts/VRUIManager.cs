using UnityEngine;
using UnityEngine.UI;

/// VR UI管理器
public class VRUIManager : MonoBehaviour
{
    [Header("UI设置")]
    [SerializeField] private Font uiFont;
    [SerializeField] private GameObject controlPanel;
    [SerializeField] private UnityEngine.UI.Button playPauseButton;
    [SerializeField] private UnityEngine.UI.Button stopButton;
    [SerializeField] private Slider progressBar;
    [SerializeField] private UnityEngine.UI.Text timeDisplay;
    [SerializeField] private UnityEngine.UI.Button volumeUpButton;
    [SerializeField] private UnityEngine.UI.Button volumeDownButton;
    
    private VRVideoPlayer videoPlayer;
    private Canvas canvas;
    private bool isControlPanelVisible = true;
    private RectTransform controlPanelRect;
    
    void Start()
    {
        // 获取视频播放器
        videoPlayer = FindObjectOfType<VRVideoPlayer>();
        
        // 获取Canvas
        canvas = FindObjectOfType<Canvas>();
        
        if (controlPanel != null)
        {
            CreateDefaultControlPanel();
        }
        
        // 设置按钮事件
        SetupButtonEvents();
        
        // 设置滑块
        if (progressBar != null)
        {
            progressBar.onValueChanged.AddListener(OnProgressChanged);
            progressBar.wholeNumbers = false;
        }
    }
    
    /// 创建默认控制面板
    private void CreateDefaultControlPanel()
    {
        // 创建控制面板
        controlPanel = new GameObject("ControlPanel");
        controlPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = controlPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0, 0);
        panelRect.pivot = new Vector2(0, 0);
        panelRect.anchoredPosition = new Vector2(50, -400);
        panelRect.sizeDelta = new Vector2(300, 350);
        
        // 创建背景
        Image panelBackground = controlPanel.AddComponent<Image>();
        panelBackground.color = new Color(0, 0, 0, 0.8f);
        
        // 创建播放/暂停按钮
        CreateButton("PlayPauseButton", "▶/⏸", -100, 100);
        
        // 创建停止按钮
        CreateButton("StopButton", "■", 100, 100);
        
        // 创建进度条
        CreateProgressBar(0, 0);
        
        // 创建时间显示
        CreateText("TimeDisplay", "00:00 / 00:00", 0, -60);
        
        // 创建音量控制
        CreateButton("VolumeUpButton", "+", -80, -100);
        CreateButton("VolumeDownButton", "-", -80, -160);
        
        // 创建隐藏/显示按钮
        CreateToggleUIButton("ToggleUI", "显示/隐藏", 100, -160);
    }
    
    /// 创建按钮
    private void CreateButton(string name, string text, float x, float y)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(controlPanel.transform, false);
        
        UnityEngine.UI.Button button = buttonGO.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonGO.AddComponent<Image>();
        
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(80, 40);
        
        Text buttonText = buttonGO.AddComponent<Text>();
        buttonText.font = uiFont;
        buttonText.fontSize = 20;
        buttonText.text = text;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
        
        // 设置引用
        if (name == "PlayPauseButton") playPauseButton = button;
        if (name == "StopButton") stopButton = button;
        if (name == "VolumeUpButton") volumeUpButton = button;
        if (name == "VolumeDownButton") volumeDownButton = button;
    }
    
    /// 创建进度条
    private void CreateProgressBar(float x, float y)
    {
        GameObject sliderGO = new GameObject("ProgressBar");
        sliderGO.transform.SetParent(controlPanel.transform, false);
        
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 0f;
        
        RectTransform rect = sliderGO.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(200, 20);
        
        // 创建背景
        GameObject bgGO = new GameObject("ProgressBarBackground");
        bgGO.transform.SetParent(sliderGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.2f);
        
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(1, 0.5f);
        bgRect.sizeDelta = new Vector2(0, 6);
        bgRect.anchoredPosition = new Vector2(0, 0);
    }
    
    /// 创建文本
    private void CreateText(string name, string text, float x, float y)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(controlPanel.transform, false);
        
        Text textComponent = textGO.AddComponent<Text>();
        textComponent.font = uiFont;
        textComponent.fontSize = 18;
        textComponent.text = text;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.color = Color.white;
        
        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(200, 30);
        
        if (name == "TimeDisplay") timeDisplay = textComponent;
    }
    
    /// 创建切换UI按钮
    private void CreateToggleUIButton(string name, string text, float x, float y)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(controlPanel.transform, false);
        
        UnityEngine.UI.Button button = buttonGO.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonGO.AddComponent<Image>();
        
        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(120, 30);
        
        Text buttonText = buttonGO.AddComponent<Text>();
        buttonText.font = uiFont;
        buttonText.fontSize = 16;
        buttonText.text = text;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
    }
    
    /// 设置按钮事件
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
    
    /// 播放/暂停点击
    private void OnPlayPauseClick()
    {
        if (videoPlayer != null) return;
        
        if (videoPlayer.GetIsPlaying())
        {
            videoPlayer.PauseVideo();
        }
        else
        {
            videoPlayer.ResumeVideo();
        }
    }
    
    /// 停止点击
    private void OnStopClick()
    {
        if (videoPlayer != null) return;
        videoPlayer.StopVideo();
    }
    
    /// 音量增加
    private void OnVolumeUpClick()
    {
        Debug.Log("音量增加（功能待实现）");
    }
    
    /// 音量减少
    private void OnVolumeDownClick()
    {
        Debug.Log("音量减少（功能待实现）");
    }
    
    /// 进度条变化
    private void OnProgressChanged(float value)
    {
        if (videoPlayer == null || !videoPlayer.GetIsInitialized()) return;
        
        // 计算视频时长（假设）
        float videoDuration = 100f; // TODO: 从视频播放器获取实际时长
        
        float targetTime = (value / 100f) * videoDuration;
        videoPlayer.SeekTo(targetTime);
    }
    
    void Update()
    {
        // 更新进度条
        if (progressBar != null && videoPlayer != null && videoPlayer.GetIsInitialized())
        {
            // TODO: 从视频播放器获取实际进度
            // progressBar.value = (videoPlayer.currentTime / videoPlayer.duration) * 100;
        }
    }
    
    /// 切换控制面板显示
    public void ToggleControlPanel()
    {
        isControlPanelVisible = !isControlPanelVisible;
        
        if (controlPanel != null)
        {
            controlPanel.SetActive(isControlPanelVisible);
        }
    }
    
    /// 更新时间显示
    public void UpdateTimeDisplay(string currentTime, string totalTime)
    {
        if (timeDisplay != null) return;
        timeDisplay.text = currentTime + " / " + totalTime;
    }
    
    /// 更新播放状态文本
    public void UpdatePlayPauseButton(bool isPlaying)
    {
        if (playPauseButton == null) return;
        
        Text text = playPauseButton.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = isPlaying ? "⏸" : "▶";
        }
    }
    
    void OnDestroy()
    {
        if (progressBar != null)
        {
            progressBar.onValueChanged.RemoveListener(OnProgressChanged);
        }
    }
}
