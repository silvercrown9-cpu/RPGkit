using System;
using System.Threading.Tasks;
using UnityEngine;
using PlyGame.Runtime.Core;
using PlyGame.Runtime.Variables;

namespace PlyGame.Runtime.Nodes
{
    /// <summary>
    /// Узел установки значения переменной
    /// </summary>
    [Serializable]
    public class SetVariableNode : GraphNode
    {
        public string VariableId;
        public SetValueMode Mode;
        
        [SerializeReference]
        public object Value;
        
        [SerializeField]
        private int _intValue;
        [SerializeField]
        private float _floatValue;
        [SerializeField]
        private string _stringValue;
        [SerializeField]
        private bool _boolValue;
        [SerializeField]
        private GameObject _gameObjectValue;

        protected override async Task<NodeExecutionResult> OnExecuteAsync(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(VariableId))
            {
                return NodeExecutionResult.FailureResult("Variable ID is empty");
            }

            var variable = VariableManager.Instance?.GetVariable(VariableId);
            if (variable == null)
            {
                return NodeExecutionResult.FailureResult($"Variable {VariableId} not found");
            }

            try
            {
                switch (Mode)
                {
                    case SetValueMode.Int:
                        variable.SetValue(_intValue);
                        break;
                    case SetValueMode.Float:
                        variable.SetValue(_floatValue);
                        break;
                    case SetValueMode.String:
                        variable.SetValue(_stringValue);
                        break;
                    case SetValueMode.Bool:
                        variable.SetValue(_boolValue);
                        break;
                    case SetValueMode.GameObject:
                        variable.SetValue(_gameObjectValue);
                        break;
                    case SetValueMode.FromContext:
                        if (Value != null)
                        {
                            variable.SetValue(Value);
                        }
                        break;
                }

                Debug.Log($"[SetVariableNode] Set variable {VariableId} = {variable.GetValue()}");
            }
            catch (Exception ex)
            {
                return NodeExecutionResult.FailureResult($"Error setting variable: {ex.Message}");
            }

