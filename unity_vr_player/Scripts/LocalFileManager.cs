using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_STANDALONE_WIN
using UnityEngine.Windows;
#elif UNITY_STANDALONE_OSX
using UnityEngine.Windows;
#elif UNITY_EDITOR
using UnityEditor;
#endif

/// 本地文件管理器
public class LocalFileManager : MonoBehaviour
{
    private static LocalFileManager instance;
    public static LocalFileManager Instance => instance;
    
    private List<VideoFile> localVideos = new List<VideoFile>();
    private string cacheDirectory = "";
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        
        DontDestroyOnLoad(gameObject);
        InitializeCacheDirectory();
    }
    
    /// 初始化缓存目录
    private void InitializeCacheDirectory()
    {
        // 根据平台设置缓存路径
#if UNITY_ANDROID || UNITY_IOS
        cacheDirectory = Application.persistentDataPath + "/VRVideos/";
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
        cacheDirectory = Application.dataPath + "/VRVideos/";
#elif UNITY_EDITOR
        cacheDirectory = Application.dataPath + "/VRVideos/";
#endif
        
        if (!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }
        
        Debug.Log("缓存目录: " + cacheDirectory);
    }
    
    /// 打开文件选择器
    public void OpenFilePicker()
    {
#if UNITY_EDITOR
        // Unity Editor中打开目录选择
        string path = EditorUtility.OpenFilePanel("选择视频文件", "", "mp4,mkv,mov", "Select Video");
        if (!string.IsNullOrEmpty(path))
        {
            AddLocalVideo(path);
        }
#elif UNITY_STANDALONE_WIN
        // Windows 文件对话框
        OpenFilePickerWindows();
#elif UNITY_ANDROID
        // Android 文件选择
        OpenFilePickerAndroid();
#elif UNITY_IOS
        // iOS 文件选择
        OpenFilePickerIOS();
#endif
    }
    
    /// Windows 文件选择器
    private void OpenFilePickerWindows()
    {
#if UNITY_STANDALONE_WIN
        try
        {
            FileOpenDialog dialog = new FileOpenDialog();
            dialog.filter = "Video Files|*.mp4;*.mkv;*.mov";
            dialog.multiselect = true;
            
            if (dialog.ShowDialog())
            {
                string[] paths = dialog.paths;
                if (paths != null && paths.Length > 0)
                {
                    foreach (string path in paths)
                    {
                        AddLocalVideo(path);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("文件选择失败: " + e.Message);
        }
#endif
    }
    
    /// Android 文件选择器
    private void OpenFilePickerAndroid()
    {
#if UNITY_ANDROID
        // Android 文件选择需要原生插件
        // 这里使用简化的模拟
        Debug.Log("Android 文件选择（需要原生插件）");
        
        // 实际项目应该使用 AndroidJavaObject 调用系统文件选择器
#endif
    }
    
    /// iOS 文件选择器
    private void OpenFilePickerIOS()
    {
#if UNITY_IOS
        // iOS 文件选择需要原生插件
        // 这里使用简化的模拟
        Debug.Log("iOS 文件选择（需要原生插件）");
        
        // 实际项目应该使用 UnityiOS 导入 PHPickerViewController
#endif
    }
    
    /// 添加本地视频
    public void AddLocalVideo(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("文件不存在: " + filePath);
            return;
        }
        
        VideoFile video = new VideoFile();
        video.name = Path.GetFileName(filePath);
        video.path = filePath;
        video.localPath = filePath;
        video.url = "file://" + filePath;
        video.is360 = video.name.ToLower().Contains("360");
        video.size = new FileInfo(filePath).Length;
        
        localVideos.Add(video);
        
        Debug.Log("添加本地视频: " + video.name);
    }
    
    /// 获取本地视频列表
    public List<VideoFile> GetLocalVideos()
    {
        RefreshLocalVideos();
        return localVideos;
    }
    
    /// 刷新本地视频列表
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
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".mp4" || extension == ".mkv" || extension == ".mov")
                {
                    VideoFile video = new VideoFile();
                    video.name = Path.GetFileName(filePath);
                    video.path = filePath;
                    video.localPath = filePath;
                    video.url = "file://" + filePath;
                    video.is360 = video.name.ToLower().Contains("360");
                    video.size = new FileInfo(filePath).Length;
                    
                    localVideos.Add(video);
                }
            }
        }
        
        Debug.Log("刷新本地视频列表: " + localVideos.Count + " 个视频");
    }
        catch (System.Exception e)
        {
            Debug.LogError("刷新视频列表失败: " + e.Message);
        }
    }
    
    /// 删除本地视频
    public bool DeleteLocalVideo(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                
                // 从列表中移除
                VideoFile toRemove = localVideos.Find(v => v.localPath == filePath);
                if (toRemove != null)
                {
                    localVideos.Remove(toRemove);
                }
                
                Debug.Log("删除视频: " + filePath);
                return true;
            }
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError("删除视频失败: " + e.Message);
            return false;
        }
    }
    
    /// 清空所有缓存
    public void ClearAllCache()
    {
        try
        {
            if (Directory.Exists(cacheDirectory))
            {
                Directory.Delete(cacheDirectory, true);
                Directory.CreateDirectory(cacheDirectory);
                localVideos.Clear();
                
                Debug.Log("清空缓存");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("清空缓存失败: " + e.Message);
        }
    }
    
    /// 获取缓存目录
    public string GetCacheDirectory()
    {
        return cacheDirectory;
    }
    
    /// 获取缓存大小
    public long GetCacheSize()
    {
        if (!Directory.Exists(cacheDirectory)) return 0;
        
        long totalSize = 0;
        DirectoryInfo di = new DirectoryInfo(cacheDirectory);
        
        foreach (FileInfo file in di.GetFiles("*.*", SearchOption.AllDirectories))
        {
            totalSize += file.Length;
        }
        
        return totalSize;
    }
}
