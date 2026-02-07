using UnityEngine;
using UnityEngine.Video;

/// VR视频播放器主控制器
public class VRVideoPlayer : MonoBehaviour
{
    [Header("视频播放器设置")]
    [SerializeField] private string defaultVideoPath = "";
    [SerializeField] private GameObject skySpherePrefab;
    [SerializeField] private bool enableHeadTracking = true;
    [SerializeField] private float rotationSensitivity = 0.5f;
    [SerializeField] private float smoothingFactor = 0.1f;
    
    // 视频播放器
    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private Material videoMaterial;
    private bool isPlaying = false;
    private bool isInitialized = false;
    
    // VR 头部追踪
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float targetYaw = 0f;
    private float targetPitch = 0f;
    
    // 360° 球体
    private GameObject skySphere;
    private Transform skySphereTransform;
    
    // UI
    private GameObject canvas;
    private UnityEngine.UI.Text playbackStatusText;
    private UnityEngine.UI.Text timeText;
    
    void Awake()
    {
        // 创建视频播放器
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.url = defaultVideoPath;
        
        // 创建天空球体
        if (skySpherePrefab != null)
        {
            CreateDefaultSkySphere();
        }
        else
        {
            skySphere = Instantiate(skySpherePrefab, Vector3.zero, Quaternion.identity);
        }
        
        skySphereTransform = skySphere.transform;
        skySphereTransform.localScale = new Vector3(-1, 1, 1); // 反转X轴使视频正确显示
        skySphere.layer = LayerMask.NameToLayer("Default");
        
        // 创建渲染纹理
        renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        videoMaterial = new Material(Shader.Find("Standard"));
        videoMaterial.mainTexture = renderTexture;
        
        // 应用材质到球体
        Renderer sphereRenderer = skySphere.GetComponent<Renderer>();
        sphereRenderer.material = videoMaterial;
        
        // 创建UI Canvas
        CreateUI();
    }
    
    void Start()
    {
        // 准备视频播放器
        videoPlayer.prepare();
    }
    
    void Update()
    {
        // 更新视频纹理
        if (videoPlayer.isPlaying && videoPlayer.texture != null)
        {
            Graphics.Blit(videoPlayer.texture, renderTexture);
        }
        
        // 平滑头部追踪
        if (enableHeadTracking)
        {
            SmoothHeadTracking();
        }
        
        // 更新VR视角
        ApplyVRRotation();
        
        // 更新UI
        UpdateUI();
    }
    
    /// 播放视频
    public void PlayVideo(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        
        // 停止当前视频
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        // 加载新视频
        videoPlayer.url = path;
        videoPlayer.prepare();
        
        StartCoroutine(PlayWhenReady());
    }
    
    /// 准备好后播放
    private IEnumerator PlayWhenReady()
    {
        while (!videoPlayer.isPrepared)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        videoPlayer.Play();
        isPlaying = true;
        isInitialized = true;
        Debug.Log("视频开始播放: " + videoPlayer.url);
    }
    
    /// 暂停视频
    public void PauseVideo()
    {
        videoPlayer.Pause();
        isPlaying = false;
    }
    
    /// 恢复播放
    public void ResumeVideo()
    {
        videoPlayer.Play();
        isPlaying = true;
    }
    
    /// 停止视频
    public void StopVideo()
    {
        videoPlayer.Stop();
        isPlaying = false;
    }
    
    /// 设置音量
    public void SetVolume(float volume)
    {
        videoPlayer.SetDirectAudioVolume(0, volume);
        videoPlayer.SetDirectAudioVolume(1, volume);
    }
    
    /// 跳转到指定时间（秒）
    public void SeekTo(float seconds)
    {
        videoPlayer.frame = (long)(seconds * videoPlayer.frameRate);
    }
    
    /// 设置VR旋转角度
    public void SetVRRotation(float yaw, float pitch)
    {
        targetYaw = yaw;
        targetPitch = Mathf.Clamp(pitch, -90f, 90f);
    }
    
