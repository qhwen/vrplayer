namespace VRPlayer.Core.Config
{
    /// <summary>
    /// 应用配置接口
    /// </summary>
    public interface IAppConfig
    {
        /// <summary>
        /// 获取配置值
        /// </summary>
        T Get<T>(string key, T defaultValue = default);

        /// <summary>
        /// 设置配置值
        /// </summary>
        void Set<T>(string key, T value);

        /// <summary>
        /// 检查配置键是否存在
        /// </summary>
        bool HasKey(string key);

        /// <summary>
        /// 删除配置键
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// 保存所有配置
        /// </summary>
        void Save();

        /// <summary>
        /// 加载所有配置
        /// </summary>
        void Load();

        /// <summary>
        /// 清空所有配置
        /// </summary>
        void Clear();
    }
}
