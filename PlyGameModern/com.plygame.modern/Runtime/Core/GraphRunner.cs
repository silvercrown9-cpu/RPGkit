using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace PlyGame.Runtime.Core
{
    /// <summary>
    /// Менеджер выполнения графов
    /// </summary>
    public class GraphRunner : MonoBehaviour
    {
        public static GraphRunner Instance { get; private set; }
        
        [SerializeField]
        private bool _autoInitialize = true;
        
        private Dictionary<string, GraphAsset> _loadedGraphs = new Dictionary<string, GraphAsset>();
        private Dictionary<string, Task> _runningGraphs = new Dictionary<string, Task>();
        
        public event UnityAction<GraphAsset> OnGraphStarted;
        public event UnityAction<GraphAsset> OnGraphCompleted;
        public event UnityAction<GraphAsset, string> OnGraphFailed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (_autoInitialize)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public async Task<bool> RunGraphAsync(GraphAsset graph, GameObject sender = null, GameObject target = null)
        {
            if (graph == null)
            {
                Debug.LogError("[GraphRunner] Graph is null");
                return false;
            }

            if (graph.StartNode == null && graph.Nodes.Count > 0)
            {
                graph.StartNode = graph.Nodes[0];
            }

            if (graph.StartNode == null)
            {
                Debug.LogError($"[GraphRunner] Graph {graph.GraphName} has no start node");
                return false;
            }

            try
            {
                OnGraphStarted?.Invoke(graph);
                
                var context = new ExecutionContext(graph.GraphId, sender, target);
                await ExecuteNodeAsync(graph.StartNode, context);
                
                OnGraphCompleted?.Invoke(graph);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GraphRunner] Error running graph {graph.GraphName}: {ex.Message}");
                OnGraphFailed?.Invoke(graph, ex.Message);
                return false;
            }
        }

        public async Task RunGraphFromNodeAsync(GraphAsset graph, string nodeId, GameObject sender = null, GameObject target = null)
        {
            if (graph == null || string.IsNullOrEmpty(nodeId))
            {
                Debug.LogError("[GraphRunner] Invalid graph or node ID");
                return;
            }

            var node = graph.GetNodeById(nodeId);
            if (node == null)
            {
                Debug.LogError($"[GraphRunner] Node {nodeId} not found in graph {graph.GraphName}");
                return;
            }

            var context = new ExecutionContext(graph.GraphId, sender, target);
            await ExecuteNodeAsync(node, context);
        }

        private async Task ExecuteNodeAsync(GraphNode node, ExecutionContext context)
        {
            if (node == null || context.CancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!node.Validate())
            {
                Debug.LogWarning($"[GraphRunner] Node {node.Name} validation failed");
                return;
            }

            node.OnEnter();
            
            var result = await node.ExecuteAsync(context);
            
            node.OnExit();

            if (!result.Success)
            {
                Debug.LogError($"[GraphRunner] Node {node.Name} execution failed: {result.ErrorMessage}");
                return;
            }

            if (!result.ShouldContinue)
            {
                return;
            }

            if (!string.IsNullOrEmpty(result.NextNodeId))
            {
                var nextNode = GetNodeFromGraph(node, result.NextNodeId);
                if (nextNode != null)
                {
                    await ExecuteNodeAsync(nextNode, context);
                }
            }
            else if (result.BranchNodeIds != null && result.BranchNodeIds.Length > 0)
            {
                var tasks = new List<Task>();
                foreach (var branchId in result.BranchNodeIds)
                {
                    var branchNode = GetNodeFromGraph(node, branchId);
                    if (branchNode != null)
                    {
                        tasks.Add(ExecuteNodeAsync(branchNode, context));
                    }
                }
                
                await Task.WhenAll(tasks);
            }
            else if (node.OutputNodes != null && node.OutputNodes.Length > 0)
            {
                foreach (var outputNode in node.OutputNodes)
                {
                    if (outputNode != null)
                    {
                        await ExecuteNodeAsync(outputNode, context);
                    }
                }
            }
        }

        private GraphNode GetNodeFromGraph(GraphNode currentNode, string nodeId)
        {
            if (currentNode == null || string.IsNullOrEmpty(nodeId))
            {
                return null;
            }

            var graph = FindGraphContainingNode(currentNode);
            if (graph != null)
            {
                return graph.GetNodeById(nodeId);
            }

            foreach (var loadedGraph in _loadedGraphs.Values)
            {
                var node = loadedGraph.GetNodeById(nodeId);
                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        private GraphAsset FindGraphContainingNode(GraphNode node)
        {
            foreach (var graph in _loadedGraphs.Values)
            {
                if (graph.Nodes.Contains(node))
                {
                    return graph;
                }
            }
            return null;
        }

        public void RegisterGraph(GraphAsset graph)
        {
            if (graph != null && !_loadedGraphs.ContainsKey(graph.GraphId))
            {
                _loadedGraphs[graph.GraphId] = graph;
            }
        }

        public void UnregisterGraph(string graphId)
        {
            if (_loadedGraphs.ContainsKey(graphId))
            {
                _loadedGraphs.Remove(graphId);
            }
        }

        public GraphAsset GetGraph(string graphId)
        {
            return _loadedGraphs.TryGetValue(graphId, out var graph) ? graph : null;
        }

        public void StopAllGraphs()
        {
            foreach (var task in _runningGraphs.Values)
            {
                if (task != null && !task.IsCompleted)
                {
                    // Note: CancellationToken should be used for proper cancellation
                }
            }
            _runningGraphs.Clear();
        }
    }
}
