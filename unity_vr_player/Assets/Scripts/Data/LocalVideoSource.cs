using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Adapter for local files to the IVideoSource contract.
/// </summary>
public class LocalVideoSource : IVideoSource
{
    private readonly LocalFileManager localFileManager;

    public string SourceName => "Local";

    public LocalVideoSource(LocalFileManager manager)
    {
        localFileManager = manager;
    }

    public Task<List<VideoFile>> ListAsync(string path = "")
    {
        if (localFileManager == null)
        {
            return Task.FromResult(new List<VideoFile>());
        }

        return Task.FromResult(localFileManager.GetLocalVideos());
    }

    public Task<bool> DownloadAsync(VideoFile sourceFile, string localPath, Action<float> onProgress = null)
    {
        if (sourceFile == null || string.IsNullOrWhiteSpace(localPath))
        {
            return Task.FromResult(false);
        }

        string sourcePath = sourceFile.localPath;
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            sourcePath = sourceFile.path;
        }

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return Task.FromResult(false);
        }

        return Task.Run(() =>
        {
            try
            {
                string directory = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourcePath, localPath, true);
                onProgress?.Invoke(100f);
                return true;
            }
            catch
            {
                return false;
            }
        });
    }
}
