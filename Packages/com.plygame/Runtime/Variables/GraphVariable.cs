using System;
using UnityEngine;

namespace PlyGame.Runtime.Variables
{
    /// <summary>
    /// Базовый класс для переменных графа
    /// </summary>
    [Serializable]
    public abstract class GraphVariable
    {
        [SerializeField] protected string variableName;
        [SerializeField] protected string variableId;
        
        public string Name => variableName;
        public string Id => variableId;
        
        protected GraphVariable()
        {
            variableId = Guid.NewGuid().ToString();
        }
        
        public abstract Type ValueType { get; }
        public abstract object GetValue();
        public abstract void SetValue(object value);
        public abstract GraphVariable Clone();
        
        public event Action OnValueChanged;
        
        protected virtual void RaiseValueChanged()
        {
            OnValueChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// Целочисленная переменная
    /// </summary>
    [Serializable]
    public class IntVariable : GraphVariable
    {
        [SerializeField] private int value;
        
        public int Value
        {
            get => value;
            set
            {
                var old = this.value;
                this.value = value;
                if (old != value)
                    RaiseValueChanged();
            }
        }
        
        public override Type ValueType => typeof(int);
        
        public IntVariable() : base() { }
        
        public IntVariable(string name, int initialValue = 0)
        {
            variableName = name;
            value = initialValue;
        }
        
        public override object GetValue() => Value;
        
        public override void SetValue(object val)
        {
            if (val is int intValue)
                Value = intValue;
        }
        
        public override GraphVariable Clone()
        {
            return new IntVariable(variableName, value);
        }
    }
    
    /// <summary>
    /// Переменная с плавающей точкой
    /// </summary>
    [Serializable]
    public class FloatVariable : GraphVariable
    {
        [SerializeField] private float value;
        
        public float Value
        {
            get => value;
            set
            {
                var old = this.value;
                this.value = value;
                if (Math.Abs(old - value) > 0.0001f)
                    RaiseValueChanged();
            }
        }
        
        public override Type ValueType => typeof(float);
        
        public FloatVariable() : base() { }
        
        public FloatVariable(string name, float initialValue = 0f)
        {
            variableName = name;
            value = initialValue;
        }
        
        public override object GetValue() => Value;
        
        public override void SetValue(object val)
        {
            if (val is float floatValue)
                Value = floatValue;
        }
        
        public override GraphVariable Clone()
        {
            return new FloatVariable(variableName, value);
        }
    }
    
    /// <summary>
    /// Строковая переменная
    /// </summary>
    [Serializable]
    public class StringVariable : GraphVariable
    {
        [SerializeField] private string value;
        
        public string Value
        {
            get => value;
            set
            {
                var old = this.value;
                this.value = value;
                if (old != value)
                    RaiseValueChanged();
            }
        }
        
        public override Type ValueType => typeof(string);
        
        public StringVariable() : base() { }
        
        public StringVariable(string name, string initialValue = "")
        {
            variableName = name;
            value = initialValue;
        }
        
        public override object GetValue() => Value;
        
        public override void SetValue(object val)
        {
            Value = val as string;
        }
        
        public override GraphVariable Clone()
        {
            return new StringVariable(variableName, value);
        }
    }
    
    /// <summary>
    /// Булева переменная
    /// </summary>
    [Serializable]
    public class BoolVariable : GraphVariable
    {
        [SerializeField] private bool value;
        
        public bool Value
        {
            get => value;
            set
            {
                var old = this.value;
                this.value = value;
                if (old != value)
                    RaiseValueChanged();
            }
        }
        
        public override Type ValueType => typeof(bool);
        
        public BoolVariable() : base() { }
        
        public BoolVariable(string name, bool initialValue = false)
        {
            variableName = name;
            value = initialValue;
        }
        
        public override object GetValue() => Value;
        
        public override void SetValue(object val)
        {
            if (val is bool boolValue)
                Value = boolValue;
        }
        
        public override GraphVariable Clone()
        {
            return new BoolVariable(variableName, value);
        }
    }
    
    /// <summary>
    /// Переменная для хранения ссылки на GameObject
    /// </summary>
    [Serializable]
    public class GameObjectVariable : GraphVariable
    {
        [SerializeField] private UnityEngine.Object gameObjectReference;
        
        public UnityEngine.Object Value
        {
            get => gameObjectReference;
            set
            {
                var old = gameObjectReference;
                gameObjectReference = value;
                if (old != value)
                    RaiseValueChanged();
            }
        }
        
        public override Type ValueType => typeof(UnityEngine.Object);
        
        public GameObjectVariable() : base() { }
        
        public GameObjectVariable(string name, UnityEngine.Object initialValue = null)
        {
            variableName = name;
            gameObjectReference = initialValue;
        }
        
        public override object GetValue() => Value;
        
        public override void SetValue(object val)
        {
            Value = val as UnityEngine.Object;
        }
        
        public override GraphVariable Clone()
        {
            return new GameObjectVariable(variableName, gameObjectReference);
        }
    }
}
