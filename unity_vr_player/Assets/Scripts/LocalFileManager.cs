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
    [SerializeField, Range(1, 6)] private int scanDepth = 3;

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
            RequestRuntimePermissions();
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

    private static void RequestRuntimePermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }

        const string readMediaVideo = "android.permission.READ_MEDIA_VIDEO";
        if (!Permission.HasUserAuthorizedPermission(readMediaVideo))
        {
            Permission.RequestUserPermission(readMediaVideo);
        }
#endif
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
        Debug.LogWarning("Android file picker plugin is not integrated yet. Put video files into Movies or app cache folder.");
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
        foreach (VideoFile video in localVideos)
        {
            if (!string.IsNullOrWhiteSpace(video.localPath))
            {
                deduplicate.Add(video.localPath);
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
            ScanDirectoryRecursive(roots[i], deduplicate, 0);
        }

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
        AddSearchRoot(roots, "/storage/emulated/0/DCIM");
        AddSearchRoot(roots, "/storage/emulated/0/Download");
        AddSearchRoot(roots, "/sdcard/Movies");
        AddSearchRoot(roots, "/sdcard/DCIM");
        AddSearchRoot(roots, "/sdcard/Download");
#else
        AddSearchRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
        AddSearchRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        AddSearchRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
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

        if (!Directory.Exists(directory))
        {
            return;
        }

        try
        {
            string[] files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                TryAddVideo(files[i], deduplicate);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Cannot read files from directory: " + directory + " | " + e.Message);
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
                ScanDirectoryRecursive(subDirectories[i], deduplicate, depth + 1);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Cannot enumerate sub directories: " + directory + " | " + e.Message);
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
