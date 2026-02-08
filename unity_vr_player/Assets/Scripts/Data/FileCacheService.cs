using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// File-system cache service with stable hashed key mapping.
/// </summary>
public class FileCacheService : ICacheService
{
    private readonly string cacheRoot;

    public FileCacheService(string rootDirectory)
    {
        cacheRoot = string.IsNullOrWhiteSpace(rootDirectory)
            ? Path.Combine(Environment.CurrentDirectory, "VRVideos")
            : rootDirectory;

        Directory.CreateDirectory(cacheRoot);
    }

    public string GetPath(string key, string extension = ".mp4")
    {
        string safeKey = BuildSafeKey(key);
        string safeExtension = NormalizeExtension(extension);
        return Path.Combine(cacheRoot, safeKey + safeExtension);
    }

    public bool Exists(string key, string extension = ".mp4")
    {
        return File.Exists(GetPath(key, extension));
    }

    public bool Store(string key, string sourcePath, string extension = ".mp4")
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return false;
        }

        string targetPath = GetPath(key, extension);

        try
        {
            string directory = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(sourcePath, targetPath, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool Evict(string key, string extension = ".mp4")
    {
        string targetPath = GetPath(key, extension);

        try
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public long GetTotalSizeBytes()
    {
        if (!Directory.Exists(cacheRoot))
        {
            return 0;
        }

        long total = 0;

        try
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(cacheRoot);
            FileInfo[] files = directoryInfo.GetFiles("*", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                total += files[i].Length;
            }
        }
        catch
        {
            return total;
        }

        return total;
    }

    public void Clear()
    {
        if (!Directory.Exists(cacheRoot))
        {
            return;
        }

        try
        {
            string[] files = Directory.GetFiles(cacheRoot, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                File.Delete(files[i]);
            }
        }
        catch
        {
            // Keep this method best effort to avoid throwing on startup flows.
        }
    }

    private static string BuildSafeKey(string key)
    {
        string input = string.IsNullOrWhiteSpace(key) ? Guid.NewGuid().ToString("N") : key.Trim();

        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            StringBuilder sb = new StringBuilder(hash.Length * 2);

            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return ".bin";
        }

        return extension.StartsWith(".") ? extension : "." + extension;
    }
}
