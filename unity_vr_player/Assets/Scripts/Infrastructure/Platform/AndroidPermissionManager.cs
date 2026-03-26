using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace VRPlayer.Infrastructure.Platform
{
    /// <summary>
    /// Android 权限管理器实现
    /// 负责请求和管理 Android 运行时权限
    /// </summary>
    public class AndroidPermissionManager : MonoBehaviour, IPermissionManager
    {
        private const string AndroidPermissionReadMediaVideo = "android.permission.READ_MEDIA_VIDEO";
        private const string AndroidPermissionReadMediaVisualUserSelected = "android.permission.READ_MEDIA_VISUAL_USER_SELECTED";
        private const string AndroidPermissionReadExternalStorage = "android.permission.READ_EXTERNAL_STORAGE";

        private bool permissionRequestInFlight;
        private bool lastPermissionRequestDenied;
        private bool lastPermissionRequestDontAskAgain;

        private VRPlayer.Core.Logging.ILogger logger;

        public event Action<bool> OnPermissionResult;
        public event Action OnPermissionRequestStarted;

        private void Awake()
        {
            logger = VRPlayer.Core.Logging.LoggerManager.For("AndroidPermissionManager");
            logger.Info("AndroidPermissionManager initialized");
        }

        /// <summary>
        /// 检查权限请求是否正在进行
        /// </summary>
        public bool IsPermissionRequestInFlight()
        {
            return permissionRequestInFlight;
        }

        /// <summary>
        /// 检查上次权限请求是否被拒绝
        /// </summary>
        public bool WasLastPermissionRequestDenied()
        {
            return lastPermissionRequestDenied;
        }

        /// <summary>
        /// 检查上次权限请求是否被拒绝且选择了"不再询问"
        /// </summary>
        public bool WasLastPermissionRequestDontAskAgain()
        {
            return lastPermissionRequestDontAskAgain;
        }

        /// <summary>
        /// 检查是否有可读媒体权限
        /// </summary>
        public bool HasReadableMediaPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return HasAnyReadableMediaPermission();
#else
            logger.Debug("非 Android 平台，默认返回 true");
            return true;
#endif
        }

        /// <summary>
        /// 检查是否有 Movies 目录扫描权限
        /// </summary>
        public bool HasMoviesScanPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return HasMoviesDirectoryPermission();
#else
            logger.Debug("非 Android 平台，默认返回 true");
            return true;
#endif
        }

        /// <summary>
        /// 请求可读媒体权限
        /// </summary>
        public void RequestReadableMediaPermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (permissionRequestInFlight)
            {
                logger.Warning("权限请求已在进行中");
                return;
            }

            StartCoroutine(RequestReadableMediaPermissionRoutine());
#else
            logger.Debug("非 Android 平台，跳过权限请求");
            OnPermissionResult?.Invoke(true);
#endif
        }

        /// <summary>
        /// 打开应用权限设置
        /// </summary>
        public void OpenAppPermissionSettings()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                string packageName = activity.Call<string>("getPackageName");

                AndroidJavaClass settingsClass = new AndroidJavaClass("android.provider.Settings");
                AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
                AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("fromParts", "package", packageName, null);

                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", settingsClass.GetStatic<string>("ACTION_APPLICATION_DETAILS_SETTINGS"));
                intent.Call<AndroidJavaObject>("setData", uri);
                activity.Call("startActivity", intent);

                logger.Info("已打开应用权限设置");
            }
            catch (Exception e)
            {
                logger.Error($"打开应用权限设置失败: {e.Message}", e);
            }
