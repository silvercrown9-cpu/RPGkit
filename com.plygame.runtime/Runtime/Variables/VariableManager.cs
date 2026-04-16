using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlyGame.Runtime.Variables
{
    /// <summary>
    /// Менеджер переменных для хранения состояния графа
    /// </summary>
    public class VariableManager
    {
        private readonly Dictionary<string, GraphVariable> variables = new();
        private readonly List<GraphVariable> exposedVariables = new();
        
        public event Action<string, object> OnVariableChanged;
        
        /// <summary>
        /// Добавить переменную в менеджер
        /// </summary>
        public void AddVariable(GraphVariable variable)
        {
            if (variable == null)
            {
                Debug.LogError("Cannot add null variable");
                return;
            }
            
            if (variables.ContainsKey(variable.Name))
            {
                Debug.LogWarning($"Variable '{variable.Name}' already exists. Overwriting.");
                variables[variable.Name] = variable;
            }
            else
            {
                variables.Add(variable.Name, variable);
            }
            
            if (variable.IsExposed && !exposedVariables.Contains(variable))
            {
                exposedVariables.Add(variable);
            }
        }
        
        /// <summary>
        /// Получить переменную по имени
        /// </summary>
        public T GetVariable<T>(string name) where T : GraphVariable
        {
            if (variables.TryGetValue(name, out var variable))
            {
                if (variable is T typedVar)
                    return typedVar;
                else
                {
                    Debug.LogError($"Variable '{name}' is not of type {typeof(T).Name}");
                    return null;
                }
            }
            
            Debug.LogError($"Variable '{name}' not found");
            return null;
        }
        
        /// <summary>
        /// Получить значение переменной
        /// </summary>
        public T GetValue<T>(string name)
        {
            var variable = GetVariable<GraphVariable>(name);
            if (variable != null)
            {
                try
                {
                    return (T)variable.GetValue();
                }
                catch (InvalidCastException)
                {
                    Debug.LogError($"Cannot cast variable '{name}' to type {typeof(T).Name}");
                    return default;
                }
            }
            return default;
        }
        
        /// <summary>
        /// Установить значение переменной
        /// </summary>
        public void SetValue(string name, object value)
        {
            if (variables.TryGetValue(name, out var variable))
            {
                variable.SetValue(value);
                OnVariableChanged?.Invoke(name, value);
            }
            else
            {
                Debug.LogError($"Cannot set value for non-existent variable '{name}'");
            }
        }
        
        /// <summary>
        /// Проверить существование переменной
        /// </summary>
        public bool HasVariable(string name)
        {
            return variables.ContainsKey(name);
        }
        
        /// <summary>
        /// Удалить переменную
        /// </summary>
        public void RemoveVariable(string name)
        {
            if (variables.Remove(name, out var variable))
            {
                exposedVariables.Remove(variable);
            }
        }
        
        /// <summary>
        /// Очистить все переменные
        /// </summary>
        public void Clear()
        {
            variables.Clear();
            exposedVariables.Clear();
        }
        
        /// <summary>
        /// Получить все открытые (exposed) переменные
        /// </summary>
        public IReadOnlyList<GraphVariable> GetExposedVariables() => exposedVariables.AsReadOnly();
        
        /// <summary>
        /// Клонировать менеджер переменных
        /// </summary>
        public VariableManager Clone()
        {
            var clone = new VariableManager();
            foreach (var variable in variables.Values)
            {
                clone.AddVariable(variable.Clone());
            }
            return clone;
        }
        
        /// <summary>
        /// Экспорт переменных в словарь для сериализации
        /// </summary>
        public Dictionary<string, object> Export()
        {
            var dict = new Dictionary<string, object>();
            foreach (var kvp in variables)
            {
                dict[kvp.Key] = kvp.Value.GetValue();
            }
            return dict;
        }
        
        /// <summary>
        /// Импорт переменных из словаря
        /// </summary>
        public void Import(Dictionary<string, object> data)
        {
            if (data == null) return;
            
            foreach (var kvp in data)
            {
                if (variables.TryGetValue(kvp.Key, out var variable))
                {
                    variable.SetValue(kvp.Value);
                }
            }
        }
    }
}
