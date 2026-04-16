using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PlyGame.Core
{
    /// <summary>
    /// Контекст выполнения графа, передающий данные между узлами
    /// </summary>
    [Serializable]
    public class ExecutionContext
    {
        public string GraphId { get; set; }
        public Dictionary<string, object> Data { get; private set; } = new();
        public CancellationToken CancellationToken { get; set; }
        public object CurrentResult { get; set; }
        
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }
        
        public void SetData<T>(string key, T value)
        {
            Data[key] = value;
        }
        
        public bool HasData(string key) => Data.ContainsKey(key);
        
        public void Clear()
        {
            Data.Clear();
            CurrentResult = null;
        }
    }
    
    /// <summary>
    /// Результат выполнения узла
    /// </summary>
    [Serializable]
    public class NodeExecutionResult
    {
        public bool Success { get; set; } = true;
        public bool ShouldContinue { get; set; } = true;
        public string NextNodeId { get; set; }
        public object Result { get; set; }
        public string ErrorMessage { get; set; }
        
        public static NodeExecutionResult SuccessResult(string nextNodeId = null, object result = null) =>
            new() { Success = true, ShouldContinue = true, NextNodeId = nextNodeId, Result = result };
            
        public static NodeExecutionResult StopResult(object result = null) =>
            new() { Success = true, ShouldContinue = false, Result = result };
            
        public static NodeExecutionResult FailureResult(string error) =>
            new() { Success = false, ShouldContinue = false, ErrorMessage = error };
    }
    
    /// <summary>
    /// Базовый класс для всех узлов графа
    /// </summary>
    [Serializable]
    public abstract class GraphNode
    {
        [SerializeField] protected string nodeId;
        [SerializeField] protected string nodeName;
        [SerializeField] protected string nodeDescription;
        [SerializeField] protected Vector2 position;
        
        public string NodeId => nodeId;
        public string NodeName => nodeName;
        public string Description => nodeDescription;
        public Vector2 Position { get => position; set => position = value; }
        
        protected GraphNode()
        {
            nodeId = Guid.NewGuid().ToString();
            nodeName = GetType().Name;
        }
        
        /// <summary>
        /// Асинхронное выполнение узла
        /// </summary>
        public abstract Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context);
        
        /// <summary>
        /// Валидация узла перед выполнением
        /// </summary>
        public virtual bool Validate(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }
        
        /// <summary>
        /// Событие перед выполнением
        /// </summary>
        public virtual void OnEnter(ExecutionContext context) { }
        
        /// <summary>
        /// Событие после выполнения
        /// </summary>
        public virtual void OnExit(ExecutionContext context, NodeExecutionResult result) { }
        
        /// <summary>
        /// Клонирование узла
        /// </summary>
        public virtual GraphNode Clone()
        {
            var clone = MemberwiseClone() as GraphNode;
            clone.nodeId = Guid.NewGuid().ToString();
            return clone;
        }
    }
    
    /// <summary>
    /// Узел-стартер, точка входа в граф
    /// </summary>
    [Serializable]
    public class StartNode : GraphNode
    {
        public StartNode()
        {
            nodeName = "Start";
            nodeDescription = "Точка входа в граф";
        }
        
        public override Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            return Task.FromResult(NodeExecutionResult.SuccessResult());
        }
    }
    
    /// <summary>
    /// Узел-завершитель графа
    /// </summary>
    [Serializable]
    public class EndNode : GraphNode
    {
        [SerializeField] private object returnValue;
        
        public object ReturnValue => returnValue;
        
        public EndNode()
        {
            nodeName = "End";
            nodeDescription = "Завершение выполнения графа";
        }
        
        public override Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            return Task.FromResult(NodeExecutionResult.StopResult(returnValue));
        }
    }
    
    /// <summary>
    /// Узел задержки выполнения
    /// </summary>
    [Serializable]
    public class DelayNode : GraphNode
    {
        [SerializeField] private float delaySeconds = 1f;
        
        public float DelaySeconds => delaySeconds;
        
        public DelayNode()
        {
            nodeName = "Delay";
            nodeDescription = "Задержка выполнения на указанное время";
        }
        
        public override async Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), context.CancellationToken);
                return NodeExecutionResult.SuccessResult();
            }
            catch (OperationCanceledException)
            {
                return NodeExecutionResult.FailureResult("Delay cancelled");
            }
        }
    }
}
