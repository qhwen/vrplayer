using System;
using System.Threading.Tasks;

namespace VRPlayer.Domain.Storage
{
    /// <summary>
    /// 权限管理接口 - 定义权限请求和管理行为
    /// </summary>
    public interface IPermissionManager
    {
        /// <summary>
        /// 权限改变事件
        /// </summary>
        event Action<PermissionType, PermissionStatus> PermissionChanged;

        /// <summary>
        /// 异步检查权限状态
        /// </summary>
        /// <param name="permission">权限类型</param>
        /// <returns>权限状态</returns>
        Task<PermissionStatus> CheckPermissionAsync(PermissionType permission);

        /// <summary>
        /// 异步请求权限
        /// </summary>
        /// <param name="permission">权限类型</param>
        /// <returns>请求结果</returns>
        Task<PermissionRequestResult> RequestPermissionAsync(PermissionType permission);

        /// <summary>
        /// 是否应该显示权限请求说明
        /// </summary>
        /// <param name="permission">权限类型</param>
        /// <returns>是否应该显示</returns>
        bool ShouldShowRequestRationale(PermissionType permission);

        /// <summary>
        /// 打开应用设置页面
        /// </summary>
        void OpenAppSettings();

        /// <summary>
        /// 检查权限请求是否正在进行中
        /// </summary>
        bool IsRequestInFlight { get; }
    }

    /// <summary>
    /// 权限类型
    /// </summary>
    public enum PermissionType
    {
        /// <summary>
        /// 读取媒体视频（Android 13+）
        /// </summary>
        ReadMediaVideo,

        /// <summary>
        /// 读取外部存储（Android 10-12）
        /// </summary>
        ReadExternalStorage,

        /// <summary>
        /// 写入外部存储
        /// </summary>
        WriteExternalStorage,

        /// <summary>
        /// 相机权限
        /// </summary>
        Camera,

        /// <summary>
        /// 麦克风权限
        /// </summary>
        Microphone,

        /// <summary>
        /// 互联网权限
        /// </summary>
        Internet,

        /// <summary>
        /// 自定义权限
        /// </summary>
        Custom
    }

    /// <summary>
    /// 权限状态
    /// </summary>
    public enum PermissionStatus
    {
        /// <summary>
        /// 未请求
        /// </summary>
        NotRequested,

        /// <summary>
        /// 已授予
        /// </summary>
        Granted,

        /// <summary>
        /// 已拒绝（但可以再次请求）
        /// </summary>
        Denied,

        /// <summary>
        /// 已永久拒绝（用户选择了"不再询问"）
        /// </summary>
        DeniedPermanently,

        /// <summary>
        /// 未知状态
        /// </summary>
        Unknown
    }

    /// <summary>
    /// 权限请求结果
    /// </summary>
    [Serializable]
    public class PermissionRequestResult
    {
        /// <summary>
        /// 权限状态
        /// </summary>
        public PermissionStatus Status { get; set; }

        /// <summary>
        /// 是否永久拒绝
        /// </summary>
        public bool IsPermanentlyDenied { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 用户是否选择了"不再询问"
        /// </summary>
        public bool DontAskAgain { get; set; }

        public PermissionRequestResult()
        {
        }
    }
}
