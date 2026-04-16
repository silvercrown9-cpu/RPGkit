using System;
using UnityEngine;
using UnityEngine.Events;

namespace PlyGame.Runtime.Variables
{
    /// <summary>
    /// Базовый класс для всех переменных графа
    /// </summary>
    [Serializable]
    public abstract class GraphVariable
    {
        public string Id;
        public string VariableName;
        public string Description;
        
        [SerializeField]
        protected bool _isReadOnly = false;
        
        public event UnityAction<GraphVariable> OnValueChanged;
        
        protected virtual void RaiseValueChanged()
        {
            OnValueChanged?.Invoke(this);
        }
        
        public abstract object GetValue();
        public abstract void SetValue(object value);
        public abstract Type GetValueType();
        
        public void SetReadOnly(bool readOnly)
        {
            _isReadOnly = readOnly;
        }
        
        public bool IsReadOnly() => _isReadOnly;
    }

    /// <summary>
    /// Переменная целочисленного типа
    /// </summary>
    [Serializable]
    public class IntVariable : GraphVariable
    {
        [SerializeField]
        private int _value;
        
        public int Value
        {
            get => _value;
            set
            {
                if (_isReadOnly)
                {
                    Debug.LogWarning($"[IntVariable] Variable {VariableName} is read-only");
                    return;
                }
                
                _value = value;
                RaiseValueChanged();
            }
        }
        
        public override object GetValue() => _value;
        
        public override void SetValue(object value)
        {
            if (value is int intValue)
            {
                Value = intValue;
            }
            else
            {
                Debug.LogError($"[IntVariable] Cannot assign {value.GetType()} to IntVariable");
            }
        }
        
        public override Type GetValueType() => typeof(int);
        
        public static implicit operator int(IntVariable variable) => variable.Value;
    }

    /// <summary>
    /// Переменная вещественного типа
    /// </summary>
    [Serializable]
    public class FloatVariable : GraphVariable
    {
        [SerializeField]
        private float _value;
        
        public float Value
        {
            get => _value;
            set
            {
                if (_isReadOnly)
                {
                    Debug.LogWarning($"[FloatVariable] Variable {VariableName} is read-only");
                    return;
                }
                
                _value = value;
                RaiseValueChanged();
            }
        }
        
        public override object GetValue() => _value;
        
        public override void SetValue(object value)
        {
            if (value is float floatValue)
            {
                Value = floatValue;
            }
            else
            {
                Debug.LogError($"[FloatVariable] Cannot assign {value.GetType()} to FloatVariable");
            }
        }
        
        public override Type GetValueType() => typeof(float);
        
        public static implicit operator float(FloatVariable variable) => variable.Value;
    }

    /// <summary>
    /// Строковая переменная
    /// </summary>
    [Serializable]
    public class StringVariable : GraphVariable
    {
        [SerializeField]
        private string _value;
        
        public string Value
        {
            get => _value ?? string.Empty;
            set
            {
                if (_isReadOnly)
                {
                    Debug.LogWarning($"[StringVariable] Variable {VariableName} is read-only");
                    return;
                }
                
                _value = value;
                RaiseValueChanged();
            }
        }
        
        public override object GetValue() => _value;
        
        public override void SetValue(object value)
        {
            Value = value?.ToString() ?? string.Empty;
        }
        
        public override Type GetValueType() => typeof(string);
        
        public static implicit operator string(StringVariable variable) => variable.Value;
    }

    /// <summary>
    /// Булева переменная
    /// </summary>
    [Serializable]
    public class BoolVariable : GraphVariable
    {
        [SerializeField]
        private bool _value;
        
        public bool Value
        {
            get => _value;
            set
            {
                if (_isReadOnly)
                {
                    Debug.LogWarning($"[BoolVariable] Variable {VariableName} is read-only");
                    return;
                }
                
                _value = value;
                RaiseValueChanged();
            }
        }
        
        public override object GetValue() => _value;
        
        public override void SetValue(object value)
        {
            if (value is bool boolValue)
            {
                Value = boolValue;
            }
            else
            {
                Debug.LogError($"[BoolVariable] Cannot assign {value.GetType()} to BoolVariable");
            }
        }
        
        public override Type GetValueType() => typeof(bool);
        
        public static implicit operator bool(BoolVariable variable) => variable.Value;
    }

    /// <summary>
    /// Переменная для хранения GameObject
    /// </summary>
    [Serializable]
    public class GameObjectVariable : GraphVariable
    {
        [SerializeField]
        private GameObject _value;
        
        public GameObject Value
        {
            get => _value;
            set
            {
                if (_isReadOnly)
                {
                    Debug.LogWarning($"[GameObjectVariable] Variable {VariableName} is read-only");
                    return;
                }
                
                _value = value;
                RaiseValueChanged();
            }
        }
        
        public override object GetValue() => _value;
        
        public override void SetValue(object value)
        {
            if (value is GameObject gameObject)
            {
                Value = gameObject;
            }
            else
            {
                Debug.LogError($"[GameObjectVariable] Cannot assign {value.GetType()} to GameObjectVariable");
            }
        }
        
        public override Type GetValueType() => typeof(GameObject);
        
        public static implicit operator GameObject(GameObjectVariable variable) => variable.Value;
    }
}
