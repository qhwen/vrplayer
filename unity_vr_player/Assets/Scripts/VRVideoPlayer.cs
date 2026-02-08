using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// VR video player controller.
/// </summary>
public class VRVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private string defaultVideoPath = "";
    [SerializeField] private GameObject skySpherePrefab;
    [SerializeField] private bool enableHeadTracking = true;
    [SerializeField] private float rotationSensitivity = 0.5f;
    [SerializeField, Range(0.01f, 1f)] private float smoothingFactor = 0.1f;

    [Header("Input Settings")]
    [SerializeField] private bool enablePointerDrag = true;
    [SerializeField] private float pointerDeltaScale = 1f;

    private VideoPlayer videoPlayer;
    private RenderTexture renderTexture;
    private Material videoMaterial;

    private bool isPlaying;
    private bool isInitialized;
    private bool hasVideoSource;

    private float currentYaw;
    private float currentPitch;
    private float targetYaw;
    private float targetPitch;

    private GameObject skySphere;
    private Transform skySphereTransform;

    private Canvas uiCanvas;
    private Text playbackStatusText;
    private Text timeText;

    private Vector2 lastPointerPosition;
    private bool isPointerDragging;

    private void Awake()
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.prepareCompleted += OnPrepareCompleted;

        if (!string.IsNullOrWhiteSpace(defaultVideoPath))
        {
            videoPlayer.url = defaultVideoPath;
            hasVideoSource = true;
        }

        if (skySpherePrefab != null)
        {
            skySphere = Instantiate(skySpherePrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            CreateDefaultSkySphere();
        }

        if (skySphere == null)
        {
            Debug.LogError("Sky sphere creation failed. Player is disabled.");
            enabled = false;
            return;
        }

        skySphereTransform = skySphere.transform;
        skySphereTransform.localScale = new Vector3(-1f, 1f, 1f) * 50f;

        renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);

        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        videoMaterial = new Material(shader);
        videoMaterial.mainTexture = renderTexture;

        Renderer sphereRenderer = skySphere.GetComponent<Renderer>();
        if (sphereRenderer != null)
        {
            sphereRenderer.material = videoMaterial;
        }

        CreateUI();
    }

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(defaultVideoPath))
        {
            videoPlayer.Prepare();
        }
    }

    private void Update()
    {
        if (videoPlayer != null && videoPlayer.isPlaying && videoPlayer.texture != null && renderTexture != null)
        {
            Graphics.Blit(videoPlayer.texture, renderTexture);
        }

        if (enablePointerDrag)
        {
            HandlePointerDrag();
        }

        if (enableHeadTracking)
        {
            SmoothHeadTracking();
        }

        ApplyVRRotation();
        UpdateUI();
    }

    public void PlayVideo(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || videoPlayer == null)
        {
            return;
        }

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        string finalPath = path.Trim();
        if (finalPath.StartsWith("file://") && !finalPath.StartsWith("file:///"))
        {
            finalPath = "file:///" + finalPath.Substring("file://".Length).TrimStart('/');
        }

        hasVideoSource = true;
        isInitialized = false;
        videoPlayer.url = finalPath;
        videoPlayer.Prepare();

        Debug.Log("Prepare video: " + finalPath);
    }

    public void PauseVideo()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.Pause();
        isPlaying = false;
    }

    public void ResumeVideo()
    {
        if (videoPlayer == null)
        {
            return;
        }

        if (!isInitialized)
        {
            if (hasVideoSource && !string.IsNullOrWhiteSpace(videoPlayer.url))
            {
                videoPlayer.Prepare();
            }

            return;
        }

        videoPlayer.Play();
        isPlaying = true;
    }

    public void StopVideo()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.Stop();
        isPlaying = false;
        isInitialized = false;
    }

    public void SetVolume(float volume)
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.SetDirectAudioVolume(0, Mathf.Clamp01(volume));
    }

    public void SeekTo(float seconds)
    {
        if (videoPlayer == null || !videoPlayer.canSetTime)
        {
            return;
        }

        videoPlayer.time = Mathf.Max(0f, seconds);
    }

    public void SetVRRotation(float yaw, float pitch)
    {
        targetYaw = yaw;
        targetPitch = Mathf.Clamp(pitch, -90f, 90f);
    }

    private void HandlePointerDrag()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                isPointerDragging = true;
                lastPointerPosition = touch.position;
                return;
            }

            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isPointerDragging = false;
                return;
            }

            if (!isPointerDragging)
            {
                return;
            }

            if (IsPointerOverUI(touch.fingerId))
            {
                lastPointerPosition = touch.position;
                return;
            }

            Vector2 delta = touch.position - lastPointerPosition;
            lastPointerPosition = touch.position;

            OnDrag(delta.x * pointerDeltaScale, delta.y * pointerDeltaScale);
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isPointerDragging = true;
            lastPointerPosition = Input.mousePosition;
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isPointerDragging = false;
            return;
        }

        if (isPointerDragging && Input.GetMouseButton(0))
        {
            if (IsPointerOverUI(-1))
            {
                lastPointerPosition = Input.mousePosition;
                return;
            }

            Vector2 mousePosition = Input.mousePosition;
            Vector2 delta = mousePosition - lastPointerPosition;
            lastPointerPosition = mousePosition;

            OnDrag(delta.x * pointerDeltaScale, delta.y * pointerDeltaScale);
        }
    }

    private static bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (pointerId >= 0)
        {
            return EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void SmoothHeadTracking()
    {
        currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, smoothingFactor);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, smoothingFactor);
    }

    private void ApplyVRRotation()
    {
        if (skySphereTransform == null)
        {
            return;
        }

        skySphereTransform.rotation = Quaternion.Euler(
            currentPitch,
            -currentYaw,
            0f
        );
    }

    public void OnDrag(float deltaX, float deltaY)
    {
        targetYaw += deltaX * rotationSensitivity;
        targetPitch = Mathf.Clamp(targetPitch - deltaY * rotationSensitivity, -90f, 90f);
    }

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

    private void CreateDefaultSkySphere()
    {
        skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        skySphere.name = "SkySphere";
    }

    private void CreateUI()
    {
        GameObject canvasObject = new GameObject("UICanvas");
        uiCanvas = canvasObject.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        playbackStatusText = CreateText("StatusText", "Waiting for video selection", new Vector2(0f, 300f), 24, font);
        timeText = CreateText("TimeText", "00:00 / 00:00", new Vector2(0f, 250f), 18, font);
    }

    private Text CreateText(string name, string content, Vector2 anchoredPosition, int fontSize, Font font)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(uiCanvas.transform, false);

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = content;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(600f, 40f);
        rect.anchoredPosition = anchoredPosition;

        return text;
    }

    private void UpdateUI()
    {
        if (playbackStatusText == null || timeText == null || videoPlayer == null)
        {
            return;
        }

        if (!hasVideoSource)
        {
            playbackStatusText.text = "Choose a video from the right panel";
            playbackStatusText.color = Color.white;
            return;
        }

        if (!isInitialized)
        {
            playbackStatusText.text = "Loading...";
            playbackStatusText.color = Color.cyan;
            return;
        }

        if (isPlaying)
        {
            playbackStatusText.text = "Playing";
            playbackStatusText.color = Color.green;
        }
        else
        {
            playbackStatusText.text = "Paused";
            playbackStatusText.color = Color.yellow;
        }

        float currentTime = (float)videoPlayer.time;
        double durationRaw = videoPlayer.length;
        float duration = durationRaw > 0 ? (float)durationRaw : 0f;

        timeText.text = string.Format(
            "{0} / {1}",
            FormatTime(currentTime),
            FormatTime(duration)
        );
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int minutes = totalSeconds / 60;
        int secs = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, secs);
    }

    private void OnPrepareCompleted(VideoPlayer source)
    {
        if (source == null)
        {
            return;
        }

        source.Play();
        isPlaying = true;
        isInitialized = true;

        Debug.Log("Video started: " + source.url);
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        isPlaying = false;
        isInitialized = false;
        Debug.LogError("Video playback error: " + message);
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepareCompleted;
            videoPlayer.errorReceived -= OnVideoError;
        }

        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (videoMaterial != null)
        {
            Destroy(videoMaterial);
        }

        if (skySphere != null && skySphere != skySpherePrefab)
        {
            Destroy(skySphere);
        }

        if (uiCanvas != null)
        {
            Destroy(uiCanvas.gameObject);
        }
    }

    public VideoPlayer GetVideoPlayer()
    {
        return videoPlayer;
    }

    public bool GetIsPlaying()
    {
        return isPlaying;
    }

    public bool GetIsInitialized()
    {
        return isInitialized;
    }
}

