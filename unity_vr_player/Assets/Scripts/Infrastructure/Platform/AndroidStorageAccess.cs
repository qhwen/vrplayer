using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VRPlayer.Infrastructure.Platform
{
    /// <summary>
    /// Android 存储访问实现
    /// 负责文件选择器和文件操作
    /// </summary>
    public class AndroidStorageAccess : MonoBehaviour, IStorageAccess
    {
        private const string PICKED_VIDEOS_PREFS_KEY = "local_picked_videos_v2";

        private readonly List<VideoFile> pickedVideos = new List<VideoFile>();
        private VRPlayer.Core.Logging.ILogger logger;

        public event Action<VideoFile> OnFilePicked;
        public event Action<VideoFile> OnFileDeleted;

        private void Awake()
        {
            logger = VRPlayer.Core.Logging.LoggerManager.For("AndroidStorageAccess");
            LoadPickedVideos();
            logger.Info($"AndroidStorageAccess initialized, loaded {pickedVideos.Count} picked videos");
        }

        /// <summary>
        /// 打开文件选择器
        /// </summary>
        public void OpenFilePicker()
        {
#if UNITY_EDITOR
            string path = EditorUtility.OpenFilePanel("选择视频文件", "", "mp4,mkv,mov");
            if (!string.IsNullOrWhiteSpace(path))
            {
                AddPickedVideo(path, Path.GetFileName(path), new FileInfo(path).Length);
            }
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (!LaunchAndroidVideoPicker())
            {
                logger.Error("打开 Android 文件选择器失败");
            }
#elif UNITY_IOS
            logger.Warning("iOS 文件选择器插件尚未集成");
#else
            logger.Warning("此平台不支持文件选择器");
#endif
        }

        /// <summary>
        /// 删除本地文件
        /// </summary>
        public bool DeleteLocalFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    logger.Error("文件路径不能为空");
                    return false;
                }

                if (!File.Exists(filePath))
                {
                    logger.Warning($"文件不存在: {filePath}");
                    return false;
                }

                File.Delete(filePath);

                // 从 picked videos 中移除
                var video = pickedVideos.Find(v => v.localPath == filePath || v.path == filePath);
                if (video != null)
                {
                    pickedVideos.Remove(video);
                    SavePickedVideos();
                }

                logger.Info($"文件已删除: {filePath}");
                OnFileDeleted?.Invoke(video);

                return true;
            }
            catch (Exception e)
            {
                logger.Error($"删除文件失败: {e.Message}", e);
                return false;
            }
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        public bool FileExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;

            try
            {
                return File.Exists(filePath);
            }
            catch (Exception e)
            {
                logger.Error($"检查文件存在性失败: {e.Message}", e);
                return false;
            }
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        public FileInfo GetFileInfo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return null;

            try
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    return new FileInfo
                    {
                        name = fileInfo.Name,
                        path = fileInfo.FullName,
                        size = fileInfo.Length,
                        extension = fileInfo.Extension,
                        lastModified = fileInfo.LastWriteTime
                    };
                }
                return null;
            }
            catch (Exception e)
            {
                logger.Error($"获取文件信息失败: {e.Message}", e);
                return null;
            }
        }

        /// <summary>
        /// 获取所有已选择的视频
        /// </summary>
        public VideoFile[] GetPickedVideos()
        {
            return pickedVideos.ToArray();
        }

        /// <summary>
        /// 清空已选择的视频
        /// </summary>
        public void ClearPickedVideos()
        {
            pickedVideos.Clear();
            SavePickedVideos();
            logger.Info("已清空所有选择的视频");
        }

        /// <summary>
        /// 添加已选择的视频
        /// </summary>
        public void AddPickedVideo(string uri, string displayName, long size)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                logger.Error("URI 不能为空");
                return;
            }

            string normalizedUri = uri.Trim();

            // 检查是否已存在
            for (int i = 0; i < pickedVideos.Count; i++)
            {
                string existingKey = pickedVideos[i].localPath;
                if (string.IsNullOrWhiteSpace(existingKey))
                {
                    existingKey = pickedVideos[i].path;
                }

                if (string.Equals(existingKey, normalizedUri, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warning($"视频已存在: {displayName}");
                    return;
                }
            }

            string resolvedName = string.IsNullOrWhiteSpace(displayName) 
                ? DeriveNameFromUri(normalizedUri) 
                : displayName.Trim();

            if (!IsSupportedVideoSelection(resolvedName, normalizedUri))
            {
                logger.Warning($"不支持的视频格式: {displayName}");
                return;
            }

            var video = new VideoFile
            {
                name = resolvedName,
                path = normalizedUri,
                localPath = normalizedUri,
                url = normalizedUri,
                is360 = resolvedName.ToLowerInvariant().Contains("360"),
                size = Math.Max(0, size)
            };

            pickedVideos.Add(video);
            SavePickedVideos();

            logger.Info($"添加视频: {resolvedName} ({video.sizeFormatted})");
            OnFilePicked?.Invoke(video);
        }

        /// <summary>
        /// 移除已选择的视频
        /// </summary>
        public bool RemovePickedVideo(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri)) return false;

            string normalizedUri = uri.Trim();
            var video = pickedVideos.Find(v => 
                string.Equals(v.localPath, normalizedUri, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v.path, normalizedUri, StringComparison.OrdinalIgnoreCase));

            if (video == null)
            {
                logger.Warning($"视频不存在: {uri}");
                return false;
            }

            pickedVideos.Remove(video);
            SavePickedVideos();

            logger.Info($"移除视频: {video.name}");
            return true;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// 启动 Android 视频选择器
        /// </summary>
        private bool LaunchAndroidVideoPicker()
        {
            try
            {
                AndroidJavaClass bridge = new AndroidJavaClass("com.vrplayer.saf.SafPickerBridge");
                bridge.CallStatic("launchVideoPicker", gameObject.name, "OnAndroidVideoPickerResult");
                logger.Info("启动 Android 视频选择器");
                return true;
            }
            catch (Exception e)
            {
                logger.Error($"调用 Android 选择器桥接失败: {e.Message}", e);
                return false;
            }
        }

        /// <summary>
        /// Android 视频选择器回调
        /// </summary>
        public void OnAndroidVideoPickerResult(string payload)
        {
            logger.Debug($"收到 Android 选择器结果: {payload?.Length ?? 0} 字符");

            if (string.IsNullOrWhiteSpace(payload))
            {
                logger.Info("用户取消了选择");
                return;
            }

            AndroidPickerResult result;
            try
            {
                result = JsonUtility.FromJson<AndroidPickerResult>(payload);
            }
            catch (Exception e)
            {
                logger.Error($"解析选择器结果失败: {e.Message}", e);
                return;
            }

            if (result == null)
            {
                logger.Error("选择器结果为 null");
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.error))
            {
                logger.Error($"选择器返回错误: {result.error}");
                return;
            }

            if (result.videos == null || result.videos.Length == 0)
            {
                logger.Info("没有选择任何视频");
                return;
            }

            int added = 0;
            foreach (var video in result.videos)
            {
                if (video == null) continue;

                if (AddPickedVideoIfNew(video.uri, video.name, video.size))
                {
                    added++;
                }
            }

            if (added > 0)
            {
                SavePickedVideos();
                logger.Info($"添加了 {added} 个视频");
            }
        }
