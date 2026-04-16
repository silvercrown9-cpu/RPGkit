# PlyGame Modern - Visual Scripting Framework for Unity

Современный аналог фреймворка plyGame для создания RPG-механик в Unity. Полностью переработан с использованием актуальных API Unity 2022 LTS+ и современных стандартов разработки.

## Требования

- Unity 2022.3 LTS или новее
- .NET Standard 2.1 / C# 10+
- UI Toolkit
- GraphView (Unity Experimental)

## Архитектура

Проект состоит из трёх основных UPM-пакетов:

### 1. com.plygame.runtime
Ядро системы выполнения графов. Содержит:
- **Core/Graph**: Базовые классы узлов, исполнение графов, контекст выполнения
- **Core/Events**: Шина событий для декуплированной коммуникации
- **Core/Serialization**: Современная сериализация через System.Text.Json
- **Data**: ScriptableObject-контейнеры для данных

**Ключевые особенности:**
- Асинхронное выполнение узлов (async/await)
- Потокобезопасная шина событий
- AOT-совместимая сериализация
- Поддержка CancellationToken для отмены выполнения

### 2. com.plygame.editor
Инструменты редактора на базе GraphView и UI Toolkit:
- **GraphView**: Визуальный редактор графов
- **Editors**: Кастомные инспекторы для узлов
- **Windows**: Основное окно редактора

**Ключевые особенности:**
- Интеграция с Unity GraphView
- Поиск узлов через SearchWindow
- Валидация графов в реальном времени
- Сохранение состояния между сессиями

### 3. com.plygame.tests
Набор юнит-тестов:
- **Runtime**: Тесты runtime-логики
- **Editor**: Тесты редакторских функций

## Установка

### Вариант 1: Через Package Manager (рекомендуется)

1. Откройте `Window > Package Manager`
2. Нажмите `+` → `Add package from disk...`
3. Выберите `Packages/com.plygame.runtime/package.json`
4. Повторите для `com.plygame.editor`

### Вариант 2: Через manifest.json

Добавьте в `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.plygame.runtime": "file:../Packages/com.plygame.runtime",
    "com.plygame.editor": "file:../Packages/com.plygame.editor"
  }
}
```

## Быстрый старт

### 1. Создание первого графа

1. Откройте редактор: `Tools > PlyGame > Graph Editor`
2. Нажмите `New` для создания нового графа
3. Сохраните в нужной директории
4. ПКМ → `Create Node` для добавления узлов
5. Соедините узлы перетаскиванием от порта к порту

### 2. Программное выполнение графа

```csharp
using PlyGame.Runtime.Core.Graph;
using PlyGame.Runtime.Core.Serialization;
using System.Threading.Tasks;
using UnityEngine;

public class GraphExecutor : MonoBehaviour
{
    [SerializeField] private GraphAsset _graphAsset;
    
    private async void Start()
    {
        var context = new ExecutionContext();
        
        try
        {
            // Получить входные узлы
            var entryNodes = _graphAsset.GetEntryNodes();
            
            // Выполнить каждый входной узел
            foreach (var nodeData in entryNodes)
            {
                var node = nodeData.Deserialize();
                if (node != null)
                {
                    var result = await node.ExecuteAsync(context);
                    
                    if (!result.Success)
                    {
                        Debug.LogError($"Node execution failed: {result.ErrorMessage}");
                        break;
                    }
                    
                    if (!result.ShouldContinue)
                        break;
                }
            }
        }
        finally
        {
            context.Dispose();
        }
    }
}
```

### 3. Работа с событиями

