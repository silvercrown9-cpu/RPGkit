using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlyGame.Runtime.Core
{
    /// <summary>
    /// Контейнер графа на базе ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewGraph", menuName = "PlyGame/Graph Asset")]
    public class GraphAsset : ScriptableObject
    {
        public string GraphId;
        public string GraphName;
        public string Description;
        
        [SerializeReference]
        public List<GraphNode> Nodes = new List<GraphNode>();
        
        public GraphNode StartNode;
        
        [SerializeField]
        private List<string> _nodeIds = new List<string>();

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(GraphId))
            {
                GraphId = Guid.NewGuid().ToString();
            }
            
            UpdateNodeIds();
            ValidateConnections();
        }

        public void UpdateNodeIds()
        {
            _nodeIds.Clear();
            foreach (var node in Nodes)
            {
                if (node != null && string.IsNullOrEmpty(node.Id))
                {
                    node.Id = Guid.NewGuid().ToString();
                }
                
                if (node != null && !_nodeIds.Contains(node.Id))
                {
                    _nodeIds.Add(node.Id);
                }
            }
        }

        public void ValidateConnections()
        {
            var validIds = new HashSet<string>(_nodeIds);
            
            foreach (var node in Nodes)
            {
                if (node == null) continue;
                
                if (node.OutputNodes != null)
                {
                    node.OutputNodes = node.OutputNodes
                        .Where(n => n != null && validIds.Contains(n.Id))
                        .ToArray();
                }
                
                if (node.BranchNodes != null)
                {
                    node.BranchNodes = node.BranchNodes
                        .Where(n => n != null && validIds.Contains(n.Id))
                        .ToArray();
                }
            }
        }

        public bool HasCycle()
        {
            var visited = new HashSet<string>();
            var recStack = new HashSet<string>();
            
            foreach (var node in Nodes)
            {
                if (node == null) continue;
                if (!visited.Contains(node.Id))
                {
                    if (HasCycleUtil(node, visited, recStack))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        private bool HasCycleUtil(GraphNode node, HashSet<string> visited, HashSet<string> recStack)
        {
            if (node == null) return false;
            
            visited.Add(node.Id);
            recStack.Add(node.Id);
            
            if (node.OutputNodes != null)
            {
                foreach (var output in node.OutputNodes)
                {
                    if (output == null) continue;
                    
                    if (!visited.Contains(output.Id))
                    {
                        if (HasCycleUtil(output, visited, recStack))
                            return true;
                    }
                    else if (recStack.Contains(output.Id))
                    {
                        return true;
                    }
                }
            }
            
            if (node.BranchNodes != null)
            {
                foreach (var branch in node.BranchNodes)
                {
                    if (branch == null) continue;
                    
                    if (!visited.Contains(branch.Id))
                    {
                        if (HasCycleUtil(branch, visited, recStack))
                            return true;
                    }
                    else if (recStack.Contains(branch.Id))
                    {
                        return true;
                    }
                }
            }
            
            recStack.Remove(node.Id);
            return false;
        }

        public GraphNode GetNodeById(string id)
        {
            return Nodes.FirstOrDefault(n => n != null && n.Id == id);
        }

        public T GetNodeById<T>(string id) where T : GraphNode
        {
            return Nodes.FirstOrDefault(n => n != null && n.Id == id) as T;
        }

        public List<GraphNode> GetNodesByType<T>() where T : GraphNode
        {
            return Nodes.Where(n => n is T).ToList();
        }

        public void AddNode(GraphNode node)
        {
            if (node == null) return;
            
            if (string.IsNullOrEmpty(node.Id))
            {
                node.Id = Guid.NewGuid().ToString();
            }
            
            Nodes.Add(node);
            _nodeIds.Add(node.Id);
        }

        public void RemoveNode(string nodeId)
        {
            var node = GetNodeById(nodeId);
            if (node == null) return;
            
            Nodes.Remove(node);
            _nodeIds.Remove(nodeId);
            
            foreach (var n in Nodes)
            {
                if (n == null) continue;
                
                if (n.OutputNodes != null)
                {
                    n.OutputNodes = n.OutputNodes.Where(o => o?.Id != nodeId).ToArray();
                }
                
                if (n.BranchNodes != null)
                {
                    n.BranchNodes = n.BranchNodes.Where(b => b?.Id != nodeId).ToArray();
                }
            }
        }
    }
}
