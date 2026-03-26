using System;

namespace VRPlayer.Domain.Storage
{
    /// <summary>
    /// 存储访问接口 - 定义统一的文件选择器行为
    /// </summary>
    public interface IStorageAccess
    {
        /// <summary>
        /// 文件选择完成事件
        /// </summary>
        event Action<FilePickerResult> FileSelected;

        /// <summary>
        /// 打开文件选择器（单选）
        /// </summary>
        /// <param name="options">选择器选项</param>
        void OpenFilePicker(FilePickerOptions options);

        /// <summary>
        /// 打开文件选择器（多选）
        /// </summary>
        /// <param name="options">选择器选项</param>
        void OpenMultipleFilePicker(FilePickerOptions options);

        /// <summary>
        /// 打开目录选择器
        /// </summary>
        /// <param name="options">选择器选项</param>
        void OpenDirectoryPicker(FilePickerOptions options);

        /// <summary>
        /// 检查文件选择器是否可用
        /// </summary>
        bool IsAvailable { get; }
    }

    /// <summary>
    /// 文件选择器选项
    /// </summary>
    [Serializable]
    public class FilePickerOptions
    {
        /// <summary>
        /// 允许的文件扩展名
        /// </summary>
        public string[] AllowedExtensions { get; set; }

        /// <summary>
        /// 选择器标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 是否允许多选
        /// </summary>
        public bool AllowMultiple { get; set; }

        /// <summary>
        /// 初始目录路径
        /// </summary>
        public string InitialDirectory { get; set; }

        /// <summary>
        /// 选择器模式
        /// </summary>
        public PickerMode Mode { get; set; } = PickerMode.File;

        /// <summary>
        /// MIME类型过滤器（用于Android）
        /// </summary>
        public string[] MimeTypes { get; set; }

        public FilePickerOptions()
        {
            AllowedExtensions = new[] { ".mp4", ".mkv", ".mov" };
            Title = "Select Video";
            AllowMultiple = false;
            MimeTypes = new string[0];
        }
    }

    /// <summary>
    /// 选择器模式
    /// </summary>
    public enum PickerMode
    {
        /// <summary>
        /// 选择文件
        /// </summary>
        File,

        /// <summary>
        /// 选择目录
        /// </summary>
        Directory,

        /// <summary>
        /// 保存文件
        /// </summary>
        Save
    }

    /// <summary>
    /// 文件选择结果
    /// </summary>
    [Serializable]
    public class FilePickerResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// 选中的文件路径列表
        /// </summary>
        public string[] SelectedPaths { get; set; }

        /// <summary>
        /// 选中的目录路径
        /// </summary>
        public string SelectedDirectory { get; set; }

        /// <summary>
        /// 保存的文件路径（仅用于Save模式）
        /// </summary>
        public string SavedPath { get; set; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 用户是否取消操作
        /// </summary>
        public bool Cancelled { get; set; }

        public FilePickerResult()
        {
            SelectedPaths = new string[0];
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static FilePickerResult SuccessResult(string[] paths)
        {
            return new FilePickerResult
            {
                Success = true,
                SelectedPaths = paths ?? new string[0]
            };
        }

        /// <summary>
        /// 创建取消结果
        /// </summary>
        public static FilePickerResult CancelResult()
        {
            return new FilePickerResult
            {
                Success = false,
                Cancelled = true
            };
        }

        /// <summary>
        /// 创建错误结果
        /// </summary>
        public static FilePickerResult ErrorResult(string errorMessage)
        {
            return new FilePickerResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
