using UnityEditor;
using UnityEngine.UIElements;
using PlyGame.Editor.GraphView;
using PlyGame.Runtime.Core.Graph;

namespace PlyGame.Editor.Windows
{
    /// <summary>
    /// Main editor window for PlyGame graph editor
    /// Uses UI Toolkit for modern Unity editor integration
    /// </summary>
    public class PlyGameEditorWindow : EditorWindow
    {
        private PlyGraphView _graphView;
        private GraphAsset _currentGraph;
        private Toolbar _toolbar;
        private VisualElement _inspectorPanel;
        
        [MenuItem("Tools/PlyGame/Graph Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<PlyGameEditorWindow>();
            window.titleContent = new GUIContent("PlyGame Editor");
            window.minSize = new UnityEngine.Vector2(800, 600);
        }
        
        public void CreateGUI()
        {
            try
            {
                // Root visual element
                var root = rootVisualElement;
                root.name = "PlyGameEditorRoot";
                
                // Load USS stylesheet
                var styleSheet = Resources.Load<StyleSheet>("PlyGameEditorStyles");
                if (styleSheet != null)
                    root.styleSheets.Add(styleSheet);
                
                // Create toolbar
                CreateToolbar();
                
                // Create main content area with graph view and inspector
                CreateMainContent();
                
                // Load last opened graph or create new one
                LoadLastOpenedGraph();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to create PlyGame editor GUI: {e}");
            }
        }
        
        private void CreateToolbar()
        {
            _toolbar = new Toolbar();
            
            // New button
            var newButton = new ToolbarButton(() => CreateNewGraph())
            {
                text = "New",
                tooltip = "Create new graph"
            };
            _toolbar.Add(newButton);
            
            // Open button
            var openButton = new ToolbarButton(() => OpenGraph())
            {
                text = "Open",
                tooltip = "Open existing graph"
            };
            _toolbar.Add(openButton);
            
            // Save button
            var saveButton = new ToolbarButton(() => SaveGraph())
            {
                text = "Save",
                tooltip = "Save current graph"
            };
            _toolbar.Add(saveButton);
            
            _toolbar.Add(new ToolbarSpacer());
            
            // Validate button
            var validateButton = new ToolbarButton(() => ValidateGraph())
            {
                text = "Validate",
                tooltip = "Validate graph structure"
            };
            _toolbar.Add(validateButton);
            
            // Zoom controls
            var zoomField = new ToolbarPopupSearchField();
            zoomField.tooltip = "Search nodes";
            _toolbar.Add(zoomField);
            
            rootVisualElement.Add(_toolbar);
        }
        
        private void CreateMainContent()
        {
            var mainContainer = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            
            // Left pane - Graph view
            var graphContainer = new VisualElement { name = "GraphContainer" };
            graphContainer.style.flexGrow = 1;
            mainContainer.Add(graphContainer);
            
            // Right pane - Inspector
            _inspectorPanel = new VisualElement { name = "InspectorPanel" };
            _inspectorPanel.style.minWidth = 250;
            mainContainer.Add(_inspectorPanel);
            
            rootVisualElement.Add(mainContainer);
        }
        
        private void CreateNewGraph()
        {
            var graph = ScriptableObject.CreateInstance<GraphAsset>();
            graph.name = "NewGraph";
            
            // Set default path
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Graph",
                "NewGraph",
                "asset",
                "Choose a location for the new graph");
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssets();
                LoadGraph(graph);
            }
        }
        
        private void OpenGraph()
        {
            var path = EditorUtility.OpenFilePanel(
                "Open Graph",
                "Assets",
                "asset");
            
            if (string.IsNullOrEmpty(path)) return;
            
            if (!path.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("Error", "Must select an asset inside the project", "OK");
                return;
            }
            
            var assetPath = "Assets" + path.Substring(Application.dataPath.Length);
            var graph = AssetDatabase.LoadAssetAtPath<GraphAsset>(assetPath);
            
            if (graph != null)
                LoadGraph(graph);
            else
                EditorUtility.DisplayDialog("Error", "Failed to load graph asset", "OK");
        }
        
        private void SaveGraph()
        {
            if (_currentGraph == null || _graphView == null)
            {
                EditorUtility.DisplayDialog("Info", "No graph to save", "OK");
                return;
            }
            
            _graphView.SaveGraph();
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Success", "Graph saved successfully", "OK");
        }
        
