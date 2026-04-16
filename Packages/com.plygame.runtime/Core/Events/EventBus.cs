using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlyGame.Runtime.Core.Events
{
    /// <summary>
    /// Event bus for decoupled communication between systems.
    /// Supports async event handlers and type-safe event payloads.
    /// </summary>
    public class EventBus : IEventBus
    {
        private readonly Dictionary<Type, List<object>> _handlers = new Dictionary<Type, List<object>>();
        private readonly object _lock = new object();
        
        private static EventBus _instance;
        public static EventBus Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new EventBus();
                return _instance;
            }
            set => _instance = value;
        }
        
        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : IEvent
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_handlers.ContainsKey(eventType))
                    _handlers[eventType] = new List<object>();
                
                _handlers[eventType].Add(handler);
            }
        }
        
        /// <summary>
        /// Subscribe to an event type with async handler
        /// </summary>
        public void SubscribeAsync<T>(Func<T, Task> handler) where T : IEvent
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_handlers.ContainsKey(eventType))
                    _handlers[eventType] = new List<object>();
                
                _handlers[eventType].Add(handler);
            }
        }
        
        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : IEvent
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                if (_handlers.ContainsKey(eventType))
                    _handlers[eventType].RemoveAll(h => h.Equals(handler));
            }
        }
        
        /// <summary>
        /// Publish an event synchronously
        /// </summary>
        public void Publish<T>(T eventData) where T : IEvent
        {
            List<object> handlersToCall;
            
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_handlers.ContainsKey(eventType))
                    return;
                
                // Create a copy to avoid modification during iteration
                handlersToCall = new List<object>(_handlers[eventType]);
            }
            
            foreach (var handler in handlersToCall)
            {
                try
                {
                    if (handler is Action<T> syncHandler)
                        syncHandler(eventData);
                    else if (handler is Func<T, Task> asyncHandler)
                    {
                        // Fire and forget for async handlers - consider using Unity's main thread dispatcher
                        asyncHandler(eventData).ContinueWith(t =>
                        {
                            if (t.Exception != null)
                                UnityEngine.Debug.LogError($"Async event handler error: {t.Exception}");
                        });
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Event handler error: {e}");
                }
            }
        }
        
        /// <summary>
        /// Publish an event and wait for all handlers to complete
        /// </summary>
        public async Task PublishAsync<T>(T eventData) where T : IEvent
        {
            List<object> handlersToCall;
            
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_handlers.ContainsKey(eventType))
                    return;
                
                handlersToCall = new List<object>(_handlers[eventType]);
            }
            
            var tasks = new List<Task>();
            
            foreach (var handler in handlersToCall)
            {
                try
                {
                    if (handler is Func<T, Task> asyncHandler)
                        tasks.Add(asyncHandler(eventData));
                    else if (handler is Action<T> syncHandler)
                    {
                        syncHandler(eventData);
                        // Wrap sync handlers in completed task
                        tasks.Add(Task.CompletedTask);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Event handler error: {e}");
                }
            }
            
            await Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// Clear all handlers for a specific event type
        /// </summary>
        public void Clear<T>() where T : IEvent
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                if (_handlers.ContainsKey(eventType))
                    _handlers[eventType].Clear();
            }
        }
        
        /// <summary>
        /// Clear all handlers
        /// </summary>
        public void ClearAll()
        {
            lock (_lock)
            {
                _handlers.Clear();
            }
        }
        
        /// <summary>
        /// Get handler count for an event type
        /// </summary>
        public int GetHandlerCount<T>() where T : IEvent
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                return _handlers.ContainsKey(eventType) ? _handlers[eventType].Count : 0;
            }
        }
    }
    
    /// <summary>
    /// Base interface for all events
    /// </summary>
    public interface IEvent
    {
        DateTime Timestamp { get; set; }
        string Source { get; set; }
    }
    
    /// <summary>
    /// Base class for events with common properties
    /// </summary>
    [Serializable]
    public abstract class EventBase : IEvent
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = "";
    }
    
    /// <summary>
    /// Interface for event bus
    /// </summary>
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler) where T : IEvent;
        void SubscribeAsync<T>(Func<T, Task> handler) where T : IEvent;
        void Unsubscribe<T>(Action<T> handler) where T : IEvent;
        void Publish<T>(T eventData) where T : IEvent;
        Task PublishAsync<T>(T eventData) where T : IEvent;
        void Clear<T>() where T : IEvent;
        void ClearAll();
        int GetHandlerCount<T>() where T : IEvent;
    }
}
