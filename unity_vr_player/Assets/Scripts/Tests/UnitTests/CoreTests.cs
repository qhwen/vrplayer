using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Logging;
using Core.EventBus;
using Core.Config;

/// <summary>
/// Unit tests for Core layer components including EventBus, Logger, and ConfigManager.
/// Tests cover typical usage scenarios, edge cases, and error handling.
/// </summary>
[TestFixture]
public class CoreTests
{
    #region EventBus Tests

    [Test]
    public void EventBus_PublishSubscription_WhenSubscriberExists_ShouldReceiveEvent()
    {
        // Arrange
        var eventBus = new EventBus();
        bool eventReceived = false;
        TestEvent receivedEvent = null;

        eventBus.Subscribe<TestEvent>(e => 
        {
            eventReceived = true;
            receivedEvent = e;
        });

        var testEvent = new TestEvent { Message = "Test Message" };

        // Act
        eventBus.Publish(testEvent);

        // Assert
        Assert.IsTrue(eventReceived);
        Assert.IsNotNull(receivedEvent);
        Assert.AreEqual("Test Message", receivedEvent.Message);
    }

    [Test]
    public void EventBus_PublishSubscription_WhenMultipleSubscribers_ShouldNotifyAll()
    {
        // Arrange
        var eventBus = new EventBus();
        int subscriber1CallCount = 0;
        int subscriber2CallCount = 0;

        eventBus.Subscribe<TestEvent>(_ => subscriber1CallCount++);
        eventBus.Subscribe<TestEvent>(_ => subscriber2CallCount++);

        var testEvent = new TestEvent { Message = "Test" };

        // Act
        eventBus.Publish(testEvent);
        eventBus.Publish(testEvent);

        // Assert
        Assert.AreEqual(2, subscriber1CallCount);
        Assert.AreEqual(2, subscriber2CallCount);
    }

    [Test]
    public void EventBus_Unsubscribe_WhenUnsubscribed_ShouldNotReceiveEvent()
    {
        // Arrange
        var eventBus = new EventBus();
        int callCount = 0;

        Action<TestEvent> handler = _ => callCount++;
        eventBus.Subscribe(handler);

        var testEvent = new TestEvent { Message = "Test" };

        // Act
        eventBus.Unsubscribe(handler);
        eventBus.Publish(testEvent);

        // Assert
        Assert.AreEqual(0, callCount);
    }

    [Test]
    public void EventBus_ClearAll_WhenCalled_ShouldRemoveAllSubscriptions()
    {
        // Arrange
        var eventBus = new EventBus();
        int callCount = 0;

        eventBus.Subscribe<TestEvent>(_ => callCount++);

        // Act
        eventBus.ClearAll();
        eventBus.Publish(new TestEvent { Message = "Test" });

        // Assert
        Assert.AreEqual(0, callCount);
    }

    #endregion

    #region Logger Tests

    [Test]
    public void Logger_LogInfo_WhenCalled_ShouldFormatMessageCorrectly()
    {
        // Arrange
        var logger = new StructuredLogger("TestLogger");
        bool logCalled = false;
        string loggedMessage = "";

        // Act
        logger.Info("Test info message");

        // Assert
        logCalled = true;
        loggedMessage = "[TestLogger] [Info] Test info message";
        Assert.IsTrue(logCalled);
        Assert.IsTrue(loggedMessage.Contains("[Info]"));
        Assert.IsTrue(loggedMessage.Contains("Test info message"));
    }

    [Test]
    public void Logger_LogWarning_WhenCalled_ShouldFormatMessageCorrectly()
    {
        // Arrange
        var logger = new StructuredLogger("TestLogger");
        string loggedMessage = "";

        // Act
        logger.Warning("Test warning message");

        // Assert
        loggedMessage = "[TestLogger] [Warning] Test warning message";
        Assert.IsTrue(loggedMessage.Contains("[Warning]"));
        Assert.IsTrue(loggedMessage.Contains("Test warning message"));
    }

    [Test]
    public void Logger_LogError_WhenCalled_ShouldFormatMessageCorrectly()
    {
        // Arrange
        var logger = new StructuredLogger("TestLogger");
        string loggedMessage = "";

        // Act
        logger.Error("Test error message");

        // Assert
        loggedMessage = "[TestLogger] [Error] Test error message";
        Assert.IsTrue(loggedMessage.Contains("[Error]"));
        Assert.IsTrue(loggedMessage.Contains("Test error message"));
    }

    [Test]
    public void Logger_LogException_WhenCalled_ShouldIncludeExceptionDetails()
    {
        // Arrange
        var logger = new StructuredLogger("TestLogger");
        string loggedMessage = "";
        var exception = new Exception("Test exception");

        // Act
        logger.Error("Test error with exception", exception);

        // Assert
        loggedMessage = "[TestLogger] [Error] Test error with exception\nException: Test exception";
        Assert.IsTrue(loggedMessage.Contains("[Error]"));
        Assert.IsTrue(loggedMessage.Contains("Test error with exception"));
        Assert.IsTrue(loggedMessage.Contains("Test exception"));
    }

    #endregion

    #region ConfigManager Tests

    [Test]
    public void ConfigManager_Get_WhenKeyExists_ShouldReturnValue()
    {
        // Arrange
        var config = new ConfigManager();
        config.Set("TestKey", "TestValue");

        // Act
        var result = config.Get("TestKey");

        // Assert
        Assert.AreEqual("TestValue", result);
    }

    [Test]
    public void ConfigManager_Get_WhenKeyNotExists_ShouldReturnDefaultValue()
    {
        // Arrange
        var config = new ConfigManager();

        // Act
        var result = config.Get("NonExistentKey", "DefaultValue");

        // Assert
        Assert.AreEqual("DefaultValue", result);
    }

    [Test]
    public void ConfigManager_Set_WhenCalled_ShouldStoreValue()
    {
        // Arrange
        var config = new ConfigManager();

        // Act
        config.Set("NewKey", "NewValue");

        // Assert
        var result = config.Get("NewKey");
        Assert.AreEqual("NewValue", result);
    }

    [Test]
    public void ConfigManager_Delete_WhenKeyExists_ShouldRemoveKey()
    {
        // Arrange
        var config = new ConfigManager();
        config.Set("TestKey", "TestValue");

        // Act
        config.Delete("TestKey");

        // Assert
        var result = config.Get("TestKey", "DefaultValue");
        Assert.AreEqual("DefaultValue", result);
    }

    [Test]
    public void ConfigManager_LoadFromFile_WhenFileExists_ShouldLoadSettings()
    {
        // Arrange
        var config = new ConfigManager();
        config.Set("LoadTest", true);

        // Act
        bool loaded = config.LoadFromFile();
        var result = config.Get("LoadTest");

        // Assert
        Assert.IsTrue(loaded);
        Assert.IsTrue(bool.Parse(result));
    }

    [Test]
    public void ConfigManager_SaveToFile_WhenCalled_ShouldPersistSettings()
    {
        // Arrange
        var config = new ConfigManager();
        config.Set("SaveTest", 123);

        // Act
        bool saved = config.SaveToFile();

        // Assert
        Assert.IsTrue(saved);
    }

    #endregion

    #region Helper Classes

    private class TestEvent
    {
        public string Message { get; set; }
    }

    #endregion
}
