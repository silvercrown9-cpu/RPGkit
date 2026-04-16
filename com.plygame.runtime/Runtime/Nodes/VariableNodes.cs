using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using PlyGame.Runtime.Variables;

namespace PlyGame.Runtime.Nodes
{
    /// <summary>
    /// Узел установки переменной
    /// </summary>
    [System.Serializable]
    public class SetVariableNode : GraphNode
    {
        [SerializeField] private string variableName;
        [SerializeField] private VariableType variableType = VariableType.Int;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;
        [SerializeField] private string stringValue;
        [SerializeField] private bool boolValue;
        
        public enum VariableType { Int, Float, String, Bool }
        
        public override string Title => $"Set Variable: {variableName}";
        public override string Category => "Variables";
        
        public SetVariableNode() : base() { }
        
        public SetVariableNode(string varName, VariableType type = VariableType.Int) : base()
        {
            variableName = varName;
            variableType = type;
        }
        
        protected override async Task<NodeExecutionResult> ExecuteInternalAsync(ExecutionContext context, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(variableName))
            {
                return NodeExecutionResult.Failure("Variable name is empty");
            }
            
            var variableManager = context.Get<VariableManager>("VariableManager");
            if (variableManager == null)
            {
                return NodeExecutionResult.Failure("VariableManager not found in context");
            }
            
            object value = variableType switch
            {
                VariableType.Int => intValue,
                VariableType.Float => floatValue,
                VariableType.String => stringValue,
                VariableType.Bool => boolValue,
                _ => null
            };
            
            // Создаём переменную если не существует
            if (!variableManager.HasVariable(variableName))
            {
                GraphVariable newVar = variableType switch
                {
                    VariableType.Int => new IntVariable(variableName),
                    VariableType.Float => new FloatVariable(variableName),
                    VariableType.String => new StringVariable(variableName),
                    VariableType.Bool => new BoolVariable(variableName),
                    _ => null
                };
                
                if (newVar != null)
                    variableManager.AddVariable(newVar);
            }
            
            variableManager.SetValue(variableName, value);
            
            await Task.Yield(); // Симуляция асинхронности
            
            return NodeExecutionResult.Success();
        }
        
