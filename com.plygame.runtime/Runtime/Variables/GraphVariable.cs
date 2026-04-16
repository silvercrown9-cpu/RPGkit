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
        [SerializeField] private string variableName;
        [SerializeField] private bool isExposed;
        
        public string Name => variableName;
        public bool IsExposed => isExposed;
        
        public abstract Type ValueType { get; }
        public abstract object GetValue();
        public abstract void SetValue(object value);
        public abstract GraphVariable Clone();
        
        protected GraphVariable(string name, bool exposed = false)
        {
            variableName = name;
            isExposed = exposed;
        }
    }

    /// <summary>
    /// Переменная целочисленного типа
    /// </summary>
    [Serializable]
    public class IntVariable : GraphVariable
    {
        [SerializeField] private int value;
        
        public override Type ValueType => typeof(int);
        
        public IntVariable(string name, int initialValue = 0, bool exposed = false) 
            : base(name, exposed)
        {
            value = initialValue;
        }
        
        public int Value
        {
            get => value;
            set => this.value = value;
        }
        
        public override object GetValue() => value;
        
        public override void SetValue(object newValue)
        {
            if (newValue is int intValue)
                value = intValue;
            else
                Debug.LogWarning($"Cannot set IntVariable with type {newValue?.GetType()}");
        }
        
        public override GraphVariable Clone()
        {
            return new IntVariable(Name, value, IsExposed);
        }
    }

    /// <summary>
    /// Переменная строкового типа
    /// </summary>
    [Serializable]
    public class StringVariable : GraphVariable
    {
        [SerializeField] private string value;
        
        public override Type ValueType => typeof(string);
        
        public StringVariable(string name, string initialValue = "", bool exposed = false) 
            : base(name, exposed)
        {
            value = initialValue;
        }
        
        public string Value
        {
            get => value;
            set => this.value = value;
        }
        
        public override object GetValue() => value;
        
        public override void SetValue(object newValue)
        {
            value = newValue as string ?? "";
        }
        
        public override GraphVariable Clone()
        {
            return new StringVariable(Name, value, IsExposed);
        }
    }

    /// <summary>
    /// Переменная булевого типа
    /// </summary>
    [Serializable]
    public class BoolVariable : GraphVariable
    {
        [SerializeField] private bool value;
        
        public override Type ValueType => typeof(bool);
        
        public BoolVariable(string name, bool initialValue = false, bool exposed = false) 
            : base(name, exposed)
        {
            value = initialValue;
        }
        
        public bool Value
        {
            get => value;
            set => this.value = value;
        }
        
        public override object GetValue() => value;
        
        public override void SetValue(object newValue)
        {
            if (newValue is bool boolValue)
                value = boolValue;
        }
        
        public override GraphVariable Clone()
        {
            return new BoolVariable(Name, value, IsExposed);
        }
    }

    /// <summary>
    /// Переменная float типа
    /// </summary>
    [Serializable]
    public class FloatVariable : GraphVariable
    {
        [SerializeField] private float value;
        
        public override Type ValueType => typeof(float);
        
        public FloatVariable(string name, float initialValue = 0f, bool exposed = false) 
            : base(name, exposed)
        {
            value = initialValue;
        }
        
        public float Value
        {
            get => value;
            set => this.value = value;
        }
        
        public override object GetValue() => value;
        
        public override void SetValue(object newValue)
        {
            if (newValue is float floatValue)
                value = floatValue;
            else if (newValue is int intValue)
                value = intValue;
        }
        
        public override GraphVariable Clone()
        {
            return new FloatVariable(Name, value, IsExposed);
        }
    }

    /// <summary>
    /// Переменная ссылочного типа на GameObject
    /// </summary>
    [Serializable]
    public class GameObjectVariable : GraphVariable
    {
        [SerializeField] private GameObject value;
        
        public override Type ValueType => typeof(GameObject);
        
        public GameObjectVariable(string name, GameObject initialValue = null, bool exposed = false) 
            : base(name, exposed)
        {
            value = initialValue;
        }
        
        public GameObject Value
        {
            get => value;
            set => this.value = value;
        }
        
        public override object GetValue() => value;
        
        public override void SetValue(object newValue)
        {
            value = newValue as GameObject;
        }
        
        public override GraphVariable Clone()
        {
            return new GameObjectVariable(Name, value, IsExposed);
        }
    }
}
