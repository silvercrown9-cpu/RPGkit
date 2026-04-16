# PlyGame Modern - Архитектурное руководство

## Обзор архитектуры

Данный документ описывает архитектурные решения и паттерны, использованные при разработке современного аналога plyGame для Unity.

## 1. Модульная архитектура на базе UPM

### Структура пакетов

```
Packages/
├── com.plygame.runtime/          # Runtime ядро
│   ├── package.json
│   ├── Core/
│   │   ├── Graph/                # Система графов
│   │   ├── Events/               # Шина событий
│   │   └── Serialization/        # Сериализация
│   └── Data/                     # ScriptableObject данные
│
├── com.plygame.editor/           # Editor инструменты
│   ├── package.json
│   ├── GraphView/                # Визуальный редактор
│   ├── Editors/                  # Кастомные инспекторы
│   └── Windows/                  # Editor окна
│
├── com.plygame.tests/            # Тесты
│   ├── Runtime/
│   └── Editor/
│
└── com.plygame.samples/          # Примеры использования
    └── DialogueSystem/
```

### Принципы разделения

1. **Runtime не зависит от Editor** - все editor-классы изолированы через `#if UNITY_EDITOR`
2. **Ядро не зависит от UI** - графовая система абстрагирована от конкретных реализаций UI
3. **Событийная декупляция** - компоненты общаются через EventBus, а не напрямую

## 2. Графовая система

### Компоненты

#### GraphNode (Базовый класс узла)

```csharp
public abstract class GraphNode
{
    public string Guid { get; set; }
    public string Name { get; set; }
    public List<string> Connections { get; set; }
    
    public abstract Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context);
    public virtual List<string> Validate() { ... }
}
```

**Ответственность:**
- Хранение метаданных узла
- Определение логики выполнения
- Валидация конфигурации

#### GraphAsset (Контейнер графа)

```csharp
[CreateAssetMenu(fileName = "NewGraph", menuName = "PlyGame/Graph")]
public class GraphAsset : ScriptableObject
{
    [SerializeField] private List<GraphNodeData> _nodes;
    
    public List<GraphNodeData> GetEntryNodes();
    public List<string> ValidateGraph();
    private List<string> DetectCycles();
}
```

**Ответственность:**
- Хранение коллекции узлов
- Поиск входных точек
- Валидация структуры графа
- Обнаружение циклов

#### ExecutionContext (Контекст выполнения)

```csharp
public class ExecutionContext
{
    private readonly Dictionary<string, object> _data;
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    public void SetVariable<T>(string key, T value);
    public T GetVariable<T>(string key, T defaultValue);
    public CancellationToken CancellationToken { get; }
    public void Cancel();
}
```

**Ответственность:**
- Передача данных между узлами
- Управление отменой выполнения
- Изоляция состояния выполнения

### Паттерн выполнения

```
1. Загрузка GraphAsset
2. Получение entry nodes через GetEntryNodes()
3. Для каждого entry node:
   a. Десериализация в GraphNode
   b. Создание ExecutionContext
   c. Асинхронное выполнение ExecuteAsync()
   d. Переход к connected nodes если ShouldContinue = true
4. Очистка ресурсов (Dispose контекста)
```

## 3. Шина событий (EventBus)

### Архитектура

```
┌─────────────┐     Publish      ┌──────────┐
│  Publisher  │ ───────────────► │ EventBus │
└─────────────┘                  └────┬─────┘
                                      │
                    ┌─────────────────┼─────────────────┐
                    │                 │                 │
              Subscribe         Subscribe         Subscribe
                    │                 │                 │
                    ▼                 ▼                 ▼
            ┌───────────┐     ┌───────────┐     ┌───────────┐
            │ Handler 1 │     │ Handler 2 │     │ Handler 3 │
            └───────────┘     └───────────┘     └───────────┘
```

### Типы подписок

1. **Синхронные**: `Subscribe<T>(Action<T>)`
2. **Асинхронные**: `SubscribeAsync<T>(Func<T, Task>)`
3. **Ожидающие**: `PublishAsync<T>()` - ждёт завершения всех handlers

### Потокобезопасность

```csharp
private readonly object _lock = new object();
private readonly Dictionary<Type, List<object>> _handlers;

public void Subscribe<T>(Action<T> handler) where T : IEvent
{
    lock (_lock)
    {
        // Безопасное добавление handler
    }
}

public void Publish<T>(T eventData) where T : IEvent
{
    List<object> handlersToCall;
    
    lock (_lock)
    {
        // Копирование списка для итерации
        handlersToCall = new List<object>(_handlers[typeof(T)]);
    }
    
    // Итерация вне lock для избежания deadlock
    foreach (var handler in handlersToCall) { ... }
}
```

## 4. Сериализация

### System.Text.Json конфигурация

```csharp
public class SerializationService
{
    private readonly JsonSerializerOptions _jsonOptions;
    
    public SerializationService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
```

### Формат сохранения графа

```json
{
  "Version": "1.0",
  "Description": "Main dialogue tree",
  "Nodes": [
    {
      "Guid": "550e8400-e29b-41d4-a716-446655440000",
      "Name": "Start",
      "NodeType": "PlyGame.Runtime.Core.Graph.StartNode",
      "JsonData": "{}",
      "Connections": ["660e8400-e29b-41d4-a716-446655440001"],
      "PositionX": 100,
      "PositionY": 200
    }
  ]
}
```

### AOT-совместимость

