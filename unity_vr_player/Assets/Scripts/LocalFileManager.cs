using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 本地文件管理器。
/// </summary>
public class LocalFileManager : MonoBehaviour
{
    private static LocalFileManager instance;
    public static LocalFileManager Instance => instance;

    private readonly List<VideoFile> localVideos = new List<VideoFile>();
    private string cacheDirectory = string.Empty;

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

        Debug.Log("缓存目录: " + cacheDirectory);
    }

    public void OpenFilePicker()
    {
#if UNITY_EDITOR
        string path = EditorUtility.OpenFilePanel("选择视频文件", "", "mp4,mkv,mov");
        if (!string.IsNullOrWhiteSpace(path))
        {
            AddLocalVideo(path);
        }
#elif UNITY_ANDROID
        OpenFilePickerAndroid();
#elif UNITY_IOS
        OpenFilePickerIOS();
#else
        Debug.LogWarning("当前平台未接入文件选择器，请先将视频放入缓存目录。");
#endif
    }

    private void OpenFilePickerAndroid()
    {
#if UNITY_ANDROID
        Debug.LogWarning("Android 文件选择器需要 SAF 原生插件，当前尚未接入。");
#endif
    }

    private void OpenFilePickerIOS()
    {
#if UNITY_IOS
        Debug.LogWarning("iOS 文件选择器需要原生插件，当前尚未接入。");
#endif
    }

    public void AddLocalVideo(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Debug.LogError("文件不存在: " + filePath);
            return;
        }

        if (localVideos.Exists(v => v.localPath == filePath))
        {
            return;
        }

        FileInfo info = new FileInfo(filePath);

        VideoFile video = new VideoFile
        {
            name = Path.GetFileName(filePath),
            path = filePath,
            localPath = filePath,
            url = "file://" + filePath,
            is360 = Path.GetFileName(filePath).ToLower().Contains("360"),
            size = info.Length
        };

        localVideos.Add(video);
        Debug.Log("添加本地视频: " + video.name);
    }

    public List<VideoFile> GetLocalVideos()
    {
        RefreshLocalVideos();
        return new List<VideoFile>(localVideos);
    }

    public void RefreshLocalVideos()
    {
        localVideos.Clear();

        if (!Directory.Exists(cacheDirectory))
        {
            return;
        }

        try
        {
            string[] files = Directory.GetFiles(cacheDirectory, "*.*", SearchOption.AllDirectories);
            foreach (string filePath in files)
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                if (extension != ".mp4" && extension != ".mkv" && extension != ".mov")
                {
                    continue;
                }

                FileInfo info = new FileInfo(filePath);
                localVideos.Add(new VideoFile
                {
                    name = Path.GetFileName(filePath),
                    path = filePath,
                    localPath = filePath,
                    url = "file://" + filePath,
                    is360 = Path.GetFileName(filePath).ToLower().Contains("360"),
                    size = info.Length
                });
            }

            Debug.Log("刷新本地视频列表: " + localVideos.Count + " 个视频");
        }
        catch (System.Exception e)
        {
            Debug.LogError("刷新视频列表失败: " + e.Message);
        }
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

            Debug.Log("删除视频: " + filePath);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("删除视频失败: " + e.Message);
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

            Debug.Log("清空缓存");
        }
        catch (System.Exception e)
        {
            Debug.LogError("清空缓存失败: " + e.Message);
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
        DirectoryInfo directory = new DirectoryInfo(cacheDirectory);

        foreach (FileInfo file in directory.GetFiles("*.*", SearchOption.AllDirectories))
        {
            totalSize += file.Length;
        }

        return totalSize;
    }
}
