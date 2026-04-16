# PlyGame Modern - Расширенные возможности

## Обзор новых функций

PlyGame Modern был значительно расширен по сравнению с базовой версией. Ниже приведено подробное описание всех доступных систем.

---

## 📊 Система переменных (Variables)

### Архитектура

Система переменных предоставляет типобезопасный способ хранения и манипуляции данными во время выполнения графов.

#### Типы переменных

```csharp
// Базовый класс
GraphVariable (абстрактный)
├── IntVariable      // Целочисленные значения
├── FloatVariable    // Числа с плавающей точкой
├── StringVariable   // Строковые значения
├── BoolVariable     // Булевы значения
└── GameObjectVariable // Ссылки на объекты
```

### Использование

```csharp
using PlyGame.Runtime.Variables;

// Создание менеджера переменных
var variableManager = new VariableManager();

// Добавление переменных
variableManager.AddVariable(new IntVariable("PlayerLevel", 1));
variableManager.AddVariable(new StringVariable("PlayerName", "Hero"));
variableManager.AddVariable(new BoolVariable("HasKey", false));
variableManager.AddVariable(new FloatVariable("Health", 100f));

// Чтение значений
int level = variableManager.GetValue<int>("PlayerLevel");
string name = variableManager.GetValue<string>("PlayerName");

// Изменение значений
variableManager.SetValue("PlayerLevel", 2);
variableManager.SetValue("HasKey", true);

// Подписка на изменения
variableManager.OnVariableChanged += (varName, newValue) => {
    Debug.Log($"Variable {varName} changed to {newValue}");
};

// Проверка существования
if (variableManager.HasVariable("PlayerLevel")) { ... }

// Экспорт/Импорт для сохранений
var data = variableManager.Export();
// ... сохранение данных ...
variableManager.Import(data);
```

### Узлы работы с переменными

#### SetVariableNode
Устанавливает значение переменной.

**Параметры:**
- `Variable Name` - имя переменной
- `Variable Type` - тип (Int, Float, String, Bool)
- Значение соответствующего типа

#### CheckVariableNode
Проверяет условие с переменной.

**Параметры:**
- `Variable Name` - имя переменной
- `Comparison Type` - тип сравнения (Equals, NotEquals, Greater, Less, GreaterOrEqual, LessOrEqual)
- Значение для сравнения

**Выходы:**
- Выход 0 (True) - условие выполнено
- Выход 1 (False) - условие не выполнено

#### MathOperationNode
Выполняет математическую операцию над числовой переменной.

**Параметры:**
- `Variable Name` - имя переменной
- `Operation` - операция (Add, Subtract, Multiply, Divide, Modulo)
- Значение операнда

---

## 💾 Система сохранений (Save System)

### Архитектура

Система сохранений обеспечивает персистентность данных игры между сессиями.

#### SaveData
Класс данных сохранения:
```csharp
public class SaveData
{
    public string saveName;          // Имя сохранения
    public long timestamp;           // Временная метка
    public int playTimeSeconds;      // Время игры
    public string sceneName;         // Текущая сцена
    public Dictionary<string, object> variables;    // Переменные
    public Dictionary<string, object> customData;   // Пользовательские данные
}
```

### Использование

```csharp
using PlyGame.Runtime.SaveSystem;

// Создание менеджера сохранений
var saveManager = new SaveGameManager("MyGameSaves");

// Создание нового сохранения
await saveManager.CreateNewSave("SaveSlot1");

// Сохранение текущего состояния
await saveManager.Save();

// Загрузка сохранения
var saveData = await saveManager.LoadSave("SaveSlot1");

// Работа с переменными в сохранении
saveManager.SetVariable("Gold", 500);
int gold = saveManager.GetVariable<int>("Gold", 0);

// Пользовательские данные
saveManager.SetCustomData("Difficulty", "Hard");
string difficulty = saveManager.GetCustomData<string>("Difficulty", "Normal");

// Список сохранений
List<string> saves = saveManager.GetAllSaveNames();

// Проверка существования
if (saveManager.SaveExists("SaveSlot1")) { ... }

// Удаление сохранения
saveManager.DeleteSave("SaveSlot1");

// События
saveManager.OnSaveCreated += (save) => Debug.Log("Save created");
saveManager.OnSaveLoaded += (save) => Debug.Log("Save loaded");
saveManager.OnSaveDeleted += (name) => Debug.Log("Save deleted");
```

