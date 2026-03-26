using System;

namespace VRPlayer.Core.Logging
{
    /// <summary>
    /// 日志级别枚举
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        None = 99
    }

    /// <summary>
    /// 统一的日志接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 设置最低日志级别
        /// </summary>
        LogLevel MinLevel { get; set; }

        /// <summary>
        /// 获取或设置模块名称
        /// </summary>
        string ModuleName { get; set; }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        void Debug(string message, object context = null);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        void Info(string message, object context = null);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        void Warning(string message, object context = null);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        void Error(string message, Exception exception = null, object context = null);

        /// <summary>
        /// 创建子日志记录器
        /// </summary>
        ILogger CreateChildLogger(string subModule);
    }
}
