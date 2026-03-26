using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace VRPlayer.Infrastructure.Storage
{
    /// <summary>
    /// 文件缓存管理器实现
    /// 负责管理视频文件的缓存
    /// </summary>
    public class FileCacheManager : MonoBehaviour, ICacheManager
    {
        private const long DEFAULT_MAX_CACHE_SIZE_BYTES = 5L * 1024L * 1024L * 1024L; // 5 GB
        private const int DEFAULT_CACHE_ENTRY_COUNT_LIMIT = 100;
        private const string CACHE_DIRECTORY_NAME = "VRVideos";
        private const string METADATA_FILE_NAME = "cache_metadata.json";

        private string cacheDirectory;
        private long maxCacheSizeBytes = DEFAULT_MAX_CACHE_SIZE_BYTES;
        private int cacheEntryCountLimit = DEFAULT_CACHE_ENTRY_COUNT_LIMIT;

        private VRPlayer.Core.Logging.ILogger logger;

        /// <summary>
        /// 缓存配置
        /// </summary>
        [Serializable]
        private class CacheMetadata
        {
            public CacheEntry[] entries;
        }

        /// <summary>
        /// 缓存条目
        /// </summary>
        [Serializable]
        private class CacheEntry
        {
            public string key;
            public string path;
            public long size;
            public long lastAccessTime;
            public string sourceUrl;
        }

        private CacheMetadata metadata;

        public event Action<VideoFile> OnCached;
        public event Action<VideoFile> OnCacheRemoved;

        private void Awake()
        {
            logger = VRPlayer.Core.Logging.LoggerManager.For("FileCacheManager");
            InitializeCache();
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        private void InitializeCache()
        {
            cacheDirectory = Path.Combine(Application.persistentDataPath, CACHE_DIRECTORY_NAME);

            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
                logger.Info($"创建缓存目录: {cacheDirectory}");
            }

            LoadMetadata();
            logger.Info($"缓存目录: {cacheDirectory}");
            logger.Info($"缓存大小: {GetTotalCacheSize().SizeInGB:F2} GB");
            logger.Info($"缓存条目数: {GetCachedCount()}");
        }

        /// <summary>
        /// 获取缓存路径
        /// </summary>
        public string GetPath(string key, string extension = ".mp4")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                key = Guid.NewGuid().ToString("N");
            }

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".mp4";
            }

            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            return Path.Combine(cacheDirectory, key + extension);
        }

        /// <summary>
        /// 检查是否缓存
        /// </summary>
        public bool IsCached(string key)
        {
            if (metadata == null || metadata.entries == null) return false;

            var entry = metadata.entries.FirstOrDefault(e => e.key == key);
            if (entry == null) return false;

            string path = GetPath(key, Path.GetExtension(entry.path));
            return File.Exists(path);
        }

        /// <summary>
        /// 获取缓存的视频
        /// </summary>
        public VideoFile GetCachedVideo(string key)
        {
            if (metadata == null || metadata.entries == null) return null;

            var entry = metadata.entries.FirstOrDefault(e => e.key == key);
            if (entry == null) return null;

            string path = GetPath(key, Path.GetExtension(entry.path));
            if (!File.Exists(path))
            {
                logger.Warning($"缓存文件不存在: {path}");
                return null;
            }

            // 更新访问时间
            entry.lastAccessTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            SaveMetadata();

            return new VideoFile
            {
                name = Path.GetFileName(path),
                path = path,
                localPath = path,
                url = "file://" + path,
                isCached = true,
                size = entry.size
            };
        }

        /// <summary>
        /// 添加到缓存
        /// </summary>
        public bool AddToCache(string key, VideoFile video, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                logger.Error("缓存 key 不能为空");
                return false;
            }

            if (video == null)
            {
                logger.Error("视频文件不能为 null");
                return false;
            }

            try
            {
                string extension = string.IsNullOrWhiteSpace(Path.GetExtension(video.name)) 
                    ? Path.GetExtension(video.localPath) 
                    : Path.GetExtension(video.name);
                
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = ".mp4";
                }

                string path = GetPath(key, extension);

                // 写入文件
                File.WriteAllBytes(path, data);

                // 更新元数据
                var entry = metadata.entries.FirstOrDefault(e => e.key == key);
                if (entry == null)
                {
                    entry = new CacheEntry { key = key };
                    Array.Resize(ref metadata.entries, metadata.entries.Length + 1);
                    metadata.entries[metadata.entries.Length - 1] = entry;
                }

                entry.path = path;
                entry.size = data.Length;
                entry.lastAccessTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                entry.sourceUrl = video.url ?? video.path;

                SaveMetadata();

                logger.Info($"缓存已添加: {key} ({data.Length.SizeInMB:F2} MB)");

                OnCached?.Invoke(video);
                return true;
            }
            catch (Exception e)
            {
                logger.Error($"添加到缓存失败: {e.Message}", e);
                return false;
            }
        }

        /// <summary>
        /// 从缓存移除
        /// </summary>
        public bool RemoveFromCache(string key)
        {
            if (metadata == null || metadata.entries == null) return false;

            var entry = metadata.entries.FirstOrDefault(e => e.key == key);
            if (entry == null)
            {
                logger.Warning($"缓存条目不存在: {key}");
                return false;
            }

            try
            {
                if (File.Exists(entry.path))
                {
                    File.Delete(entry.path);
                    logger.Info($"缓存文件已删除: {entry.path}");
                }

                // 从元数据中移除
                var newEntries = metadata.entries.Where(e => e.key != key).ToArray();
                metadata.entries = newEntries;
                SaveMetadata();

                logger.Info($"缓存已移除: {key}");

                OnCacheRemoved?.Invoke(new VideoFile { name = key });
                return true;
            }
            catch (Exception e)
            {
                logger.Error($"从缓存移除失败: {e.Message}", e);
                return false;
            }
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public void ClearCache()
        {
            try
            {
                if (Directory.Exists(cacheDirectory))
                {
                    Directory.Delete(cacheDirectory, true);
                    Directory.CreateDirectory(cacheDirectory);
                    logger.Info("缓存已清空");
                }

                metadata = new CacheMetadata { entries = new CacheEntry[0] };
                SaveMetadata();
            }
            catch (Exception e)
            {
                logger.Error($"清空缓存失败: {e.Message}", e);
            }
        }

        /// <summary>
        /// 获取缓存大小
        /// </summary>
        public long GetTotalCacheSize()
        {
            if (metadata == null || metadata.entries == null) return 0;

            long total = 0;
            foreach (var entry in metadata.entries)
            {
                total += entry.size;
            }

            return total;
        }

        /// <summary>
        /// 获取缓存条目数量
        /// </summary>
        public int GetCachedCount()
        {
            return metadata?.entries?.Length ?? 0;
        }

        /// <summary>
        /// 获取缓存目录
        /// </summary>
        public string GetCacheDirectory()
        {
            return cacheDirectory;
        }

        /// <summary>
        /// 设置最大缓存大小
        /// </summary>
        public void SetMaxCacheSize(long maxSizeBytes)
        {
            maxCacheSizeBytes = maxSizeBytes;
            logger.Info($"最大缓存大小设置为: {maxSizeBytes.SizeInGB:F2} GB");
        }

        /// <summary>
        /// 设置缓存条目数量限制
        /// </summary>
        public void SetCacheEntryCountLimit(int limit)
        {
            cacheEntryCountLimit = limit;
            logger.Info($"缓存条目数量限制设置为: {limit}");
        }

        /// <summary>
        /// 清理旧缓存
        /// </summary>
        public void CleanupOldCache()
        {
            if (metadata == null || metadata.entries == null || metadata.entries.Length == 0)
            {
                return;
            }

            logger.Info("开始清理旧缓存...");

            int removed = 0;

            // 按访问时间排序，移除最旧的
            var sortedEntries = metadata.entries.OrderBy(e => e.lastAccessTime).ToList();

            // 如果超过大小限制，移除最旧的
            while (GetTotalCacheSize() > maxCacheSizeBytes && sortedEntries.Count > 0)
            {
                var entry = sortedEntries[0];
                if (RemoveFromCache(entry.key))
                {
                    removed++;
                    sortedEntries.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            // 如果超过数量限制，移除最旧的
            while (metadata.entries.Length > cacheEntryCountLimit && sortedEntries.Count > 0)
            {
                var entry = sortedEntries[0];
                if (RemoveFromCache(entry.key))
                {
                    removed++;
                    sortedEntries.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            logger.Info($"清理完成，移除了 {removed} 个缓存条目");
        }

        /// <summary>
        /// 获取所有缓存条目
        /// </summary>
        public VideoFile[] GetAllCachedVideos()
        {
            if (metadata == null || metadata.entries == null) return new VideoFile[0];

            var videos = new VideoFile[metadata.entries.Length];
            for (int i = 0; i < metadata.entries.Length; i++)
            {
                var entry = metadata.entries[i];
                videos[i] = new VideoFile
                {
                    name = entry.key,
                    path = entry.path,
                    localPath = entry.path,
                    url = "file://" + entry.path,
                    isCached = true,
                    size = entry.size
                };
            }

            return videos;
        }

        /// <summary>
        /// 加载元数据
        /// </summary>
        private void LoadMetadata()
        {
            string metadataPath = Path.Combine(cacheDirectory, METADATA_FILE_NAME);

            if (!File.Exists(metadataPath))
            {
                metadata = new CacheMetadata { entries = new CacheEntry[0] };
                return;
            }

            try
            {
                string json = File.ReadAllText(metadataPath);
                metadata = JsonUtility.FromJson<CacheMetadata>(json);

                if (metadata == null)
                {
                    metadata = new CacheMetadata { entries = new CacheEntry[0] };
                }

                if (metadata.entries == null)
                {
                    metadata.entries = new CacheEntry[0];
                }

                logger.Debug($"加载了 {metadata.entries.Length} 个缓存条目");
            }
            catch (Exception e)
            {
                logger.Error($"加载缓存元数据失败: {e.Message}", e);
                metadata = new CacheMetadata { entries = new CacheEntry[0] };
            }
        }

        /// <summary>
        /// 保存元数据
        /// </summary>
        private void SaveMetadata()
        {
            string metadataPath = Path.Combine(cacheDirectory, METADATA_FILE_NAME);

            try
            {
                string json = JsonUtility.ToJson(metadata, true);
                File.WriteAllText(metadataPath, json);
            }
            catch (Exception e)
            {
                logger.Error($"保存缓存元数据失败: {e.Message}", e);
            }
        }

        private void OnDestroy()
        {
            SaveMetadata();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveMetadata();
            }
        }
    }

    /// <summary>
    /// 文件大小扩展方法
    /// </summary>
    public static class FileSizeExtensions
    {
        public static float SizeInMB(this long bytes)
        {
            return bytes / (1024f * 1024f);
        }

        public static float SizeInGB(this long bytes)
        {
            return bytes / (1024f * 1024f * 1024f);
        }
    }
}