### Интеграция с переменными

```csharp
// Синхронизация VariableManager с SaveGameManager
void SyncVariables(VariableManager varManager, SaveGameManager saveManager)
{
    // Экспорт переменных в сохранение
    var varData = varManager.Export();
    foreach (var kvp in varData)
    {
        saveManager.SetVariable(kvp.Key, kvp.Value);
    }
    
    // Импорт переменных из сохранения
    var loadedData = saveManager.CurrentSave?.variables;
    if (loadedData != null)
    {
        varManager.Import(loadedData);
    }
}
```

---

## 💬 Диалоговая система (Dialogue System)

### Архитектура

Диалоговая система позволяет создавать ветвящиеся диалоги с выбором реплик.

#### Ключевые классы

```csharp
// Реплика персонажа
DialogueLine {
    speakerId, text, audioClipId, duration
}

// Вариант ответа
DialogueChoice {
    text, targetNodeId, conditionVariable, requiredValue
}

// Данные узла диалога
DialogueNodeData {
    nodeId, lines[], choices[], nextNodeId, isEndNode
}
```

### Использование

```csharp
using PlyGame.Runtime.Dialogue;

// Создание менеджера диалогов
var dialogueManager = new DialogueManager();

// Загрузка диалога из графа
dialogueManager.LoadDialogue(graphAsset);

// Начало диалога
dialogueManager.StartDialogue("StartNode");

// Подписка на события
dialogueManager.OnLineStarted += (line) => {
    ShowDialogueText(line.speakerId, line.text);
    PlayAudio(line.audioClipId);
};

dialogueManager.OnLineEnded += (line) => {
    HideDialogueText();
};

dialogueManager.OnChoicesAvailable += (choices) => {
    ShowChoiceButtons(choices);
};

dialogueManager.OnDialogueEnded += () => {
    CloseDialogueUI();
};

// Продолжение диалога (по клику игрока)
dialogueManager.Continue();

// Выбор варианта ответа
dialogueManager.SelectChoice(choiceIndex);

// Проверка доступности выбора с условиями
bool available = dialogueManager.IsChoiceAvailable(choice, variableManager);
```

### Компонент DialogueRunner

```csharp
// MonoBehaviour компонент для сцены
public class DialogueRunner : MonoBehaviour
{
    // Запуск диалога из графа
    public void StartDialogueFromGraph(GraphAsset graphAsset, string startNodeId = null);
    
    // Продолжение текущего диалога
    public void ContinueDialogue();
    
    // Выбор ответа
    public void SelectDialogueChoice(int index);
}

// Пример использования в сцене
var runner = FindObjectOfType<DialogueRunner>();
runner.StartDialogueFromGraph(myDialogueGraph, "Intro");
```

### Создание диалогового графа

1. Создайте новый Graph Asset
2. Добавьте узлы типа "DialogueNode"
3. Настройте реплики и выборы в инспекторе узла
4. Соедините узлы связями
5. Используйте CheckVariableNode для условных переходов

---

## 📜 Система квестов (Quest System)

### Архитектура

Система квестов управляет задачами игрока, их прогрессом и наградами.

#### Статусы квеста

```csharp
enum QuestStatus {
    NotStarted,   // Не начат
    Active,       // Активен
    Completed,    // Завершён
    Failed        // Провален
}
```

#### Структура квеста

```csharp
QuestData {
    id, title, description, status,
    objectives[], rewards, prerequisiteQuestId, isRepeatable
}

QuestObjective {
    id, description, targetAmount, currentAmount, isCompleted
}
```

### Использование

