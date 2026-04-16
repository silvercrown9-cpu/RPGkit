using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlyGame.Runtime.Core.Graph
{
    /// <summary>
    /// Связь между узлами графа
    /// </summary>
    [Serializable]
    public class GraphEdge
    {
        [SerializeField] private string sourceNodeId;
        [SerializeField] private string targetNodeId;
        [SerializeField] private string edgeId;
        
        public string SourceNodeId => sourceNodeId;
        public string TargetNodeId => targetNodeId;
        public string EdgeId => edgeId;
        
        public GraphEdge(string sourceId, string targetId)
        {
            edgeId = Guid.NewGuid().ToString();
            sourceNodeId = sourceId;
            targetNodeId = targetId;
        }
    }
    
    /// <summary>
    /// ScriptableObject контейнер для хранения графа
    /// </summary>
    [CreateAssetMenu(fileName = "NewGraph", menuName = "PlyGame/Graph Asset")]
    public class GraphAsset : ScriptableObject
    {
        [SerializeField] private string graphId;
        [SerializeField] private string graphName;
        [SerializeField] private string graphDescription;
        [SerializeField] private List<GraphNodeData> nodesData = new();
        [SerializeField] private List<GraphEdge> edges = new();
        [SerializeField] private Vector2 viewOffset;
        [SerializeField] private float viewScale = 1f;
        
        public string GraphId => graphId;
        public string GraphName => graphName;
        public string Description => graphDescription;
        public IReadOnlyList<GraphNodeData> Nodes => nodesData.AsReadOnly();
        public IReadOnlyList<GraphEdge> Edges => edges.AsReadOnly();
        
        private Dictionary<string, GraphNode> _nodeCache;
        private Dictionary<string, List<string>> _adjacencyList;
        
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(graphId))
                graphId = Guid.NewGuid().ToString();
            
            DeserializeNodes();
        }
        
        /// <summary>
        /// Десериализация узлов из данных
        /// </summary>
        private void DeserializeNodes()
        {
            _nodeCache = new Dictionary<string, GraphNode>();
            
            foreach (var nodeData in nodesData)
            {
                var node = nodeData.CreateNode();
                if (node != null)
                {
                    _nodeCache[node.NodeId] = node;
                }
            }
        }
        
        /// <summary>
        /// Получить узел по ID
        /// </summary>
        public GraphNode GetNode(string nodeId)
        {
            if (_nodeCache == null) DeserializeNodes();
            return _nodeCache.TryGetValue(nodeId, out var node) ? node : null;
        }
        
        /// <summary>
        /// Получить все исходящие связи из узла
        /// </summary>
        public List<GraphEdge> GetOutgoingEdges(string nodeId)
        {
            return edges.Where(e => e.SourceNodeId == nodeId).ToList();
        }
        
        /// <summary>
        /// Добавить узел в граф
        /// </summary>
        public void AddNode(GraphNode node, Vector2 position)
        {
            if (_nodeCache == null) DeserializeNodes();
            
            node.Position = position;
            var nodeData = new GraphNodeData(node);
            nodesData.Add(nodeData);
            _nodeCache[node.NodeId] = node;
        }
        
        /// <summary>
        /// Удалить узел из графа
        /// </summary>
        public void RemoveNode(string nodeId)
        {
            if (_nodeCache == null) DeserializeNodes();
            
            nodesData.RemoveAll(n => n.NodeId == nodeId);
            edges.RemoveAll(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId);
            _nodeCache.Remove(nodeId);
        }
        
        /// <summary>
        /// Добавить связь между узлами
        /// </summary>
        public void AddEdge(string sourceId, string targetId)
        {
            edges.Add(new GraphEdge(sourceId, targetId));
        }
        
        /// <summary>
        /// Удалить связь
        /// </summary>
        public void RemoveEdge(string edgeId)
        {
            edges.RemoveAll(e => e.EdgeId == edgeId);
        }
        
        /// <summary>
        /// Валидация графа на наличие циклов
        /// </summary>
        public bool ValidateGraph(out List<string> errors)
        {
            errors = new List<string>();
            
            // Проверка на наличие стартового узла
            var startNodes = nodesData.Count(n => n.TypeName == typeof(StartNode).FullName);
            if (startNodes == 0)
                errors.Add("Граф должен содержать хотя бы один StartNode");
            else if (startNodes > 1)
                errors.Add("Граф должен содержать только один StartNode");
            
            // Проверка на циклы
            if (HasCycle())
                errors.Add("Граф содержит циклы (кроме допустимых обратных связей)");
            
            // Проверка изолированных узлов
            var isolatedNodes = FindIsolatedNodes();
            if (isolatedNodes.Count > 0)
                errors.Add($"Найдено изолированных узлов: {string.Join(", ", isolatedNodes)}");
            
            return errors.Count == 0;
        }
        
        /// <summary>
        /// Проверка графа на наличие циклов с помощью DFS
        /// </summary>
        private bool HasCycle()
        {
            BuildAdjacencyList();
            
            var visited = new HashSet<string>();
            var recStack = new HashSet<string>();
            
            foreach (var node in nodesData)
            {
                if (!visited.Contains(node.NodeId))
                {
                    if (HasCycleUtil(node.NodeId, visited, recStack))
                        return true;
                }
            }
            
            return false;
        }
        
        private bool HasCycleUtil(string nodeId, HashSet<string> visited, HashSet<string> recStack)
        {
            visited.Add(nodeId);
            recStack.Add(nodeId);
            
            if (_adjacencyList.TryGetValue(nodeId, out var neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        if (HasCycleUtil(neighbor, visited, recStack))
                            return true;
                    }
                    else if (recStack.Contains(neighbor))
                    {
                        return true;
                    }
                }
            }
            
            recStack.Remove(nodeId);
            return false;
        }
        
        private void BuildAdjacencyList()
        {
            _adjacencyList = new Dictionary<string, List<string>>();
            
            foreach (var edge in edges)
            {
                if (!_adjacencyList.ContainsKey(edge.SourceNodeId))
                    _adjacencyList[edge.SourceNodeId] = new List<string>();
                
                _adjacencyList[edge.SourceNodeId].Add(edge.TargetNodeId);
            }
        }
        
        /// <summary>
        /// Поиск изолированных узлов
        /// </summary>
        private List<string> FindIsolatedNodes()
        {
            var connectedNodes = new HashSet<string>();
            
            foreach (var edge in edges)
            {
                connectedNodes.Add(edge.SourceNodeId);
                connectedNodes.Add(edge.TargetNodeId);
            }
            
            return nodesData
                .Where(n => !connectedNodes.Contains(n.NodeId))
                .Select(n => n.NodeId)
                .ToList();
        }
        
        /// <summary>
        /// Очистка графа
        /// </summary>
        public void Clear()
        {
            nodesData.Clear();
            edges.Clear();
            _nodeCache?.Clear();
            _adjacencyList?.Clear();
        }
    }
    
    /// <summary>
    /// Сериализуемые данные узла для хранения в ScriptableObject
    /// </summary>
    [Serializable]
    public class GraphNodeData
    {
        [SerializeField] private string nodeId;
        [SerializeField] private string typeName;
        [SerializeField] private string nodeName;
        [SerializeField] private string nodeDescription;
        [SerializeField] private Vector2 position;
        [SerializeField] private string jsonData;
        
        public string NodeId => nodeId;
        public string TypeName => typeName;
        public string NodeName => nodeName;
        public string Description => nodeDescription;
        public Vector2 Position => position;
        public string JsonData => jsonData;
        
        public GraphNodeData(GraphNode node)
        {
            nodeId = node.NodeId;
            typeName = node.GetType().FullName;
            nodeName = node.NodeName;
            nodeDescription = node.Description;
            position = node.Position;
            // jsonData сериализуется отдельно через SerializationService
        }
        
        /// <summary>
        /// Создание экземпляра узла из данных
        /// </summary>
        public GraphNode CreateNode()
        {
            var type = Type.GetType(typeName);
            if (type == null)
            {
                Debug.LogError($"Тип узла не найден: {typeName}");
                return null;
            }
            
            var node = Activator.CreateInstance(type) as GraphNode;
            if (node != null)
            {
                // Восстановление полей через рефлексию или JSON
                // Детали реализации зависят от SerializationService
            }
            
            return node;
        }
    }
}