#endif

        /// <summary>
        /// 添加已选择的视频（内部方法）
        /// </summary>
        private bool AddPickedVideoIfNew(string uri, string displayName, long size)
        {
            if (string.IsNullOrWhiteSpace(uri)) return false;

            string normalizedUri = uri.Trim();
            for (int i = 0; i < pickedVideos.Count; i++)
            {
                string existingKey = pickedVideos[i].localPath;
                if (string.IsNullOrWhiteSpace(existingKey))
                {
                    existingKey = pickedVideos[i].path;
                }

                if (string.Equals(existingKey, normalizedUri, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            string resolvedName = string.IsNullOrWhiteSpace(displayName) 
                ? DeriveNameFromUri(normalizedUri) 
                : displayName.Trim();

            if (!IsSupportedVideoSelection(resolvedName, normalizedUri))
            {
                return false;
            }

            var video = new VideoFile
            {
                name = resolvedName,
                path = normalizedUri,
                localPath = normalizedUri,
                url = normalizedUri,
                is360 = resolvedName.ToLowerInvariant().Contains("360"),
                size = Math.Max(0, size)
            };

            pickedVideos.Add(video);
            OnFilePicked?.Invoke(video);

            return true;
        }

        /// <summary>
        /// 从 URI 推导文件名
        /// </summary>
        private static string DeriveNameFromUri(string uri)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                return "Selected video";
            }

            string text = uri.Trim();
            int queryIndex = text.IndexOf('?');
            if (queryIndex >= 0)
            {
                text = text.Substring(0, queryIndex);
            }

            int slashIndex = text.LastIndexOf('/');
            string name = slashIndex >= 0 ? text.Substring(slashIndex + 1) : text;
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Selected video";
            }

            try
            {
                return Uri.UnescapeDataString(name);
            }
            catch
            {
                return name;
            }
        }

        /// <summary>
        /// 检查是否支持的视频格式
        /// </summary>
        private bool IsSupportedVideoSelection(string displayName, string uri)
        {
            string extension = Path.GetExtension(displayName);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                extension = extension.ToLowerInvariant();
                if (extension == ".mp4" || extension == ".mkv" || extension == ".mov")
                {
                    return true;
                }
            }

            extension = Path.GetExtension(uri);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                extension = extension.ToLowerInvariant();
                if (extension == ".mp4" || extension == ".mkv" || extension == ".mov")
                {
                    return true;
                }
            }

            return uri.StartsWith("content://", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 加载已选择的视频
        /// </summary>
        private void LoadPickedVideos()
        {
            pickedVideos.Clear();

            string payload = PlayerPrefs.GetString(PICKED_VIDEOS_PREFS_KEY, string.Empty);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            PickedVideoStore store;
            try
            {
                store = JsonUtility.FromJson<PickedVideoStore>(payload);
            }
            catch (Exception e)
            {
                logger.Error($"解析已选择视频存储失败: {e.Message}", e);
                return;
            }

            if (store == null || store.items == null) return;

            foreach (var entry in store.items)
            {
                if (entry == null) continue;

                AddPickedVideoIfNew(entry.uri, entry.name, entry.size);
            }

            logger.Debug($"加载了 {pickedVideos.Count} 个已选择的视频");
        }

        /// <summary>
        /// 保存已选择的视频
        /// </summary>
        private void SavePickedVideos()
        {
            var store = new PickedVideoStore
            {
                items = new PickedVideoEntry[pickedVideos.Count]
            };

            for (int i = 0; i < pickedVideos.Count; i++)
            {
                var video = pickedVideos[i];
                store.items[i] = new PickedVideoEntry
                {
                    uri = video.localPath,
                    name = video.name,
                    size = video.size
                };
            }

            string payload = JsonUtility.ToJson(store);
            PlayerPrefs.SetString(PICKED_VIDEOS_PREFS_KEY, payload);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 序列化类：Android 选择器结果
        /// </summary>
        [Serializable]
        private class AndroidPickerResult
        {
            public bool cancelled;
            public string error;
            public AndroidPickerVideo[] videos;
        }

        /// <summary>
        /// 序列化类：Android 选择器视频
        /// </summary>
        [Serializable]
        private class AndroidPickerVideo
        {
            public string uri;
            public string name;
            public long size;
        }

        /// <summary>
        /// 序列化类：已选择视频存储
        /// </summary>
        [Serializable]
        private class PickedVideoStore
        {
            public PickedVideoEntry[] items;
        }

        /// <summary>
        /// 序列化类：已选择视频条目
        /// </summary>
        [Serializable]
        private class PickedVideoEntry
        {
            public string uri;
            public string name;
            public long size;
        }
    }
}