```csharp
using PlyGame.Runtime.Quests;

// Создание менеджера квестов
var questManager = new QuestManager();

// Создание квеста
var quest = new QuestData("kill_wolves", "Wolf Hunter");
quest.description = "Kill 5 wolves in the forest";
quest.objectives.Add(new QuestObjective("Kill wolves", 5));
quest.rewardDescription = "100 Gold, Wolf Cloak";
quest.rewards["gold"] = 100;
quest.isRepeatable = false;

// Добавление квеста
questManager.AddQuest(quest);

// Подписка на события
questManager.OnQuestStarted += (q) => {
    Debug.Log($"Quest started: {q.title}");
};

questManager.OnQuestCompleted += (q) => {
    Debug.Log($"Quest completed: {q.title}");
    GiveRewards(q.rewards);
};

questManager.OnObjectiveUpdated += (q, obj) => {
    UpdateQuestUI(q.id, obj);
};

// Начало квеста
questManager.StartQuest("kill_wolves");

// Прогресс цели
questManager.ProgressObjective("kill_wolves", objectiveId, amount: 1);

// Завершение/Провал
questManager.CompleteQuest("kill_wolves");
questManager.FailQuest("kill_wolves");

// Получение информации
var activeQuests = questManager.ActiveQuests;
var completedQuests = questManager.CompletedQuests;
var specificQuest = questManager.GetQuest("kill_wolves");

// Сброс повторяемого квеста
questManager.ResetRepeatableQuest("daily_quest");
```

### Узел QuestNode

```csharp
// Использование в графе
var questNode = new QuestNode("main_quest_1", QuestNode.QuestAction.Start);

// Доступные действия:
// - Start    : Начать квест
// - Complete : Завершить квест
// - Fail     : Провалить квест
// - Progress : Прогресс (требует дополнительных данных)
```

### Интеграция с другими системами

```csharp
// Квест завершается при выполнении условия в переменных
var checkNode = new CheckVariableNode("WolvesKilled", CheckVariableNode.ComparisonType.GreaterOrEqual);
checkNode.intValue = 5;

// После проверки - запустить квест
var questNode = new QuestNode("wolf_hunter_complete", QuestNode.QuestAction.Complete);

// Соединить: checkNode (True) -> questNode
```

---

## 🔗 Интеграция систем

### Полный пример RPG-сценария

```csharp
using PlyGame.Runtime;
using PlyGame.Runtime.Variables;
using PlyGame.Runtime.Quests;
using PlyGame.Runtime.Dialogue;
using PlyGame.Runtime.SaveSystem;

public class RPGSceneManager : MonoBehaviour
{
    [SerializeField] private GraphAsset dialogueGraph;
    [SerializeField] private GraphAsset questGraph;
    
    private VariableManager variableManager;
    private QuestManager questManager;
    private DialogueManager dialogueManager;
    private SaveGameManager saveManager;
    
    private async void Start()
    {
        // Инициализация систем
        variableManager = new VariableManager();
        questManager = new QuestManager();
        dialogueManager = new DialogueManager();
        saveManager = new SaveGameManager("RPGGame");
        
        // Настройка начальных переменных
        variableManager.AddVariable(new IntVariable("Level", 1));
        variableManager.AddVariable(new IntVariable("Gold", 100));
        variableManager.AddVariable(new IntVariable("WolvesKilled", 0));
        variableManager.AddVariable(new BoolVariable("MetElder", false));
        
        // Создание квестов
        var wolfQuest = new QuestData("wolf_hunt", "Wolf Hunt");
        wolfQuest.objectives.Add(new QuestObjective("Kill wolves", 5));
        wolfQuest.rewards["gold"] = 100;
        questManager.AddQuest(wolfQuest);
        
        // Загрузка диалогов
        dialogueManager.LoadDialogue(dialogueGraph);
        
        // Загрузка сохранения если существует
        if (saveManager.SaveExists("autosave"))
        {
            var save = await saveManager.LoadSave("autosave");
            variableManager.Import(save.variables);
        }
        
        // Контекст для выполнения графов
        var context = new ExecutionContext();
        context.Set("VariableManager", variableManager);
        context.Set("QuestManager", questManager);
        
        // Выполнение графа квестов
        var executor = new GraphExecutor();
        await executor.ExecuteGraph(questGraph, context);
    }
    
    // Обработчик убийства волка
    public void OnWolfKilled()
    {
        variableManager.SetValue("WolvesKilled", 
            variableManager.GetValue<int>("WolvesKilled") + 1);
        
        var wolvesKilled = variableManager.GetValue<int>("WolvesKilled");
        questManager.ProgressObjective("wolf_hunt", "objective_id", 1);
        
        if (wolvesKilled >= 5)
        {
            // Авто-завершение квеста через граф
            ExecuteQuestCompleteGraph();
        }
    }
    
    // Сохранение игры
    public async void SaveGame()
    {
        if (!saveManager.HasActiveSave)
        {
            await saveManager.CreateNewSave("autosave");
        }
        
        // Синхронизация переменных
        foreach (var var in variableManager.GetExposedVariables())
        {
            saveManager.SetVariable(var.Name, var.GetValue());
        }
        
        await saveManager.Save();
    }
}
```

