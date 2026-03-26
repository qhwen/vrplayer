using System;
using System.Collections.Generic;

namespace VRPlayer.Core.Config
{
    /// <summary>
    /// 应用配置管理器 - 使用 PlayerPrefs 持久化配置
    /// </summary>
    public class AppConfigManager : IAppConfig
    {
        private static AppConfigManager instance;
        public static AppConfigManager Instance => instance ?? (instance = new AppConfigManager());

        private readonly Dictionary<string, object> configCache = new Dictionary<string, object>();
        private const string ConfigPrefix = "vrplayer_config_";

        private AppConfigManager()
        {
            Load();
        }

        /// <summary>
        /// 获取配置值
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            string fullKey = ConfigPrefix + key;

            // 先从缓存中获取
            if (configCache.TryGetValue(fullKey, out object cachedValue))
            {
                return (T)cachedValue;
            }

            // 从 PlayerPrefs 获取
            try
            {
                string value = UnityEngine.PlayerPrefs.GetString(fullKey, null);
                if (!string.IsNullOrEmpty(value))
                {
                    if (typeof(T) == typeof(string))
                    {
                        configCache[fullKey] = value;
                        return (T)(object)value;
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        if (int.TryParse(value, out int intValue))
                        {
                            configCache[fullKey] = intValue;
                            return (T)(object)intValue;
                        }
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        if (float.TryParse(value, out float floatValue))
                        {
                            configCache[fullKey] = floatValue;
                            return (T)(object)floatValue;
                        }
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        if (bool.TryParse(value, out bool boolValue))
                        {
                            configCache[fullKey] = boolValue;
                            return (T)(object)boolValue;
                        }
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        if (double.TryParse(value, out double doubleValue))
                        {
                            configCache[fullKey] = doubleValue;
                            return (T)(object)doubleValue;
                        }
                    }
                    else if (typeof(T) == typeof(long))
                    {
                        if (long.TryParse(value, out long longValue))
                        {
                            configCache[fullKey] = longValue;
                            return (T)(object)longValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[AppConfigManager] Error getting config '{key}': {ex.Message}");
            }

            return defaultValue;
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        public void Set<T>(string key, T value)
        {
            if (value == null)
            {
                UnityEngine.Debug.LogWarning($"[AppConfigManager] Cannot set null value for key '{key}'");
                return;
            }

            string fullKey = ConfigPrefix + key;

            // 更新缓存
            configCache[fullKey] = value;

            // 持久化到 PlayerPrefs
            try
            {
                string stringValue = ConvertToString(value);
                UnityEngine.PlayerPrefs.SetString(fullKey, stringValue);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[AppConfigManager] Error setting config '{key}': {ex.Message}");
            }
        }

        /// <summary>
        /// 检查配置键是否存在
        /// </summary>
        public bool HasKey(string key)
        {
            string fullKey = ConfigPrefix + key;
            return UnityEngine.PlayerPrefs.HasKey(fullKey) || configCache.ContainsKey(fullKey);
        }

        /// <summary>
        /// 删除配置键
        /// </summary>
        public void Remove(string key)
        {
            string fullKey = ConfigPrefix + key;
            UnityEngine.PlayerPrefs.DeleteKey(fullKey);
            configCache.Remove(fullKey);
        }

        /// <summary>
        /// 保存所有配置
        /// </summary>
        public void Save()
        {
            UnityEngine.PlayerPrefs.Save();
        }

        /// <summary>
        /// 加载所有配置（已在构造函数中调用）
        /// </summary>
        public void Load()
        {
            // 配置在 Get 时懒加载，这里可以加载预设配置
        }

        /// <summary>
        /// 清空所有配置
        /// </summary>
        public void Clear()
        {
            UnityEngine.PlayerPrefs.DeleteAll();
            configCache.Clear();
        }

        private string ConvertToString<T>(T value)
        {
            if (value is string str)
            {
                return str;
            }
            else if (value is IConvertible convertible)
            {
                return convertible.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                return value?.ToString() ?? "";
            }
        }

        /// <summary>
        /// 获取播放器配置
        /// </summary>
        public PlaybackConfig GetPlaybackConfig()
        {
            return PlaybackConfig.Load(this);
        }

        /// <summary>
        /// 保存播放器配置
        /// </summary>
        public void SavePlaybackConfig(PlaybackConfig config)
        {
            config.Save(this);
            Save();
        }

        /// <summary>
        /// 获取扫描配置
        /// </summary>
        public ScanConfig GetScanConfig()
        {
            return ScanConfig.Load(this);
        }

        /// <summary>
        /// 保存扫描配置
        /// </summary>
        public void SaveScanConfig(ScanConfig config)
        {
            config.Save(this);
            Save();
        }

        /// <summary>
        /// 获取缓存配置
        /// </summary>
        public CacheConfig GetCacheConfig()
        {
            return CacheConfig.Load(this);
        }

        /// <summary>
        /// 保存缓存配置
        /// </summary>
        public void SaveCacheConfig(CacheConfig config)
        {
            config.Save(this);
            Save();
        }
    }

    /// <summary>
    /// 便捷访问类
    /// </summary>
    public static class Config
    {
        public static IAppConfig Instance => AppConfigManager.Instance;

        public static T Get<T>(string key, T defaultValue = default)
        {
            return Instance.Get(key, defaultValue);
        }

        public static void Set<T>(string key, T value)
        {
            Instance.Set(key, value);
        }

        public static void Save()
        {
            Instance.Save();
        }

        public static PlaybackConfig Playback => Instance.GetPlaybackConfig();
        public static ScanConfig Scan => Instance.GetScanConfig();
        public static CacheConfig Cache => Instance.GetCacheConfig();
    }
}
