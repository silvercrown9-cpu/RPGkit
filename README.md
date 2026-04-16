# PlyGame Modern - Visual Scripting Framework для Unity

Современный аналог фреймворка plyGame для создания RPG-механик в Unity. Полностью переработан с использованием актуальных API Unity 2022 LTS+ и современных стандартов разработки.

## 📦 Структура проекта

```
/workspace
├── Packages/
│   ├── com.plygame.runtime/     # Runtime ядро (Graph, Events, Serialization)
│   ├── com.plygame.editor/      # Editor инструменты (GraphView, UI Toolkit)
│   ├── com.plygame.tests/       # Юнит-тесты
│   └── com.plygame.samples/     # Примеры и документация
└── README.md
```

## 🚀 Быстрый старт

1. Откройте проект в Unity 2022.3 LTS или новее
2. Установите пакеты через Package Manager
3. Откройте редактор: `Tools > PlyGame > Graph Editor`

## 📚 Документация

- [README](Packages/com.plygame.samples/README.md) - полное руководство пользователя
- [ARCHITECTURE](Packages/com.plygame.samples/ARCHITECTURE.md) - архитектурное руководство

## ✨ Ключевые особенности

- **Графовая система узлов** - визуальное программирование диалогов, квестов и событий
- **Асинхронное выполнение** - async/await паттерн для неблокирующей работы
- **Современный редактор** - Unity GraphView + UI Toolkit
- **Событийная архитектура** - декуплированная коммуникация через EventBus
- **UPM-пакеты** - модульная структура для удобного управления

## 🛠 Требования

- Unity 2022.3 LTS или новее
- .NET Standard 2.1 / C# 10+
- UI Toolkit
- GraphView (Unity Experimental)

## 📄 Лицензия

MIT License