Для IL2CPP/AOT платформ требуется предварительная регистрация типов:

```csharp
// В инициализационном коде
AOTCompiler.RegisterGenericInstanceType(typeof(Task<>));
AOTCompiler.RegisterGenericInstanceType(typeof(Dictionary<,>));
```

## 5. Editor интеграция

### GraphView архитектура

```
┌────────────────────────────────────────────┐
│          PlyGameEditorWindow               │
│  ┌──────────────────────────────────────┐  │
│  │              Toolbar                 │  │
│  ├──────────────────────────────────────┤  │
│  │                                      │  │
│  │         PlyGraphView                 │  │
│  │  ┌────────┐    ┌────────┐           │  │
│  │  │ Node 1 │───►│ Node 2 │           │  │
│  │  └────────┘    └────────┘           │  │
│  │                                      │  │
│  └──────────────────────────────────────┘  │
│  ┌──────────────────────────────────────┐  │
│  │         Inspector Panel              │  │
│  └──────────────────────────────────────┘  │
└────────────────────────────────────────────┘
```

### Компоненты GraphView

1. **PlyGraphNode** - визуальное представление узла
2. **PlyGraphEdge** - визуальное представление связи
3. **PlyGraphView** - контейнер графа
4. **PlyGraphNodeProvider** - поиск и создание узлов

### UI Toolkit стилизация

```uss
/* PlyGameEditorStyles.uss */
PlyGameEditorRoot {
    flex-direction: column;
}

#GraphContainer {
    flex-grow: 1;
    background-color: #2b2b2b;
}

#InspectorPanel {
    min-width: 250px;
    padding: 10px;
}
```

## 6. Расширяемость

### Создание кастомного узла

```csharp
[System.Serializable]
public class CustomActionNode : GraphNode
{
    [SerializeField] private string _actionName;
    [SerializeField] private float _delay;
    
    public override async Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
    {
        await Task.Delay(TimeSpan.FromSeconds(_delay), context.CancellationToken);
        
        EventBus.Instance.Publish(new ActionExecutedEvent
        {
            ActionName = _actionName,
            Source = Guid
        });
        
        return NodeExecutionResult.SuccessResult();
    }
    
    public override List<string> Validate()
    {
        var errors = base.Validate();
        if (_delay < 0) errors.Add("Delay must be non-negative");
        return errors;
    }
}
```

### Регистрация типа узла

```csharp
// В редакторе добавить в PlyGraphNodeProvider
tree.Add(new SearchTreeEntry(new GUIContent("Custom Action")) 
{ 
    level = 2, 
    userData = "CustomActionNode" 
});
```

## 7. Производительность

### Оптимизации runtime

1. **Кэширование десериализации**
   ```csharp
   private static readonly Dictionary<string, GraphNode> _cache = new();
   ```

2. **Объектные пулы**
   ```csharp
   private static readonly ObjectPool<ExecutionContext> _pool = new(...);
   ```

3. **Асинхронное выполнение без блокировок**
   ```csharp
   await Task.Yield(); // Возврат в main thread
   ```

### Профилирование

Используйте Unity Profiler с фильтрами:
- `PlyGame.*` для runtime методов
- `GC.Alloc` для выделения памяти
- `WaitForTargetFPS` для синхронизации

## 8. Тестирование

### Стратегия тестирования

1. **Unit тесты** - изолированная логика узлов
2. **Integration тесты** - выполнение полных графов
3. **Editor тесты** - UI и сериализация

### Пример unit теста

```csharp
[TestFixture]
public class GraphNodeTests
{
    [Test]
    public async Task ExecuteAsync_ReturnsSuccess_WhenValid()
    {
        // Arrange
        var node = new TestNode();
        var context = new ExecutionContext();
        
        // Act
        var result = await node.ExecuteAsync(context);
        
        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsTrue(result.ShouldContinue);
    }
    
    [Test]
    public void Validate_ReturnsError_WhenNameEmpty()
    {
        // Arrange
        var node = new TestNode { Name = "" };
        
        // Act
        var errors = node.Validate();
        
        // Assert
        Assert.IsNotEmpty(errors);
        Assert.IsTrue(errors.Contains("Node must have a name"));
    }
}
```

## 9. Миграция с оригинального plyGame

### Отличия в API

| Оригинал plyGame | PlyGame Modern |
|-----------------|----------------|
| `Node.Execute()` | `await Node.ExecuteAsync()` |
| `Graph.Run()` | `GetEntryNodes() + ExecuteAsync()` |
| XML сериализация | System.Text.Json |
| Legacy GUI | UI Toolkit + GraphView |
| Синхронное выполнение | Async/await паттерн |

### Шаги миграции

1. Экспорт графов из старого формата в JSON
2. Обновление кастомных узлов под новый API
3. Замена прямых вызовов на событийную модель
4. Тестирование в Unity 2022 LTS+

## 10. Best Practices

### Do's

✅ Используйте async/await для всех IO операций
✅ Освобождайте ресурсы через `using` или `Dispose()`
✅ Валидируйте узлы перед сохранением
✅ Используйте EventBus для межсистемной коммуникации
✅ Пишите unit тесты для кастомных узлов

### Don'ts

❌ Не блокируйте главный поток (Thread.Sleep, Wait())
❌ Не храните ссылки на MonoBehaviour в узлах
❌ Не используйте dynamic для сериализации
❌ Не игнорируйте CancellationToken
❌ Не создавайте новые экземпляры EventBus

---

*Документ актуален для версии 1.0.0*
