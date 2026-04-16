using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlyGame.Runtime.Variables
{
    /// <summary>
    /// Менеджер переменных для хранения и управления переменными графа
    /// </summary>
    public class VariableManager : MonoBehaviour
    {
        public static VariableManager Instance { get; private set; }
        
        [SerializeField]
        private List<GraphVariable> _variables = new List<GraphVariable>();
        
        private Dictionary<string, GraphVariable> _variableMap = new Dictionary<string, GraphVariable>();
        private Dictionary<Type, List<GraphVariable>> _variablesByType = new Dictionary<Type, List<GraphVariable>>();
        
        public event Action<string, object> OnVariableChanged;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            _variableMap.Clear();
            _variablesByType.Clear();
            
            foreach (var variable in _variables)
            {
                if (variable != null)
                {
                    RegisterVariable(variable);
                }
            }
        }

        public void RegisterVariable(GraphVariable variable)
        {
            if (variable == null || string.IsNullOrEmpty(variable.Id))
            {
                Debug.LogError("[VariableManager] Cannot register null variable or variable without ID");
                return;
            }
            
            if (_variableMap.ContainsKey(variable.Id))
            {
                Debug.LogWarning($"[VariableManager] Variable with ID {variable.Id} already exists. Updating reference.");
                _variableMap[variable.Id] = variable;
            }
            else
            {
                _variableMap[variable.Id] = variable;
                
                var valueType = variable.GetValueType();
                if (!_variablesByType.ContainsKey(valueType))
                {
                    _variablesByType[valueType] = new List<GraphVariable>();
                }
                _variablesByType[valueType].Add(variable);
                
                variable.OnValueChanged += OnVariableValueChanged;
            }
        }

        public void UnregisterVariable(string variableId)
        {
            if (_variableMap.TryGetValue(variableId, out var variable))
            {
                variable.OnValueChanged -= OnVariableValueChanged;
                
                var valueType = variable.GetValueType();
                if (_variablesByType.ContainsKey(valueType))
                {
                    _variablesByType[valueType].Remove(variable);
                }
                
                _variableMap.Remove(variableId);
            }
        }

        private void OnVariableValueChanged(GraphVariable variable)
        {
            OnVariableChanged?.Invoke(variable.Id, variable.GetValue());
        }

        public T GetVariable<T>(string variableId) where T : GraphVariable
        {
            if (_variableMap.TryGetValue(variableId, out var variable))
            {
                return variable as T;
            }
            
            Debug.LogError($"[VariableManager] Variable {variableId} not found");
            return null;
        }

        public GraphVariable GetVariable(string variableId)
        {
            if (_variableMap.TryGetValue(variableId, out var variable))
            {
                return variable;
            }
            
            Debug.LogError($"[VariableManager] Variable {variableId} not found");
            return null;
        }

        public T GetVariableValue<T>(string variableId)
        {
            var variable = GetVariable(variableId);
            if (variable != null)
            {
                try
                {
                    return (T)variable.GetValue();
                }
                catch (InvalidCastException)
                {
                    Debug.LogError($"[VariableManager] Cannot cast variable {variableId} to type {typeof(T)}");
                }
            }
            return default;
        }

        public void SetVariableValue<T>(string variableId, T value)
        {
            var variable = GetVariable(variableId);
            if (variable != null)
            {
                variable.SetValue(value);
            }
        }

        public bool HasVariable(string variableId)
        {
            return _variableMap.ContainsKey(variableId);
        }

        public List<GraphVariable> GetAllVariables()
        {
            return new List<GraphVariable>(_variableMap.Values);
        }

        public List<GraphVariable> GetVariablesByType<T>()
        {
            var type = typeof(T);
            if (_variablesByType.TryGetValue(type, out var variables))
            {
                return new List<GraphVariable>(variables);
            }
            return new List<GraphVariable>();
        }

        public void ClearAllVariables()
        {
            foreach (var variable in _variableMap.Values)
            {
                variable.OnValueChanged -= OnVariableValueChanged;
            }
            
            _variableMap.Clear();
            _variablesByType.Clear();
        }

        public VariableSaveData ExportVariables()
        {
            var data = new VariableSaveData();
            data.VariableValues = new Dictionary<string, object>();
            
            foreach (var variable in _variableMap.Values)
            {
                data.VariableValues[variable.Id] = variable.GetValue();
            }
            
            return data;
        }

        public void ImportVariables(VariableSaveData data)
        {
            if (data == null || data.VariableValues == null)
            {
                return;
            }
            
            foreach (var kvp in data.VariableValues)
            {
                if (_variableMap.TryGetValue(kvp.Key, out var variable))
                {
                    variable.SetValue(kvp.Value);
                }
            }
        }

        [Serializable]
        public class VariableSaveData
        {
            public Dictionary<string, object> VariableValues;
        }
    }
}
