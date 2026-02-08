using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Adapter for WebDAV source to the IVideoSource contract.
/// </summary>
public class WebDavVideoSource : IVideoSource
{
    private readonly WebDAVManager webDavManager;

    public string SourceName => "WebDAV";

    public WebDavVideoSource(WebDAVManager manager)
    {
        webDavManager = manager;
    }

    public Task<List<VideoFile>> ListAsync(string path = "")
    {
        if (webDavManager == null)
        {
            return Task.FromResult(new List<VideoFile>());
        }

        return webDavManager.ListFiles(path);
    }

    public Task<bool> DownloadAsync(VideoFile sourceFile, string localPath, Action<float> onProgress = null)
    {
        if (webDavManager == null || sourceFile == null || string.IsNullOrWhiteSpace(localPath))
        {
            return Task.FromResult(false);
        }

        string remotePath = sourceFile.path;
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            remotePath = sourceFile.url;
        }

        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return Task.FromResult(false);
        }

        return webDavManager.DownloadFile(remotePath, localPath, onProgress);
    }
}
