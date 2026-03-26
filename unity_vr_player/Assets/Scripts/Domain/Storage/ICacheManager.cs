using System;
using System.Threading;
using System.Threading.Tasks;

namespace VRPlayer.Domain.Storage
{
    /// <summary>
    /// 缓存管理接口 - 定义视频文件缓存管理行为
    /// </summary>
    public interface ICacheManager
    {
        /// <summary>
        /// 缓存目录路径
        /// </summary>
        string CacheDirectory { get; }

        /// <summary>
        /// 获取缓存文件路径
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>完整路径</returns>
        string GetCachePath(string key, string extension = ".mp4");

        /// <summary>
        /// 检查缓存是否存在
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>是否存在</returns>
        bool Exists(string key, string extension = ".mp4");

        /// <summary>
        /// 异步存储到缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="extension">文件扩展名</param>
        /// <param name="onProgress">进度回调（0.0 - 1.0）</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        Task<bool> StoreAsync(
            string key,
            string sourcePath,
            string extension = ".mp4",
            Action<float> onProgress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 从缓存中移除
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="extension">文件扩展名</param>
        /// <returns>是否成功</returns>
        bool Evict(string key, string extension = ".mp4");

        /// <summary>
        /// 获取缓存总大小（字节）
        /// </summary>
        long GetTotalSizeBytes();

        /// <summary>
        /// 获取缓存可用空间（字节）
        /// </summary>
        long GetFreeSpaceBytes();

        /// <summary>
        /// 获取缓存文件数量
        /// </summary>
        int GetFileCount();

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        void Clear();

        /// <summary>
        /// 异步清理缓存
        /// </summary>
        /// <param name="options">清理选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>清理结果</returns>
        Task<CacheCleanupResult> CleanupAsync(
            CleanupOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 获取缓存信息
        /// </summary>
        CacheInfo GetCacheInfo();
    }

    /// <summary>
    /// 清理选项
    /// </summary>
    [Serializable]
    public class CleanupOptions
    {
        /// <summary>
        /// 目标可用空间（字节）
        /// </summary>
        public long TargetFreeBytes { get; set; }

        /// <summary>
        /// 最大缓存年龄
        /// </summary>
        public TimeSpan? MaxAge { get; set; }

        /// <summary>
        /// 清理策略
        /// </summary>
        public CleanupStrategy Strategy { get; set; } = CleanupStrategy.LRU;

        /// <summary>
        /// 是否在清理前确认
        /// </summary>
        public bool RequireConfirmation { get; set; } = false;

        /// <summary>
        /// 要保留的文件键列表
        /// </summary>
        public string[] KeepKeys { get; set; } = new string[0];

        public CleanupOptions()
        {
        }
    }

    /// <summary>
    /// 缓存清理结果
    /// </summary>
    [Serializable]
    public class CacheCleanupResult
    {
        /// <summary>
        /// 释放的字节数
        /// </summary>
        public long BytesFreed { get; set; }

        /// <summary>
        /// 删除的文件数量
        /// </summary>
        public int FilesRemoved { get; set; }

        /// <summary>
        /// 新的缓存总大小
        /// </summary>
        public long NewTotalSize { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 清理耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        public CacheCleanupResult()
        {
        }
    }

    /// <summary>
    /// 缓存清理策略
    /// </summary>
    public enum CleanupStrategy
    {
        /// <summary>
        /// 最近最少使用（LRU）
        /// </summary>
        LRU,

        /// <summary>
        /// 先进先出（FIFO）
        /// </summary>
        FIFO,

        /// <summary>
        /// 最旧优先
        /// </summary>
        Oldest,

        /// <summary>
        /// 文件大小优先（大文件优先删除）
        /// </summary>
        SizeFirst,

        /// <summary>
        /// 最少访问优先
        /// </summary>
        LeastAccessed
    }

    /// <summary>
    /// 缓存信息
    /// </summary>
    [Serializable]
    public class CacheInfo
    {
        /// <summary>
        /// 缓存目录
        /// </summary>
        public string CacheDirectory { get; set; }

        /// <summary>
        /// 总大小（字节）
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// 可用空间（字节）
        /// </summary>
        public long FreeSpaceBytes { get; set; }

        /// <summary>
        /// 文件数量
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// 格式化的总大小
        /// </summary>
        public string FormattedTotalSize { get; set; }

        /// <summary>
        /// 格式化的可用空间
        /// </summary>
        public string FormattedFreeSpace { get; set; }

        public CacheInfo()
        {
        }
    }
}
