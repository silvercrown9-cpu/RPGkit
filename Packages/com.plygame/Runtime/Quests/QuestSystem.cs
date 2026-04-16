using System;
using System.Collections.Generic;
using System.Linq;
using PlyGame.Runtime.Core.Graph;
using PlyGame.Runtime.Core.Events;
using UnityEngine;
using UnityEngine.Events;

namespace PlyGame.Runtime.Quests
{
    /// <summary>
    /// Менеджер системы квестов
    /// </summary>
    public class QuestManager
    {
        private readonly Dictionary<string, QuestData> _quests = new();
        private readonly Dictionary<string, QuestStatus> _questStatuses = new();
        
        public event UnityEvent<QuestData> OnQuestStarted;
        public event UnityEvent<QuestData> OnQuestCompleted;
        public event UnityEvent<QuestData> OnQuestFailed;
        public event UnityEvent<QuestData, QuestObjective> OnObjectiveCompleted;
        public event UnityEvent<QuestData> OnQuestUpdated;
        
        /// <summary>
        /// Добавить квест в систему
        /// </summary>
        public void AddQuest(QuestData quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.QuestId))
            {
                Debug.LogError("Попытка добавить невалидный квест");
                return;
            }
            
            _quests[quest.QuestId] = quest;
            
            if (!_questStatuses.ContainsKey(quest.QuestId))
                _questStatuses[quest.QuestId] = QuestStatus.NotStarted;
        }
        
        /// <summary>
        /// Получить квест по ID
        /// </summary>
        public QuestData GetQuest(string questId)
        {
            return _quests.TryGetValue(questId, out var quest) ? quest : null;
        }
        
        /// <summary>
        /// Получить статус квеста
        /// </summary>
        public QuestStatus GetQuestStatus(string questId)
        {
            return _questStatuses.TryGetValue(questId, out var status) ? status : QuestStatus.NotStarted;
        }
        
        /// <summary>
        /// Проверка доступности квеста (выполнение пререквизитов)
        /// </summary>
        public bool IsQuestAvailable(string questId)
        {
            var quest = GetQuest(questId);
            if (quest == null) return false;
            
            // Проверка статусов пререквизитов
            foreach (var prereqId in quest.Prerequisites)
            {
                var prereqStatus = GetQuestStatus(prereqId);
                if (prereqStatus != QuestStatus.Completed)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Запуск квеста
        /// </summary>
        public bool StartQuest(string questId)
        {
            if (!IsQuestAvailable(questId))
            {
                Debug.LogWarning($"Квест {questId} недоступен для запуска");
                return false;
            }
            
            var quest = GetQuest(questId);
            if (quest == null) return false;
            
            _questStatuses[questId] = QuestStatus.Active;
            quest.StartTime = DateTime.Now;
            
            // Сброс прогресса целей
            foreach (var objective in quest.Objectives)
            {
                objective.IsCompleted = false;
                objective.CurrentProgress = 0;
            }
            
            Debug.Log($"Квест начат: {quest.Title}");
            OnQuestStarted?.Invoke(quest);
            
            return true;
        }
        
        /// <summary>
        /// Обновление прогресса цели квеста
        /// </summary>
        public bool UpdateObjective(string questId, string objectiveId, int progress)
        {
            var quest = GetQuest(questId);
            if (quest == null) return false;
            
            if (GetQuestStatus(questId) != QuestStatus.Active)
                return false;
            
            var objective = quest.Objectives.Find(o => o.ObjectId == objectiveId);
            if (objective == null) return false;
            
            objective.CurrentProgress = Mathf.Min(progress, objective.TargetAmount);
            
            if (objective.CurrentProgress >= objective.TargetAmount && !objective.IsCompleted)
            {
                objective.IsCompleted = true;
                OnObjectiveCompleted?.Invoke(quest, objective);
                
                // Проверка завершения всех целей
                CheckQuestCompletion(questId);
            }
            
            OnQuestUpdated?.Invoke(quest);
            return true;
        }
        
        /// <summary>
        /// Увеличение прогресса цели
        /// </summary>
        public bool IncrementObjective(string questId, string objectiveId, int amount = 1)
        {
            var quest = GetQuest(questId);
            if (quest == null) return false;
            
            var objective = quest.Objectives.Find(o => o.ObjectId == objectiveId);
            if (objective == null) return false;
            
            return UpdateObjective(questId, objectiveId, objective.CurrentProgress + amount);
        }
        
        /// <summary>
        /// Проверка завершения квеста
        /// </summary>
        private void CheckQuestCompletion(string questId)
        {
            var quest = GetQuest(questId);
            if (quest == null) return;
            
            var allCompleted = quest.Objectives.All(o => o.IsCompleted);
            
            if (allCompleted && GetQuestStatus(questId) == QuestStatus.Active)
            {
                CompleteQuest(questId);
            }
        }
        
        /// <summary>
        /// Завершение квеста
        /// </summary>
        public bool CompleteQuest(string questId)
        {
            var quest = GetQuest(questId);
            if (quest == null) return false;
            
            if (GetQuestStatus(questId) != QuestStatus.Active)
                return false;
            
            _questStatuses[questId] = QuestStatus.Completed;
            quest.CompletionTime = DateTime.Now;
            
            Debug.Log($"Квест завершен: {quest.Title}");
            OnQuestCompleted?.Invoke(quest);
            
            return true;
        }
        
        /// <summary>
        /// Провал квеста
        /// </summary>
        public bool FailQuest(string questId)
        {
            var quest = GetQuest(questId);
            if (quest == null) return false;
            
            if (GetQuestStatus(questId) != QuestStatus.Active)
                return false;
            
            _questStatuses[questId] = QuestStatus.Failed;
            
            Debug.Log($"Квест провален: {quest.Title}");
            OnQuestFailed?.Invoke(quest);
            
            return true;
        }
        
        /// <summary>
        /// Получить все активные квесты
        /// </summary>
        public List<QuestData> GetActiveQuests()
        {
            return _quests.Values.Where(q => GetQuestStatus(q.QuestId) == QuestStatus.Active).ToList();
        }
        
        /// <summary>
        /// Получить все завершенные квесты
        /// </summary>
        public List<QuestData> GetCompletedQuests()
        {
            return _quests.Values.Where(q => GetQuestStatus(q.QuestId) == QuestStatus.Completed).ToList();
        }
        
        /// <summary>
        /// Получить все доступные для запуска квесты
        /// </summary>
        public List<QuestData> GetAvailableQuests()
        {
            return _quests.Values.Where(q => 
                GetQuestStatus(q.QuestId) == QuestStatus.NotStarted && 
                IsQuestAvailable(q.QuestId)).ToList();
        }
        
        /// <summary>
        /// Сброс квеста (для повторяемых)
        /// </summary>
        public bool ResetQuest(string questId)
        {
            var quest = GetQuest(questId);
            if (quest == null || !quest.IsRepeatable) return false;
            
            _questStatuses[questId] = QuestStatus.NotStarted;
            
            foreach (var objective in quest.Objectives)
            {
                objective.IsCompleted = false;
                objective.CurrentProgress = 0;
            }
            
            quest.StartTime = null;
            quest.CompletionTime = null;
            
            return true;
        }
    }
    
    /// <summary>
    /// Данные квеста
    /// </summary>
    [Serializable]
    public class QuestData
    {
        public string QuestId;
        public string Title;
        public string Description;
        public string GiverId;
        public List<string> Prerequisites = new();
        public List<QuestObjective> Objectives = new();
        public List<QuestReward> Rewards = new();
        public bool IsRepeatable;
        public DateTime? StartTime;
        public DateTime? CompletionTime;
        
        public QuestObjective GetObjective(string objectiveId)
        {
            return Objectives.Find(o => o.ObjectId == objectiveId);
        }
    }
    
    /// <summary>
    /// Цель квеста
    /// </summary>
    [Serializable]
    public class QuestObjective
    {
        public string ObjectId;
        public string Description;
        public ObjectiveType Type;
        public int TargetAmount;
        public int CurrentProgress;
        public bool IsCompleted;
        public string TargetEntityId;
    }
    
    /// <summary>
    /// Тип цели квеста
    /// </summary>
    public enum ObjectiveType
    {
        Kill,
        Collect,
        Deliver,
        TalkTo,
        VisitLocation,
        UseItem,
        Custom
    }
    
    /// <summary>
    /// Награда за квест
    /// </summary>
    [Serializable]
    public class QuestReward
    {
        public RewardType Type;
        public int Amount;
        public string ItemId;
        public string VariableId;
    }
    
    /// <summary>
    /// Тип награды
    /// </summary>
    public enum RewardType
    {
        Experience,
        Gold,
        Item,
        Variable,
        Reputation
    }
    
    /// <summary>
    /// Узел графа для управления квестами
    /// </summary>
    [Serializable]
    public class QuestNode : GraphNode
    {
        [SerializeField] private QuestAction action;
        [SerializeField] private string questId;
        
        public override Task<NodeExecutionResult> ExecuteAsync(ExecutionContext context)
        {
            var questManager = context.GetData<QuestManager>("QuestManager");
            
            if (questManager == null)
            {
                return Task.FromResult(NodeExecutionResult.FailureResult("QuestManager not found"));
            }
            
            bool success = action switch
            {
                QuestAction.Start => questManager.StartQuest(questId),
                QuestAction.Complete => questManager.CompleteQuest(questId),
                QuestAction.Fail => questManager.FailQuest(questId),
                QuestAction.Reset => questManager.ResetQuest(questId),
                _ => false
            };
            
            return success 
                ? Task.FromResult(NodeExecutionResult.SuccessResult())
                : Task.FromResult(NodeExecutionResult.FailureResult($"Failed to {action} quest {questId}"));
        }
    }
    
    /// <summary>
    /// Действие над квестом
    /// </summary>
    public enum QuestAction
    {
        Start,
        Complete,
        Fail,
        Reset
    }
}
