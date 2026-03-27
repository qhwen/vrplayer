using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Abstraction for video providers (local, WebDAV, etc.).
/// </summary>
public interface IVideoSource
{
    string SourceName { get; }

    Task<List<VideoFile>> ListAsync(string path = "");

    Task<bool> DownloadAsync(VideoFile sourceFile, string localPath, Action<float> onProgress = null);
}
