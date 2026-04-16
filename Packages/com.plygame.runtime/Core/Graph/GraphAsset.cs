using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PlyGame.Runtime.Core.Graph
{
    /// <summary>
    /// Graph asset that contains nodes and edges.
    /// Stored as a ScriptableObject for Unity integration.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGraph", menuName = "PlyGame/Graph", order = 1)]
    public class GraphAsset : ScriptableObject
    {
        [SerializeField] private List<GraphNodeData> _nodes = new List<GraphNodeData>();
        [SerializeField] private string _description;
        [SerializeField] private string _version = "1.0";
        
        public List<GraphNodeData> Nodes => _nodes;
        public string Description => _description;
        public string Version => _version;
        
        /// <summary>
        /// Find node by GUID
        /// </summary>
        public GraphNodeData FindNode(string guid)
        {
            return _nodes.Find(n => n.Guid == guid);
        }
        
        /// <summary>
        /// Get all entry nodes (nodes with no incoming connections)
        /// </summary>
        public List<GraphNodeData> GetEntryNodes()
        {
            var allTargets = new HashSet<string>();
            foreach (var node in _nodes)
            {
                foreach (var connection in node.Connections)
                {
                    allTargets.Add(connection);
                }
            }
            
            return _nodes.FindAll(n => !allTargets.Contains(n.Guid));
        }
        
        /// <summary>
        /// Validate entire graph
        /// </summary>
        public List<string> ValidateGraph()
        {
            var errors = new List<string>();
            
            // Check for duplicate GUIDs
            var guids = new HashSet<string>();
            foreach (var node in _nodes)
            {
                if (!guids.Add(node.Guid))
                    errors.Add($"Duplicate GUID found: {node.Guid}");
                
                // Check for broken connections
                foreach (var connection in node.Connections)
                {
                    if (FindNode(connection) == null)
                        errors.Add($"Node '{node.Name}' has broken connection to '{connection}'");
                }
                
                // Validate individual nodes
                var nodeErrors = node.Validate();
                foreach (var error in nodeErrors)
                {
                    errors.Add($"Node '{node.Name}': {error}");
                }
            }
            
            // Check for cycles (optional, depending on graph type)
            var cycleErrors = DetectCycles();
            errors.AddRange(cycleErrors);
            
            return errors;
        }
        
        /// <summary>
        /// Detect cycles in the graph using DFS
        /// </summary>
        private List<string> DetectCycles()
        {
            var errors = new List<string>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            
            bool Dfs(string nodeId)
            {
                visited.Add(nodeId);
                recursionStack.Add(nodeId);
                
                var node = FindNode(nodeId);
                if (node == null) return false;
                
                foreach (var connection in node.Connections)
                {
                    if (!visited.Contains(connection))
                    {
                        if (Dfs(connection))
                            return true;
                    }
                    else if (recursionStack.Contains(connection))
                    {
                        errors.Add($"Cycle detected involving node: {connection}");
                        return true;
                    }
                }
                
                recursionStack.Remove(nodeId);
                return false;
            }
            
            foreach (var node in _nodes)
            {
                if (!visited.Contains(node.Guid))
                {
                    Dfs(node.Guid);
                }
            }
            
            return errors;
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Add a new node to the graph (Editor only)
        /// </summary>
        public void AddNode(GraphNodeData node)
        {
            _nodes.Add(node);
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        /// <summary>
        /// Remove a node from the graph (Editor only)
        /// </summary>
        public void RemoveNode(string guid)
        {
            _nodes.RemoveAll(n => n.Guid == guid);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
    
    /// <summary>
    /// Serializable wrapper for GraphNode to support Unity serialization
    /// </summary>
    [Serializable]
    public class GraphNodeData
    {
        [SerializeField] private string _guid;
        [SerializeField] private string _name;
        [SerializeField] private string _nodeType;
        [SerializeField] private string _jsonData;
        [SerializeField] private List<string> _connections = new List<string>();
        [SerializeField] private float _positionX;
        [SerializeField] private float _positionY;
        
        public string Guid 
        { 
            get => _guid; 
            set => _guid = value; 
        }
        
        public string Name 
        { 
            get => _name; 
            set => _name = value; 
        }
        
        public string NodeType 
        { 
            get => _nodeType; 
            set => _nodeType = value; 
        }
        
        public string JsonData 
        { 
            get => _jsonData; 
            set => _jsonData = value; 
        }
        
        public List<string> Connections 
        { 
            get => _connections; 
            set => _connections = value; 
        }
        
        public float PositionX 
        { 
            get => _positionX; 
            set => _positionX = value; 
        }
        
        public float PositionY 
        { 
            get => _positionY; 
            set => _positionY = value; 
        }
        
        /// <summary>
        /// Deserialize node data into actual GraphNode instance
        /// </summary>
        public GraphNode Deserialize()
        {
            var type = Type.GetType(_nodeType);
            if (type == null)
            {
                Debug.LogError($"Failed to find node type: {_nodeType}");
                return null;
            }
            
            var node = Activator.CreateInstance(type) as GraphNode;
            if (node == null)
            {
                Debug.LogError($"Failed to create instance of type: {_nodeType}");
                return null;
            }
            
            node.Guid = _guid;
            node.Name = _name;
            node.PositionX = _positionX;
            node.PositionY = _positionY;
            node.Connections = new List<string>(_connections);
            
            // Deserialize custom data using JSON
            if (!string.IsNullOrEmpty(_jsonData))
            {
                try
                {
                    var jsonOptions = new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    var additionalData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(_jsonData, jsonOptions);
                    if (additionalData != null)
                    {
                        foreach (var kvp in additionalData)
                        {
                            // Set properties via reflection or custom deserialization logic
                            // This is simplified - in production, use proper polymorphic serialization
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to deserialize node data: {e.Message}");
                }
            }
            
            return node;
        }
        
        /// <summary>
        /// Validate node data
        /// </summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(_guid))
                errors.Add("Node must have a GUID");
            if (string.IsNullOrWhiteSpace(_name))
                errors.Add("Node must have a name");
            if (string.IsNullOrWhiteSpace(_nodeType))
                errors.Add("Node must have a type");
            return errors;
        }
    }
}
