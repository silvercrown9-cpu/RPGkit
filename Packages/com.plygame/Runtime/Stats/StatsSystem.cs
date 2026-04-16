using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlyGame.Stats
{
    /// <summary>
    /// Менеджер характеристик персонажа
    /// </summary>
    public class StatsManager
    {
        private readonly Dictionary<string, StatData> _stats = new();
        private readonly Dictionary<string, List<StatModifier>> _modifiers = new();
        
        public event Action<string, float> OnStatChanged;
        public event Action<string> OnStatAdded;
        public event Action<string> OnStatRemoved;
        
        /// <summary>
        /// Добавить характеристику
        /// </summary>
        public void AddStat(string statId, float baseValue, StatType type = StatType.Base)
        {
            if (_stats.ContainsKey(statId))
            {
                Debug.LogWarning($"Характеристика {statId} уже существует");
                return;
            }
            
            _stats[statId] = new StatData
            {
                StatId = statId,
                BaseValue = baseValue,
                Type = type
            };
            
            _modifiers[statId] = new List<StatModifier>();
            OnStatAdded?.Invoke(statId);
        }
        
        /// <summary>
        /// Получить базовое значение характеристики
        /// </summary>
        public float GetBaseValue(string statId)
        {
            return _stats.TryGetValue(statId, out var stat) ? stat.BaseValue : 0;
        }
        
        /// <summary>
        /// Получить итоговое значение характеристики с учетом модификаторов
        /// </summary>
        public float GetValue(string statId)
        {
            if (!_stats.TryGetValue(statId, out var stat))
                return 0;
            
            var value = stat.BaseValue;
            
            if (_modifiers.TryGetValue(statId, out var mods))
            {
                // Сначала применяем плоские модификаторы
                var flatBonus = 0f;
                foreach (var mod in mods.FindAll(m => mod.Type == ModifierType.Flat))
                {
                    flatBonus += mod.Value;
                }
                value += flatBonus;
                
                // Затем процентные модификаторы
                var percentMultiplier = 1f;
                foreach (var mod in mods.FindAll(m => m.Type == ModifierType.Percent))
                {
                    percentMultiplier += mod.Value / 100f;
                }
                value *= percentMultiplier;
            }
            
            // Применяем ограничения
            if (stat.MinValue.HasValue)
                value = Mathf.Max(value, stat.MinValue.Value);
            if (stat.MaxValue.HasValue)
                value = Mathf.Min(value, stat.MaxValue.Value);
            
            return value;
        }
        
        /// <summary>
        /// Установить базовое значение характеристики
        /// </summary>
        public void SetBaseValue(string statId, float value)
        {
            if (_stats.TryGetValue(statId, out var stat))
            {
                var oldValue = GetValue(statId);
                stat.BaseValue = value;
                var newValue = GetValue(statId);
                
                if (!Mathf.Approximately(oldValue, newValue))
                    OnStatChanged?.Invoke(statId, newValue);
            }
        }
        
        /// <summary>
        /// Добавить модификатор к характеристике
        /// </summary>
        public void AddModifier(string statId, StatModifier modifier)
        {
            if (!_stats.ContainsKey(statId))
            {
                Debug.LogError($"Характеристика {statId} не найдена");
                return;
            }
            
            if (!_modifiers.ContainsKey(statId))
                _modifiers[statId] = new List<StatModifier>();
            
            modifier.Id = Guid.NewGuid().ToString();
            _modifiers[statId].Add(modifier);
            
            var newValue = GetValue(statId);
            OnStatChanged?.Invoke(statId, newValue);
        }
        
        /// <summary>
        /// Удалить модификатор по ID
        /// </summary>
        public bool RemoveModifier(string statId, string modifierId)
        {
            if (!_modifiers.TryGetValue(statId, out var mods))
                return false;
            
            var removed = mods.RemoveAll(m => m.Id == modifierId);
            
            if (removed > 0)
            {
                var newValue = GetValue(statId);
                OnStatChanged?.Invoke(statId, newValue);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Удалить все модификаторы от источника
        /// </summary>
        public bool RemoveModifiersFromSource(string statId, string sourceId)
        {
            if (!_modifiers.TryGetValue(statId, out var mods))
                return false;
            
            var removed = mods.RemoveAll(m => m.SourceId == sourceId);
            
            if (removed > 0)
            {
                var newValue = GetValue(statId);
                OnStatChanged?.Invoke(statId, newValue);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Получить текущее значение здоровья
        /// </summary>
        public int Health => Mathf.FloorToInt(GetValue("Health"));
        
        /// <summary>
        /// Получить максимальное здоровье
        /// </summary>
        public int MaxHealth => Mathf.FloorToInt(GetValue("MaxHealth"));
        
        /// <summary>
        /// Получить текущее значение маны
        /// </summary>
        public int Mana => Mathf.FloorToInt(GetValue("Mana"));
        
        /// <summary>
        /// Получить максимальную ману
        /// </summary>
        public int MaxMana => Mathf.FloorToInt(GetValue("MaxMana"));
        
        /// <summary>
        /// Получить уровень
        /// </summary>
        public int Level => Mathf.FloorToInt(GetValue("Level"));
        
        /// <summary>
        /// Получить опыт
        /// </summary>
        public float Experience => GetValue("Experience");
        
        /// <summary>
        /// Получить силу
        /// </summary>
        public float Strength => GetValue("Strength");
        
        /// <summary>
        /// Получить ловкость
        /// </summary>
        public float Dexterity => GetValue("Dexterity");
        
        /// <summary>
        /// Получить интеллект
        /// </summary>
        public float Intelligence => GetValue("Intelligence");
        
        /// <summary>
        /// Нанести урон здоровью
        /// </summary>
        public bool DamageHealth(int amount)
        {
            if (!_stats.ContainsKey("Health")) return false;
            
            var currentHealth = Health;
            var newHealth = Mathf.Max(0, currentHealth - amount);
            SetBaseValue("Health", newHealth);
            
            return newHealth <= 0;
        }
        
        /// <summary>
        /// Восстановить здоровье
        /// </summary>
        public int HealHealth(int amount)
        {
            if (!_stats.ContainsKey("Health") || !_stats.ContainsKey("MaxHealth"))
                return 0;
            
            var currentHealth = Health;
            var maxHealth = MaxHealth;
            var newHealth = Mathf.Min(maxHealth, currentHealth + amount);
            var healedAmount = newHealth - currentHealth;
            
            SetBaseValue("Health", newHealth);
            return healedAmount;
        }
        
        /// <summary>
        /// Добавить опыт
        /// </summary>
        public bool AddExperience(float amount)
        {
            if (!_stats.ContainsKey("Experience") || !_stats.ContainsKey("Level"))
                return false;
            
            var currentExp = Experience;
            var expToNextLevel = GetValue("ExpToNextLevel");
            
            SetBaseValue("Experience", currentExp + amount);
            
            // Проверка повышения уровня
            while (Experience >= expToNextLevel)
            {
                SetBaseValue("Experience", Experience - expToNextLevel);
                SetBaseValue("Level", Level + 1);
                expToNextLevel = GetValue("ExpToNextLevel");
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Данные характеристики
    /// </summary>
    [Serializable]
    public class StatData
    {
        public string StatId;
        public float BaseValue;
        public StatType Type;
        public float? MinValue;
        public float? MaxValue;
    }
    
    /// <summary>
    /// Тип характеристики
    /// </summary>
    public enum StatType
    {
        Base,       // Базовая характеристика
        Derived,    // Производная характеристика
        Resource,   // Ресурс (здоровье, мана)
        Combat      // Боевая характеристика
    }
    
    /// <summary>
    /// Модификатор характеристики
    /// </summary>
    [Serializable]
    public class StatModifier
    {
        public string Id;
        public string SourceId;
        public ModifierType Type;
        public float Value;
        public int Priority;
        
        public StatModifier() { }
        
        public StatModifier(ModifierType type, float value, string sourceId = null, int priority = 0)
        {
            Type = type;
            Value = value;
            SourceId = sourceId;
            Priority = priority;
        }
    }
    
    /// <summary>
    /// Тип модификатора
    /// </summary>
    public enum ModifierType
    {
        Flat,       // Плоское значение (+5)
        Percent     // Процентное значение (+10%)
    }
}
