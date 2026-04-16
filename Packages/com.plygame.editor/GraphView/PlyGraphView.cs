using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using PlyGame.Runtime.Core.Graph;

namespace PlyGame.Editor.GraphView
{
    /// <summary>
    /// Custom node for PlyGame graph editor using Unity GraphView
    /// </summary>
    public class PlyGraphNode : Node
    {
        private readonly GraphNodeData _nodeData;
        private readonly PlyGraphView _graphView;
        
        public string NodeGuid => _nodeData.Guid;
        
        public PlyGraphNode(GraphNodeData nodeData, PlyGraphView graphView) : base("Assets/BuiltinResources/TextRendering/Resources/Skins/Common.uss")
        {
            _nodeData = nodeData;
            _graphView = graphView;
            
            SetupNode();
        }
        
        private void SetupNode()
        {
            // Set basic node properties
            title = _nodeData.Name;
            capabilities = Capabilities.Movable | Capabilities.Deletable | Capabilities.Renamable;
            
            // Set GUID as user data for identification
            userData = _nodeData.Guid;
            
            // Create input port
            var inputPort = CreatePort("Input", Direction.Input, Port.Capacity.Multi);
            inputContainer.Add(inputPort);
            
            // Create output port
            var outputPort = CreatePort("Output", Direction.Output, Port.Capacity.Multi);
            outputContainer.Add(outputPort);
            
            // Add description label if available
            if (!string.IsNullOrEmpty(_nodeData.JsonData))
            {
                var descriptionLabel = new Label(_nodeData.JsonData);
                descriptionLabel.style.fontSize = 10;
                descriptionLabel.style.color = Color.gray;
                mainContainer.Add(descriptionLabel);
            }
            
            // Set position from saved data
            var rect = GetPosition();
            rect.position = new Vector2(_nodeData.PositionX, _nodeData.PositionY);
            SetPosition(rect);
            
            // Register callbacks
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        private Port CreatePort(string name, Direction direction, Port.Capacity capacity)
        {
            var port = InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(bool));
            port.portName = name;
            return port;
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Update node position in data
            var rect = GetPosition();
            _nodeData.PositionX = rect.x;
            _nodeData.PositionY = rect.y;
        }
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            
            evt.menu.AppendAction("Duplicate", DuplicateNode);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Delete", DeleteNode);
        }
        
        private void DuplicateNode(DropdownMenuAction action)
        {
            _graphView.DuplicateNode(this);
        }
        
        private void DeleteNode(DropdownMenuAction action)
        {
            _graphView.DeleteNode(this);
        }
        
