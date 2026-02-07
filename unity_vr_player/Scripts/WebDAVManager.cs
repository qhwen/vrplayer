using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// WebDAV 管理器
public class WebDAVManager : MonoBehaviour
{
    private static WebDAVManager instance;
    public static WebDAVManager Instance => instance;
    
    [Header("WebDAV配置")]
    [SerializeField] private string serverUrl = "";
    [SerializeField] private string username = "";
    [SerializeField] private string password = "";
    [SerializeField] private string basePath = "/";
    
    private bool isConnected = false;
    private List<VideoFile> cachedFileList = new List<VideoFile>();
    private Dictionary<string, VideoFile> fileCache = new Dictionary<string, VideoFile>();
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        
        DontDestroyOnLoad(gameObject);
    }
    
    /// 连接 WebDAV
    public async Task<bool> Connect(string url, string user, string pass)
    {
        // 保存配置
        serverUrl = url;
        username = user;
        password = pass;
        
        // 测试连接
        return await TestConnection();
    }
    
    /// 测试连接
    private async Task<bool> TestConnection()
    {
        try
        {
            // 创建测试请求
            using (UnityWebRequest request = UnityWebRequest.Get(serverUrl))
            {
                request.SetRequestHeader("Authorization", "Basic " + Base64Encode(username + ":" + password));
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    isConnected = true;
                    Debug.Log("WebDAV 连接成功: " + serverUrl);
                    return true;
                }
                else
                {
                    isConnected = false;
                    Debug.LogError("WebDAV 连接失败: " + request.error);
                    return false;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("WebDAV 连接错误: " + e.Message);
            isConnected = false;
            return false;
        }
    }
    
    /// 获取文件列表
    public async Task<List<VideoFile>> ListFiles(string path = "")
    {
        if (!isConnected)
        {
            Debug.LogWarning("WebDAV 未连接");
            return new List<VideoFile>();
        }
        
        string targetPath = string.IsNullOrEmpty(path) ? basePath : basePath + path.TrimEnd('/') + '/';
        
        // WebDAV PROPFIND 请求获取文件列表
        using (UnityWebRequest request = UnityWebRequest.Put(targetPath, System.Text.Encoding.UTF8.GetBytes("")))
        {
            request.SetRequestHeader("Authorization", "Basic " + Base64Encode(username + ":" + password));
            request.SetRequestHeader("Depth", "1");
            request.SetRequestHeader("Content-Type", "application/xml");
            request.method = "PROPFIND";
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                List<VideoFile> files = ParseWebDAVResponse(response);
                
                cachedFileList = files;
                return files;
            }
            else
            {
                Debug.LogError("获取文件列表失败: " + request.error);
                return new List<VideoFile>();
            }
        }
    }
    
    /// 解析 WebDAV 响应
    private List<VideoFile> ParseWebDAVResponse(string xml)
    {
        List<VideoFile> files = new List<VideoFile>();
        
        // 简单的XML解析（实际应使用XmlDocument）
        string[] lines = xml.Split('\n');
        foreach (string line in lines)
        {
            // 提取 href（文件路径）
            if (line.Contains("href>"))
            {
                int start = line.IndexOf("<D:href>") + 8;
                int end = line.IndexOf("</D:href>");
                if (start >= 0 && end > start)
                {
                    string filePath = line.Substring(start, end - start);
                    
                    // 提取显示名称
                    if (line.Contains("displayname>"))
                    {
                        int nameStart = line.IndexOf("<displayname>") + 12;
                        int nameEnd = line.IndexOf("</displayname>");
                        if (nameStart >= 0 && nameEnd > nameStart)
                        {
                            string displayName = line.Substring(nameStart, nameEnd - nameStart);
                            
                            // 过滤视频文件
                            string extension = Path.GetExtension(displayName).ToLower();
                            if (extension == ".mp4" || extension == ".mkv" || extension == ".mov")
                            {
                                VideoFile file = new VideoFile();
                                file.name = displayName;
                                file.path = filePath;
                                file.url = serverUrl + filePath;
                                file.is360 = displayName.ToLower().Contains("360");
                                
                                files.Add(file);
                            }
                        }
                    }
                }
            }
        }
        
        return files;
    }
    
    /// 下载文件
    public async Task<bool> DownloadFile(string remotePath, string localPath, System.Action<float> onProgress)
    {
        try
        {
            string downloadUrl = serverUrl + remotePath;
            string directory = Path.GetDirectoryName(localPath);
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            using (UnityWebRequest request = UnityWebRequest.Get(downloadUrl))
            {
                request.SetRequestHeader("Authorization", "Basic " + Base64Encode(username + ":" + password));
                
                // 设置下载处理
                DownloadHandlerFile handler = new DownloadHandlerFile(localPath);
                request.downloadHandler = handler;
                
                // 开始下载
                AsyncOperation operation = request.SendWebRequest();
                
                while (!operation.isDone)
                {
                    // 更新进度
                    if (onProgress != null && request.downloadedBytes > 0)
                    {
                        // 估算文件大小（假设100MB）
                        float estimatedSize = 100f * 1024 * 1024;
                        float progress = (request.downloadedBytes / estimatedSize) * 100f;
                        onProgress(Mathf.Clamp(progress, 0f, 100f));
                    }
                    
                    yield return null;
                }
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("文件下载成功: " + localPath);
                    return true;
                }
                else
                {
                    Debug.LogError("文件下载失败: " + request.error);
                    return false;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("下载异常: " + e.Message);
            return false;
        }
    }
    
    /// Base64 编码
    private string Base64Encode(string input)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(input);
        return System.Convert.ToBase64String(plainTextBytes);
    }
    
    /// 获取缓存文件列表
    public List<VideoFile> GetCachedFiles()
    {
        return cachedFileList;
    }
    
    /// 检查连接状态
    public bool GetIsConnected()
    {
        return isConnected;
    }
}

/// 视频文件信息
[System.Serializable]
public class VideoFile
{
    public string name;
    public string path;
    public string url;
    public bool is360;
    public long size;
    public string localPath;
}
