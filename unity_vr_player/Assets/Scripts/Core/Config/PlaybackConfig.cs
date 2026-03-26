using UnityEngine;

namespace VRPlayer.Core.Config
{
    /// <summary>
    /// 播放器配置
    /// </summary>
    [System.Serializable]
    public class PlaybackConfig
    {
        [Header("播放设置")]
        [Range(5f, 120f)]
        public float prepareTimeoutSeconds = 30f;

        public bool autoPlayOnOpen = false;
        public bool loopPlayback = false;

        [Range(0f, 1f)]
        public float initialVolume = 1f;

        [Header("渲染设置")]
        public int renderTextureWidth = 1920;
        public int renderTextureHeight = 1080;

        [Header("VR设置")]
        public bool enableHeadTracking = true;
        public float rotationSensitivity = 0.5f;

        [Range(0.01f, 1f)]
        public float smoothingFactor = 0.1f;

        public bool enablePointerDrag = true;

        public PlaybackConfig()
        {
            // 默认值已在字段初始化中设置
        }

        /// <summary>
        /// 从配置管理器加载
        /// </summary>
        public static PlaybackConfig Load(IAppConfig config)
        {
            string json = config.Get("playback_config", "{}");
            if (string.IsNullOrEmpty(json) || json == "{}")
            {
                return new PlaybackConfig();
            }

            try
            {
                return JsonUtility.FromJson<PlaybackConfig>(json);
            }
            catch
            {
                return new PlaybackConfig();
            }
        }

        /// <summary>
        /// 保存到配置管理器
        /// </summary>
        public void Save(IAppConfig config)
        {
            string json = JsonUtility.ToJson(this, true);
            config.Set("playback_config", json);
        }
    }
}
