# PlyGame Modern - RPG Framework для Unity

## 📖 Описание

**PlyGame Modern** — это полнофункциональный фреймворк с открытым исходным кодом для создания RPG-игр в Unity. Представляет собой современный аналог легендарного плагина plyGame, полностью переписанный с использованием актуальных технологий Unity 2022 LTS+.

## ✨ Ключевые возможности

### 🎯 Ядро системы
- **Визуальный редактор графов** на базе Unity GraphView и UI Toolkit
- **Асинхронное выполнение** узлов с поддержкой CancellationToken
- **Система переменных** (int, float, string, bool, GameObject)
- **Сериализация** через System.Text.Json с async I/O

### 📊 RPG Системы
| Система | Описание |
|---------|----------|
| **Характеристики** | Сила, ловкость, интеллект, здоровье, мана, опыт с модификаторами |
| **Инвентарь** | Предметы, стакинг, редкость, перемещение между слотами |
| **Навыки** | Активные/пассивные способности с кулдаунами и эффектами |
| **Квесты** | Цели, пререквизиты, награды, повторяемые квесты |
| **Диалоги** | Ветвящиеся диалоги с выборами и условиями |
| **Достижения** | Прогресс, категории, проценты выполнения |
| **Сохранения** | Асинхронная загрузка/сохранение, слоты, метаданные |

## 📦 Установка

### Вариант 1: Через Package Manager (рекомендуется)
1. Откройте `Window > Package Manager`
2. Нажмите `+` → `Add package from disk...`
3. Выберите файл `Packages/com.plygame/package.json`

### Вариант 2: Ручная установка
1. Скопируйте папку `com.plygame` в `Assets/Packages/`
2. Или добавьте в `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.plygame": "file:com.plygame"
  }
}
```

## 🚀 Быстрый старт

### Создание первого графа
```csharp
using PlyGame.Core;
using PlyGame.Variables;
using PlyGame.Nodes;

// Создание менеджера переменных
var variableManager = new VariableManager();
variableManager.AddVariable(new IntVariable("Gold", 100));
variableManager.AddVariable(new BoolVariable("HasQuest", false));

// Создание контекста выполнения
var context = new ExecutionContext
{
    GraphId = "TutorialGraph"
};
context.SetData("VariableManager", variableManager);

// Запуск графа через GraphAsset
var graphAsset = Resources.Load<GraphAsset>("MyGraph");
var executor = new GraphExecutor(graphAsset);
await executor.ExecuteAsync(context);
```

### Работа с инвентарём
```csharp
using PlyGame.Inventory;

var inventory = new InventoryManager(30);

// Регистрация предметов
inventory.RegisterItem(new ItemData 
{ 
    ItemId = "potion_health", 
    Name = "Зелье лечения",
    IsStackable = true,
    MaxStackSize = 99
});

// Добавление предмета
inventory.AddItem("potion_health", 5);

// Проверка наличия
if (inventory.HasItem("potion_health", 3))
{
    Debug.Log("Есть минимум 3 зелья");
}
```

### Управление характеристиками
```csharp
using PlyGame.Stats;

var stats = new StatsManager();

// Добавление характеристик
stats.AddStat("Health", 100f, StatType.Resource);
stats.AddStat("MaxHealth", 100f, StatType.Base);
stats.AddStat("Strength", 10f, StatType.Base);

// Добавление модификатора (+5 силы от предмета)
stats.AddModifier("Strength", new StatModifier(ModifierType.Flat, 5, "Equipment_Sword"));

// Получение итогового значения
float totalStrength = stats.GetValue("Strength"); // 15

// Нанесение урона
bool isDead = stats.DamageHealth(50);
```

### Диалоговая система
```csharp
using PlyGame.Dialogue;

var dialogue = new DialogueData
{
    DialogueId = "NPC_Greeting",
    Title = "Приветствие торговца",
    StartNodeId = "node_1",
    Nodes = new List<DialogueNodeData>
    {
        new DialogueNodeData
        {
            NodeId = "node_1",
            SpeakerId = "Merchant",
            Text = "Добро пожаловать в мою лавку!",
            Choices = new List<DialogueChoiceData>
            {
                new DialogueChoiceData { Text = "Показать товары", TargetNodeId = "node_shop" },
                new DialogueChoiceData { Text = "Уйти", TargetNodeId = "node_end" }
            }
        }
    }
};

var manager = new DialogueManager();
manager.OnLineDisplayed += line => Debug.Log($"{line.SpeakerId}: {line.Text}");
manager.StartDialogue(dialogue);
```

### Система квестов
```csharp
using PlyGame.Quests;

var questManager = new QuestManager();

// Создание квеста
var quest = new QuestData
{
    QuestId = "wolf_hunt",
    Title = "Охота на волков",
    Description = "Убейте 5 волков",
    Objectives = new List<QuestObjective>
    {
        new QuestObjective
        {
            ObjectId = "kill_wolves",
            Description = "Убито волков",
            Type = ObjectiveType.Kill,
            TargetAmount = 5
        }
    },
    Rewards = new List<QuestReward>
    {
        new QuestReward { Type = RewardType.Gold, Amount = 100 },
        new QuestReward { Type = RewardType.Experience, Amount = 500 }
    }
};

questManager.AddQuest(quest);
questManager.StartQuest("wolf_hunt");

// Обновление прогресса при убийстве волка
questManager.IncrementObjective("wolf_hunt", "kill_wolves");
```

### Сохранение игры
```csharp
using PlyGame.SaveSystem;

var saveManager = new SaveGameManager();

// Создание данных сохранения
var saveData = new GameSaveData
{
    Description = "Автосохранение",
    TotalPlayTime = Time.time,
    SceneName = SceneManager.GetActiveScene().name,
    PlayerData = new PlayerSaveData
    {
        PlayerName = "Hero",
        Level = 5,
        Health = 80,
        Position = player.transform.position
    }
};

// Сохранение
await saveManager.SaveGameAsync("save_slot_1", saveData);

// Загрузка
var loadedData = await saveManager.LoadGameAsync("save_slot_1");
```

## 🏗️ Архитектура

```
com.plygame/
├── Runtime/
│   ├── Core/           # Ядро: узлы, графы, события, сериализация
│   ├── Variables/      # Система переменных
│   ├── Nodes/          # Базовые узлы графа
│   ├── SaveSystem/     # Сохранения
│   ├── Dialogue/       # Диалоги
│   ├── Quests/         # Квесты
│   ├── Inventory/      # Инвентарь
│   ├── Stats/          # Характеристики
│   ├── Skills/         # Навыки
│   └── Achievements/   # Достижения
├── Editor/             # Визуальный редактор
├── Documentation/      # Документация
└── package.json        # Манифест пакета
```

## 🔧 Требования

- **Unity**: 2022.3 LTS или новее
- **.NET**: .NET Standard 2.1 / C# 10+
- **Платформы**: Windows, macOS, Linux, iOS, Android, WebGL

## 📝 Лицензия

MIT License — свободное использование в коммерческих и некоммерческих проектах.

## 🔗 Полезные ссылки

- [Документация](Documentation/)
- [Примеры использования](Samples~/)
- [GitHub Repository](https://github.com/plygame/plygame-modern)
- [Discord сообщество](https://discord.gg/plygame)

---

**PlyGame Modern** создан сообществом для сообщества. Вклад в развитие проекта приветствуется!
