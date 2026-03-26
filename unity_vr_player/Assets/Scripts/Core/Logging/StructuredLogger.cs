using System;
using System.Text;

namespace VRPlayer.Core.Logging
{
    /// <summary>
    /// 结构化日志记录器 - 提供统一的日志格式和上下文支持
    /// </summary>
    public class StructuredLogger : ILogger
    {
        public LogLevel MinLevel { get; set; } = LogLevel.Info;
        public string ModuleName { get; private set; }

        private const string TimestampFormat = "HH:mm:ss.fff";

        public StructuredLogger(string moduleName, LogLevel minLevel = LogLevel.Info)
        {
            ModuleName = moduleName;
            MinLevel = minLevel;
        }

        public void Debug(string message, object context = null)
        {
            if (MinLevel > LogLevel.Debug) return;
            Log("DEBUG", message, context);
        }

        public void Info(string message, object context = null)
        {
            if (MinLevel > LogLevel.Info) return;
            Log("INFO", message, context);
        }

        public void Warning(string message, object context = null)
        {
            if (MinLevel > LogLevel.Warning) return;
            Log("WARN", message, context);
        }

        public void Error(string message, Exception exception = null, object context = null)
        {
            if (MinLevel > LogLevel.Error) return;

            var sb = new StringBuilder(message);
            if (exception != null)
            {
                sb.AppendLine();
                sb.Append($"Exception: {exception.GetType().Name}");
                sb.AppendLine();
                sb.Append(exception.Message);

                if (exception.StackTrace != null)
                {
                    sb.AppendLine();
                    sb.Append(exception.StackTrace);
                }

                if (exception.InnerException != null)
                {
                    sb.AppendLine();
                    sb.Append($"Inner Exception: {exception.InnerException.Message}");
                }
            }

            Log("ERROR", sb.ToString(), context);
        }

        public ILogger CreateChildLogger(string subModule)
        {
            string fullModuleName = string.IsNullOrEmpty(subModule) ? ModuleName : $"{ModuleName}.{subModule}";
            return new StructuredLogger(fullModuleName, MinLevel);
        }

        private void Log(string level, string message, object context = null)
        {
            string timestamp = DateTime.Now.ToString(TimestampFormat);
            string contextStr = context != null ? $"[{context.GetType().Name}] " : "";
            string logMessage = $"[{timestamp}] [{level}] [{ModuleName}] {contextStr}{message}";

            // 使用Unity的日志系统
            switch (level)
            {
                case "DEBUG":
                    UnityEngine.Debug.Log(logMessage);
                    break;
                case "INFO":
                    UnityEngine.Debug.Log(logMessage);
                    break;
                case "WARN":
                    UnityEngine.Debug.LogWarning(logMessage);
                    break;
                case "ERROR":
                    UnityEngine.Debug.LogError(logMessage);
                    break;
            }
        }
    }
}