    /// 平滑头部追踪
    private void SmoothHeadTracking()
    {
        // 简单的线性插值
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, smoothingFactor);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, smoothingFactor);
    }
    
    /// 应用VR旋转
    private void ApplyVRRotation()
    {
        if (skySphereTransform != null) return;
        
        // 应用旋转：偏航角（左右）+ 俯仰角（上下）
        skySphereTransform.rotation = Quaternion.Euler(
            currentPitch,    // 上下倾斜
            -currentYaw,   // 左右旋转（负号因为Unity的坐标系）
            0f
        );
    }
    
    /// 手势控制：拖动旋转视角
    public void OnDrag(float deltaX, float deltaY)
    {
        targetYaw += deltaX * rotationSensitivity;
        targetPitch = Mathf.Clamp(targetPitch - deltaY * rotationSensitivity, -90f, 90f);
    }
    
    /// 手势控制：点击暂停/播放
    public void OnTap()
    {
        if (isPlaying)
        {
            PauseVideo();
        }
        else
        {
            ResumeVideo();
        }
    }
    
    /// 创建默认天空球体
    private void CreateDefaultSkySphere()
    {
        // 创建球体几何体
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        skySphere = sphere;
        skySphereTransform = sphere.transform;
        skySphere.name = "SkySphere";
        
        // 缩放到合适大小（半径50单位）
        skySphereTransform.localScale = new Vector3(-1, 1, 1);
        skySphereTransform.localScale *= 50;
    }
    
    /// 创建UI
    private void CreateUI()
    {
        // 创建Canvas
        GameObject uiGO = new GameObject("UICanvas");
        canvas = uiGO;
        canvas.AddComponent<UnityEngine.UI.Canvas>();
        UnityEngine.UI.Canvas uiCanvas = canvas.GetComponent<UnityEngine.UI.Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // 创建状态文本
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(canvas.transform, false);
        playbackStatusText = statusGO.AddComponent<UnityEngine.UI.Text>();
        playbackStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playbackStatusText.alignment = TextAnchor.MiddleCenter;
        playbackStatusText.fontSize = 24;
        playbackStatusText.text = "准备就绪";
        
        // 创建时间文本
        GameObject timeGO = new GameObject("TimeText");
        timeGO.transform.SetParent(canvas.transform, false);
        timeText = timeGO.AddComponent<UnityEngine.UI.Text>();
        timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        timeText.alignment = TextAnchor.MiddleCenter;
        timeText.fontSize = 18;
        timeText.text = "00:00 / 00:00";
        
        // 定位文本
        RectTransform statusRect = statusGO.GetComponent<RectTransform>();
        RectTransform timeRect = timeGO.GetComponent<RectTransform>();
        
        statusRect.anchoredPosition = new Vector2(0, 300);
        timeRect.anchoredPosition = new Vector2(0, 250);
    }
    
    /// 更新UI
    private void UpdateUI()
    {
        if (!isInitialized)
        {
            playbackStatusText.text = "加载中...";
            return;
        }
        
        if (isPlaying)
        {
            playbackStatusText.text = "播放中";
            playbackStatusText.color = Color.green;
        }
        else
        {
            playbackStatusText.text = "已暂停";
            playbackStatusText.color = Color.yellow;
        }
        
        // 显示当前时间/总时间
        if (videoPlayer.isPlaying)
        {
            float currentTime = (float)videoPlayer.time;
            float duration = videoPlayer.length;
            timeText.text = string.Format("{0:00}:{1:00} / {2:00}:{3:00}",
                (int)(currentTime / 60),
                (int)(currentTime % 60),
                (int)(duration / 60),
                (int)(duration % 60));
        }
        else
        {
            timeText.text = "00:00 / " + FormatTime(videoPlayer.length);
        }
    }
    
    /// 格式化时间
    private string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }
    
    void OnDestroy()
    {
        // 清理资源
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
        if (videoMaterial != null)
        {
            Destroy(videoMaterial);
        }
        if (skySphere != null && skySphere != skySpherePrefab)
        {
            Destroy(skySphere);
        }
        if (canvas != null)
        {
            Destroy(canvas);
        }
    }
    
    /// 获取视频播放器（用于外部访问）
    public VideoPlayer GetVideoPlayer()
    {
        return videoPlayer;
    }
    
    /// 获取播放状态
    public bool GetIsPlaying()
    {
        return isPlaying;
    }
    
    /// 获取初始化状态
    public bool GetIsInitialized()
    {
        return isInitialized;
    }
}
