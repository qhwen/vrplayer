using System;
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
    [SerializeField, Range(1, 5)] private int scanDepth = 3;
    [SerializeField, Range(20, 500)] private int maxCollectedVideos = 200;

    private readonly List<VideoFile> localVideos = new List<VideoFile>();
    private readonly string[] supportedExtensions = { ".mp4", ".mkv", ".mov" };

    private string cacheDirectory = string.Empty;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCacheDirectory();
            EnsureAndroidReadPermission();
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

        Debug.Log("Cache directory: " + cacheDirectory);
    }

    private bool EnsureAndroidReadPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        const string readMediaVideo = "android.permission.READ_MEDIA_VIDEO";
        const string readSelectedVisualMedia = "android.permission.READ_MEDIA_VISUAL_USER_SELECTED";

        bool hasLegacy = Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead);
        bool hasMediaVideo = Permission.HasUserAuthorizedPermission(readMediaVideo);
        bool hasSelectedMedia = Permission.HasUserAuthorizedPermission(readSelectedVisualMedia);

        if (!hasLegacy)
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }

        if (!hasMediaVideo)
        {
            Permission.RequestUserPermission(readMediaVideo);
        }

        hasLegacy = Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead);
        hasMediaVideo = Permission.HasUserAuthorizedPermission(readMediaVideo);
        hasSelectedMedia = Permission.HasUserAuthorizedPermission(readSelectedVisualMedia);

        return hasLegacy || hasMediaVideo || hasSelectedMedia;
#else
        return true;
#endif
    }

    public bool HasReadableMediaPermission()
    {
        return EnsureAndroidReadPermission();
    }

    public void OpenFilePicker()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("Select video file", "", "mp4,mkv,mov");
        if (!string.IsNullOrWhiteSpace(path))
        {
            AddLocalVideo(path);
        }
#elif UNITY_ANDROID
        Debug.LogWarning("Android file picker plugin is not integrated yet. Put video files into Movies or Download folder.");
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

        HashSet<string> deduplicate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> roots = BuildSearchRoots();

        for (int i = 0; i < roots.Count; i++)
        {
            if (deduplicate.Count >= maxCollectedVideos)
            {
                break;
            }

            ScanDirectoryRecursive(roots[i], deduplicate, 0);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (localVideos.Count == 0)
        {
            AddVideosFromAndroidMediaStore(deduplicate);
        }
#endif

        localVideos.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        Debug.Log("Refreshed local videos: " + localVideos.Count);
    }

    private List<string> BuildSearchRoots()
    {
        List<string> roots = new List<string>();

        AddSearchRoot(roots, cacheDirectory);
        AddSearchRoot(roots, Application.persistentDataPath);

        if (!includeCommonMediaFolders)
        {
            return roots;
        }

#if UNITY_ANDROID
        AddSearchRoot(roots, "/storage/emulated/0/Movies");
        AddSearchRoot(roots, "/storage/emulated/0/Download");
        AddSearchRoot(roots, "/storage/emulated/0/DCIM/Camera");
        AddSearchRoot(roots, "/storage/self/primary/Movies");
        AddSearchRoot(roots, "/storage/self/primary/Download");
        AddSearchRoot(roots, "/sdcard/Movies");
        AddSearchRoot(roots, "/sdcard/Download");
#else
        AddSearchRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        AddSearchRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
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

    private void ScanDirectoryRecursive(string directory, HashSet<string> deduplicate, int depth)
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

        if (depth >= scanDepth)
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

                ScanDirectoryRecursive(subDirectories[i], deduplicate, depth + 1);
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
        if (!EnsureAndroidReadPermission())
        {
            return;
        }

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

            cursor = resolver.Call<AndroidJavaObject>(
                "query",
                externalContentUri,
                projection,
                null,
                null,
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
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    deduplicate.Add(contentUri);
                }
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
            localVideos.Clear();

            Debug.Log("Cache cleared");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to clear cache: " + e.Message);
        }
    }

    public string GetCacheDirectory()
    {
        return cacheDirectory;
    }

    public long GetCacheSize()
    {
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