        public override GraphNode Clone()
        {
            var clone = new SetVariableNode(variableName, variableType)
            {
                intValue = intValue,
                floatValue = floatValue,
                stringValue = stringValue,
                boolValue = boolValue
            };
            CopyBaseFields(clone);
            return clone;
        }
    }

    /// <summary>
    /// Узел проверки условия переменной
    /// </summary>
    [System.Serializable]
    public class CheckVariableNode : GraphNode
    {
        [SerializeField] private string variableName;
        [SerializeField] private ComparisonType comparison = ComparisonType.Equals;
        [SerializeField] private int intValue;
        [SerializeField] private float floatValue;
        [SerializeField] private string stringValue;
        [SerializeField] private bool boolValue;
        
        public enum ComparisonType { Equals, NotEquals, Greater, Less, GreaterOrEqual, LessOrEqual }
        
        public override string Title => $"Check: {variableName}";
        public override string Category => "Variables/Conditions";
        
        public CheckVariableNode() : base() { }
        
        public CheckVariableNode(string varName, ComparisonType comp = ComparisonType.Equals) : base()
        {
            variableName = varName;
            comparison = comp;
        }
        
        protected override async Task<NodeExecutionResult> ExecuteInternalAsync(ExecutionContext context, CancellationToken cancellationToken)
        {
            var variableManager = context.Get<VariableManager>("VariableManager");
            if (variableManager == null)
            {
                return NodeExecutionResult.Failure("VariableManager not found in context");
            }
            
            if (!variableManager.HasVariable(variableName))
            {
                return NodeExecutionResult.Failure($"Variable '{variableName}' not found");
            }
            
            var variable = variableManager.GetVariable<GraphVariable>(variableName);
            var currentValue = variable.GetValue();
            
            bool result = comparison switch
            {
                ComparisonType.Equals => CompareEqual(currentValue, GetCompareValue()),
                ComparisonType.NotEquals => !CompareEqual(currentValue, GetCompareValue()),
                ComparisonType.Greater => CompareGreater(currentValue, GetCompareValue()),
                ComparisonType.Less => CompareLess(currentValue, GetCompareValue()),
                ComparisonType.GreaterOrEqual => CompareGreaterOrEqual(currentValue, GetCompareValue()),
                ComparisonType.LessOrEqual => CompareLessOrEqual(currentValue, GetCompareValue()),
                _ => false
            };
            
            await Task.Yield();
            
            // Возвращаем true/false через данные контекста
            context.Set("ConditionResult", result);
            
            // Если true - идём по первому выходу, если false - по второму
            var outputId = result ? GetOutputId(0) : GetOutputId(1);
            return NodeExecutionResult.Continue(outputId);
        }
        
        private object GetCompareValue()
        {
            return variableName.Contains("Int") || variableName.Contains("Count") || variableName.Contains("Level") ? (object)intValue :
                   variableName.Contains("Float") || variableName.Contains("Health") || variableName.Contains("Speed") ? floatValue :
                   variableName.Contains("Bool") || variableName.Contains("Is") || variableName.Contains("Has") ? boolValue :
                   (object)stringValue;
        }
        
        private bool CompareEqual(object a, object b)
        {
            if (a == null || b == null) return a == b;
            return a.Equals(b);
        }
        
        private bool CompareGreater(object a, object b)
        {
            if (a is IComparable comparable)
                return comparable.CompareTo(b) > 0;
            return false;
        }
        
        private bool CompareLess(object a, object b)
        {
            if (a is IComparable comparable)
                return comparable.CompareTo(b) < 0;
            return false;
        }
        
        private bool CompareGreaterOrEqual(object a, object b)
        {
            if (a is IComparable comparable)
                return comparable.CompareTo(b) >= 0;
            return false;
        }
        
        private bool CompareLessOrEqual(object a, object b)
        {
            if (a is IComparable comparable)
                return comparable.CompareTo(b) <= 0;
            return false;
        }
        
        public override GraphNode Clone()
        {
            var clone = new CheckVariableNode(variableName, comparison)
            {
                intValue = intValue,
                floatValue = floatValue,
                stringValue = stringValue,
                boolValue = boolValue
            };
            CopyBaseFields(clone);
            return clone;
        }
    }

    /// <summary>
    /// Узел математической операции
    /// </summary>
    [System.Serializable]
    public class MathOperationNode : GraphNode
    {
        [SerializeField] private string variableName;
        [SerializeField] private MathOperation operation = MathOperation.Add;
        [SerializeField] private int intValue = 1;
        [SerializeField] private float floatValue = 1f;
        
        public enum MathOperation { Add, Subtract, Multiply, Divide, Modulo }
        
        public override string Title => $"{operation} {variableName}";
        public override string Category => "Variables/Math";
        
        public MathOperationNode() : base() { }
        
        public MathOperationNode(string varName, MathOperation op = MathOperation.Add) : base()
        {
            variableName = varName;
            operation = op;
        }
        
        protected override async Task<NodeExecutionResult> ExecuteInternalAsync(ExecutionContext context, CancellationToken cancellationToken)
        {
            var variableManager = context.Get<VariableManager>("VariableManager");
            if (variableManager == null)
            {
                return NodeExecutionResult.Failure("VariableManager not found in context");
            }
            
            if (!variableManager.HasVariable(variableName))
            {
                return NodeExecutionResult.Failure($"Variable '{variableName}' not found");
            }
            
            var variable = variableManager.GetVariable<GraphVariable>(variableName);
            
            if (variable is IntVariable intVar)
            {
                int result = operation switch
                {
                    MathOperation.Add => intVar.Value + intValue,
                    MathOperation.Subtract => intVar.Value - intValue,
                    MathOperation.Multiply => intVar.Value * intValue,
                    MathOperation.Divide => intValue != 0 ? intVar.Value / intValue : intVar.Value,
                    MathOperation.Modulo => intValue != 0 ? intVar.Value % intValue : intVar.Value,
                    _ => intVar.Value
                };
                intVar.Value = result;
                variableManager.SetValue(variableName, result);
            }
            else if (variable is FloatVariable floatVar)
            {
                float result = operation switch
                {
                    MathOperation.Add => floatVar.Value + floatValue,
                    MathOperation.Subtract => floatVar.Value - floatValue,
                    MathOperation.Multiply => floatVar.Value * floatValue,
                    MathOperation.Divide => floatValue != 0 ? floatVar.Value / floatValue : floatVar.Value,
                    MathOperation.Modulo => floatValue != 0 ? floatVar.Value % floatValue : floatVar.Value,
                    _ => floatVar.Value
                };
                floatVar.Value = result;
                variableManager.SetValue(variableName, result);
            }
            else
            {
                return NodeExecutionResult.Failure($"Variable '{variableName}' is not numeric");
            }
            
            await Task.Yield();
            
            return NodeExecutionResult.Success();
        }
        
        public override GraphNode Clone()
        {
            var clone = new MathOperationNode(variableName, operation)
            {
                intValue = intValue,
                floatValue = floatValue
            };
            CopyBaseFields(clone);
            return clone;
        }
    }
}