```csharp
using PlyGame.Runtime.Core.Events;
using System.Threading.Tasks;
using UnityEngine;

// Определение события
public class DialogueStartedEvent : EventBase
{
    public string DialogueId { get; set; }
    public string SpeakerName { get; set; }
}

// Подписка на событие
public class DialogueListener : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Instance.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
        EventBus.Instance.SubscribeAsync<DialogueStartedEvent>(OnDialogueStartedAsync);
    }
    
    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
    }
    
    private void OnDialogueStarted(DialogueStartedEvent evt)
    {
        Debug.Log($"Dialogue started: {evt.DialogueId} by {evt.SpeakerName}");
    }
    
    private Task OnDialogueStartedAsync(DialogueStartedEvent evt)
    {
        // Асинхронная обработка
        return Task.CompletedTask;
    }
}

// Публикация события
public class DialogueTrigger : MonoBehaviour
{
    public void TriggerDialogue(string dialogueId, string speaker)
    {
        var evt = new DialogueStartedEvent
        {
            Source = name,
            DialogueId = dialogueId,
            SpeakerName = speaker
        };
        
        EventBus.Instance.Publish(evt);
        // Или асинхронно:
        // await EventBus.Instance.PublishAsync(evt);
    }
}
```

### 4. Сериализация и сохранение

```csharp
using PlyGame.Runtime.Core.Serialization;
using PlyGame.Runtime.Core.Graph;
using UnityEngine;
using System.IO;

public class GraphSaver : MonoBehaviour
{
    private readonly SerializationService _serializer = new SerializationService();
    
    public async void SaveGraph(GraphAsset graph, string filePath)
    {
        var json = _serializer.SerializeGraph(graph);
        await _serializer.SaveToFileAsync(json, filePath);
    }
    
    public async void LoadGraph(string filePath)
    {
        var json = await _serializer.LoadFromFileAsync<string>(filePath);
        var graph = _serializer.DeserializeGraph(json);
    }
}
```

## Создание кастомных узлов

```csharp
using PlyGame.Runtime.Core.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public class DialogueNode : GraphNode
{
    [SerializeField] private string _dialogueText;
    [SerializeField] private string _speakerName;
    [SerializeField] private List<string> _choices = new List<string>();
    
    public override async Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
    {
        // Ожидание главного потока для UI операций
        await MainThreadDispatcher.ExecuteAsync(() =>
        {
            // Показать диалог
            DialogueUI.Show(_speakerName, _dialogueText, _choices);
        });
        
        // Ждать выбора игрока
        var choiceIndex = await WaitForPlayerChoice();
        
        context.SetVariable("SelectedChoice", choiceIndex);
        
        return NodeExecutionResult.SuccessResult(new Dictionary<string, object>
        {
            ["ChoiceIndex"] = choiceIndex
        });
    }
    
    public override List<string> Validate()
    {
        var errors = base.Validate();
        
        if (string.IsNullOrWhiteSpace(_dialogueText))
            errors.Add("Dialogue text is required");
        
        if (_choices.Count == 0)
            errors.Add("At least one choice is required");
        
        return errors;
    }
}
```

## Производительность и оптимизация

### Рекомендации

1. **Избегайте блокирующих операций в узлах**
   - Используйте async/await для IO-операций
   - Не вызывайте синхронные методы загрузки ресурсов

2. **Кэширование десериализации**
   ```csharp
   private static readonly Dictionary<string, GraphNode> _nodeCache = new();
   
   public GraphNode GetCachedNode(string guid)
   {
       if (!_nodeCache.TryGetValue(guid, out var node))
       {
           node = DeserializeNode(guid);
           _nodeCache[guid] = node;
       }
       return node;
   }
   ```

3. **Объектные пулы для часто создаваемых объектов**
   ```csharp
   private static readonly ObjectPool<ExecutionContext> _contextPool = 
       new ObjectPool<ExecutionContext>(() => new ExecutionContext());
   
   public void ExecuteGraph()
   {
       var context = _contextPool.Get();
       try
       {
           // Выполнение
       }
       finally
       {
           _contextPool.Release(context);
       }
   }
   ```

## Тестирование

Запуск тестов через Test Runner (`Window > General > Test Runner`):

```bash
# Runtime тесты
dotnet test --filter "Category=Runtime"

# Editor тесты
dotnet test --filter "Category=Editor"
```

## Лицензия

MIT License - см. файл LICENSE

## Поддержка

- Документация: [Wiki](https://github.com/your-org/plygame-modern/wiki)
- Issues: [GitHub Issues](https://github.com/your-org/plygame-modern/issues)
- Discord: [Community Server](https://discord.gg/your-server)
