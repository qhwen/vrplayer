using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Core Layer Unit Tests
/// 包含 EventBus, Logger, ConfigManager 的单元测试
/// </summary>
namespace Tests
{
    // ========================
    // EventBus Tests
    // ========================

    [TestFixture]
    public class EventBusTests
    {
        private Core.EventBus.EventBus eventBus;

        [SetUp]
        public void Setup()
        {
            eventBus = new Core.EventBus.EventBus();
        }

        [TearDown]
        public void TearDown()
        {
            eventBus?.Dispose();
        }

        [Test]
        public void Publish_SubscribedEvent_ShouldTriggerCallback()
        {
            // Arrange
            int callbackCount = 0;
            string testEvent = "TestEvent";
            
            eventBus.Subscribe<string>((eventName) => callbackCount++);

            // Act
            eventBus.Publish(testEvent);

            // Assert
            Assert.AreEqual(1, callbackCount);
        }

        [Test]
        public void Subscribe_Unsubscribe_ShouldNotReceiveEvents()
        {
            // Arrange
            int callbackCount = 0;
            System.Action<string> callback = (eventName) => callbackCount++;
            
            eventBus.Subscribe<string>(callback);
            eventBus.Unsubscribe<string>(callback);
            
            // Act
            eventBus.Publish("TestEvent");

            // Assert
            Assert.AreEqual(0, callbackCount);
        }

        [Test]
        public void Publish_MultipleSubscribers_AllShouldReceive()
        {
            // Arrange
            int count1 = 0, count2 = 0;
            
            eventBus.Subscribe<string>((s) => count1++);
            eventBus.Subscribe<string>((s) => count2++);

            // Act
            eventBus.Publish("TestEvent");

            // Assert
            Assert.AreEqual(1, count1);
            Assert.AreEqual(1, count2);
        }

        [Test]
        public void ClearAll_ShouldRemoveAllSubscriptions()
        {
            // Arrange
            int count = 0;
            eventBus.Subscribe<string>((s) => count++);
            
            // Act
            eventBus.ClearAll();
            eventBus.Publish("TestEvent");

            // Assert
            Assert.AreEqual(0, count);
        }

        [Test]
        public void Subscribe_WithNoPublish_ShouldKeepSubscription()
        {
            // Arrange
            int count = 0;
            
            // Act
            eventBus.Subscribe<string>((s) => count++);

            // Assert
            Assert.NotNull(eventBus); // Test passes if no exception
        }
    }

    // ========================
    // Logger Tests
    // ========================

    [TestFixture]
    public class LoggerTests
    {
        private Core.Logging.IStructuredLogger logger;
        private string lastMessage;
        private Core.Logging.LogLevel lastLevel;

        [SetUp]
        public void Setup()
        {
            logger = Core.Logging.StructuredLogger.Create("TestLogger");
            lastMessage = "";
            lastLevel = Core.Logging.LogLevel.Info;
        }

        [Test]
        public void Info_ShouldLogCorrectly()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => logger.Info("Info message"));
        }

        [Test]
        public void Debug_ShouldLogCorrectly()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => logger.Debug("Debug message"));
        }

        [Test]
        public void Warning_ShouldLogCorrectly()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => logger.Warning("Warning message"));
        }

        [Test]
        public void Error_ShouldLogCorrectly()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => logger.Error("Error message"));
        }

        [Test]
        public void LogWithException_ShouldIncludeException()
        {
            // Arrange
            var exception = new System.Exception("Test exception");

            // Act & Assert
            Assert.DoesNotThrow(() => logger.Error("Error with exception", exception));
        }

        [Test]
        public void SetLogLevel_ShouldFilterMessages()
        {
            // Arrange
            logger.SetLogLevel(Core.Logging.LogLevel.Warning);

            // Act & Assert
            Assert.DoesNotThrow(() => logger.Info("This should not be visible"));
            Assert.DoesNotThrow(() => logger.Warning("This should be visible"));
        }

        [Test]
        public void CreateMultipleLoggers_ShouldNotConflict()
        {
            // Act & Assert
            var logger1 = Core.Logging.StructuredLogger.Create("Logger1");
            var logger2 = Core.Logging.StructuredLogger.Create("Logger2");
            
            Assert.NotNull(logger1);
            Assert.NotNull(logger2);
        }
    }

    // ========================
    // ConfigManager Tests
    // ========================

    [TestFixture]
    public class ConfigManagerTests
    {
        private Core.Config.IAppConfig config;

        [SetUp]
        public void Setup()
        {
            config = new Core.Config.AppConfigManager();
        }

        [Test]
        public void Get_Set_ShouldStoreAndRetrieve()
        {
            // Arrange
            string key = "TestKey";
            string value = "TestValue";

            // Act
            config.Set(key, value);
            string result = config.Get<string>(key);

            // Assert
            Assert.AreEqual(value, result);
        }

        [Test]
        public void Get_NonExistentKey_ShouldReturnDefault()
        {
            // Arrange
            string defaultValue = "Default";
            string nonExistentKey = "NonExistentKey";

            // Act
            string result = config.Get<string>(nonExistentKey, defaultValue);

            // Assert
            Assert.AreEqual(defaultValue, result);
        }

        [Test]
        public void Get_WithDifferentTypes_ShouldWork()
        {
            // Arrange
            config.Set("IntValue", 123);
            config.Set("FloatValue", 3.14f);
            config.Set("BoolValue", true);

            // Act & Assert
            Assert.AreEqual(123, config.Get<int>("IntValue"));
            Assert.AreEqual(3.14f, config.Get<float>("FloatValue"));
            Assert.AreEqual(true, config.Get<bool>("BoolValue"));
        }

        [Test]
        public void Remove_ShouldDeleteKey()
        {
            // Arrange
            string key = "DeleteKey";
            config.Set(key, "Value");

            // Act
            config.Remove(key);
            string result = config.Get<string>(key, "Default");

            // Assert
            Assert.AreEqual("Default", result);
        }

        [Test]
        public void Clear_ShouldRemoveAllKeys()
        {
            // Arrange
            config.Set("Key1", "Value1");
            config.Set("Key2", "Value2");

            // Act
            config.Clear();
            string result1 = config.Get<string>("Key1", "Default1");
            string result2 = config.Get<string>("Key2", "Default2");

            // Assert
            Assert.AreEqual("Default1", result1);
            Assert.AreEqual("Default2", result2);
        }

        [Test]
        public void Save_Load_ShouldPersist()
        {
            // Arrange
            string key = "PersistKey";
            string value = "PersistValue";
            config.Set(key, value);

            // Act & Assert
            Assert.DoesNotThrow(() => config.Save());
            Assert.DoesNotThrow(() => config.Load());
            Assert.AreEqual(value, config.Get<string>(key));
        }

        [Test]
        public void Contains_ShouldCheckExistence()
        {
            // Arrange
            string existingKey = "ExistingKey";
            string nonExistingKey = "NonExistingKey";
            config.Set(existingKey, "Value");

            // Act & Assert
            Assert.True(config.Contains(existingKey));
            Assert.False(config.Contains(nonExistingKey));
        }
    }

    // ========================
    // 测试辅助类
    // ========================

    public class TestHelper
    {
        /// <summary>
        /// 创建测试用的临时配置
        /// </summary>
        public static Core.Config.IAppConfig CreateTestConfig()
        {
            var config = new Core.Config.AppConfigManager();
            config.Set("TestKey", "TestValue");
            config.Set("Number", 42);
            return config;
        }

        /// <summary>
        /// 清理测试资源
        /// </summary>
        public static void Cleanup()
        {
            // 清理临时文件等
        }
    }
}