        /// <summary>
        /// Update node data from current state
        /// </summary>
        public void UpdateNodeData()
        {
            _nodeData.Name = title;
            var rect = GetPosition();
            _nodeData.PositionX = rect.x;
            _nodeData.PositionY = rect.y;
        }
    }
    
    /// <summary>
    /// Custom edge for PlyGame graph connections
    /// </summary>
    public class PlyGraphEdge : Edge
    {
        public string SourceGuid => (outputNode as PlyGraphNode)?.NodeGuid;
        public string TargetGuid => (inputNode as PlyGraphNode)?.NodeGuid;
        
        public PlyGraphEdge() : base("Assets/BuiltinResources/TextRendering/Resources/Skins/Common.uss")
        {
            capabilities = Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable;
        }
    }
    
    /// <summary>
    /// Main graph view for PlyGame editor
    /// </summary>
    public class PlyGraphView : GraphView
    {
        private readonly GraphAsset _graphAsset;
        private readonly Dictionary<string, PlyGraphNode> _nodesByGuid = new Dictionary<string, PlyGraphNode>();
        
        public GraphAsset GraphAsset => _graphAsset;
        
        public PlyGraphView(GraphAsset graphAsset)
        {
            _graphAsset = graphAsset;
            
            SetupGridBackground();
            SetupMiniMap();
            SetupZoom();
            LoadGraph();
        }
        
        private void SetupGridBackground()
        {
            var gridBackground = new GridBackground();
            Insert(0, gridBackground);
            gridBackground.StretchToParentSize();
        }
        
        private void SetupMiniMap()
        {
            var miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 30, 200, 150));
            Add(miniMap);
        }
        
        private void SetupZoom()
        {
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
        }
        
        private void LoadGraph()
        {
            if (_graphAsset == null) return;
            
            // Create nodes
            foreach (var nodeData in _graphAsset.Nodes)
            {
                var node = CreatePlyGraphNode(nodeData);
                AddElement(node);
                _nodesByGuid[nodeData.Guid] = node;
            }
            
            // Create edges
            foreach (var nodeData in _graphAsset.Nodes)
            {
                if (_nodesByGuid.TryGetValue(nodeData.Guid, out var sourceNode))
                {
                    foreach (var targetGuid in nodeData.Connections)
                    {
                        if (_nodesByGuid.TryGetValue(targetGuid, out var targetNode))
                        {
                            var edge = sourceNode.outputContainer[0].ConnectTo(targetNode.inputContainer[0]);
                            AddElement(edge);
                        }
                    }
                }
            }
        }
        
        private PlyGraphNode CreatePlyGraphNode(GraphNodeData nodeData)
        {
            return new PlyGraphNode(nodeData, this);
        }
        
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            
            foreach (var port in ports.ToList())
            {
                // Prevent self-connection and same-node connection
                if (startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            }
            
            return compatiblePorts;
        }
        
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            
            evt.menu.AppendAction("Create Node", CreateNodeFromContext);
        }
        
        private void CreateNodeFromContext(DropdownMenuAction action)
        {
            // Show node creation menu
            var mousePosition = action.eventInfo.localMousePosition;
            SearchWindow.Open(new SearchWindowContext(action.eventInfo.screenMousePosition), 
                new PlyGraphNodeProvider(this, mousePosition));
        }
        
        internal void DuplicateNode(PlyGraphNode node)
        {
            // Implementation for duplicating nodes
            var newNodeData = new GraphNodeData();
            // Copy data from original node...
            
            var newNode = CreatePlyGraphNode(newNodeData);
            AddElement(newNode);
            
            // Offset position slightly
            var rect = node.GetPosition();
            rect.position += new Vector2(20, 20);
            newNode.SetPosition(rect);
            
            _nodesByGuid[newNodeData.Guid] = newNode;
        }
        
        internal void DeleteNode(PlyGraphNode node)
        {
            RemoveElement(node);
            _nodesByGuid.Remove(node.NodeGuid);
        }
        
        public void SaveGraph()
        {
            if (_graphAsset == null) return;
            
            // Update all node data from current state
            foreach (var kvp in _nodesByGuid)
            {
                kvp.Value.UpdateNodeData();
            }
            
            // Update connections
            // ... implementation ...
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(_graphAsset);
#endif
        }
    }
    
    /// <summary>
    /// Provider for node creation search window
    /// </summary>
    public class PlyGraphNodeProvider : ScriptableObject, ISearchWindowProvider
    {
        private readonly PlyGraphView _graphView;
        private readonly Vector2 _mousePosition;
        
        public PlyGraphNodeProvider(PlyGraphView graphView, Vector2 mousePosition)
        {
            _graphView = graphView;
            _mousePosition = mousePosition;
        }
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
                new SearchTreeGroupEntry(new GUIContent("Basic Nodes"), 1),
            };
            
            // Add node types to search tree
            tree.Add(new SearchTreeEntry(new GUIContent("Start Node")) { level = 2, userData = "StartNode" });
            tree.Add(new SearchTreeEntry(new GUIContent("Dialogue Node")) { level = 2, userData = "DialogueNode" });
            tree.Add(new SearchTreeEntry(new GUIContent("Condition Node")) { level = 2, userData = "ConditionNode" });
            tree.Add(new SearchTreeEntry(new GUIContent("Action Node")) { level = 2, userData = "ActionNode" });
            
            return tree;
        }
        
        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            var nodeType = searchTreeEntry.userData as string;
            if (string.IsNullOrEmpty(nodeType)) return false;
            
            // Create new node based on type
            var nodeData = new GraphNodeData
            {
                Guid = System.Guid.NewGuid().ToString(),
                Name = nodeType,
                NodeType = $"PlyGame.Runtime.Core.Graph.{nodeType}",
                PositionX = _mousePosition.x,
                PositionY = _mousePosition.y
            };
            
            var node = new PlyGraphNode(nodeData, _graphView);
            node.SetPosition(new Rect(_mousePosition, new Vector2(200, 150)));
            
            _graphView.AddElement(node);
            
            return true;
        }
    }
}
