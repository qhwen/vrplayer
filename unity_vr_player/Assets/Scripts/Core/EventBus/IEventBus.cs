using System;

namespace VRPlayer.Core.EventBus
{
    /// <summary>
    /// 事件总线接口 - 提供模块间解耦的发布订阅机制
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型（必须是引用类型）</typeparam>
        /// <param name="handler">事件处理器</param>
        void Subscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型（必须是引用类型）</typeparam>
        /// <param name="handler">事件处理器</param>
        void Unsubscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <typeparam name="T">事件类型（必须是引用类型）</typeparam>
        /// <param name="eventData">事件数据</param>
        void Publish<T>(T eventData) where T : class;

        /// <summary>
        /// 清理所有订阅
        /// </summary>
        void Clear();
    }
}
