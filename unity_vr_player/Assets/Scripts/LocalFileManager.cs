using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Local media discovery and cache manager.
/// </summary>
public class LocalFileManager : MonoBehaviour
{
    private static LocalFileManager instance;
    public static LocalFileManager Instance => instance;

    [Header("Scan Settings")]
    [SerializeField] private bool includeCommonMediaFolders = true;
    [SerializeField, Range(0, 5)] private int scanDepth = 0;
    [SerializeField, Range(20, 500)] private int maxCollectedVideos = 200;

#if UNITY_ANDROID
    [Header("Android Scan Scope")]
    [SerializeField] private string androidMoviesDirectory = "/storage/emulated/0/Movies";
    [SerializeField] private bool includeMoviesSubdirectories;
#endif

    private readonly List<VideoFile> localVideos = new List<VideoFile>();
    private readonly string[] supportedExtensions = { ".mp4", ".mkv", ".mov" };

    private string cacheDirectory = string.Empty;
    private ICacheService cacheService;

    private bool permissionRequestInFlight;
    private bool lastPermissionRequestDenied;
    private bool lastPermissionRequestDontAskAgain;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCacheDirectory();
            return;
        }

        if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCacheDirectory()
    {
        cacheDirectory = Path.Combine(Application.persistentDataPath, "VRVideos");

        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }

        cacheService = new FileCacheService(cacheDirectory);
        Debug.Log("Cache directory: " + cacheDirectory);
    }

    public bool IsPermissionRequestInFlight()
    {
        return permissionRequestInFlight;
    }

    public bool WasLastPermissionRequestDenied()
    {
        return lastPermissionRequestDenied;
    }

    public bool WasLastPermissionRequestDeniedAndDontAskAgain()
    {
        return lastPermissionRequestDontAskAgain;
    }

    public bool HasReadableMediaPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return HasAnyReadableMediaPermission();
#else
        return true;
#endif
    }

    public void RequestReadableMediaPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (permissionRequestInFlight)
        {
            return;
        }

        StartCoroutine(RequestReadableMediaPermissionRoutine());
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator RequestReadableMediaPermissionRoutine()
    {
        permissionRequestInFlight = true;
        lastPermissionRequestDenied = false;
        lastPermissionRequestDontAskAgain = false;

        float focusTimeout = 5f;
        while (!Application.isFocused && focusTimeout > 0f)
        {
            focusTimeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        // Allow UI frame to settle before system permission prompt.
        yield return null;

        if (HasAnyReadableMediaPermission())
        {
            permissionRequestInFlight = false;
            yield break;
        }

        int sdkInt = GetAndroidSdkInt();

        if (sdkInt >= 33)
        {
            yield return RequestSinglePermission("android.permission.READ_MEDIA_VIDEO");
        }
        else
        {
            yield return RequestSinglePermission(Permission.ExternalStorageRead);
        }

        permissionRequestInFlight = false;
    }

    private bool HasAnyReadableMediaPermission()
    {
        int sdkInt = GetAndroidSdkInt();

        bool hasMediaVideo = Permission.HasUserAuthorizedPermission("android.permission.READ_MEDIA_VIDEO");

        if (sdkInt >= 33)
        {
            return hasMediaVideo;
        }

        return Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead);
    }

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

    private IEnumerator RequestSinglePermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission) || Permission.HasUserAuthorizedPermission(permission))
        {
            yield break;
        }

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

        float timeout = 10f;
        while (!completed && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!completed)
        {
            denied = !Permission.HasUserAuthorizedPermission(permission);
        }

        if (denied)
        {
            lastPermissionRequestDenied = true;
        }

        if (denied && !deniedDontAskAgain)
        {
            deniedDontAskAgain = !ShouldShowPermissionRationale(permission);
        }

        if (deniedDontAskAgain)
        {
            lastPermissionRequestDontAskAgain = true;
        }
    }

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

    public void OpenAppPermissionSettings()
    {
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
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to open app settings: " + e.Message);
        }
    }
