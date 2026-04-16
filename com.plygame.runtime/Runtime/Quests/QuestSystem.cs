using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlyGame.Runtime.Quests
{
    /// <summary>
    /// Статус квеста
    /// </summary>
    public enum QuestStatus
    {
        NotStarted,
        Active,
        Completed,
        Failed
    }

    /// <summary>
    /// Цель квеста
    /// </summary>
    [Serializable]
    public class QuestObjective
    {
        public string id;
        public string description;
        public int targetAmount;
        public int currentAmount;
        public bool isCompleted;
        public string completionEvent;
        
        public QuestObjective() { }
        
        public QuestObjective(string desc, int amount = 1)
        {
            id = Guid.NewGuid().ToString();
            description = desc;
            targetAmount = amount;
            currentAmount = 0;
            isCompleted = false;
        }
        
        public void Progress(int amount = 1)
        {
            currentAmount += amount;
            if (currentAmount >= targetAmount)
            {
                currentAmount = targetAmount;
                isCompleted = true;
            }
        }
        
        public void Reset()
        {
            currentAmount = 0;
            isCompleted = false;
        }
    }

    /// <summary>
    /// Данные квеста
    /// </summary>
    [Serializable]
    public class QuestData
    {
        public string id;
        public string title;
        public string description;
        public QuestStatus status;
        public List<QuestObjective> objectives;
        public string rewardDescription;
        public Dictionary<string, object> rewards;
        public string prerequisiteQuestId;
        public bool isRepeatable;
        
        public QuestData()
        {
            objectives = new List<QuestObjective>();
            rewards = new Dictionary<string, object>();
            status = QuestStatus.NotStarted;
        }
        
        public QuestData(string questId, string questTitle) : this()
        {
            id = questId;
            title = questTitle;
        }
        
        public bool CanStart()
        {
            return status == QuestStatus.NotStarted && 
                   (!isRepeatable || true);
        }
        
        public bool IsCompleted()
        {
            if (status != QuestStatus.Active) return false;
            
            foreach (var objective in objectives)
            {
                if (!objective.isCompleted)
                    return false;
            }
            return true;
        }
        
        public void Start()
        {
            if (CanStart())
            {
                status = QuestStatus.Active;
            }
        }
        
        public void Complete()
        {
            if (IsCompleted())
            {
                status = QuestStatus.Completed;
            }
        }
        
        public void Fail()
        {
            status = QuestStatus.Failed;
        }
    }

    /// <summary>
    /// Менеджер квестов
    /// </summary>
    public class QuestManager
    {
        private readonly Dictionary<string, QuestData> quests = new();
        private readonly List<string> activeQuests = new();
        private readonly List<string> completedQuests = new();
        
        public event Action<QuestData> OnQuestStarted;
        public event Action<QuestData> OnQuestCompleted;
        public event Action<QuestData> OnQuestFailed;
        public event Action<QuestData, QuestObjective> OnObjectiveUpdated;
        public event Action<QuestData, QuestObjective> OnObjectiveCompleted;
        
        public IReadOnlyCollection<QuestData> AllQuests => quests.Values;
        public IReadOnlyCollection<QuestData> ActiveQuests => GetQuestsByStatus(QuestStatus.Active);
        public IReadOnlyCollection<QuestData> CompletedQuests => GetQuestsByStatus(QuestStatus.Completed);
        
        /// <summary>
        /// Добавить квест
        /// </summary>
        public void AddQuest(QuestData quest)
        {
            if (quests.ContainsKey(quest.id))
            {
                Debug.LogWarning($"Quest '{quest.id}' already exists. Updating.");
                quests[quest.id] = quest;
            }
            else
            {
                quests.Add(quest.id, quest);
            }
        }
        
        /// <summary>
        /// Начать квест
        /// </summary>
        public bool StartQuest(string questId)
        {
            if (!quests.TryGetValue(questId, out var quest))
            {
                Debug.LogError($"Quest '{questId}' not found");
                return false;
            }
            
            if (!quest.CanStart())
            {
                Debug.LogWarning($"Quest '{questId}' cannot be started");
                return false;
            }
            
            // Проверка пререквизитов
            if (!string.IsNullOrEmpty(quest.prerequisiteQuestId) && 
                !completedQuests.Contains(quest.prerequisiteQuestId))
            {
                Debug.LogWarning($"Prerequisite quest '{quest.prerequisiteQuestId}' not completed");
                return false;
            }
            
            quest.Start();
            activeQuests.Add(questId);
            OnQuestStarted?.Invoke(quest);
            
            return true;
        }
        
        /// <summary>
        /// Прогресс цели квеста
        /// </summary>
        public void ProgressObjective(string questId, string objectiveId, int amount = 1)
        {
            if (!quests.TryGetValue(questId, out var quest)) return;
            if (quest.status != QuestStatus.Active) return;
            
            var objective = quest.objectives.Find(o => o.id == objectiveId);
            if (objective == null) return;
            
            bool wasCompleted = objective.isCompleted;
            objective.Progress(amount);
            
            OnObjectiveUpdated?.Invoke(quest, objective);
            
            if (!wasCompleted && objective.isCompleted)
            {
                OnObjectiveCompleted?.Invoke(quest, objective);
                CheckQuestCompletion(quest);
            }
        }
        
        /// <summary>
        /// Завершить квест
        /// </summary>
        public void CompleteQuest(string questId)
        {
            if (!quests.TryGetValue(questId, out var quest)) return;
            
            if (quest.IsCompleted())
            {
                quest.Complete();
                activeQuests.Remove(questId);
                completedQuests.Add(questId);
                OnQuestCompleted?.Invoke(quest);
            }
        }
        
        /// <summary>
        /// Провалить квест
        /// </summary>
        public void FailQuest(string questId)
        {
            if (!quests.TryGetValue(questId, out var quest)) return;
            
            quest.Fail();
            activeQuests.Remove(questId);
            OnQuestFailed?.Invoke(quest);
        }
        
        /// <summary>
        /// Получить квест по ID
        /// </summary>
        public QuestData GetQuest(string questId)
        {
            return quests.TryGetValue(questId, out var quest) ? quest : null;
        }
        
        /// <summary>
        /// Проверить наличие квеста
        /// </summary>
        public bool HasQuest(string questId)
        {
            return quests.ContainsKey(questId);
        }
        
        /// <summary>
        /// Получить квесты по статусу
        /// </summary>
        public List<QuestData> GetQuestsByStatus(QuestStatus status)
        {
            var result = new List<QuestData>();
            foreach (var quest in quests.Values)
            {
                if (quest.status == status)
                {
                    result.Add(quest);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Сбросить повторяемый квест
        /// </summary>
        public void ResetRepeatableQuest(string questId)
        {
            if (!quests.TryGetValue(questId, out var quest)) return;
            if (!quest.isRepeatable) return;
            
            quest.status = QuestStatus.NotStarted;
            completedQuests.Remove(questId);
            
            foreach (var objective in quest.objectives)
            {
                objective.Reset();
            }
        }
        
        private void CheckQuestCompletion(QuestData quest)
        {
            if (quest.IsCompleted())
            {
                CompleteQuest(quest.id);
            }
        }
    }

    /// <summary>
    /// Узел графа для управления квестами
    /// </summary>
    [Serializable]
    public class QuestNode : GraphNode
    {
        [SerializeField] private string questId;
        [SerializeField] private QuestAction action = QuestAction.Start;
        
        public enum QuestAction { Start, Complete, Fail, Progress }
        
        public override string Title => $"{action} Quest: {questId}";
        public override string Category => "Quests";
        
        public QuestNode() : base() { }
        
        public QuestNode(string id, QuestAction act = QuestAction.Start) : base()
        {
            questId = id;
            action = act;
        }
        
        protected override System.Threading.Tasks.Task<NodeExecutionResult> ExecuteInternalAsync(
            ExecutionContext context, 
            System.Threading.CancellationToken cancellationToken)
        {
            var questManager = context.Get<QuestManager>("QuestManager");
            if (questManager == null)
            {
                return System.Threading.Tasks.Task.FromResult(
                    NodeExecutionResult.Failure("QuestManager not found in context"));
            }
            
            switch (action)
            {
                case QuestAction.Start:
                    questManager.StartQuest(questId);
                    break;
                case QuestAction.Complete:
                    questManager.CompleteQuest(questId);
                    break;
                case QuestAction.Fail:
                    questManager.FailQuest(questId);
                    break;
                case QuestAction.Progress:
                    // Для прогресса нужны дополнительные данные
                    break;
            }
            
            return System.Threading.Tasks.Task.FromResult(NodeExecutionResult.Success());
        }
        
        public override GraphNode Clone()
        {
            var clone = new QuestNode(questId, action);
            CopyBaseFields(clone);
            return clone;
        }
    }
}
