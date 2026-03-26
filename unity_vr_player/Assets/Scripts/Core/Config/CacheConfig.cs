using UnityEngine;

namespace VRPlayer.Core.Config
{
    /// <summary>
    /// 缓存配置
    /// </summary>
    [System.Serializable]
    public class CacheConfig
    {
        [Header("缓存大小限制")]
        public long maxCacheSizeBytes = 2L * 1024 * 1024 * 1024; // 2GB

        [Range(0.1f, 0.9f)]
        public float cleanupThreshold = 0.8f; // 当使用率达到80%时触发清理

        [Header("清理策略")]
        public CleanupStrategy cleanupStrategy = CleanupStrategy.LRU;

        [Header("缓存生命周期")]
        public long maxCacheAgeDays = 30;
        public bool autoCleanupOnAppStart = true;

        [Header("并发控制")]
        [Range(1, 10)]
        public int maxConcurrentDownloads = 3;

        [Range(10, 300)]
        public int downloadTimeoutSeconds = 60;

        public enum CleanupStrategy
        {
            LRU,        // 最近最少使用
            FIFO,       // 先进先出
            Oldest,     // 最旧优先
            SizeFirst   // 文件大小优先
        }

        public CacheConfig()
        {
            // 默认值已在字段初始化中设置
        }

        /// <summary>
        /// 从配置管理器加载
        /// </summary>
        public static CacheConfig Load(IAppConfig config)
        {
            string json = config.Get("cache_config", "{}");
            if (string.IsNullOrEmpty(json) || json == "{}")
            {
                return new CacheConfig();
            }

            try
            {
                return JsonUtility.FromJson<CacheConfig>(json);
            }
            catch
            {
                return new CacheConfig();
            }
        }

        /// <summary>
        /// 保存到配置管理器
        /// </summary>
        public void Save(IAppConfig config)
        {
            string json = JsonUtility.ToJson(this, true);
            config.Set("cache_config", json);
        }

        /// <summary>
        /// 获取格式化的缓存大小限制
        /// </summary>
        public string GetFormattedMaxCacheSize()
        {
            return FormatBytes(maxCacheSizeBytes);
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