#else
    private static bool ShouldShowPermissionRationale(string permission)
    {
        return true;
    }

    public void OpenAppPermissionSettings()
    {
    }
#endif

    public void OpenFilePicker()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Select video file", "", "mp4,mkv,mov");
        if (!string.IsNullOrWhiteSpace(path))
        {
            AddLocalVideo(path);
        }
#elif UNITY_ANDROID
        Debug.LogWarning("Android file picker plugin is not integrated yet. Put video files into Movies folder.");
#elif UNITY_IOS
        Debug.LogWarning("iOS file picker plugin is not integrated yet.");
#else
        Debug.LogWarning("File picker is not available on this platform.");
#endif
    }

    public void AddLocalVideo(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Debug.LogError("File does not exist: " + filePath);
            return;
        }

        HashSet<string> deduplicate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < localVideos.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(localVideos[i].localPath))
            {
                deduplicate.Add(localVideos[i].localPath);
            }
        }

        TryAddVideo(filePath, deduplicate);
    }

    public List<VideoFile> GetLocalVideos()
    {
        RefreshLocalVideos();
        return new List<VideoFile>(localVideos);
    }

    public void RefreshLocalVideos()
    {
        localVideos.Clear();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!HasAnyReadableMediaPermission())
        {
            Debug.Log("Skip scan: readable media permission missing.");
            return;
        }

        HashSet<string> deduplicate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddVideosFromAndroidMediaStore(deduplicate);
#else
        HashSet<string> deduplicate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> roots = BuildSearchRoots();

        for (int i = 0; i < roots.Count; i++)
        {
            if (deduplicate.Count >= maxCollectedVideos)
            {
                break;
            }

            ScanDirectoryRecursive(roots[i], deduplicate, 0, ShouldScanSubdirectories());
        }
