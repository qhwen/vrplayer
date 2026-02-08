using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// WebDAV manager.
/// </summary>
public class WebDAVManager : MonoBehaviour
{
    private static WebDAVManager instance;
    public static WebDAVManager Instance => instance;

    [Header("WebDAV Configuration")]
    [SerializeField] private string serverUrl = "";
    [SerializeField] private string username = "";
    [SerializeField] private string password = "";
    [SerializeField] private string basePath = "/";
    [SerializeField, Range(10, 60)] private int requestTimeoutSeconds = 20;
    [SerializeField, Range(0, 3)] private int downloadRetryCount = 1;

    private bool isConnected;
    private List<VideoFile> cachedFileList = new List<VideoFile>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public Task<bool> Connect(string url, string user, string pass)
    {
        serverUrl = NormalizeServerUrl(url);
        username = user ?? string.Empty;
        password = pass ?? string.Empty;

        return RunAsTask<bool>(onCompleted => TestConnectionCoroutine(onCompleted));
    }

    public Task<List<VideoFile>> ListFiles(string path = "")
    {
        return RunAsTask<List<VideoFile>>(onCompleted => ListFilesCoroutine(path, onCompleted));
    }

    public Task<bool> DownloadFile(string remotePath, string localPath, Action<float> onProgress = null)
    {
        return RunAsTask<bool>(onCompleted => DownloadFileCoroutine(remotePath, localPath, onProgress, onCompleted));
    }

    public List<VideoFile> GetCachedFiles()
    {
        return new List<VideoFile>(cachedFileList);
    }

    public bool GetIsConnected()
    {
        return isConnected;
    }

    private Task<T> RunAsTask<T>(Func<Action<T>, IEnumerator> coroutineFactory)
    {
        TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
        StartCoroutine(coroutineFactory(result => tcs.TrySetResult(result)));
        return tcs.Task;
    }

