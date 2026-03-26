using System;

namespace VRPlayer.Domain.Entities
{
    /// <summary>
    /// 视频文件实体 - 增强版，包含更多元数据和状态信息
    /// </summary>
    [Serializable]
    public class VideoFile
    {
        #region 基本信息

        /// <summary>
        /// 文件名（不含扩展名）
        /// </summary>
        public string name;

        /// <summary>
        /// 唯一标识路径（远程URL或本地路径）
        /// </summary>
        public string path;

        /// <summary>
        /// 实际访问URL
        /// </summary>
        public string url;

        /// <summary>
        /// 本地缓存路径
        /// </summary>
        public string localPath;

        #endregion

        #region 元数据

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long size;

        /// <summary>
        /// 时长（秒）
        /// </summary>
        public long duration;

        /// <summary>
        /// 视频宽度（像素）
        /// </summary>
        public int width;

        /// <summary>
        /// 视频高度（像素）
        /// </summary>
        public int height;

        /// <summary>
        /// 编解码器
        /// </summary>
        public string codec;

        /// <summary>
        /// 帧率
        /// </summary>
        public float frameRate;

        /// <summary>
        /// 比特率（kbps）
        /// </summary>
        public long bitRate;

        #endregion

        #region VR 特性

        /// <summary>
        /// 是否为360°视频
        /// </summary>
        public bool is360;

        /// <summary>
        /// 是否为立体视频
        /// </summary>
        public bool isStereo;

        /// <summary>
        /// 投影类型
        /// </summary>
        public VrProjectionType projectionType;

        /// <summary>
        /// VR立体格式
        /// </summary>
        public VrStereoFormat stereoFormat;

        #endregion

        #region 状态

        /// <summary>
        /// 是否为远程视频
        /// </summary>
        public bool isRemote;

        /// <summary>
        /// 是否已缓存
        /// </summary>
        public bool isCached;

        /// <summary>
        /// 缓存时间
        /// </summary>
        public DateTime? cacheDate;

        /// <summary>
        /// 播放次数
        /// </summary>
        public int playCount;

        /// <summary>
        /// 最后播放时间
        /// </summary>
        public DateTime? lastPlayDate;

        #endregion

        #region 来源

        /// <summary>
        /// 来源类型
        /// </summary>
        public VideoSourceType sourceType;

        /// <summary>
        /// 来源名称（如WebDAV服务器名）
        /// </summary>
        public string sourceName;

        #endregion

        #region 附加信息

        /// <summary>
        /// 缩略图URL或路径
        /// </summary>
        public string thumbnail;

        /// <summary>
        /// 元数据JSON
        /// </summary>
        public string metadata;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime creationDate;

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime? modificationDate;

        #endregion

        #region 构造函数

        public VideoFile()
        {
            creationDate = DateTime.Now;
            projectionType = VrProjectionType.Equirectangular;
            stereoFormat = VrStereoFormat.None;
            sourceType = VideoSourceType.Local;
        }

        public VideoFile(string name, string path) : this()
        {
            this.name = name;
            this.path = path;
            this.url = path;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取格式化的文件大小
        /// </summary>
        public string GetFormattedSize()
        {
            if (size == 0) return "Unknown";

            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double sizeInBytes = size;

            while (sizeInBytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                sizeInBytes /= 1024;
            }

            return $"{sizeInBytes:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 获取格式化的时长
        /// </summary>
        public string GetFormattedDuration()
        {
            if (duration == 0) return "Unknown";

            TimeSpan time = TimeSpan.FromSeconds(duration);
            if (time.TotalHours >= 1)
            {
                return $"{time.Hours}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
            else
            {
                return $"{time.Minutes}:{time.Seconds:D2}";
            }
        }

        /// <summary>
        /// 是否是有效的视频文件
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(path);
        }

        /// <summary>
        /// 克隆副本
        /// </summary>
        public VideoFile Clone()
        {
            return new VideoFile
            {
                name = this.name,
                path = this.path,
                url = this.url,
                localPath = this.localPath,
                size = this.size,
                duration = this.duration,
                width = this.width,
                height = this.height,
                codec = this.codec,
                frameRate = this.frameRate,
                bitRate = this.bitRate,
                is360 = this.is360,
                isStereo = this.isStereo,
                projectionType = this.projectionType,
                stereoFormat = this.stereoFormat,
                isRemote = this.isRemote,
                isCached = this.isCached,
                cacheDate = this.cacheDate,
                playCount = this.playCount,
                lastPlayDate = this.lastPlayDate,
                sourceType = this.sourceType,
                sourceName = this.sourceName,
                thumbnail = this.thumbnail,
                metadata = this.metadata,
                creationDate = this.creationDate,
                modificationDate = this.modificationDate
            };
        }

        #endregion

        #region 枚举定义

        /// <summary>
        /// 视频来源类型
        /// </summary>
        public enum VideoSourceType
        {
            Local,           // 本地文件
            WebDAV,          // WebDAV服务器
            S3,             // AWS S3
            AzureBlob,      // Azure Blob Storage
            HTTP,           // HTTP/HTTPS URL
            Custom          // 自定义来源
        }

        /// <summary>
        /// VR投影类型
        /// </summary>
        public enum VrProjectionType
        {
            Equirectangular,  // 等距柱状投影（360°）
            CubeMap,          // 立方体贴图
            Fisheye,          // 鱼眼投影
            Cylindrical       // 柱面投影
        }

        /// <summary>
        /// VR立体格式
        /// </summary>
        public enum VrStereoFormat
        {
            None,            // 非立体
            SideBySide,      // 左右分屏
            TopBottom,       // 上下分屏
            LeftRight        // 左右眼（需要特定播放器）
        }

        #endregion
    }
}
