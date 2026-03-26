using UnityEngine;

namespace VRPlayer.Core.Config
{
    /// <summary>
    /// 扫描配置
    /// </summary>
    [System.Serializable]
    public class ScanConfig
    {
        [Header("扫描深度")]
        [Range(0, 5)]
        public int scanDepth = 2;

        [Header("扫描限制")]
        [Range(20, 500)]
        public int maxVideos = 200;

        [Range(1, 100)]
        public int maxConcurrentScans = 10;

        [Header("扫描目录")]
        public string[] scanDirectories = { "Movies", "Downloads" };

        public bool includeSubdirectories = true;
        public bool includeMoviesDirectory = true;

        [Header("Android特定")]
#if UNITY_ANDROID
        public string androidMoviesDirectory = "/storage/emulated/0/Movies";
#else
        public string androidMoviesDirectory = "";
#endif

        public bool includeMoviesSubdirectories = true;

        [Header("文件过滤")]
        public string[] supportedExtensions = { ".mp4", ".mkv", ".mov" };
        public long minFileSizeBytes = 1024 * 1024; // 1MB
        public long maxFileSizeBytes = 10L * 1024 * 1024 * 1024; // 10GB

        public ScanConfig()
        {
            // 默认值已在字段初始化中设置
        }

        /// <summary>
        /// 从配置管理器加载
        /// </summary>
        public static ScanConfig Load(IAppConfig config)
        {
            string json = config.Get("scan_config", "{}");
            if (string.IsNullOrEmpty(json) || json == "{}")
            {
                return new ScanConfig();
            }

            try
            {
                return JsonUtility.FromJson<ScanConfig>(json);
            }
            catch
            {
                return new ScanConfig();
            }
        }

        /// <summary>
        /// 保存到配置管理器
        /// </summary>
        public void Save(IAppConfig config)
        {
            string json = JsonUtility.ToJson(this, true);
            config.Set("scan_config", json);
        }
    }
}
