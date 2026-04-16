using System;
using System.Collections.Generic;

namespace PlyGame.Runtime.Core.Graph
{
    /// <summary>
    /// Base class for all graph nodes in the PlyGame system.
    /// Supports async execution and context passing.
    /// </summary>
    [Serializable]
    public abstract class GraphNode
    {
        public string Guid { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Untitled Node";
        public string Description { get; set; } = "";
        
        /// <summary>
        /// List of connected node GUIDs (outgoing edges)
        /// </summary>
        public List<string> Connections { get; set; } = new List<string>();
        
        /// <summary>
        /// Position in graph editor (for serialization)
        /// </summary>
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        
        /// <summary>
        /// Execute the node logic asynchronously
        /// </summary>
        /// <param name="context">Shared execution context</param>
        /// <returns>Task representing the execution</returns>
        public abstract Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context);
        
        /// <summary>
        /// Validate node configuration before execution
        /// </summary>
        /// <returns>List of validation errors (empty if valid)</returns>
        public virtual List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Node must have a name");
            return errors;
        }
        
        /// <summary>
        /// Called when node execution completes
        /// </summary>
        protected virtual void OnExecuteComplete(ExecutionContext context, NodeExecutionResult result)
        {
            // Override for custom completion logic
        }
    }
    
    /// <summary>
    /// Result of node execution
    /// </summary>
    [Serializable]
    public class NodeExecutionResult
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = "";
        public bool ShouldContinue { get; set; } = true;
        public Dictionary<string, object> OutputData { get; set; } = new Dictionary<string, object>();
        
        public static NodeExecutionResult SuccessResult(Dictionary<string, object> data = null)
        {
            return new NodeExecutionResult 
            { 
                Success = true, 
                ShouldContinue = true,
                OutputData = data ?? new Dictionary<string, object>()
            };
        }
        
        public static NodeExecutionResult FailureResult(string error, bool shouldContinue = false)
        {
            return new NodeExecutionResult 
            { 
                Success = false, 
                ErrorMessage = error,
                ShouldContinue = shouldContinue
            };
        }
    }
    
    /// <summary>
    /// Shared context passed through graph execution
    /// </summary>
    public class ExecutionContext
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        
        public void SetVariable(string key, object value)
        {
            _data[key] = value;
        }
        
        public T GetVariable<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }
        
        public bool HasVariable(string key)
        {
            return _data.ContainsKey(key);
        }
        
        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }
        
        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
        }
    }
}
