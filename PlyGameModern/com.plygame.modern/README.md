# PlyGame Modern

Современный визуальный фреймворк для создания RPG-механик в Unity 2022 LTS+. Полная замена устаревшему плагину plyGame с поддержкой современных стандартов разработки.

## 📦 Возможности

### Ядро системы
- **Графовая система узлов** - визуальное программирование логики через направленные ациклические графы
- **Асинхронное выполнение** - async/await паттерны для неблокирующей работы
- **GraphView редактор** - современный визуальный редактор на базе UI Toolkit
- **Сериализация данных** - System.Text.Json с поддержкой AOT компиляции

### RPG Системы
- **Переменные** - 5 типов (int, float, string, bool, GameObject) с менеджером и событиями
- **Диалоги** - ветвящиеся диалоги с выборами и условиями
- **Квесты** - система заданий с объективами, статусами и наградами
- **Инвентарь** - предметы, стакинг, редкость, использование
- **Характеристики** - сила, ловкость, интеллект, здоровье, мана, опыт, уровень
- **Навыки** - активные и пассивные способности с кулдаунами
- **Боевая система** - расчет урона, баффы, дебаффы
- **Торговля** - покупка/продажа предметов в магазинах
- **Фракции** - репутация и отношения между группировками
- **Крафт** - создание предметов из ресурсов
- **Отряд** - управление группой персонажей
- **Добыча** - таблицы лута и генерация наград
- **Достижения** - отслеживание прогресса и награды
- **Эффекты** - баффы и дебаффы с длительностью

### Утилиты
- **Сохранения** - асинхронная система с поддержкой слотов
- **События** - типобезопасная шина событий (EventBus)
- **RNG** - генератор случайных чисел с сидом
- **Таймеры** - отложенное выполнение и периодические события
- **Логирование** - расширенная система отладки

## 🚀 Установка

### Через Package Manager
1. Откройте `Window > Package Manager`
2. Нажмите `+` и выберите `Add package from git URL`
3. Введите URL репозитория: `https://github.com/YOUR_USERNAME/PlyGameModern.git`

### Через manifest.json
Добавьте в `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.plygame.modern": "https://github.com/YOUR_USERNAME/PlyGameModern.git"
  }
}
```

### Локальная установка
1. Скопируйте папку `com.plygame.modern` в `Packages/`
2. Unity автоматически импортирует пакет

## 📖 Быстрый старт

### Создание первого графа

1. **Создайте Graph Asset**: `Assets > Create > PlyGame > Graph Asset`
2. **Откройте редактор**: `Window > PlyGame > Graph Editor`
3. **Добавьте узлы**: ПКМ в редакторе > поиск узлов
4. **Соедините узлы**: перетаскивание от выхода к входу
5. **Запустите граф**: через `GraphRunner.Instance.RunGraphAsync(graph)`

### Пример кода

```csharp
using PlyGame.Runtime.Core;
using PlyGame.Runtime.Variables;

public class Example : MonoBehaviour
{
    public GraphAsset dialogueGraph;
    
    async void Start()
    {
        // Инициализация переменных
        VariableManager.Instance.SetVariableValue("PlayerName", "Hero");
        VariableManager.Instance.SetVariableValue("Gold", 100);
        
        // Запуск графа
        await GraphRunner.Instance.RunGraphAsync(dialogueGraph, gameObject);
    }
}
```

### Создание переменной

```csharp
// В редакторе Unity создайте ScriptableObject:
// Assets > Create > PlyGame > Variables > Int Variable

// Или через код:
var goldVar = ScriptableObject.CreateInstance<IntVariable>();
goldVar.Id = "gold";
goldVar.VariableName = "Gold";
goldVar.Value = 100;
VariableManager.Instance.RegisterVariable(goldVar);
```

## 🏗️ Архитектура

```
com.plygame.modern/
├── Runtime/
│   ├── Core/           # Ядро: GraphNode, GraphAsset, GraphRunner
│   ├── Variables/      # Переменные и менеджер
│   ├── Nodes/          # Узлы графа
│   ├── Dialogue/       # Диалоговая система
│   ├── Quests/         # Система квестов
│   ├── Inventory/      # Инвентарь и предметы
│   ├── Stats/          # Характеристики
│   ├── Combat/         # Боевая система
│   ├── Skills/         # Навыки
│   ├── Trade/          # Торговля
│   ├── Factions/       # Фракции
│   ├── Crafting/       # Крафт
│   ├── Party/          # Отряд
│   ├── Loot/           # Добыча
│   ├── Achievements/   # Достижения
│   ├── Effects/        # Эффекты
│   ├── SaveSystem/     # Сохранения
│   ├── Utilities/      # Утилиты (RNG, Timers, Logger)
│   └── Events/         # Шина событий
├── Editor/
│   ├── Windows/        # Окна редактора
│   ├── GraphView/      # Визуальный редактор графов
│   ├── Inspectors/     # Кастомные инспекторы
│   └── Search/         # Поиск узлов
├── Samples~/           # Примеры использования
└── Documentation~/     # Документация
```

## 🔧 Требования

- Unity 2022.3 LTS или новее
- .NET Standard 2.1 / .NET Framework 4.x
- Поддержка C# 10+

## 📝 Лицензия

MIT License - свободно используйте в коммерческих и некоммерческих проектах.

## 🤝 Вклад

1. Fork репозиторий
2. Создайте feature branch (`git checkout -b feature/amazing-feature`)
3. Commit изменения (`git commit -m 'Add amazing feature'`)
4. Push в branch (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

## 📞 Поддержка

- GitHub Issues: https://github.com/YOUR_USERNAME/PlyGameModern/issues
- Документация: https://github.com/YOUR_USERNAME/PlyGameModern/tree/main/Documentation~

## 🙏 Благодарности

Оригинальный plyGame заложил основу для этого проекта. Современная версия сохраняет совместимость концепций, но полностью переписана с использованием современных стандартов Unity.
