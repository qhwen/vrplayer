// <copyright file="VRVideoPlayer_Refactored.cs" company="VRPlayer">
// Copyright (c) 2025. All rights reserved.
// </copyright>

using UnityEngine;
using UnityEngine.Video;

namespace VRPlayer.Presentation.Player
{
    /// <summary>
    /// Refactored VR Video Player that uses the new architecture.
    /// This class is responsible for rendering and displaying VR videos.
    /// </summary>
    [AddComponentMenu("VR Player/VR Video Player (Refactored)")]
    public class VRVideoPlayer_Refactored : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Video Settings")]
        [SerializeField] private string defaultVideoPath = "";
        [SerializeField] private GameObject skySpherePrefab = null;

        [Header("Input Settings")]
        [SerializeField] private bool enableHeadTracking = true;
        [SerializeField] private float rotationSensitivity = 0.5f;
        [SerializeField] private float smoothingFactor = 0.1f;
        [SerializeField] private bool enablePointerDrag = true;
        [SerializeField] private float pointerDeltaScale = 1f;

        [Header("Rendering Settings")]
        [SerializeField] private int renderTextureWidth = 1920;
        [SerializeField] private int renderTextureHeight = 1080;

        #endregion

        #region Private Fields

        // Core Dependencies
        private Core.EventBus.IEventBus eventBus;
        private Core.Logging.ILogger logger;

        // Application Dependencies
        private Application.Playback.IPlaybackOrchestrator playbackOrchestrator;

        // Rendering
        private RenderTexture renderTexture;
        private Material videoMaterial;
        private GameObject skySphere;
        private Transform skySphereTransform;

        // VR State
        private float currentYaw;
        private float currentPitch;
        private float targetYaw;
        private float targetPitch;

        // Input State
        private Vector2 lastPointerPosition;
        private bool isPointerDragging;

