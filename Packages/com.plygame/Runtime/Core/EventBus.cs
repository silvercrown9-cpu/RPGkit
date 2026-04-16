using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PlyGame.Runtime.Core.Events
{
    /// <summary>
    /// Базовый класс для событий шины событий
    /// </summary>
    public class GameEvent { }
    
    /// <summary>
    /// Шина событий для декуплированной коммуникации между системами
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<object>> _syncHandlers = new();
        private static readonly Dictionary<Type, List<Func<object, Task>>> _asyncHandlers = new();
        private static readonly object _lock = new();
        
        /// <summary>
        /// Подписка на синхронное событие
        /// </summary>
        public static void Subscribe<T>(Action<T> handler) where T : GameEvent
        {
            lock (_lock)
            {
                if (!_syncHandlers.ContainsKey(typeof(T)))
                    _syncHandlers[typeof(T)] = new List<object>();
                
                _syncHandlers[typeof(T)].Add(handler);
            }
        }
        
        /// <summary>
        /// Отписка от синхронного события
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : GameEvent
        {
            lock (_lock)
            {
                if (_syncHandlers.TryGetValue(typeof(T), out var handlers))
                {
                    handlers.Remove(handler);
                    
                    if (handlers.Count == 0)
                        _syncHandlers.Remove(typeof(T));
                }
            }
        }
        
        /// <summary>
        /// Подписка на асинхронное событие
        /// </summary>
        public static void SubscribeAsync<T>(Func<T, Task> handler) where T : GameEvent
        {
            lock (_lock)
            {
                if (!_asyncHandlers.ContainsKey(typeof(T)))
                    _asyncHandlers[typeof(T)] = new List<Func<object, Task>>();
                
                _asyncHandlers[typeof(T)].Add(async evt => await handler((T)evt));
            }
        }
        
        /// <summary>
        /// Отписка от асинхронного события
        /// </summary>
        public static void UnsubscribeAsync<T>(Func<T, Task> handler) where T : GameEvent
        {
            lock (_lock)
            {
                if (_asyncHandlers.TryGetValue(typeof(T), out var handlers))
                {
                    var targetHandler = new Func<object, Task>(async evt => await handler((T)evt));
                    handlers.RemoveAll(h => h.Method.Name == targetHandler.Method.Name);
                    
                    if (handlers.Count == 0)
                        _asyncHandlers.Remove(typeof(T));
                }
            }
        }
        
        /// <summary>
        /// Публикация синхронного события
        /// </summary>
        public static void Publish<T>(T evt) where T : GameEvent
        {
            List<object> handlersToCall;
            
            lock (_lock)
            {
                if (!_syncHandlers.TryGetValue(typeof(T), out var handlers) || handlers.Count == 0)
                    return;
                
                handlersToCall = new List<object>(handlers);
            }
            
            foreach (var handler in handlersToCall)
            {
                try
                {
                    ((Action<T>)handler)(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Ошибка в обработчике события {typeof(T).Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Публикация асинхронного события
        /// </summary>
        public static async Task PublishAsync<T>(T evt) where T : GameEvent
        {
            List<Func<object, Task>> handlersToCall;
            
            lock (_lock)
            {
                if (!_asyncHandlers.TryGetValue(typeof(T), out var handlers) || handlers.Count == 0)
                    return;
                
                handlersToCall = new List<Func<object, Task>>(handlers);
            }
            
            var tasks = new List<Task>();
            
            foreach (var handler in handlersToCall)
            {
                try
                {
                    tasks.Add(handler(evt));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Ошибка в асинхронном обработчике события {typeof(T).Name}: {ex.Message}");
                }
            }
            
            await Task.WhenAll(tasks);
        }
        
        /// <summary>
        /// Очистка всех подписчиков
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _syncHandlers.Clear();
                _asyncHandlers.Clear();
            }
        }
        
        /// <summary>
        /// Получить количество подписчиков на событие
        /// </summary>
        public static int GetSubscriberCount<T>() where T : GameEvent
        {
            lock (_lock)
            {
                var syncCount = _syncHandlers.TryGetValue(typeof(T), out var sync) ? sync.Count : 0;
                var asyncCount = _asyncHandlers.TryGetValue(typeof(T), out var aync) ? aync.Count : 0;
                return syncCount + asyncCount;
            }
        }
    }
    
    // Примеры стандартных событий
    public class GraphStartedEvent : GameEvent
    {
        public string GraphId { get; set; }
        public string GraphName { get; set; }
    }
    
    public class GraphCompletedEvent : GameEvent
    {
        public string GraphId { get; set; }
        public object Result { get; set; }
    }
    
    public class GraphFailedEvent : GameEvent
    {
        public string GraphId { get; set; }
        public string Error { get; set; }
    }
    
    public class NodeExecutedEvent : GameEvent
    {
        public string NodeId { get; set; }
        public string NodeName { get; set; }
        public bool Success { get; set; }
    }
    
    public class VariableChangedEvent<T> : GameEvent
    {
        public string VariableName { get; set; }
        public T OldValue { get; set; }
        public T NewValue { get; set; }
    }
}
