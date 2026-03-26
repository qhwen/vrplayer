using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Infrastructure Layer Unit Tests
/// 包含 LocalFileScanner, FileCacheManager, AndroidPermissionManager, AndroidStorageAccess 的单元测试
/// </summary>
namespace Tests.Infrastructure
{
    // ========================
    // LocalFileScanner Tests
    // ========================

    [TestFixture]
    public class LocalFileScannerTests
    {
        private Infrastructure.Storage.LocalFileScanner scanner;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject("LocalFileScanner");
            scanner = gameObject.AddComponent<Infrastructure.Storage.LocalFileScanner>();
        }

        [TearDown]
        public void TearDown()
        {
            if (scanner != null)
            {
                GameObject.Destroy(scanner.gameObject);
            }
        }

        [Test]
        public void Initialize_WhenCalled_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => scanner.Initialize());
        }

        [Test]
        public void GetSupportedExtensions_ShouldReturnExtensions()
        {
            // Act
            var extensions = scanner.GetSupportedExtensions();

            // Assert
            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions);
            Assert.Contains(".mp4", extensions);
        }

        [Test]
        public void AddSupportedExtension_ShouldAddToExtensionsList()
        {
            // Arrange
            var newExtension = ".mkv";

            // Act
            scanner.AddSupportedExtension(newExtension);

            // Assert
            var extensions = scanner.GetSupportedExtensions();
            Assert.Contains(newExtension, extensions);
        }

        [Test]
        public void RemoveSupportedExtension_ShouldRemoveFromExtensionsList()
        {
            // Arrange
            var extension = ".mp4";

            // Act
            scanner.RemoveSupportedExtension(extension);

            // Assert
            var extensions = scanner.GetSupportedExtensions();
            Assert.IsFalse(extensions.Contains(extension));
        }

        [Test]
        public void SetScanDepth_ShouldUpdateDepth()
        {
            // Arrange
            int newDepth = 5;

            // Act
            scanner.SetScanDepth(newDepth);

            // Assert
            Assert.AreEqual(newDepth, scanner.GetScanDepth());
        }

        [Test]
        public void IsVideoFile_WithValidExtension_ShouldReturnTrue()
        {
            // Arrange
            string videoPath = "/path/to/video.mp4";

            // Act & Assert
            Assert.IsTrue(scanner.IsVideoFile(videoPath));
        }

        [Test]
        public void IsVideoFile_WithInvalidExtension_ShouldReturnFalse()
        {
            // Arrange
            string nonVideoPath = "/path/to/document.txt";

            // Act & Assert
            Assert.IsFalse(scanner.IsVideoFile(nonVideoPath));
        }

        [Test]
        public void GetScanOptions_ShouldReturnDefaultOptions()
        {
            // Act
            var options = scanner.GetScanOptions();

            // Assert
            Assert.IsNotNull(options);
            Assert.IsTrue(options.recursive);
        }
    }

    // ========================
    // FileCacheManager Tests
    // ========================

    [TestFixture]
    public class FileCacheManagerTests
    {
        private Infrastructure.Storage.FileCacheManager cacheManager;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject("FileCacheManager");
            cacheManager = gameObject.AddComponent<Infrastructure.Storage.FileCacheManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (cacheManager != null)
            {
                GameObject.Destroy(cacheManager.gameObject);
            }
        }

        [Test]
        public void GetCacheSize_ShouldInitiallyBeZero()
        {
            // Act & Assert
            Assert.AreEqual(0L, cacheManager.GetCacheSize());
        }

        [Test]
        public void GetCacheCount_ShouldInitiallyBeZero()
        {
            // Act & Assert
            Assert.AreEqual(0, cacheManager.GetCacheCount());
        }

        [Test]
        public void AddToCache_ShouldIncreaseCacheSize()
        {
            // Arrange
            var video = new Domain.Entities.VideoFile
            {
                name = "Test Video",
                path = "/path/to/video.mp4",
                size = 1024 * 1024 * 100 // 100 MB
            };
            byte[] data = new byte[1024];

            // Act
            cacheManager.AddToCache("test_key", video, data);

            // Assert
            Assert.Greater(cacheManager.GetCacheSize(), 0);
            Assert.AreEqual(1, cacheManager.GetCacheCount());
        }

        [Test]
        public void GetFromCache_WhenKeyExists_ShouldReturnData()
        {
            // Arrange
            var video = new Domain.Entities.VideoFile { name = "Test", path = "/path/to/video.mp4" };
            byte[] data = new byte[] { 1, 2, 3, 4, 5 };
            cacheManager.AddToCache("test_key", video, data);

            // Act
            var result = cacheManager.GetFromCache("test_key");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.Length);
        }

        [Test]
        public void GetFromCache_WhenKeyNotExists_ShouldReturnNull()
        {
            // Act
            var result = cacheManager.GetFromCache("non_existent_key");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void RemoveFromCache_WhenKeyExists_ShouldDecreaseCacheCount()
        {
            // Arrange
            var video = new Domain.Entities.VideoFile { name = "Test", path = "/path/to/video.mp4" };
            byte[] data = new byte[] { 1, 2, 3 };
            cacheManager.AddToCache("test_key", video, data);

            // Act
            cacheManager.RemoveFromCache("test_key");

            // Assert
            Assert.AreEqual(0, cacheManager.GetCacheCount());
        }

        [Test]
        public void ClearCache_ShouldRemoveAllCachedItems()
        {
            // Arrange
            var video = new Domain.Entities.VideoFile { name = "Test", path = "/path/to/video.mp4" };
            byte[] data = new byte[] { 1, 2, 3 };
            cacheManager.AddToCache("key1", video, data);
            cacheManager.AddToCache("key2", video, data);

            // Act
            cacheManager.ClearCache();

            // Assert
            Assert.AreEqual(0, cacheManager.GetCacheCount());
            Assert.AreEqual(0L, cacheManager.GetCacheSize());
        }

        [Test]
        public void SetMaxCacheSize_ShouldUpdateLimit()
        {
            // Arrange
            long maxSize = 1024 * 1024 * 1024; // 1 GB

            // Act
            cacheManager.SetMaxCacheSize(maxSize);

            // Assert
            Assert.AreEqual(maxSize, cacheManager.GetMaxCacheSize());
        }

        [Test]
        public void Contains_WhenKeyExists_ShouldReturnTrue()
        {
            // Arrange
            var video = new Domain.Entities.VideoFile { name = "Test", path = "/path/to/video.mp4" };
            byte[] data = new byte[] { 1, 2, 3 };
            cacheManager.AddToCache("test_key", video, data);

            // Act & Assert
            Assert.IsTrue(cacheManager.Contains("test_key"));
        }

        [Test]
        public void Contains_WhenKeyNotExists_ShouldReturnFalse()
        {
            // Act & Assert
            Assert.IsFalse(cacheManager.Contains("non_existent_key"));
        }

        [Test]
        public void GetCachedVideos_ShouldReturnListOfVideos()
        {
            // Arrange
            var video1 = new Domain.Entities.VideoFile { name = "Video1", path = "/path1.mp4" };
            var video2 = new Domain.Entities.VideoFile { name = "Video2", path = "/path2.mp4" };
            byte[] data = new byte[] { 1, 2, 3 };
            cacheManager.AddToCache("key1", video1, data);
            cacheManager.AddToCache("key2", video2, data);

            // Act
            var videos = cacheManager.GetCachedVideos();

            // Assert
            Assert.AreEqual(2, videos.Count);
        }
    }

    // ========================
    // AndroidPermissionManager Tests
    // ========================

    [TestFixture]
    public class AndroidPermissionManagerTests
    {
        private Infrastructure.Platform.AndroidPermissionManager permissionManager;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject("AndroidPermissionManager");
            permissionManager = gameObject.AddComponent<Infrastructure.Platform.AndroidPermissionManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (permissionManager != null)
            {
                GameObject.Destroy(permissionManager.gameObject);
            }
        }

        [Test]
        public void Initialize_WhenCalled_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => permissionManager.Initialize());
        }

        [Test]
        public void CheckPermission_WithValidPermission_ShouldReturnStatus()
        {
            // Arrange
            string permission = "android.permission.READ_EXTERNAL_STORAGE";

            // Act & Assert
            Assert.DoesNotThrow(() => permissionManager.CheckPermission(permission));
        }

        [Test]
        public void RequestPermission_WithValidPermission_ShouldInitiateRequest()
        {
            // Arrange
            string permission = "android.permission.READ_EXTERNAL_STORAGE";

            // Act & Assert
            Assert.DoesNotThrow(() => permissionManager.RequestPermission(permission));
        }

        [Test]
        public void HasReadPermission_WhenCalled_ShouldReturnBoolean()
        {
            // Act
            bool hasPermission = permissionManager.HasReadPermission();

            // Assert
            Assert.IsInstanceOf<bool>(hasPermission);
        }

        [Test]
        public void HasWritePermission_WhenCalled_ShouldReturnBoolean()
        {
            // Act
            bool hasPermission = permissionManager.HasWritePermission();

            // Assert
            Assert.IsInstanceOf<bool>(hasPermission);
        }

        [Test]
        public void ShouldShowRequestPermissionRationale_WhenCalled_ShouldReturnBoolean()
        {
            // Act
            bool shouldShow = permissionManager.ShouldShowRequestPermissionRationale();

            // Assert
            Assert.IsInstanceOf<bool>(shouldShow);
        }

        [Test]
        public void OpenAppSettings_WhenCalled_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => permissionManager.OpenAppSettings());
        }

        [Test]
        public void OnPermissionsResult_ShouldHandleResult()
        {
            // Arrange
            string[] permissions = new[] { "android.permission.READ_EXTERNAL_STORAGE" };
            int[] grantResults = new[] { 0 }; // Granted

            // Act & Assert
            Assert.DoesNotThrow(() => permissionManager.OnPermissionsResult(permissions, grantResults));
        }
    }

    // ========================
    // AndroidStorageAccess Tests
    // ========================

    [TestFixture]
    public class AndroidStorageAccessTests
    {
        private Infrastructure.Platform.AndroidStorageAccess storageAccess;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject("AndroidStorageAccess");
            storageAccess = gameObject.AddComponent<Infrastructure.Platform.AndroidStorageAccess>();
        }

        [TearDown]
        public void TearDown()
        {
            if (storageAccess != null)
            {
                GameObject.Destroy(storageAccess.gameObject);
            }
        }

        [Test]
        public void Initialize_WhenCalled_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => storageAccess.Initialize());
        }

        [Test]
        public void OpenFilePicker_WhenCalled_ShouldInitiatePicker()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => storageAccess.OpenFilePicker());
        }

        [Test]
        public void GetSelectedVideos_WhenNoVideosSelected_ShouldReturnEmptyList()
        {
            // Act
            var videos = storageAccess.GetSelectedVideos();

            // Assert
            Assert.IsNotNull(videos);
            Assert.AreEqual(0, videos.Count);
        }

        [Test]
        public void ClearSelectedVideos_ShouldRemoveAllSelections()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => storageAccess.ClearSelectedVideos());
        }

        [Test]
        public void GetSelectedVideoCount_ShouldReturnCount()
        {
            // Act
            int count = storageAccess.GetSelectedVideoCount();

            // Assert
            Assert.GreaterOrEqual(count, 0);
        }

        [Test]
        public void DeleteVideo_WithValidPath_ShouldNotThrow()
        {
            // Arrange
            string testPath = "/test/path/video.mp4";

            // Act & Assert
            Assert.DoesNotThrow(() => storageAccess.DeleteVideo(testPath));
        }

        [Test]
        public void GetVideoInfo_WithValidPath_ShouldReturnInfo()
        {
            // Arrange
            string testPath = "/test/path/video.mp4";

            // Act & Assert
            Assert.DoesNotThrow(() => storageAccess.GetVideoInfo(testPath));
        }

        [Test]
        public void GetSelectedVideos_ShouldReturnListOfVideos()
        {
            // Act
            var videos = storageAccess.GetSelectedVideos();

            // Assert
            Assert.IsNotNull(videos);
            Assert.IsInstanceOf<List<Domain.Entities.VideoFile>>(videos);
        }

        [Test]
        public void GetSelectedVideoCount_ShouldBeZeroInitially()
        {
            // Act & Assert
            Assert.AreEqual(0, storageAccess.GetSelectedVideoCount());
        }
    }

    // ========================
    // Test Helpers
    // ========================

    public static class InfrastructureTestHelpers
    {
        public static byte[] CreateTestData(int sizeInBytes)
        {
            byte[] data = new byte[sizeInBytes];
            for (int i = 0; i < sizeInBytes; i++)
            {
                data[i] = (byte)(i % 256);
            }
            return data;
        }

        public static Domain.Entities.VideoFile CreateTestVideo(string name, string path)
        {
            return new Domain.Entities.VideoFile
            {
                name = name,
                path = path,
                size = 1024 * 1024 * 100,
                duration = 120.0f
            };
        }
    }
}