#endif

        localVideos.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        Debug.Log("Refreshed local videos: " + localVideos.Count);
    }

    private bool ShouldScanSubdirectories()
    {
#if UNITY_ANDROID
        return includeMoviesSubdirectories;
#else
        return true;
#endif
    }

    private List<string> BuildSearchRoots()
    {
        List<string> roots = new List<string>();

#if UNITY_ANDROID
        AddSearchRoot(roots, androidMoviesDirectory);
        AddSearchRoot(roots, "/storage/self/primary/Movies");
#else
        AddSearchRoot(roots, cacheDirectory);
        AddSearchRoot(roots, Application.persistentDataPath);

        if (includeCommonMediaFolders)
        {
            AddSearchRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
            AddSearchRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }
#endif

        return roots;
    }

    private static void AddSearchRoot(List<string> roots, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string normalized = path.Trim();
        for (int i = 0; i < roots.Count; i++)
        {
            if (string.Equals(roots[i], normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        roots.Add(normalized);
    }

    private void ScanDirectoryRecursive(string directory, HashSet<string> deduplicate, int depth, bool allowSubdirectories)
    {
        if (string.IsNullOrWhiteSpace(directory) || depth > scanDepth)
        {
            return;
        }

        if (deduplicate.Count >= maxCollectedVideos)
        {
            return;
        }

        if (!Directory.Exists(directory))
        {
            return;
        }

        try
        {
            string[] files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                if (deduplicate.Count >= maxCollectedVideos)
                {
                    return;
                }

                TryAddVideo(files[i], deduplicate);
            }
        }
        catch
        {
            // Ignore permission denied folders.
        }

        if (!allowSubdirectories || depth >= scanDepth)
        {
            return;
        }

        try
        {
            string[] subDirectories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < subDirectories.Length; i++)
            {
                if (deduplicate.Count >= maxCollectedVideos)
                {
                    return;
                }

                ScanDirectoryRecursive(subDirectories[i], deduplicate, depth + 1, allowSubdirectories);
            }
        }
        catch
        {
            // Ignore permission denied folders.
        }
    }

    private void TryAddVideo(string filePath, HashSet<string> deduplicate)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        string extension = Path.GetExtension(filePath);
        if (!IsSupportedVideoExtension(extension))
        {
            return;
        }

        if (deduplicate.Contains(filePath))
        {
            return;
        }

        FileInfo info;
        try
        {
            info = new FileInfo(filePath);
        }
        catch
        {
            return;
        }

        localVideos.Add(new VideoFile
        {
            name = Path.GetFileName(filePath),
            path = filePath,
            localPath = filePath,
            url = "file://" + filePath,
            is360 = Path.GetFileName(filePath).ToLowerInvariant().Contains("360"),
            size = info.Exists ? info.Length : 0
        });

        deduplicate.Add(filePath);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void AddVideosFromAndroidMediaStore(HashSet<string> deduplicate)
    {
        AndroidJavaObject cursor = null;

        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject resolver = activity.Call<AndroidJavaObject>("getContentResolver");

            AndroidJavaClass mediaClass = new AndroidJavaClass("android.provider.MediaStore$Video$Media");
            AndroidJavaObject externalContentUri = mediaClass.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");

            string[] projection =
            {
                "_id",
                "_data",
                "display_name",
                "_size",
                "relative_path"
            };

            string selection;
            string[] selectionArgs;
            if (includeMoviesSubdirectories)
            {
                selection = "relative_path=? OR relative_path=? OR relative_path LIKE ?";
                selectionArgs = new[] { "Movies", "Movies/", "Movies/%" };
            }
            else
            {
                selection = "relative_path=? OR relative_path=?";
                selectionArgs = new[] { "Movies", "Movies/" };
            }

            cursor = resolver.Call<AndroidJavaObject>(
                "query",
                externalContentUri,
                projection,
                selection,
                selectionArgs,
                "date_added DESC");

            if (cursor == null)
            {
                return;
            }

            int idIndex = cursor.Call<int>("getColumnIndex", "_id");
            int dataIndex = cursor.Call<int>("getColumnIndex", "_data");
            int nameIndex = cursor.Call<int>("getColumnIndex", "display_name");
            int sizeIndex = cursor.Call<int>("getColumnIndex", "_size");
            int relativePathIndex = cursor.Call<int>("getColumnIndex", "relative_path");

            while (cursor.Call<bool>("moveToNext"))
            {
                if (localVideos.Count >= maxCollectedVideos)
                {
                    break;
                }

                string displayName = nameIndex >= 0 ? cursor.Call<string>("getString", nameIndex) : string.Empty;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    continue;
                }

                if (!IsSupportedVideoExtension(Path.GetExtension(displayName)))
                {
                    continue;
                }

                string filePath = dataIndex >= 0 ? cursor.Call<string>("getString", dataIndex) : string.Empty;
                string relativePath = relativePathIndex >= 0 ? cursor.Call<string>("getString", relativePathIndex) : string.Empty;
                long size = sizeIndex >= 0 ? cursor.Call<long>("getLong", sizeIndex) : 0;
                long mediaId = idIndex >= 0 ? cursor.Call<long>("getLong", idIndex) : -1;

                if (!MatchesMoviesScope(filePath, relativePath))
                {
                    continue;
                }

                string contentUri = mediaId >= 0 ? "content://media/external/video/media/" + mediaId : string.Empty;

                if (string.IsNullOrWhiteSpace(filePath) && !string.IsNullOrWhiteSpace(relativePath))
                {
                    string rel = relativePath.Replace("\\", "/").TrimStart('/');
                    if (!rel.EndsWith("/"))
                    {
                        rel += "/";
                    }

                    filePath = "/storage/emulated/0/" + rel + displayName;
                }

                string dedupKey = !string.IsNullOrWhiteSpace(filePath) ? filePath : contentUri;
                if (string.IsNullOrWhiteSpace(dedupKey) || deduplicate.Contains(dedupKey))
                {
                    continue;
                }

                localVideos.Add(new VideoFile
                {
                    name = displayName,
                    path = !string.IsNullOrWhiteSpace(filePath) ? filePath : contentUri,
                    localPath = !string.IsNullOrWhiteSpace(filePath) ? filePath : contentUri,
                    url = !string.IsNullOrWhiteSpace(filePath) ? ("file://" + filePath) : contentUri,
                    is360 = displayName.ToLowerInvariant().Contains("360"),
                    size = size
                });

                deduplicate.Add(dedupKey);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("MediaStore scan failed: " + e.Message);
        }
        finally
        {
            if (cursor != null)
            {
                cursor.Call("close");
                cursor.Dispose();
            }
        }
    }

    private bool MatchesMoviesScope(string filePath, string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(relativePath))
        {
            string normalizedRelative = relativePath.Replace("\\", "/").Trim().TrimStart('/').ToLowerInvariant();
            if (normalizedRelative.EndsWith("/"))
            {
                normalizedRelative = normalizedRelative.Substring(0, normalizedRelative.Length - 1);
            }

            if (includeMoviesSubdirectories)
            {
                return normalizedRelative == "movies" || normalizedRelative.StartsWith("movies/");
            }

            return normalizedRelative == "movies";
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }

        string normalizedPath = filePath.Replace("\\", "/").ToLowerInvariant();
        const string marker = "/movies/";
        int markerIndex = normalizedPath.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return false;
        }

        if (includeMoviesSubdirectories)
        {
            return true;
        }

        string trailing = normalizedPath.Substring(markerIndex + marker.Length);
        return trailing.IndexOf('/') < 0;
    }