        // State Flags
        private bool isInitialized = false;
        private bool isPreparing = false;
        private bool hasSource = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeDependencies();
        }

        private void Start()
        {
            InitializeVideoPlayer();
            SubscribeToEvents();

            if (!string.IsNullOrWhiteSpace(defaultVideoPath))
            {
                PlayVideo(defaultVideoPath);
            }
        }

        private void Update()
        {
            UpdateInput();
            UpdateVRState();
            UpdateRendering();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            Cleanup();
        }

        #endregion

        #region Initialization

        private void InitializeDependencies()
        {
            logger = Core.Logging.LoggerManager.GetLogger("VRVideoPlayer");
            eventBus = Core.EventBus.EventBus.Instance;
            playbackOrchestrator = FindObjectOfType<Application.Playback.PlaybackOrchestrator>();

            if (playbackOrchestrator == null)
            {
                logger.Error("PlaybackOrchestrator not found in scene");
                return;
            }
        }

        private void InitializeVideoPlayer()
        {
            if (skySpherePrefab != null)
            {
                skySphere = Instantiate(skySpherePrefab, Vector3.zero, Quaternion.identity);
                skySphere.name = "SkySphere";
                skySphereTransform = skySphere.transform;
            }
            else
            {
                logger.Warning("No sky sphere prefab assigned, creating default sphere");
                skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                skySphere.name = "SkySphere";
                skySphereTransform = skySphere.transform;

                // Scale and orient properly for VR
                skySphereTransform.localScale = new Vector3(1f, 1f, 1f) * 50f;
                skySphereTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }

            // Create render texture
            renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 24, RenderTextureFormat.ARGB32);
            renderTexture.name = "VideoRenderTexture";

            // Create material with render texture
            Shader shader = Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            videoMaterial = new Material(shader);
            videoMaterial.mainTexture = renderTexture;

            // Apply material to sky sphere
            Renderer sphereRenderer = skySphere.GetComponent<Renderer>();
            if (sphereRenderer != null)
            {
                sphereRenderer.material = videoMaterial;
            }

            isInitialized = true;
            logger.Info("VR Video Player initialized successfully");
        }

        private void SubscribeToEvents()
        {
            if (eventBus != null && playbackOrchestrator != null)
            {
                playbackOrchestrator.PlaybackStateChanged += OnPlaybackStateChanged;
                playbackOrchestrator.PlaybackUpdated += OnPlaybackUpdated;
                playbackOrchestrator.ErrorOccurred += OnErrorOccurred;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (eventBus != null && playbackOrchestrator != null)
            {
                playbackOrchestrator.PlaybackStateChanged -= OnPlaybackStateChanged;
                playbackOrchestrator.PlaybackUpdated -= OnPlaybackUpdated;
                playbackOrchestrator.ErrorOccurred -= OnErrorOccurred;
            }
        }

        #endregion

        #region Video Playback Methods

        /// <summary>
        /// Play a video from the specified path
        /// </summary>
        public void PlayVideo(string path)
        {
            if (playbackOrchestrator == null || string.IsNullOrWhiteSpace(path))
            {
                logger.Warning("Cannot play video: invalid parameters");
                return;
            }

            logger.Info($"Playing video: {path}");

            if (playbackOrchestrator.PreparePlayback(path))
            {
                playbackOrchestrator.StartPlayback();
                hasSource = true;
            }
            else
            {
                logger.Error($"Failed to prepare video: {path}");
            }
        }

        /// <summary>
        /// Pause the current video
        /// </summary>
        public void PauseVideo()
        {
            if (playbackOrchestrator != null)
            {
                playbackOrchestrator.PausePlayback();
                logger.Info("Playback paused");
            }
        }

        /// <summary>
        /// Resume the current video
        /// </summary>
        public void ResumeVideo()
        {
            if (playbackOrchestrator != null)
            {
                playbackOrchestrator.StartPlayback();
                logger.Info("Playback resumed");
            }
        }

        /// <summary>
        /// Stop the current video
        /// </summary>
        public void StopVideo()
        {
            if (playbackOrchestrator != null)
            {
                playbackOrchestrator.StopPlayback();
                hasSource = false;
                logger.Info("Playback stopped");
            }
        }

        /// <summary>
        /// Set the volume for the current video
        /// </summary>
        public void SetVolume(float volume)
        {
            if (playbackOrchestrator != null)
            {
                playbackOrchestrator.SetVolume(volume);
            }
        }

        /// <summary>
        /// Seek to a specific position in the video
        /// </summary>
        public void SeekTo(float seconds)
        {
            if (playbackOrchestrator != null)
            {
                playbackOrchestrator.Seek(seconds);
            }
        }

        #endregion

        #region VR Rotation Methods

        /// <summary>
        /// Set the VR rotation (yaw and pitch)
        /// </summary>
        public void SetVRRotation(float yaw, float pitch)
        {
            targetYaw = yaw;
            targetPitch = Mathf.Clamp(pitch, -90f, 90f);
        }

        /// <summary>
        /// Handle drag input for VR rotation
        /// </summary>
        public void OnDrag(float deltaX, float deltaY)
        {
            targetYaw += deltaX * rotationSensitivity;
            targetPitch += deltaY * rotationSensitivity;
            targetPitch = Mathf.Clamp(targetPitch, -90f, 90f);
        }

        #endregion

        #region Public Getters

        /// <summary>
        /// Get the current playback state
        /// </summary>
        public Application.Playback.PlaybackState GetPlaybackState()
        {
            if (playbackOrchestrator != null)
            {
                return playbackOrchestrator.State;
            }
            return Application.Playback.PlaybackState.Idle;
        }

        /// <summary>
        /// Get the current playback snapshot
        /// </summary>
        public Application.Playback.PlaybackSnapshot GetPlaybackSnapshot()
        {
            if (playbackOrchestrator != null)
            {
                return playbackOrchestrator.GetSnapshot();
            }
            return Application.Playback.PlaybackSnapshot.CreateDefault();
        }

        /// <summary>
        /// Get whether the player has a video source
        /// </summary>
        public bool HasSource()
        {
            return hasSource;
        }

        /// <summary>
        /// Get whether the player is initialized
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized;
        }

        #endregion

        #region Event Handlers

        private void OnPlaybackStateChanged(Application.Playback.PlaybackState state)
        {
            isPreparing = (state == Application.Playback.PlaybackState.Preparing);
            logger.Debug($"Playback state changed to: {state}");

            // Publish event to EventBus
            if (eventBus != null)
            {
                var stateChangedEvent = new Domain.Events.PlaybackStateChangedEvent(state);
                eventBus.Publish(stateChangedEvent);
            }
        }

        private void OnPlaybackUpdated(Application.Playback.PlaybackSnapshot snapshot)
        {
            // Update rendering with new frame
            UpdateVideoTexture();
        }

        private void OnErrorOccurred(Application.Playback.PlaybackError error)
        {
            logger.Error($"Playback error: {error.Message} (Code: {error.Code})");

            // Publish error event to EventBus
            if (eventBus != null)
            {
                var errorEvent = new Domain.Events.PlaybackErrorEvent(error);
                eventBus.Publish(errorEvent);
            }
        }

        #endregion

        #region Update Methods

        private void UpdateInput()
        {
            if (enablePointerDrag)
            {
                HandlePointerDrag();
            }
        }

        private void UpdateVRState()
        {
            if (enableHeadTracking)
            {
                SmoothHeadTracking();
            }

            ApplyVRRotation();
        }

        private void UpdateRendering()
        {
            if (playbackOrchestrator == null || renderTexture == null)
            {
                return;
            }

            // Get current video texture from playback orchestrator
            Texture currentTexture = playbackOrchestrator.GetCurrentTexture();
            if (currentTexture != null)
            {
                // Update video material
                videoMaterial.mainTexture = currentTexture;
            }
        }

        private void UpdateVideoTexture()
        {
            if (playbackOrchestrator == null || renderTexture == null)
            {
                return;
            }

            // Copy video frame to render texture
            Texture sourceTexture = playbackOrchestrator.GetCurrentTexture();
            if (sourceTexture != null)
            {
                Graphics.Blit(sourceTexture, renderTexture);
            }
        }

        #endregion

        #region VR State Methods

        private void SmoothHeadTracking()
        {
            // Smooth yaw and pitch values using lerp
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, smoothingFactor);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, smoothingFactor);
        }

        private void ApplyVRRotation()
        {
            if (skySphereTransform == null)
            {
                return;
            }

            // Apply rotation to sky sphere
            // In VR, we rotate the camera, but here we simulate it by rotating the sphere in opposite direction
            skySphereTransform.rotation = Quaternion.Euler(currentPitch, -currentYaw, 0f);
        }

        #endregion

        #region Input Handling

        private void HandlePointerDrag()
        {
            // Handle touch input
            if (Input.touchCount > 0)
            {
                HandleTouchInput();
            }
            // Handle mouse input
            else
            {
                HandleMouseInput();
            }
        }

        private void HandleTouchInput()
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                isPointerDragging = true;
                lastPointerPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isPointerDragging = false;
            }

            if (isPointerDragging && (touch.phase == TouchPhase.Moved))
            {
                Vector2 currentPosition = touch.position;
                Vector2 delta = currentPosition - lastPointerPosition;

                OnDrag(delta.x * pointerDeltaScale, delta.y * pointerDeltaScale);

                lastPointerPosition = currentPosition;
            }
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                isPointerDragging = true;
                lastPointerPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isPointerDragging = false;
            }

            if (isPointerDragging && Input.GetMouseButton(0))
            {
                Vector2 currentPosition = Input.mousePosition;
                Vector2 delta = currentPosition - lastPointerPosition;

                OnDrag(delta.x * pointerDeltaScale, delta.y * pointerDeltaScale);

                lastPointerPosition = currentPosition;
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Cleanup render texture
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }

            // Cleanup material
            if (videoMaterial != null)
            {
                Destroy(videoMaterial);
                videoMaterial = null;
            }

            // Cleanup sky sphere
            if (skySphere != null)
            {
                Destroy(skySphere);
                skySphere = null;
                skySphereTransform = null;
            }

            logger.Info("VR Video Player cleaned up");
        }

        #endregion
    }
}