---

## 📋 Best Practices

### 1. Управление памятью

```csharp
// Всегда очищайте менеджеры при смене сцены
void OnDestroy()
{
    variableManager?.Clear();
}

// Используйте CancellationToken для отмены операций
var cts = new CancellationTokenSource();
await saveManager.Save(cts.Token);
```

### 2. Ошибкоустойчивость

```csharp
// Проверяйте существование перед использованием
if (variableManager.HasVariable("Health"))
{
    var health = variableManager.GetValue<float>("Health");
}

// Обрабатывайте ошибки загрузки
var save = await saveManager.LoadSave("slot1");
if (save == null)
{
    // Создать новое сохранение
    await saveManager.CreateNewSave("slot1");
}
```

### 3. Производительность

```csharp
// Кэшируйте часто используемые значения
private int cachedGold;

void Start()
{
    cachedGold = variableManager.GetValue<int>("Gold");
    variableManager.OnVariableChanged += (name, val) => {
        if (name == "Gold") cachedGold = (int)val;
    };
}

// Избегайте частых вызовов SetValue
// Вместо этого обновляйте пакетно
```

### 4. Тестирование

```csharp
// Юнит-тест для VariableManager
[Test]
public void VariableManager_SetAndGet_IntVariable()
{
    var manager = new VariableManager();
    manager.AddVariable(new IntVariable("Test", 10));
    
    manager.SetValue("Test", 20);
    Assert.AreEqual(20, manager.GetValue<int>("Test"));
}

// Тест квестовой системы
[Test]
public void QuestManager_CompleteQuest_TriggersEvent()
{
    var manager = new QuestManager();
    bool eventCalled = false;
    manager.OnQuestCompleted += _ => eventCalled = true;
    
    var quest = new QuestData("test", "Test Quest");
    quest.objectives.Add(new QuestObjective("obj", 1));
    quest.objectives[0].Progress();
    
    manager.AddQuest(quest);
    manager.StartQuest("test");
    manager.CompleteQuest("test");
    
    Assert.IsTrue(eventCalled);
}
```

---

## 🚀 Миграция с оригинального plyGame

### Отличия в API

| Оригинал plyGame | PlyGame Modern |
|-----------------|----------------|
| `GraphNode.Execute()` | `GraphNode.ExecuteAsync()` |
| `EditorWindow` | `EditorWindow` + UI Toolkit |
| JSON/XML сериализация | System.Text.Json |
| Синхронное выполнение | Async/await |
| Глобальное состояние | ExecutionContext |

### Пошаговая миграция

1. **Обновите узлы**: Замените `Execute()` на `ExecuteInternalAsync()`
2. **Добавьте async**: Используйте `async/await` паттерны
3. **Мигрируйте данные**: Перенесите данные в ScriptableObject
4. **Обновите редактор**: Используйте GraphView вместо кастомного рендеринга
5. **Добавьте типы**: Используйте систему переменных вместо динамических словарей

---

## 📞 Поддержка и сообщество

Для вопросов и предложений:
- GitHub Issues: [ссылка]
- Discord: [ссылка]
- Документация: [ссылка]
