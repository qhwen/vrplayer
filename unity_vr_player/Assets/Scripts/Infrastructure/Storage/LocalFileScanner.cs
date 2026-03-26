using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace VRPlayer.Infrastructure.Storage
{
    /// <summary>
    /// 本地文件扫描器实现
    /// 负责扫描本地目录中的视频文件
    /// </summary>
    public class LocalFileScanner : MonoBehaviour, IFileScanner
    {
        [Header("扫描配置")]
        [SerializeField] private bool includeCommonMediaFolders = true;
        [SerializeField, Range(0, 5)] private int scanDepth = 0;
        [SerializeField, Range(20, 500)] private int maxCollectedVideos = 200;

#if UNITY_ANDROID
        [Header("Android扫描范围")]
        [SerializeField] private string androidMoviesDirectory = "/storage/emulated/0/Movies";
        [SerializeField] private bool includeMoviesSubdirectories;
#endif

        private readonly string[] supportedExtensions = { ".mp4", ".mkv", ".mov" };
        private readonly List<VideoFile> scannedVideos = new List<VideoFile>();
        private VRPlayer.Core.Logging.ILogger logger;

        public event Action<VideoFile> OnVideoDiscovered;

        private void Awake()
        {
            logger = VRPlayer.Core.Logging.LoggerManager.For("LocalFileScanner");
            logger.Info("LocalFileScanner initialized");
        }

        /// <summary>
        /// 扫描指定路径
        /// </summary>
        public async IAsyncEnumerable<VideoFile> ScanPathAsync(string path, ScanOptions options)
        {
            logger.Info($"开始扫描路径: {path}");

            HashSet<string> deduplicate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            scannedVideos.Clear();

#if UNITY_ANDROID && !UNITY_EDITOR
            await Task.Run(() =>
            {
                if (options?.useMediaStore ?? false)
                {
                    ScanFromMediaStore(deduplicate, options);
                }
                else
                {
                    ScanDirectory(path, deduplicate, 0, options?.scanSubdirectories ?? ShouldScanSubdirectories());
                }
            });
#else
            ScanDirectory(path, deduplicate, 0, options?.scanSubdirectories ?? true);
#endif

            // 返回扫描到的视频
            foreach (var video in scannedVideos)
            {
                yield return video;
            }

            logger.Info($"扫描完成，发现 {scannedVideos.Count} 个视频");
        }

        /// <summary>
        /// 扫描所有视频
        /// </summary>
        public async IAsyncEnumerable<VideoFile> ScanAllAsync(ScanOptions options)
        {
            logger.Info("开始扫描所有视频");

            HashSet<string> deduplicate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            scannedVideos.Clear();

#if UNITY_ANDROID && !UNITY_EDITOR
            await Task.Run(() =>
            {
                // Android: 先尝试直接扫描 Movies 目录
                var androidPermissionManager = FindObjectOfType<AndroidPermissionManager>();
                bool hasPermission = androidPermissionManager != null && androidPermissionManager.HasReadableMediaPermission();

                if (hasPermission)
                {
                    int beforeCount = scannedVideos.Count;
                    ScanAndroidMoviesDirectory(deduplicate, options);

                    // 如果没有找到，回退到 MediaStore
                    if (scannedVideos.Count == beforeCount)
                    {
                        ScanFromMediaStore(deduplicate, options);
                    }
                }
            });
#else
            // 非 Android 平台: 扫描常见媒体文件夹
            var roots = BuildSearchRoots();
            foreach (var root in roots)
            {
                if (deduplicate.Count >= maxCollectedVideos) break;

                ScanDirectory(root, deduplicate, 0, options?.scanSubdirectories ?? true);
            }
#endif

            // 排序
            scannedVideos.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            // 返回扫描到的视频
            foreach (var video in scannedVideos)
            {
                yield return video;
            }

            logger.Info($"扫描完成，发现 {scannedVideos.Count} 个视频");
        }

        /// <summary>
        /// 递归扫描目录
        /// </summary>
        private void ScanDirectory(string directory, HashSet<string> deduplicate, int depth, bool allowSubdirectories)
        {
            if (string.IsNullOrWhiteSpace(directory) || depth > scanDepth)
            {
                return;
            }

            if (deduplicate.Count >= maxCollectedVideos)
            {
                return;
            }

            if (!Directory.Exists(directory))
            {
                return;
            }

            try
            {
                // 扫描当前目录的文件
                string[] files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    if (deduplicate.Count >= maxCollectedVideos) break;

                    TryAddVideo(file, deduplicate);
                }
            }
            catch (Exception e)
            {
                logger.Warning($"无法扫描目录 {directory}: {e.Message}");
            }

            // 扫描子目录
            if (allowSubdirectories && depth < scanDepth)
            {
                try
                {
                    string[] subDirectories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
                    foreach (var subDir in subDirectories)
                    {
                        if (deduplicate.Count >= maxCollectedVideos) break;

                        ScanDirectory(subDir, deduplicate, depth + 1, allowSubdirectories);
                    }
                }
                catch (Exception e)
                {
                    logger.Warning($"无法列出子目录 {directory}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Android: 从 MediaStore 扫描视频
        /// </summary>
#if UNITY_ANDROID && !UNITY_EDITOR
        private void ScanFromMediaStore(HashSet<string> deduplicate, ScanOptions options)
        {
            AndroidJavaObject cursor = null;

            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject resolver = activity.Call<AndroidJavaObject>("getContentResolver");

                AndroidJavaClass mediaClass = new AndroidJavaClass("android.provider.MediaStore$Video$Media");
                AndroidJavaObject externalContentUri = mediaClass.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");

                string[] projection =
                {
                    "_id",
                    "display_name",
                    "_size",
                    "relative_path",
                    "bucket_display_name"
                };

                string selection;
                string[] selectionArgs;
                bool includeSubdirs = options?.scanSubdirectories ?? includeMoviesSubdirectories;

                if (includeSubdirs)
                {
                    selection = "relative_path=? OR relative_path=? OR relative_path LIKE ?";
                    selectionArgs = new[] { "Movies", "Movies/", "Movies/%" };
                }
                else
                {
                    selection = "relative_path=? OR relative_path=?";
                    selectionArgs = new[] { "Movies", "Movies/" };
                }

                cursor = resolver.Call<AndroidJavaObject>(
                    "query",
                    externalContentUri,
                    projection,
                    selection,
                    selectionArgs,
                    "date_added DESC");

                if (cursor == null)
                {
                    logger.Warning("MediaStore 查询失败: cursor is null");
                    return;
                }

                int idIndex = cursor.Call<int>("getColumnIndex", "_id");
                int nameIndex = cursor.Call<int>("getColumnIndex", "display_name");
                int sizeIndex = cursor.Call<int>("getColumnIndex", "_size");
                int relativePathIndex = cursor.Call<int>("getColumnIndex", "relative_path");

                while (cursor.Call<bool>("moveToNext"))
                {
                    if (scannedVideos.Count >= maxCollectedVideos) break;

                    string displayName = nameIndex >= 0 ? cursor.Call<string>("getString", nameIndex) : string.Empty;
                    if (string.IsNullOrWhiteSpace(displayName)) continue;

                    if (!IsSupportedVideoExtension(Path.GetExtension(displayName))) continue;

                    string relativePath = relativePathIndex >= 0 ? cursor.Call<string>("getString", relativePathIndex) : string.Empty;
                    long size = sizeIndex >= 0 ? cursor.Call<long>("getLong", sizeIndex) : 0;
                    long mediaId = idIndex >= 0 ? cursor.Call<long>("getLong", idIndex) : -1;

                    if (!MatchesMoviesScope(relativePath)) continue;

                    string contentUri = mediaId >= 0 ? "content://media/external/video/media/" + mediaId : string.Empty;
                    string dedupKey = !string.IsNullOrWhiteSpace(contentUri) ? contentUri : displayName;
                    if (string.IsNullOrWhiteSpace(dedupKey) || deduplicate.Contains(dedupKey)) continue;

                    var video = new VideoFile
                    {
                        name = displayName,
                        path = string.IsNullOrWhiteSpace(contentUri) ? dedupKey : contentUri,
                        localPath = dedupKey,
                        url = string.IsNullOrWhiteSpace(contentUri) ? dedupKey : contentUri,
                        is360 = displayName.ToLowerInvariant().Contains("360"),
                        size = size
                    };

                    scannedVideos.Add(video);
                    deduplicate.Add(dedupKey);

                    OnVideoDiscovered?.Invoke(video);
                }

                logger.Info($"MediaStore 扫描完成，发现 {scannedVideos.Count} 个视频");
            }
            catch (Exception e)
            {
                logger.Error($"MediaStore 扫描失败: {e.Message}", e);
            }
            finally
            {
                if (cursor != null)
                {
                    cursor.Call("close");
                    cursor.Dispose();
                }
            }
        }

        /// <summary>
        /// Android: 扫描 Movies 目录
        /// </summary>
        private void ScanAndroidMoviesDirectory(HashSet<string> deduplicate, ScanOptions options)
        {
            var roots = BuildAndroidSearchRoots();
            bool allowSubdirectories = options?.scanSubdirectories ?? includeMoviesSubdirectories;

            foreach (var root in roots)
            {
                if (deduplicate.Count >= maxCollectedVideos) break;

                ScanDirectory(root, deduplicate, 0, allowSubdirectories);
            }

            logger.Info($"Android Movies 目录扫描完成，发现 {scannedVideos.Count} 个视频");
        }

        private bool MatchesMoviesScope(string relativePath)
        {
            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                string normalizedRelative = relativePath.Replace("\\", "/").Trim().TrimStart('/').ToLowerInvariant();
                if (normalizedRelative.EndsWith("/"))
                {
                    normalizedRelative = normalizedRelative.Substring(0, normalizedRelative.Length - 1);
                }

                if (includeMoviesSubdirectories)
                {
                    return normalizedRelative == "movies" || normalizedRelative.StartsWith("movies/");
                }

                return normalizedRelative == "movies";
            }

            return false;
        }

        private List<string> BuildAndroidSearchRoots()
        {
            List<string> roots = new List<string>();
            AddSearchRoot(roots, androidMoviesDirectory);
            AddSearchRoot(roots, "/storage/self/primary/Movies");
            return roots;
        }
#endif

        /// <summary>
        /// 构建搜索根目录
        /// </summary>
        private List<string> BuildSearchRoots()
        {
            List<string> roots = new List<string>();

#if UNITY_ANDROID
            roots.AddRange(BuildAndroidSearchRoots());
#else
            // 非 Android 平台
            var cacheManager = FindObjectOfType<FileCacheManager>();
            string cacheDir = cacheManager != null ? cacheManager.GetCacheDirectory() : Path.Combine(UnityEngine.Application.persistentDataPath, "VRVideos");

            AddSearchRoot(roots, cacheDir);
            AddSearchRoot(roots, UnityEngine.Application.persistentDataPath);

            if (includeCommonMediaFolders)
            {
                AddSearchRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
                AddSearchRoot(roots, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            }
#endif

            return roots;
        }

        private static void AddSearchRoot(List<string> roots, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            string normalized = path.Trim();
            if (roots.Any(r => string.Equals(r, normalized, StringComparison.OrdinalIgnoreCase))) return;

            roots.Add(normalized);
        }

        /// <summary>
        /// 尝试添加视频
        /// </summary>
        private void TryAddVideo(string filePath, HashSet<string> deduplicate)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) return;

            string extension = Path.GetExtension(filePath);
            if (!IsSupportedVideoExtension(extension)) return;

            if (deduplicate.Contains(filePath)) return;

            FileInfo info;
            try
            {
                info = new FileInfo(filePath);
            }
            catch
            {
                return;
            }

            var video = new VideoFile
            {
                name = Path.GetFileName(filePath),
                path = filePath,
                localPath = filePath,
                url = "file://" + filePath,
                is360 = Path.GetFileName(filePath).ToLowerInvariant().Contains("360"),
                size = info.Exists ? info.Length : 0
            };

            scannedVideos.Add(video);
            deduplicate.Add(filePath);

            OnVideoDiscovered?.Invoke(video);

            logger.Debug($"发现视频: {video.name} ({video.sizeFormatted})");
        }

        private bool IsSupportedVideoExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return false;

            string normalized = extension.ToLowerInvariant();
            return supportedExtensions.Contains(normalized);
        }

        private bool ShouldScanSubdirectories()
        {
#if UNITY_ANDROID
            return includeMoviesSubdirectories;
#else
            return true;
#endif
        }

        /// <summary>
        /// 取消扫描
        /// </summary>
        public void CancelScan()
        {
            scannedVideos.Clear();
            logger.Info("扫描已取消");
        }

        /// <summary>
        /// 获取扫描配置
        /// </summary>
        public ScanOptions GetDefaultOptions()
        {
            return new ScanOptions
            {
                scanSubdirectories = ShouldScanSubdirectories(),
                maxVideos = maxCollectedVideos,
                supportedExtensions = supportedExtensions,
                useMediaStore = false
            };
        }
    }
}
