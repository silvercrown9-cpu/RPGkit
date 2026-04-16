using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlyGame.Editor
{
    /// <summary>
    /// Главное окно редактора PlyGame на базе UI Toolkit
    /// </summary>
    public class PlyGameEditorWindow : EditorWindow
    {
        private PlyGraphView _graphView;
        private Core.GraphAsset _currentGraph;
        private TwoPaneSplitView _splitView;
        
        [MenuItem("Tools/PlyGame/Graph Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<PlyGameEditorWindow>();
            window.titleContent = new GUIContent("PlyGame Editor");
            window.minSize = new Vector2(800, 600);
        }
        
        private void OnEnable()
        {
            InitializeUI();
        }
        
        private void OnDisable()
        {
            if (_graphView != null)
                rootVisualElement.Remove(_graphView);
        }
        
        private void InitializeUI()
        {
            rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("PlyGameEditorStyles"));
            
            // Создание toolbar
            var toolbar = CreateToolbar();
            rootVisualElement.Add(toolbar);
            
            // Создание split view
            _splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(_splitView);
            
            // Левая панель - дерево графов
            var leftPanel = CreateLeftPanel();
            _splitView.Add(leftPanel);
            
            // Правая панель - GraphView
            var rightPanel = CreateRightPanel();
            _splitView.Add(rightPanel);
        }
        
        private Toolbar CreateToolbar()
        {
            var toolbar = new Toolbar();
            
            // Кнопка создания нового графа
            var newButton = new ToolbarButton(() => CreateNewGraph())
            {
                text = "New Graph",
                tooltip = "Создать новый граф"
            };
            toolbar.Add(newButton);
            
            // Кнопка открытия графа
            var openButton = new ToolbarButton(() => OpenGraph())
            {
                text = "Open",
                tooltip = "Открыть существующий граф"
            };
            toolbar.Add(openButton);
            
            // Кнопка сохранения
            var saveButton = new ToolbarButton(() => SaveGraph())
            {
                text = "Save",
                tooltip = "Сохранить текущий граф"
            };
            toolbar.Add(saveButton);
            
            toolbar.Add(new ToolbarSpacer());
            
            // Кнопка валидации
            var validateButton = new ToolbarButton(() => ValidateGraph())
            {
                text = "Validate",
                tooltip = "Проверить граф на ошибки"
            };
            toolbar.Add(validateButton);
            
            // Кнопка запуска (для тестирования)
            var playButton = new ToolbarButton(() => PlayGraph())
            {
                text = "Play",
                tooltip = "Запустить граф"
            };
            toolbar.Add(playButton);
            
            return toolbar;
        }
        
        private VisualElement CreateLeftPanel()
        {
            var panel = new ScrollView();
            
            // Заголовок секции переменных
            var variablesSection = new Box();
            variablesSection.AddToClassList("section");
            
            var variablesLabel = new Label("Variables");
            variablesLabel.AddToClassList("section-header");
            variablesSection.Add(variablesLabel);
            
            // Список переменных будет добавлен здесь
            var variablesList = new ListView
            {
                makeItem = () => new Label(),
                bindItem = (element, index) =>
                {
                    var label = (Label)element;
                    // Привязка к данным переменной
                }
            };
            variablesSection.Add(variablesList);
            
            panel.Add(variablesSection);
            
            // Секция инспектора свойств
            var inspectorSection = new Box();
            inspectorSection.AddToClassList("section");
            
            var inspectorLabel = new Label("Inspector");
            inspectorLabel.AddToClassList("section-header");
            inspectorSection.Add(inspectorLabel);
            
            var propertiesContainer = new ScrollView();
            inspectorSection.Add(propertiesContainer);
            
            panel.Add(inspectorSection);
            
            return panel;
        }
        
        private VisualElement CreateRightPanel()
        {
            var container = new VisualElement();
            container.StretchToParentSize();
            
            _graphView = new PlyGraphView("");
            _graphView.StretchToParentSize();
            container.Add(_graphView);
            
            return container;
        }
        
        private void CreateNewGraph()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create New Graph",
                "NewGraph",
                "asset",
                "Выберите место для сохранения графа");
            
            if (!string.IsNullOrEmpty(path))
            {
                var graph = ScriptableObject.CreateInstance<Core.GraphAsset>();
                graph.name = System.IO.Path.GetFileNameWithoutExtension(path);
                
                AssetDatabase.CreateAsset(graph, path);
                AssetDatabase.SaveAssets();
                
                _currentGraph = graph;
                LoadGraphIntoView();
            }
        }
        
        private void OpenGraph()
        {
            var path = EditorUtility.OpenFilePanel(
                "Open Graph",
                "Assets",
                "asset");
            
            if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
            {
                var relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                _currentGraph = AssetDatabase.LoadAssetAtPath<Core.GraphAsset>(relativePath);
                
                if (_currentGraph != null)
                {
                    LoadGraphIntoView();
                }
            }
        }
        
        private void SaveGraph()
        {
            if (_currentGraph == null || _graphView == null)
            {
                EditorUtility.DisplayDialog("Error", "Нет открытого графа для сохранения", "OK");
                return;
            }
            
            _graphView.SaveGraph(_currentGraph);
        }
        
        private void ValidateGraph()
        {
            if (_currentGraph == null)
            {
                EditorUtility.DisplayDialog("Error", "Нет открытого графа", "OK");
                return;
            }
            
            if (_currentGraph.ValidateGraph(out var errors))
            {
                EditorUtility.DisplayDialog("Validation", "Граф валиден!", "OK");
            }
            else
            {
                var errorText = string.Join("\n", errors);
                EditorUtility.DisplayDialog("Validation Errors", errorText, "OK");
            }
        }
        
        private void PlayGraph()
        {
            if (_currentGraph == null)
            {
                EditorUtility.DisplayDialog("Error", "Нет открытого графа", "OK");
                return;
            }
            
            Debug.Log("Запуск графа в режиме Play...");
            // Логика запуска графа
        }
        
        private void LoadGraphIntoView()
        {
            if (_graphView != null && _currentGraph != null)
            {
                _graphView.LoadGraph(_currentGraph);
            }
        }
        
        private void OnSelectionChange()
        {
            // Обновление инспектора при изменении выделения
        }
    }
}
