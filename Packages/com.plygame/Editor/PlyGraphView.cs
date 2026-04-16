using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace PlyGame.Editor
{
    /// <summary>
    /// Визуальный редактор графов на базе Unity GraphView
    /// </summary>
    public class PlyGraphView : GraphView
    {
        private readonly string _graphDataPath;
        
        public PlyGraphView(string graphPath)
        {
            _graphDataPath = graphPath;
            
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            
            // Мини-карта
            var miniMap = new MiniMap { anchored = true };
            var corners = cornerButtonProvider.GetCornerButtons();
            miniMap.SetPosition(new Rect(corners.BottomRight.x - 200, corners.BottomRight.y - 150, 200, 150));
            Add(miniMap);
        }
        
        /// <summary>
        /// Создание узла
        /// </summary>
        public override Port InstantiatePort(Orientation orientation, Direction direction, Port.Capacity capacity, Type type)
        {
            var port = base.InstantiatePort(orientation, direction, capacity, type);
            return port;
        }
        
        /// <summary>
        /// Создание узла графа
        /// </summary>
        public PlyGraphNode CreatePlyGraphNode(Core.GraphNode node, Vector2 position)
        {
            var pNode = new PlyGraphNode(node);
            pNode.SetPosition(new Rect(position, new Vector2(160, 100)));
            
            // Создание портов входа/выхода
            var inputPort = pNode.InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            inputPort.portName = "In";
            pNode.inputContainer.Add(inputPort);
            
            var outputPort = pNode.InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            outputPort.portName = "Out";
            pNode.outputContainer.Add(outputPort);
            
            pNode.RefreshExpandedState();
            pNode.RefreshPorts();
            
            AddElement(pNode);
            
            return pNode;
        }
        
        /// <summary>
        /// Создание связи между узлами
        /// </summary>
        public PlyGraphEdge CreateEdge(Port outputPort, Port inputPort)
        {
            var edge = new PlyGraphEdge
            {
                output = outputPort,
                input = inputPort
            };
            
            edge.UpdateEdgeControl();
            
            AddElement(edge);
            
            return edge;
        }
        
        /// <summary>
        /// Удаление элементов
        /// </summary>
        public override void DeleteSelection(OperationReason reason)
        {
            base.DeleteSelection(reason);
        }
        
        /// <summary>
        /// Получение узлов для поиска
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            
            evt.menu.AppendAction("Add Node/Start", _ => CreateNodeFromSearch(typeof(Core.StartNode)));
            evt.menu.AppendAction("Add Node/End", _ => CreateNodeFromSearch(typeof(Core.EndNode)));
            evt.menu.AppendAction("Add Node/Delay", _ => CreateNodeFromSearch(typeof(Core.DelayNode)));
            evt.menu.AppendAction("Add Node/Set Variable", _ => CreateNodeFromSearch(typeof(Nodes.SetVariableNode)));
            evt.menu.AppendAction("Add Node/Check Variable", _ => CreateNodeFromSearch(typeof(Nodes.CheckVariableNode)));
            evt.menu.AppendAction("Add Node/Math Operation", _ => CreateNodeFromSearch(typeof(Nodes.MathOperationNode)));
        }
        
        private void CreateNodeFromSearch(System.Type nodeType)
        {
            var mousePos = GetLocalMousePosition(Event.current.mousePosition, true);
            var node = Activator.CreateInstance(nodeType) as Core.GraphNode;
            CreatePlyGraphNode(node, mousePos);
        }
        
        /// <summary>
        /// Сохранение графа
        /// </summary>
        public void SaveGraph(Core.GraphAsset asset)
        {
            if (asset == null) return;
            
            // Очистка старых данных
            // Сериализация узлов и связей в asset
            
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
        
        /// <summary>
        /// Загрузка графа
        /// </summary>
        public void LoadGraph(Core.GraphAsset asset)
        {
            if (asset == null) return;
            
            graphViewChanged -= OnGraphViewChanged;
            
            // Очистка текущего представления
            var nodes = graphElements.OfType<PlyGraphNode>().ToList();
            foreach (var node in nodes)
            {
                RemoveElement(node);
            }
            
            var edges = graphElements.OfType<PlyGraphEdge>().ToList();
            foreach (var edge in edges)
            {
                RemoveElement(edge);
            }
            
            // Загрузка узлов из asset
            foreach (var nodeData in asset.Nodes)
            {
                var node = nodeData.CreateNode();
                if (node != null)
                {
                    CreatePlyGraphNode(node, nodeData.Position);
                }
            }
            
            // Загрузка связей
            // ...
            
            graphViewChanged += OnGraphViewChanged;
        }
        
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            // Обработка изменений графа
            if (change.elementsToRemove != null)
            {
                foreach (var elem in change.elementsToRemove)
                {
                    if (elem is PlyGraphNode pNode)
                    {
                        // Удаление узла из asset
                    }
                    else if (elem is PlyGraphEdge pEdge)
                    {
                        // Удаление связи из asset
                    }
                }
            }
            
            return change;
        }
    }
    
    /// <summary>
    /// Узел графа в редакторе
    /// </summary>
    public class PlyGraphNode : Node
    {
        private readonly Core.GraphNode _data;
        
        public PlyGraphNode(Core.GraphNode data)
        {
            _data = data;
            title = data.NodeName;
            capabilities |= Capabilities.Deletable | Capabilities.Movable | Capabilities.Copyable;
            
            // Добавление заголовка и описания
            mainContainer.style.height = 100;
            
            var descriptionLabel = new Label(data.Description);
            descriptionLabel.style.fontSize = 10;
            descriptionLabel.style.color = Color.gray;
            mainContainer.Add(descriptionLabel);
        }
        
        public Core.GraphNode GetData() => _data;
    }
    
    /// <summary>
    /// Связь в редакторе
    /// </summary>
    public class PlyGraphEdge : Edge
    {
    }
}
