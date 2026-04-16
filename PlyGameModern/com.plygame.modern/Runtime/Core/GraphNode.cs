using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PlyGame.Runtime.Core
{
    /// <summary>
    /// Контекст выполнения графа. Передает данные между узлами.
    /// </summary>
    [Serializable]
    public class ExecutionContext
    {
        public string GraphId;
        public GameObject Sender;
        public GameObject Target;
        public float DeltaTime;
        public CancellationToken CancellationToken;
        
        public ExecutionContext(string graphId, GameObject sender = null, GameObject target = null)
        {
            GraphId = graphId;
            Sender = sender;
            Target = target;
            DeltaTime = Time.deltaTime;
            CancellationToken = CancellationToken.None;
        }
    }

    /// <summary>
    /// Результат выполнения узла
    /// </summary>
    [Serializable]
    public class NodeExecutionResult
    {
        public bool Success;
        public bool ShouldContinue;
        public string NextNodeId;
        public string[] BranchNodeIds;
        public string ErrorMessage;
        public object Data;

        public static NodeExecutionResult SuccessResult(string nextNodeId = null)
        {
            return new NodeExecutionResult 
            { 
                Success = true, 
                ShouldContinue = true, 
                NextNodeId = nextNodeId 
            };
        }

        public static NodeExecutionResult FailureResult(string error = null)
        {
            return new NodeExecutionResult 
            { 
                Success = false, 
                ShouldContinue = false, 
                ErrorMessage = error 
            };
        }

        public static NodeExecutionResult BranchResult(params string[] branchNodeIds)
        {
            return new NodeExecutionResult 
            { 
                Success = true, 
                ShouldContinue = true, 
                BranchNodeIds = branchNodeIds 
            };
        }

        public static NodeExecutionResult StopResult()
        {
            return new NodeExecutionResult 
            { 
                Success = true, 
                ShouldContinue = false 
            };
        }
    }

    /// <summary>
    /// Базовый класс для всех узлов графа
    /// </summary>
    [Serializable]
    public abstract class GraphNode
    {
        public string Id;
        public string Name;
        public string Description;
        public Vector2 Position;
        
        [SerializeReference]
        public GraphNode[] OutputNodes;
        
        [SerializeReference]
        public GraphNode[] BranchNodes;

        protected ExecutionContext _context;

        /// <summary>
        /// Асинхронное выполнение узла
        /// </summary>
        public virtual async Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            _context = context;
            
            try
            {
                var result = await OnExecuteAsync(context);
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GraphNode] Error executing node {Name}: {ex.Message}");
                return NodeExecutionResult.FailureResult(ex.Message);
            }
        }

        /// <summary>
        /// Переопределяется в наследниках для реализации логики
        /// </summary>
        protected virtual Task<NodeExecutionResult> OnExecuteAsync(ExecutionContext context)
        {
            return Task.FromResult(NodeExecutionResult.SuccessResult(
                OutputNodes?.Length > 0 ? OutputNodes[0].Id : null));
        }

        /// <summary>
        /// Вызывается при входе в узел
        /// </summary>
        public virtual void OnEnter() { }

        /// <summary>
        /// Вызывается при выходе из узла
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// Валидация узла перед выполнением
        /// </summary>
        public virtual bool Validate() => true;
    }
}
