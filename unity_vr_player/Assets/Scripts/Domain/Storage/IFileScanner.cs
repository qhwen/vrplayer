using System;
using System.Threading;
using System.Threading.Tasks;

namespace VRPlayer.Domain.Storage
{
    /// <summary>
    /// 文件扫描接口 - 定义视频文件扫描行为
    /// </summary>
    public interface IFileScanner
    {
        /// <summary>
        /// 扫描进度事件
        /// </summary>
        event Action<ScanProgress> ScanProgress;

        /// <summary>
        /// 异步扫描视频文件
        /// </summary>
        /// <param name="options">扫描选项</param>
        /// <returns>扫描结果</returns>
        Task<ScanResult> ScanAsync(ScanOptions options);

        /// <summary>
        /// 取消扫描
        /// </summary>
        void CancelScan();

        /// <summary>
        /// 检查是否正在扫描
        /// </summary>
        bool IsScanning { get; }
    }

    /// <summary>
    /// 扫描选项
    /// </summary>
    [Serializable]
    public class ScanOptions
    {
        /// <summary>
        /// 要扫描的目录列表
        /// </summary>
        public string[] Directories { get; set; }

        /// <summary>
        /// 最大扫描深度（0表示仅顶层目录）
        /// </summary>
        public int MaxDepth { get; set; } = 2;

        /// <summary>
        /// 最大结果数量
        /// </summary>
        public int MaxResults { get; set; } = 200;

        /// <summary>
        /// 是否包含子目录
        /// </summary>
        public bool IncludeSubdirectories { get; set; } = true;

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// 支持的文件扩展名
        /// </summary>
        public string[] AllowedExtensions { get; set; } = { ".mp4", ".mkv", ".mov" };

        /// <summary>
        /// 最小文件大小（字节）
        /// </summary>
        public long MinFileSize { get; set; } = 1024 * 1024; // 1MB

        /// <summary>
        /// 最大文件大小（字节）
        /// </summary>
        public long MaxFileSize { get; set; } = 10L * 1024 * 1024 * 1024; // 10GB

        public ScanOptions()
        {
            Directories = new string[0];
        }
    }

    /// <summary>
    /// 扫描结果
    /// </summary>
    [Serializable]
    public class ScanResult
    {
        /// <summary>
        /// 找到的视频文件列表
        /// </summary>
        public Entities.VideoFile[] Videos { get; set; }

        /// <summary>
        /// 扫描的总文件数
        /// </summary>
        public int TotalScanned { get; set; }

        /// <summary>
        /// 扫描耗时
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 是否被取消
        /// </summary>
        public bool IsCancelled { get; set; }

        public ScanResult()
        {
            Videos = new Entities.VideoFile[0];
        }
    }

    /// <summary>
    /// 扫描进度
    /// </summary>
    [Serializable]
    public class ScanProgress
    {
        /// <summary>
        /// 已找到的视频数量
        /// </summary>
        public int FoundCount { get; set; }

        /// <summary>
        /// 已扫描的文件数量
        /// </summary>
        public int ScannedCount { get; set; }

        /// <summary>
        /// 进度百分比（0.0 - 1.0）
        /// </summary>
        public float ProgressPercentage { get; set; }

        /// <summary>
        /// 当前扫描的目录
        /// </summary>
        public string CurrentDirectory { get; set; }

        /// <summary>
        /// 当前扫描的文件
        /// </summary>
        public string CurrentFile { get; set; }

        /// <summary>
        /// 估计剩余时间（秒）
        /// </summary>
        public int EstimatedRemainingSeconds { get; set; }

        public ScanProgress()
        {
        }
    }
}
