using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Application Layer Unit Tests
/// 包含 LibraryManager 和 PlaybackOrchestrator 的单元测试
/// </summary>
namespace Tests.Application
{
    // ========================
    // LibraryManager Tests
    // ========================

    [TestFixture]
    public class LibraryManagerTests
    {
        private Application.Library.LibraryManager libraryManager;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject("LibraryManager");
            libraryManager = gameObject.AddComponent<Application.Library.LibraryManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (libraryManager != null)
            {
                GameObject.Destroy(libraryManager.gameObject);
            }
        }

        [Test]
        public void AddVideo_ShouldIncreaseVideoCount()
        {
            // Arrange
            var video = new Domain.Entities.VideoFile
            {
                name = "Test Video",
                path = "/path/to/video.mp4",
                size = 1024 * 1024 * 100 // 100 MB
            };

            // Act
            libraryManager.AddVideo(video);

            // Assert
            Assert.AreEqual(1, libraryManager.GetVideoCount());
        }

        [Test]
        public void RemoveVideo_WhenVideoExists_ShouldDecreaseVideoCount()
        {
            // Arrange
            var video = new Domain.Entities.VideoFile
            {
                name = "Test Video",
                path = "/path/to/video.mp4",
                size = 1024 * 1024 * 100
            };
            libraryManager.AddVideo(video);

            // Act
            libraryManager.RemoveVideo(video);

            // Assert
            Assert.AreEqual(0, libraryManager.GetVideoCount());
        }

        [Test]
        public void GetVideos_ShouldReturnAllVideos()
        {
            // Arrange
            var video1 = new Domain.Entities.VideoFile { name = "Video1", path = "/path1.mp4" };
            var video2 = new Domain.Entities.VideoFile { name = "Video2", path = "/path2.mp4" };
            libraryManager.AddVideo(video1);
            libraryManager.AddVideo(video2);

            // Act
            var videos = libraryManager.GetVideos();

            // Assert
            Assert.AreEqual(2, videos.Count);
        }

        [Test]
        public void GetVideos_WhenEmpty_ShouldReturnEmptyList()
        {
            // Act
            var videos = libraryManager.GetVideos();

            // Assert
            Assert.AreEqual(0, videos.Count);
        }

        [Test]
        public void SearchVideos_WithQuery_ShouldReturnMatchingVideos()
        {
            // Arrange
            var video1 = new Domain.Entities.VideoFile { name = "Test Video One", path = "/path1.mp4" };
            var video2 = new Domain.Entities.VideoFile { name = "Other Video", path = "/path2.mp4" };
            libraryManager.AddVideo(video1);
            libraryManager.AddVideo(video2);

            // Act
            var results = libraryManager.SearchVideos("Test");

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Test Video One", results[0].name);
        }

        [Test]
        public void ClearLibrary_ShouldRemoveAllVideos()
        {
            // Arrange
            libraryManager.AddVideo(new Domain.Entities.VideoFile { name = "Video1", path = "/path1.mp4" });
            libraryManager.AddVideo(new Domain.Entities.VideoFile { name = "Video2", path = "/path2.mp4" });

            // Act
            libraryManager.ClearLibrary();

            // Assert
            Assert.AreEqual(0, libraryManager.GetVideoCount());
        }
    }

    // ========================
    // PlaybackOrchestrator Tests
    // ========================

    [TestFixture]
    public class PlaybackOrchestratorTests
    {
        private Application.Playback.PlaybackOrchestrator orchestrator;

        [SetUp]
        public void Setup()
        {
            var gameObject = new GameObject("PlaybackOrchestrator");
            orchestrator = gameObject.AddComponent<Application.Playback.PlaybackOrchestrator>();
        }

        [TearDown]
        public void TearDown()
        {
            if (orchestrator != null)
            {
                GameObject.Destroy(orchestrator.gameObject);
            }
        }

        [Test]
        public void State_Initially_ShouldBeIdle()
        {
            // Act & Assert
            Assert.AreEqual(Application.Playback.PlaybackState.Idle, orchestrator.State);
        }

        [Test]
        public void GetSnapshot_ShouldReturnValidSnapshot()
        {
            // Act
            var snapshot = orchestrator.GetSnapshot();

            // Assert
            Assert.IsNotNull(snapshot);
            Assert.AreEqual(0f, snapshot.durationSeconds);
            Assert.AreEqual(0f, snapshot.positionSeconds);
        }

        [Test]
        public void SetVolume_ShouldUpdateVolume()
        {
            // Arrange
            float newVolume = 0.75f;

            // Act
            orchestrator.SetVolume(newVolume);

            // Assert
            Assert.AreEqual(newVolume, orchestrator.Volume);
        }

        [Test]
        public void SetVolume_WithInvalidValue_ShouldClamp()
        {
            // Act
            orchestrator.SetVolume(-0.5f); // Should clamp to 0
            float clampedLow = orchestrator.Volume;

            orchestrator.SetVolume(1.5f); // Should clamp to 1
            float clampedHigh = orchestrator.Volume;

            // Assert
            Assert.AreEqual(0f, clampedLow);
            Assert.AreEqual(1f, clampedHigh);
        }

        [Test]
        public void GetSnapshot_ShouldHaveDefaultValues()
        {
            // Act
            var snapshot = orchestrator.GetSnapshot();

            // Assert
            Assert.AreEqual(0f, snapshot.normalizedProgress);
            Assert.IsFalse(snapshot.isPlaying);
            Assert.IsFalse(snapshot.isBuffering);
        }

        [Test]
        public void Stop_WhenIdle_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => orchestrator.StopPlayback());
        }

        [Test]
        public void Pause_WhenIdle_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => orchestrator.PausePlayback());
        }

        [Test]
        public void Start_WhenIdle_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => orchestrator.StartPlayback());
        }

        [Test]
        public void GetCurrentTexture_ShouldReturnNullWhenIdle()
        {
            // Act
            var texture = orchestrator.GetCurrentTexture();

            // Assert
            Assert.IsNull(texture);
        }

        [Test]
        public void SetVolumeMultipleTimes_ShouldKeepLastValue()
        {
            // Arrange
            orchestrator.SetVolume(0.5f);
            orchestrator.SetVolume(0.7f);
            orchestrator.SetVolume(0.9f);

            // Act & Assert
            Assert.AreEqual(0.9f, orchestrator.Volume);
        }
    }

    // ========================
    // Test Helpers
    // ========================

    public static class ApplicationTestHelpers
    {
        public static Domain.Entities.VideoFile CreateTestVideo(string name, string path, long size)
        {
            return new Domain.Entities.VideoFile
            {
                name = name,
                path = path,
                size = size,
                duration = 120.0f
            };
        }

        public static List<Domain.Entities.VideoFile> CreateTestVideos(int count)
        {
            var videos = new List<Domain.Entities.VideoFile>();
            for (int i = 0; i < count; i++)
            {
                videos.Add(CreateTestVideo(
                    $"Test Video {i}",
                    $"/path/to/video{i}.mp4",
                    1024 * 1024 * (i + 1) * 100 // 100-500 MB
                ));
            }
            return videos;
        }
    }
}
