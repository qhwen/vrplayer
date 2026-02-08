using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;

/// <summary>
/// VR rendering and interaction layer. Playback is delegated to IPlaybackService.
/// </summary>
public class VRVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [SerializeField] private string defaultVideoPath = "";
    [SerializeField] private GameObject skySpherePrefab;
    [SerializeField, Range(512, 4096)] private int renderTextureWidth = 1920;
    [SerializeField, Range(512, 4096)] private int renderTextureHeight = 1080;

    [Header("Input Settings")]
    [SerializeField] private bool enableHeadTracking = true;
    [SerializeField] private float rotationSensitivity = 0.5f;
    [SerializeField, Range(0.01f, 1f)] private float smoothingFactor = 0.1f;
    [SerializeField] private bool enablePointerDrag = true;
    [SerializeField] private float pointerDeltaScale = 1f;

    private IPlaybackService playbackService;

    private RenderTexture renderTexture;
    private Material videoMaterial;

    private GameObject skySphere;
    private Transform skySphereTransform;

    private float currentYaw;
    private float currentPitch;
    private float targetYaw;
    private float targetPitch;

    private Vector2 lastPointerPosition;
    private bool isPointerDragging;

    private static readonly List<RaycastResult> UIRaycastResults = new List<RaycastResult>(8);

    private void Awake()
    {
        EnsurePlaybackService();
        InitializeRenderPipeline();
    }

    private void Start()
    {
        if (!string.IsNullOrWhiteSpace(defaultVideoPath))
        {
            PlayVideo(defaultVideoPath);
        }
    }

    private void Update()
    {
        UpdateVideoTexture();

        if (enablePointerDrag)
        {
            HandlePointerDrag();
        }

        if (enableHeadTracking)
        {
            SmoothHeadTracking();
        }

        ApplyVRRotation();
    }

    public void PlayVideo(string path)
    {
        if (playbackService == null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (playbackService.Open(path))
        {
            playbackService.Play();
        }
    }

    public void PauseVideo()
    {
        playbackService?.Pause();
    }

    public void ResumeVideo()
    {
        playbackService?.Play();
    }

    public void StopVideo()
    {
        playbackService?.Stop();
    }

    public void SetVolume(float volume)
    {
        playbackService?.SetVolume(volume);
    }

    public void SeekTo(float seconds)
    {
        playbackService?.Seek(seconds);
    }

    public void SetVRRotation(float yaw, float pitch)
    {
        targetYaw = yaw;
        targetPitch = Mathf.Clamp(pitch, -90f, 90f);
    }

    public void OnDrag(float deltaX, float deltaY)
    {
        targetYaw += deltaX * rotationSensitivity;
        targetPitch = Mathf.Clamp(targetPitch - deltaY * rotationSensitivity, -90f, 90f);
    }

    public IPlaybackService GetPlaybackService()
    {
        return playbackService;
    }

    public VideoPlayer GetVideoPlayer()
    {
        return playbackService != null ? playbackService.GetNativePlayer() : null;
    }

    public bool GetIsPlaying()
    {
        return playbackService != null && playbackService.State == PlaybackState.Playing;
    }

    public bool GetIsInitialized()
    {
        if (playbackService == null)
        {
            return false;
        }

        PlaybackState state = playbackService.State;
        return state == PlaybackState.Ready || state == PlaybackState.Playing || state == PlaybackState.Paused;
    }

    public bool GetIsPreparing()
    {
        return playbackService != null && playbackService.State == PlaybackState.Preparing;
    }

    public bool GetHasVideoSource()
    {
        return playbackService != null && playbackService.HasSource;
    }

    public string GetLastErrorMessage()
    {
        if (playbackService == null)
        {
            return string.Empty;
        }

        return playbackService.LastError.message;
    }

    public string GetCurrentVideoUrl()
    {
        if (playbackService == null)
        {
            return string.Empty;
        }

        return playbackService.CurrentSource;
    }

    private void EnsurePlaybackService()
    {
        UnityVideoPlaybackService nativeService = GetComponent<UnityVideoPlaybackService>();
        if (nativeService == null)
        {
            nativeService = gameObject.AddComponent<UnityVideoPlaybackService>();
        }

        playbackService = nativeService;
    }

    private void InitializeRenderPipeline()
    {
        if (skySpherePrefab != null)
        {
            skySphere = Instantiate(skySpherePrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skySphere.name = "SkySphere";
        }

        if (skySphere == null)
        {
            Debug.LogError("Sky sphere creation failed. Player is disabled.");
            enabled = false;
            return;
        }

        skySphereTransform = skySphere.transform;
        skySphereTransform.localScale = new Vector3(-1f, 1f, 1f) * 50f;

        renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24, RenderTextureFormat.ARGB32);

        Shader shader = Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        videoMaterial = new Material(shader)
        {
            mainTexture = renderTexture
        };

        Renderer sphereRenderer = skySphere.GetComponent<Renderer>();
        if (sphereRenderer != null)
        {
            sphereRenderer.material = videoMaterial;
        }
    }

    private void UpdateVideoTexture()
    {
        if (playbackService == null || renderTexture == null)
        {
            return;
        }

        Texture sourceTexture = playbackService.CurrentTexture;
        if (sourceTexture == null)
        {
            return;
        }

        Graphics.Blit(sourceTexture, renderTexture);
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

            if (IsPointerOverUI(touch.fingerId, touch.position))
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
            Vector2 mousePosition = Input.mousePosition;

            if (IsPointerOverUI(-1, mousePosition))
            {
                lastPointerPosition = mousePosition;
                return;
            }

            Vector2 delta = mousePosition - lastPointerPosition;
            lastPointerPosition = mousePosition;
            OnDrag(delta.x * pointerDeltaScale, delta.y * pointerDeltaScale);
        }
    }

    private static bool IsPointerOverUI(int pointerId, Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        if (pointerId >= 0 && EventSystem.current.IsPointerOverGameObject(pointerId))
        {
            return true;
        }

        if (pointerId < 0 && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        UIRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, UIRaycastResults);
        return UIRaycastResults.Count > 0;
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

        skySphereTransform.rotation = Quaternion.Euler(currentPitch, -currentYaw, 0f);
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (videoMaterial != null)
        {
            Destroy(videoMaterial);
        }

        if (skySphere != null)
        {
            Destroy(skySphere);
        }
    }
}