    private IEnumerator TestConnectionCoroutine(Action<bool> onCompleted)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            isConnected = false;
            onCompleted?.Invoke(false);
            yield break;
        }

        bool connected = false;

        const string depth0Body =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<d:propfind xmlns:d=\"DAV:\"><d:prop><d:displayname/></d:prop></d:propfind>";

        byte[] body = Encoding.UTF8.GetBytes(depth0Body);

        using (UnityWebRequest request = new UnityWebRequest(serverUrl + "/", "PROPFIND"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = requestTimeoutSeconds;
            request.SetRequestHeader("Authorization", BuildAuthHeader());
            request.SetRequestHeader("Depth", "0");
            request.SetRequestHeader("Content-Type", "application/xml; charset=utf-8");

            yield return request.SendWebRequest();

            connected = IsRequestSuccess(request) || request.responseCode == 207;
        }

        if (!connected)
        {
            using (UnityWebRequest fallback = UnityWebRequest.Get(serverUrl + "/"))
            {
                fallback.timeout = requestTimeoutSeconds;
                fallback.SetRequestHeader("Authorization", BuildAuthHeader());

                yield return fallback.SendWebRequest();
                connected = IsRequestSuccess(fallback);
            }
        }

        isConnected = connected;

        if (connected)
        {
            Debug.Log("WebDAV connected: " + serverUrl);
        }
        else
        {
            Debug.LogError("WebDAV connection failed: " + serverUrl);
        }

        onCompleted?.Invoke(connected);
    }

    private IEnumerator ListFilesCoroutine(string path, Action<List<VideoFile>> onCompleted)
    {
        if (!isConnected)
        {
            onCompleted?.Invoke(new List<VideoFile>());
            yield break;
        }

        string normalizedPath = string.IsNullOrWhiteSpace(path) ? basePath : path;

        if (!normalizedPath.StartsWith("/"))
        {
            normalizedPath = "/" + normalizedPath;
        }

        string targetUrl = CombineUrl(serverUrl, normalizedPath);

        const string propfindBody =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<d:propfind xmlns:d=\"DAV:\">" +
            "<d:prop><d:displayname/><d:getcontentlength/><d:resourcetype/></d:prop>" +
            "</d:propfind>";

        byte[] body = Encoding.UTF8.GetBytes(propfindBody);

        using (UnityWebRequest request = new UnityWebRequest(targetUrl, "PROPFIND"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = requestTimeoutSeconds;

            request.SetRequestHeader("Authorization", BuildAuthHeader());
            request.SetRequestHeader("Depth", "1");
            request.SetRequestHeader("Content-Type", "application/xml; charset=utf-8");

            yield return request.SendWebRequest();

            if (IsRequestSuccess(request) || request.responseCode == 207)
            {
                string responseXml = request.downloadHandler.text;
                List<VideoFile> files = ParseWebDavResponse(responseXml);
                cachedFileList = files;
                onCompleted?.Invoke(files);
            }
            else
            {
                Debug.LogError("List files failed: " + request.error + " (HTTP " + request.responseCode + ")");
                onCompleted?.Invoke(new List<VideoFile>());
            }
        }
    }

    private IEnumerator DownloadFileCoroutine(string remotePath, string localPath, Action<float> onProgress, Action<bool> onCompleted)
    {
        if (!isConnected || string.IsNullOrWhiteSpace(remotePath) || string.IsNullOrWhiteSpace(localPath))
        {
            onCompleted?.Invoke(false);
            yield break;
        }

        string downloadUrl = CombineUrl(serverUrl, remotePath);
        string directory = Path.GetDirectoryName(localPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        int maxAttempts = Mathf.Max(1, downloadRetryCount + 1);
        string tempPath = localPath + ".part";

        bool success = false;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            using (UnityWebRequest request = UnityWebRequest.Get(downloadUrl))
            {
                request.downloadHandler = new DownloadHandlerFile(tempPath);
                request.timeout = 0;
                request.SetRequestHeader("Authorization", BuildAuthHeader());

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    float baseProgress = (attempt - 1f) / maxAttempts;
                    float attemptProgress = Mathf.Clamp01(request.downloadProgress) / maxAttempts;
                    onProgress?.Invoke((baseProgress + attemptProgress) * 100f);
                    yield return null;
                }

                if (IsRequestSuccess(request))
                {
                    success = true;
                    break;
                }

                Debug.LogWarning("Download failed (attempt " + attempt + "/" + maxAttempts + "): " + request.error + " (HTTP " + request.responseCode + ")");
            }

            if (attempt < maxAttempts)
            {
                yield return new WaitForSecondsRealtime(0.4f);
            }
        }

        if (!success)
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            onCompleted?.Invoke(false);
            yield break;
        }

        try
        {
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
            }

            File.Move(tempPath, localPath);
            onProgress?.Invoke(100f);
            Debug.Log("Download succeeded: " + localPath);
            onCompleted?.Invoke(true);
        }
        catch (Exception e)
        {
            Debug.LogError("Move downloaded file failed: " + e.Message);
            onCompleted?.Invoke(false);
        }
    }

    private List<VideoFile> ParseWebDavResponse(string xml)
    {
        List<VideoFile> files = new List<VideoFile>();

        if (string.IsNullOrWhiteSpace(xml))
        {
            return files;
        }

        try
        {
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);

            XmlNamespaceManager ns = new XmlNamespaceManager(document.NameTable);
            ns.AddNamespace("d", "DAV:");

            XmlNodeList responseNodes = document.SelectNodes("//d:response", ns);
            if (responseNodes == null)
            {
                return files;
            }

            HashSet<string> seenHrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (XmlNode response in responseNodes)
            {
                string href = response.SelectSingleNode("d:href", ns)?.InnerText ?? string.Empty;
                href = Uri.UnescapeDataString(href);

                if (string.IsNullOrWhiteSpace(href) || seenHrefs.Contains(href))
                {
                    continue;
                }

                seenHrefs.Add(href);

                XmlNode propNode = response.SelectSingleNode("d:propstat/d:prop", ns);
                if (propNode == null)
                {
                    continue;
                }

                bool isCollection = propNode.SelectSingleNode("d:resourcetype/d:collection", ns) != null;
                if (isCollection)
                {
                    continue;
                }

                string displayName = propNode.SelectSingleNode("d:displayname", ns)?.InnerText ?? string.Empty;
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    displayName = Path.GetFileName(href.TrimEnd('/'));
                }

                string extension = Path.GetExtension(displayName).ToLowerInvariant();
                if (extension != ".mp4" && extension != ".mkv" && extension != ".mov")
                {
                    continue;
                }

                long size = 0;
                string sizeText = propNode.SelectSingleNode("d:getcontentlength", ns)?.InnerText;
                if (!string.IsNullOrWhiteSpace(sizeText))
                {
                    long.TryParse(sizeText, out size);
                }

                files.Add(new VideoFile
                {
                    name = displayName,
                    path = href,
                    url = CombineUrl(serverUrl, href),
                    is360 = displayName.ToLowerInvariant().Contains("360"),
                    size = size,
                    localPath = string.Empty
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Parse WebDAV response failed: " + e.Message);
        }

        return files;
    }

    private static bool IsRequestSuccess(UnityWebRequest request)
    {
        return request.result == UnityWebRequest.Result.Success
               || request.responseCode == 200
               || request.responseCode == 201
               || request.responseCode == 204
               || request.responseCode == 206
               || request.responseCode == 207;
    }

    private string BuildAuthHeader()
    {
        string raw = username + ":" + password;
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static string NormalizeServerUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        return url.Trim().TrimEnd('/');
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return path ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return baseUrl;
        }

        string normalizedBase = baseUrl.TrimEnd('/');
        string normalizedPath = path.Trim();

        if (normalizedPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || normalizedPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath;
        }

        if (!normalizedPath.StartsWith("/"))
        {
            normalizedPath = "/" + normalizedPath;
        }

        return normalizedBase + normalizedPath;
    }
}