#endif

    private bool IsSupportedVideoExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        string normalized = extension.ToLowerInvariant();
        for (int i = 0; i < supportedExtensions.Length; i++)
        {
            if (normalized == supportedExtensions[i])
            {
                return true;
            }
        }

        return false;
    }

    public bool DeleteLocalVideo(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);

            VideoFile toRemove = localVideos.Find(v => v.localPath == filePath);
            if (toRemove != null)
            {
                localVideos.Remove(toRemove);
            }

            Debug.Log("Deleted video: " + filePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("Delete failed: " + e.Message);
            return false;
        }
    }

    public void ClearAllCache()
    {
        try
        {
            if (!Directory.Exists(cacheDirectory))
            {
                return;
            }

            Directory.Delete(cacheDirectory, true);
            Directory.CreateDirectory(cacheDirectory);
            cacheService = new FileCacheService(cacheDirectory);
            localVideos.Clear();

            Debug.Log("Cache cleared");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to clear cache: " + e.Message);
        }
    }

    public ICacheService GetCacheService()
    {
        return cacheService;
    }

    public string GetCachePath(string cacheKey, string extension = ".mp4")
    {
        if (cacheService == null)
        {
            string key = string.IsNullOrWhiteSpace(cacheKey) ? Guid.NewGuid().ToString("N") : cacheKey;
            string ext = string.IsNullOrWhiteSpace(extension) ? ".mp4" : (extension.StartsWith(".") ? extension : "." + extension);
            return Path.Combine(cacheDirectory, key + ext);
        }

        return cacheService.GetPath(cacheKey, extension);
    }

    public string GetCacheDirectory()
    {
        return cacheDirectory;
    }

    public long GetCacheSize()
    {
        if (cacheService != null)
        {
            return cacheService.GetTotalSizeBytes();
        }

        if (!Directory.Exists(cacheDirectory))
        {
            return 0;
        }

        long totalSize = 0;

        try
        {
            DirectoryInfo directory = new DirectoryInfo(cacheDirectory);
            FileInfo[] files = directory.GetFiles("*.*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                totalSize += files[i].Length;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to calculate cache size: " + e.Message);
        }

        return totalSize;
    }
}
