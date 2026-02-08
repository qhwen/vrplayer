using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IVideoSource
{
    string SourceName { get; }

    Task<List<VideoFile>> ListAsync(string path = "");
    Task<bool> DownloadAsync(VideoFile sourceFile, string localPath, Action<float> onProgress = null);
}
