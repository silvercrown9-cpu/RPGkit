using System;
using System.Collections.Generic;
using System.Linq;
using PlyGame.Runtime.Core.Events;
using UnityEngine;

namespace PlyGame.Runtime.Variables
{
    /// <summary>
    /// Менеджер переменных для хранения и управления переменными графа
    /// </summary>
    public class VariableManager
    {
        private readonly Dictionary<string, GraphVariable> _variables = new();
        
        /// <summary>
        /// Добавить переменную
        /// </summary>
        public void AddVariable(GraphVariable variable)
        {
            if (variable == null)
            {
                Debug.LogError("Попытка добавить null переменную");
                return;
            }
            
            if (_variables.ContainsKey(variable.Id))
            {
                Debug.LogWarning($"Переменная с ID {variable.Id} уже существует");
                return;
            }
            
            _variables[variable.Id] = variable;
            variable.OnValueChanged += () => OnVariableChanged(variable);
        }
        
        /// <summary>
        /// Удалить переменную
        /// </summary>
        public void RemoveVariable(string variableId)
        {
            if (_variables.TryGetValue(variableId, out var variable))
            {
                variable.OnValueChanged -= () => OnVariableChanged(variable);
                _variables.Remove(variableId);
            }
        }
        
        /// <summary>
        /// Получить переменную по ID
        /// </summary>
        public T GetVariable<T>(string variableId) where T : GraphVariable
        {
            if (_variables.TryGetValue(variableId, out var variable))
                return variable as T;
            return null;
        }
        
        /// <summary>
        /// Получить переменную по имени
        /// </summary>
        public T GetVariableByName<T>(string name) where T : GraphVariable
        {
            return _variables.Values
                .OfType<T>()
                .FirstOrDefault(v => v.Name == name);
        }
        
        /// <summary>
        /// Получить значение переменной
        /// </summary>
        public T GetValue<T>(string variableId)
        {
            if (GetVariable<GraphVariable>(variableId) is var variable && variable != null)
            {
                var value = variable.GetValue();
                return value is T typedValue ? typedValue : default;
            }
            return default;
        }
        
        /// <summary>
        /// Установить значение переменной
        /// </summary>
        public void SetValue<T>(string variableId, T value)
        {
            if (GetVariable<GraphVariable>(variableId) is var variable && variable != null)
            {
                variable.SetValue(value);
            }
        }
        
        /// <summary>
        /// Проверить существование переменной
        /// </summary>
        public bool HasVariable(string variableId)
        {
            return _variables.ContainsKey(variableId);
        }
        
        /// <summary>
        /// Получить все переменные
        /// </summary>
        public IReadOnlyCollection<GraphVariable> GetAllVariables()
        {
            return _variables.Values.ToList().AsReadOnly();
        }
        
        /// <summary>
        /// Очистить все переменные
        /// </summary>
        public void Clear()
        {
            foreach (var variable in _variables.Values)
            {
                variable.OnValueChanged -= () => OnVariableChanged(variable);
            }
            _variables.Clear();
        }
        
        /// <summary>
        /// Экспорт переменных в словарь для сохранения
        /// </summary>
        public Dictionary<string, object> Export()
        {
            var result = new Dictionary<string, object>();
            
            foreach (var variable in _variables.Values)
            {
                result[variable.Id] = new VariableData
                {
                    Id = variable.Id,
                    Name = variable.Name,
                    TypeName = variable.GetType().FullName,
                    Value = variable.GetValue()
                };
            }
            
            return result;
        }
        
        /// <summary>
        /// Импорт переменных из словаря
        /// </summary>
        public void Import(Dictionary<string, object> data)
        {
            Clear();
            
            if (data == null) return;
            
            foreach (var kvp in data)
            {
                if (kvp.Value is VariableData varData)
                {
                    var variable = CreateVariableFromType(varData.TypeName, varData.Name);
                    if (variable != null)
                    {
                        variable.SetValue(varData.Value);
                        AddVariable(variable);
                    }
                }
            }
        }
        
        /// <summary>
        /// Клонирование менеджера переменных
        /// </summary>
        public VariableManager Clone()
        {
            var clone = new VariableManager();
            
            foreach (var variable in _variables.Values)
            {
                var clonedVar = variable.Clone();
                clone.AddVariable(clonedVar);
            }
            
            return clone;
        }
        
        private void OnVariableChanged(GraphVariable variable)
        {
            // Публикация события об изменении переменной
            EventBus.Publish(new VariableChangedEvent<object>
            {
                VariableName = variable.Name,
                NewValue = variable.GetValue()
            });
        }
        
        private static GraphVariable CreateVariableFromType(string typeName, string name)
        {
            var type = Type.GetType(typeName);
            if (type == null || !typeof(GraphVariable).IsAssignableFrom(type))
                return null;
            
            var variable = Activator.CreateInstance(type) as GraphVariable;
            
            // Установка имени через рефлексию если есть поле
            var nameField = variable?.GetType().GetField("variableName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nameField?.SetValue(variable, name);
            
            return variable;
        }
    }
    
    /// <summary>
    /// Данные переменной для сериализации
    /// </summary>
    [Serializable]
    public class VariableData
    {
        public string Id;
        public string Name;
        public string TypeName;
        public object Value;
    }
}
