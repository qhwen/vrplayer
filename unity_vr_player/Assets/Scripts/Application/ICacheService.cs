public interface ICacheService
{
    string GetPath(string key, string extension = ".mp4");
    bool Exists(string key, string extension = ".mp4");
    bool Store(string key, string sourcePath, string extension = ".mp4");
    bool Evict(string key, string extension = ".mp4");
    long GetTotalSizeBytes();
    void Clear();
}