#else
            logger.Debug("非 Android 平台，跳过打开权限设置");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// 请求可读媒体权限的协程
        /// </summary>
        private IEnumerator RequestReadableMediaPermissionRoutine()
        {
            permissionRequestInFlight = true;
            lastPermissionRequestDenied = false;
            lastPermissionRequestDontAskAgain = false;

            OnPermissionRequestStarted?.Invoke();
            logger.Info("开始请求可读媒体权限");

            // 等待应用获得焦点
            float focusTimeout = 5f;
            while (!Application.isFocused && focusTimeout > 0f)
            {
                focusTimeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            // 允许 UI 框架稳定
            yield return null;

            // 检查是否已经有权限
            if (HasAnyReadableMediaPermission())
            {
                logger.Info("已有可读媒体权限");
                permissionRequestInFlight = false;
                OnPermissionResult?.Invoke(true);
                yield break;
            }

            // 获取 Android SDK 版本
            int sdkInt = GetAndroidSdkInt();
            logger.Info($"Android SDK 版本: {sdkInt}");

            if (sdkInt >= 33)
            {
                // Android 13+: 首先请求完整的视频媒体权限
                yield return RequestSinglePermission(AndroidPermissionReadMediaVideo);

                // Android 14+ (API 34+): 如果用户拒绝了完整权限，请求部分权限
                if (sdkInt >= 34)
                {
                    bool hasVideoPermission = Permission.HasUserAuthorizedPermission(AndroidPermissionReadMediaVideo);
                    bool hasSelectedPermission = Permission.HasUserAuthorizedPermission(AndroidPermissionReadMediaVisualUserSelected);

                    if (!hasVideoPermission && !hasSelectedPermission)
                    {
                        logger.Info("请求部分访问权限");
                        yield return RequestSinglePermission(AndroidPermissionReadMediaVisualUserSelected);
                    }
                }
            }
            else
            {
                // Android 12-: 请求旧的存储权限
                logger.Info("请求旧版存储权限");
                yield return RequestSinglePermission(AndroidPermissionReadExternalStorage);
            }

            permissionRequestInFlight = false;

            // 检查最终结果
            bool granted = HasAnyReadableMediaPermission();
            logger.Info($"权限请求完成，结果: {(granted ? "已授予" : "已拒绝")}");
            OnPermissionResult?.Invoke(granted);
        }

        /// <summary>
        /// 检查是否有任何可读媒体权限
        /// </summary>
        private bool HasAnyReadableMediaPermission()
        {
            int sdkInt = GetAndroidSdkInt();

            bool hasMediaVideo = Permission.HasUserAuthorizedPermission(AndroidPermissionReadMediaVideo);

            if (sdkInt >= 34)
            {
                bool hasSelectedVisual = Permission.HasUserAuthorizedPermission(AndroidPermissionReadMediaVisualUserSelected);
                return hasMediaVideo || hasSelectedVisual;
            }

            if (sdkInt >= 33)
            {
                return hasMediaVideo;
            }

            return Permission.HasUserAuthorizedPermission(AndroidPermissionReadExternalStorage);
        }

        /// <summary>
        /// 检查是否有 Movies 目录权限
        /// </summary>
        private bool HasMoviesDirectoryPermission()
        {
            int sdkInt = GetAndroidSdkInt();
            if (sdkInt >= 33)
            {
                return Permission.HasUserAuthorizedPermission(AndroidPermissionReadMediaVideo);
            }

            return Permission.HasUserAuthorizedPermission(AndroidPermissionReadExternalStorage);
        }

        /// <summary>
        /// 获取 Android SDK 版本
        /// </summary>
        private static int GetAndroidSdkInt()
        {
            try
            {
                AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION");
                return versionClass.GetStatic<int>("SDK_INT");
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 请求单个权限
        /// </summary>
        private IEnumerator RequestSinglePermission(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission) || Permission.HasUserAuthorizedPermission(permission))
            {
                logger.Debug($"权限已授予或为空: {permission}");
                yield break;
            }

            logger.Info($"请求权限: {permission}");

            bool completed = false;
            bool denied = false;
            bool deniedDontAskAgain = false;

            PermissionCallbacks callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => { completed = true; };
            callbacks.PermissionDenied += _ =>
            {
                completed = true;
                denied = true;
            };
            callbacks.PermissionDeniedAndDontAskAgain += _ =>
            {
                completed = true;
                denied = true;
                deniedDontAskAgain = true;
            };

            Permission.RequestUserPermission(permission, callbacks);

            // 等待结果，最多 10 秒
            float timeout = 10f;
            while (!completed && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            // 超时处理
            if (!completed)
            {
                denied = !Permission.HasUserAuthorizedPermission(permission);
                logger.Warning($"权限请求超时: {permission}");
            }

            if (denied)
            {
                lastPermissionRequestDenied = true;
                logger.Info($"权限被拒绝: {permission}");
            }
            else
            {
                logger.Info($"权限已授予: {permission}");
            }

            if (denied && !deniedDontAskAgain)
            {
                // 检查是否显示权限说明
                deniedDontAskAgain = !ShouldShowPermissionRationale(permission);
            }

            if (deniedDontAskAgain)
            {
                lastPermissionRequestDontAskAgain = true;
                logger.Info($"用户选择了\"不再询问\"");
            }
        }

        /// <summary>
        /// 检查是否应该显示权限说明
        /// </summary>
        private static bool ShouldShowPermissionRationale(string permission)
        {
            if (string.IsNullOrWhiteSpace(permission))
            {
                return false;
            }

            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                return activity.Call<bool>("shouldShowRequestPermissionRationale", permission);
            }
            catch
            {
                return true;
            }
        }
#endif
    }
}