            return NodeExecutionResult.SuccessResult(
                OutputNodes?.Length > 0 ? OutputNodes[0].Id : null);
        }
    }

    /// <summary>
    /// Узел проверки значения переменной
    /// </summary>
    [Serializable]
    public class CheckVariableNode : GraphNode
    {
        public string VariableId;
        public ComparisonType Comparison;
        
        [SerializeField]
        private int _intCompareValue;
        [SerializeField]
        private float _floatCompareValue;
        [SerializeField]
        private string _stringCompareValue;
        [SerializeField]
        private bool _boolCompareValue;

        [BranchNodes]
        public GraphNode TrueNode;
        
        [BranchNodes]
        public GraphNode FalseNode;

        protected override async Task<NodeExecutionResult> OnExecuteAsync(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(VariableId))
            {
                return NodeExecutionResult.FailureResult("Variable ID is empty");
            }

            var variable = VariableManager.Instance?.GetVariable(VariableId);
            if (variable == null)
            {
                return NodeExecutionResult.FailureResult($"Variable {VariableId} not found");
            }

            bool result = false;

            try
            {
                switch (variable)
                {
                    case IntVariable intVar:
                        result = CompareValues(intVar.Value, _intCompareValue);
                        break;
                    case FloatVariable floatVar:
                        result = CompareValues(floatVar.Value, _floatCompareValue);
                        break;
                    case StringVariable stringVar:
                        result = CompareValues(stringVar.Value, _stringCompareValue);
                        break;
                    case BoolVariable boolVar:
                        result = CompareValues(boolVar.Value, _boolCompareValue);
                        break;
                }
            }
            catch (Exception ex)
            {
                return NodeExecutionResult.FailureResult($"Error comparing variable: {ex.Message}");
            }

            var nextNodeId = result 
                ? (TrueNode?.Id ?? OutputNodes?.Length > 0 ? OutputNodes[0].Id : null)
                : (FalseNode?.Id ?? OutputNodes?.Length > 1 ? OutputNodes[1].Id : null);

            return NodeExecutionResult.SuccessResult(nextNodeId);
        }

        private bool CompareValues<T>(T actual, T compare) where T : IComparable
        {
            switch (Comparison)
            {
                case ComparisonType.Equals:
                    return actual.CompareTo(compare) == 0;
                case ComparisonType.NotEquals:
                    return actual.CompareTo(compare) != 0;
                case ComparisonType.Greater:
                    return actual.CompareTo(compare) > 0;
                case ComparisonType.Less:
                    return actual.CompareTo(compare) < 0;
                case ComparisonType.GreaterOrEqual:
                    return actual.CompareTo(compare) >= 0;
                case ComparisonType.LessOrEqual:
                    return actual.CompareTo(compare) <= 0;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Узел математической операции
    /// </summary>
    [Serializable]
    public class MathOperationNode : GraphNode
    {
        public string TargetVariableId;
        public MathOperation Operation;
        public OperationMode Mode;
        
        [SerializeField]
        private float _value;
        [SerializeField]
        private string _sourceVariableId;

        protected override async Task<NodeExecutionResult> OnExecuteAsync(ExecutionContext context)
        {
            if (string.IsNullOrEmpty(TargetVariableId))
            {
                return NodeExecutionResult.FailureResult("Target variable ID is empty");
            }

            var targetVariable = VariableManager.Instance?.GetVariable(TargetVariableId);
            if (targetVariable == null)
            {
                return NodeExecutionResult.FailureResult($"Target variable {TargetVariableId} not found");
            }

            float operand = _value;
            
            if (Mode == OperationMode.FromVariable && !string.IsNullOrEmpty(_sourceVariableId))
            {
                var sourceVariable = VariableManager.Instance?.GetVariable(_sourceVariableId);
                if (sourceVariable != null)
                {
                    operand = Convert.ToSingle(sourceVariable.GetValue());
                }
            }

            try
            {
                switch (targetVariable)
                {
                    case IntVariable intVar:
                        intVar.Value = PerformOperation(Convert.ToInt32(intVar.Value), Convert.ToInt32(operand));
                        break;
                    case FloatVariable floatVar:
                        floatVar.Value = PerformOperation(floatVar.Value, operand);
                        break;
                }

                Debug.Log($"[MathOperationNode] Variable {TargetVariableId} = {targetVariable.GetValue()}");
            }
            catch (Exception ex)
            {
                return NodeExecutionResult.FailureResult($"Error performing operation: {ex.Message}");
            }

            return NodeExecutionResult.SuccessResult(
                OutputNodes?.Length > 0 ? OutputNodes[0].Id : null);
        }

        private int PerformOperation(int a, int b)
        {
            switch (Operation)
            {
                case MathOperation.Add: return a + b;
                case MathOperation.Subtract: return a - b;
                case MathOperation.Multiply: return a * b;
                case MathOperation.Divide: return b != 0 ? a / b : a;
                case MathOperation.Modulo: return b != 0 ? a % b : a;
                default: return a;
            }
        }

        private float PerformOperation(float a, float b)
        {
            switch (Operation)
            {
                case MathOperation.Add: return a + b;
                case MathOperation.Subtract: return a - b;
                case MathOperation.Multiply: return a * b;
                case MathOperation.Divide: return b != 0 ? a / b : a;
                case MathOperation.Modulo: return b != 0 ? a % b : a;
                default: return a;
            }
        }
    }

    public enum SetValueMode
    {
        Int,
        Float,
        String,
        Bool,
        GameObject,
        FromContext
    }

    public enum ComparisonType
    {
        Equals,
        NotEquals,
        Greater,
        Less,
        GreaterOrEqual,
        LessOrEqual
    }

    public enum MathOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo
    }

    public enum OperationMode
    {
        Constant,
        FromVariable
    }

    public class BranchNodesAttribute : PropertyAttribute { }
}
