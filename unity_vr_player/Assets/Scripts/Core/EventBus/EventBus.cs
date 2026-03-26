using System;
using System.Collections.Generic;

namespace VRPlayer.Core.EventBus
{
    /// <summary>
    /// 简单的事件总线实现 - 用于模块间松耦合通信
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> subscriptions = new Dictionary<Type, List<Delegate>>();
        private readonly object lockObject = new object();

        private static EventBus instance;
        public static EventBus Instance => instance ?? (instance = new EventBus());

        private EventBus() { }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null)
            {
                UnityEngine.Debug.LogWarning("[EventBus] Cannot subscribe null handler");
                return;
            }

            lock (lockObject)
            {
                var eventType = typeof(T);
                if (!subscriptions.ContainsKey(eventType))
                {
                    subscriptions[eventType] = new List<Delegate>();
                }

                if (!subscriptions[eventType].Contains(handler))
                {
                    subscriptions[eventType].Add(handler);
                }
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            if (handler == null) return;

            lock (lockObject)
            {
                var eventType = typeof(T);
                if (subscriptions.ContainsKey(eventType))
                {
                    subscriptions[eventType].Remove(handler);

                    if (subscriptions[eventType].Count == 0)
                    {
                        subscriptions.Remove(eventType);
                    }
                }
            }
        }

        /// <summary>
        /// 发布事件 - 异步调用所有订阅者
        /// </summary>
        public void Publish<T>(T eventData) where T : class
        {
            if (eventData == null)
            {
                UnityEngine.Debug.LogWarning("[EventBus] Cannot publish null event data");
                return;
            }

            List<Delegate> handlers = null;

            lock (lockObject)
            {
                var eventType = typeof(T);
                if (subscriptions.ContainsKey(eventType))
                {
                    handlers = new List<Delegate>(subscriptions[eventType]);
                }
            }

            if (handlers != null)
            {
                foreach (var handler in handlers)
                {
                    try
                    {
                        handler.DynamicInvoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"[EventBus] Error executing handler for {typeof(T).Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 清理所有订阅
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                subscriptions.Clear();
            }
        }
    }
}
