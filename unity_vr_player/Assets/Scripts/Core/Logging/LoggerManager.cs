using System.Collections.Generic;

namespace VRPlayer.Core.Logging
{
    /// <summary>
    /// 日志管理器 - 管理多个模块的日志记录器
    /// </summary>
    public class LoggerManager
    {
        private static LoggerManager instance;
        public static LoggerManager Instance => instance ?? (instance = new LoggerManager());

        private readonly Dictionary<string, ILogger> loggers = new Dictionary<string, ILogger>();
        private LogLevel globalMinLevel = LogLevel.Info;

        private LoggerManager() { }

        /// <summary>
        /// 设置全局最低日志级别
        /// </summary>
        public void SetGlobalMinLevel(LogLevel level)
        {
            globalMinLevel = level;

            foreach (var logger in loggers.Values)
            {
                logger.MinLevel = level;
            }
        }

        /// <summary>
        /// 获取或创建日志记录器
        /// </summary>
        public ILogger GetLogger(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName))
            {
                moduleName = "Default";
            }

            if (loggers.TryGetValue(moduleName, out var logger))
            {
                return logger;
            }

            logger = new StructuredLogger(moduleName, globalMinLevel);
            loggers[moduleName] = logger;
            return logger;
        }

        /// <summary>
        /// 获取日志记录器（静态方法，便于快速访问）
        /// </summary>
        public static ILogger For(string moduleName)
        {
            return Instance.GetLogger(moduleName);
        }

        /// <summary>
        /// 清理所有日志记录器
        /// </summary>
        public void Clear()
        {
            loggers.Clear();
        }
    }

    /// <summary>
    /// 便捷访问类
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 获取默认日志记录器
        /// </summary>
        public static ILogger Default => LoggerManager.For("Default");

        /// <summary>
        /// 获取指定模块的日志记录器
        /// </summary>
        public static ILogger For(string moduleName)
        {
            return LoggerManager.For(moduleName);
        }

        /// <summary>
        /// 设置全局日志级别
        /// </summary>
        public static void SetLevel(LogLevel level)
        {
            LoggerManager.Instance.SetGlobalMinLevel(level);
        }
    }
}
