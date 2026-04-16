using System;
using System.Threading.Tasks;
using PlyGame.Runtime.Core.Graph;
using PlyGame.Runtime.Variables;
using UnityEngine;

namespace PlyGame.Runtime.Nodes
{
    /// <summary>
    /// Узел установки значения переменной
    /// </summary>
    [Serializable]
    public class SetVariableNode : GraphNode
    {
        [SerializeField] private string variableId;
        [SerializeField] private VariableValue valueToSet;
        
        public string VariableId => variableId;
        public VariableValue ValueToSet => valueToSet;
        
        public SetVariableNode()
        {
            Name = "Set Variable";
            Description = "Устанавливает значение переменной";
        }
        
        public override async Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var variableManager = context.GetVariable<VariableManager>("VariableManager");
            
            if (variableManager == null)
            {
                return NodeExecutionResult.FailureResult("VariableManager not found in context");
            }
            
            if (!variableManager.HasVariable(variableId))
            {
                return NodeExecutionResult.FailureResult($"Variable {variableId} not found");
            }
            
            var value = valueToSet.GetValue(context);
            variableManager.SetValue(variableId, value);
            
            Debug.Log($"Переменная {variableId} установлена в {value}");
            
            return NodeExecutionResult.SuccessResult();
        }
    }
    
    /// <summary>
    /// Узел проверки значения переменной с ветвлением
    /// </summary>
    [Serializable]
    public class CheckVariableNode : GraphNode
    {
        [SerializeField] private string variableId;
        [SerializeField] private ComparisonType comparisonType;
        [SerializeField] private VariableValue compareValue;
        [SerializeField] private string trueNodeId;
        [SerializeField] private string falseNodeId;
        
        public string TrueNodeId => trueNodeId;
        public string FalseNodeId => falseNodeId;
        
        public CheckVariableNode()
        {
            Name = "Check Variable";
            Description = "Проверяет условие и переходит по соответствующей ветке";
        }
        
        public override async Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var variableManager = context.GetVariable<VariableManager>("VariableManager");
            
            if (variableManager == null)
            {
                return NodeExecutionResult.FailureResult("VariableManager not found");
            }
            
            var variable = variableManager.GetVariable<GraphVariable>(variableId);
            if (variable == null)
            {
                return NodeExecutionResult.FailureResult($"Variable {variableId} not found");
            }
            
            var currentValue = variable.GetValue();
            var compareVal = compareValue.GetValue(context);
            
            bool result = comparisonType switch
            {
                ComparisonType.Equals => Equals(currentValue, compareVal),
                ComparisonType.NotEquals => !Equals(currentValue, compareVal),
                ComparisonType.GreaterThan => CompareValues(currentValue, compareVal, 1),
                ComparisonType.LessThan => CompareValues(currentValue, compareVal, -1),
                ComparisonType.GreaterThanOrEqual => CompareValues(currentValue, compareVal, 1) || Equals(currentValue, compareVal),
                ComparisonType.LessThanOrEqual => CompareValues(currentValue, compareVal, -1) || Equals(currentValue, compareVal),
                _ => false
            };
            
            var nextNodeId = result ? trueNodeId : falseNodeId;
            Debug.Log($"Проверка переменной {variableId}: {(result ? "true" : "false")}");
            
            return NodeExecutionResult.SuccessResult(nextNodeId);
        }
        
        private bool CompareValues(object a, object b, int direction)
        {
            try
            {
                if (a is IComparable comparableA && b is IComparable comparableB)
                {
                    return comparableA.CompareTo(comparableB) * direction > 0;
                }
            }
            catch
            {
                // Игнорируем ошибки сравнения
            }
            return false;
        }
    }
    
    /// <summary>
    /// Узел математической операции над переменными
    /// </summary>
    [Serializable]
    public class MathOperationNode : GraphNode
    {
        [SerializeField] private string targetVariableId;
        [SerializeField] private MathOperationType operationType;
        [SerializeField] private VariableValue operand;
        
        public MathOperationNode()
        {
            Name = "Math Operation";
            Description = "Выполняет математическую операцию над переменной";
        }
        
        public override async Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var variableManager = context.GetVariable<VariableManager>("VariableManager");
            
            if (variableManager == null)
            {
                return NodeExecutionResult.FailureResult("VariableManager not found");
            }
            
            var variable = variableManager.GetVariable<GraphVariable>(targetVariableId);
            if (variable == null)
            {
                return NodeExecutionResult.FailureResult($"Variable {targetVariableId} not found");
            }
            
            var currentValue = variable.GetValue();
            var operandValue = operand.GetValue(context);
            
            var result = operationType switch
            {
                MathOperationType.Add => AddValues(currentValue, operandValue),
                MathOperationType.Subtract => SubtractValues(currentValue, operandValue),
                MathOperationType.Multiply => MultiplyValues(currentValue, operandValue),
                MathOperationType.Divide => DivideValues(currentValue, operandValue),
                MathOperationType.Modulo => ModuloValues(currentValue, operandValue),
                _ => currentValue
            };
            
            variable.SetValue(result);
            Debug.Log($"Математическая операция: {currentValue} {operationType} {operandValue} = {result}");
            
            return NodeExecutionResult.SuccessResult();
        }
        
        private object AddValues(object a, object b)
        {
            if (a is int ai && b is int bi) return ai + bi;
            if (a is float af && b is float bf) return af + bf;
            return a;
        }
        
        private object SubtractValues(object a, object b)
        {
            if (a is int ai && b is int bi) return ai - bi;
            if (a is float af && b is float bf) return af - bf;
            return a;
        }
        
        private object MultiplyValues(object a, object b)
        {
            if (a is int ai && b is int bi) return ai * bi;
            if (a is float af && b is float bf) return af * bf;
            return a;
        }
        
        private object DivideValues(object a, object b)
        {
            if (a is int ai && b is int bi) return bi != 0 ? ai / bi : ai;
            if (a is float af && b is float bf) return bf != 0 ? af / bf : af;
            return a;
        }
        
        private object ModuloValues(object a, object b)
        {
            if (a is int ai && b is int bi) return bi != 0 ? ai % bi : ai;
            if (a is float af && b is float bf) return bf != 0 ? af % bf : af;
            return a;
        }
    }
    
    /// <summary>
    /// Типы сравнения для CheckVariableNode
    /// </summary>
    public enum ComparisonType
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual
    }
    
    /// <summary>
    /// Типы математических операций
    /// </summary>
    public enum MathOperationType
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo
    }
    
    /// <summary>
    /// Значение переменной (может быть константой или ссылкой на другую переменную)
    /// </summary>
    [Serializable]
    public class VariableValue
    {
        [SerializeField] private ValueType valueType;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;
        [SerializeField] private string stringValue;
        [SerializeField] private bool boolValue;
        [SerializeField] private string variableReferenceId;
        
        public object GetValue(ExecutionContext context)
        {
            if (valueType == ValueType.VariableReference && !string.IsNullOrEmpty(variableReferenceId))
            {
                var variableManager = context.GetVariable<VariableManager>("VariableManager");
                if (variableManager != null)
                {
                    var variable = variableManager.GetVariable<GraphVariable>(variableReferenceId);
                    return variable?.GetValue();
                }
            }
            
            return valueType switch
            {
                ValueType.Int => intValue,
                ValueType.Float => floatValue,
                ValueType.String => stringValue,
                ValueType.Bool => boolValue,
                _ => null
            };
        }
    }
    
    /// <summary>
    /// Тип значения для VariableValue
    /// </summary>
    public enum ValueType
    {
        Int,
        Float,
        String,
        Bool,
        VariableReference
    }
}