        private void ValidateGraph()
        {
            if (_currentGraph == null)
            {
                EditorUtility.DisplayDialog("Info", "No graph loaded", "OK");
                return;
            }
            
            var errors = _currentGraph.ValidateGraph();
            
            if (errors.Count == 0)
                EditorUtility.DisplayDialog("Validation", "Graph is valid!", "OK");
            else
            {
                var errorMessage = string.Join("\n", errors);
                EditorUtility.DisplayDialog("Validation Errors", $"Found {errors.Count} error(s):\n\n{errorMessage}", "OK");
            }
        }
        
        private void LoadGraph(GraphAsset graph)
        {
            if (_graphView != null && _graphView.parent != null)
                _graphView.RemoveFromHierarchy();
            
            _currentGraph = graph;
            _graphView = new PlyGraphView(graph);
            _graphView.StretchToParentSize();
            
            // Add graph view to container
            var graphContainer = rootVisualElement.Q<VisualElement>("GraphContainer");
            if (graphContainer != null)
            {
                graphContainer.Clear();
                graphContainer.Add(_graphView);
            }
            
            UpdateInspector();
        }
        
        private void LoadLastOpenedGraph()
        {
            // Try to load last opened graph from session state
            var lastGraphPath = SessionState.GetString("PlyGame.LastGraphPath", "");
            
            if (!string.IsNullOrEmpty(lastGraphPath))
            {
                var graph = AssetDatabase.LoadAssetAtPath<GraphAsset>(lastGraphPath);
                if (graph != null)
                {
                    LoadGraph(graph);
                    return;
                }
            }
            
            // Create empty graph view if no last graph
            if (_graphView == null)
            {
                _graphView = new PlyGraphView(null);
                var graphContainer = rootVisualElement.Q<VisualElement>("GraphContainer");
                if (graphContainer != null)
                {
                    graphContainer.Clear();
                    graphContainer.Add(_graphView);
                }
            }
        }
        
        private void UpdateInspector()
        {
            if (_inspectorPanel == null) return;
            
            _inspectorPanel.Clear();
            
            if (_currentGraph != null)
            {
                var titleLabel = new Label(_currentGraph.name) { name = "InspectorTitle" };
                titleLabel.style.fontSize = 14;
                titleLabel.style.bold = true;
                _inspectorPanel.Add(titleLabel);
                
                var descLabel = new Label("Description:") { name = "DescLabel" };
                descLabel.style.marginTop = 10;
                _inspectorPanel.Add(descLabel);
                
                var descField = new TextField
                {
                    value = _currentGraph.Description,
                    multiline = true,
                    name = "DescriptionField"
                };
                descField.RegisterValueChangedCallback(evt =>
                {
                    Undo.RecordObject(_currentGraph, "Change Description");
                    // Update description via reflection or direct access
                    EditorUtility.SetDirty(_currentGraph);
                });
                _inspectorPanel.Add(descField);
                
                var versionLabel = new Label($"Version: {_currentGraph.Version}") { name = "VersionLabel" };
                versionLabel.style.marginTop = 10;
                _inspectorPanel.Add(versionLabel);
                
                var nodeCountLabel = new Label($"Nodes: {_currentGraph.Nodes.Count}") { name = "NodeCountLabel" };
                nodeCountLabel.style.marginTop = 5;
                _inspectorPanel.Add(nodeCountLabel);
            }
            else
            {
                var infoLabel = new Label("No graph selected") { name = "NoSelectionLabel" };
                infoLabel.style.color = UnityEngine.Color.gray;
                _inspectorPanel.Add(infoLabel);
            }
        }
        
        private void OnSelectionChange()
        {
            // Update inspector when selection changes
            if (Selection.activeObject is GraphAsset graph)
            {
                LoadGraph(graph);
            }
        }
        
        public void OnDestroy()
        {
            // Save current graph path to session state
            if (_currentGraph != null)
            {
                var path = AssetDatabase.GetAssetPath(_currentGraph);
                SessionState.SetString("PlyGame.LastGraphPath", path);
            }
            
            // Cleanup
            if (_graphView != null)
            {
                _graphView.SaveGraph();
            }
        }
    }
}
