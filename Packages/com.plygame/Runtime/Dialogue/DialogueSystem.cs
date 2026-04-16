using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlyGame.Runtime.Core.Graph;
using PlyGame.Runtime.Core.Events;
using UnityEngine;
using UnityEngine.Events;

namespace PlyGame.Runtime.Dialogue
{
    /// <summary>
    /// Менеджер диалоговой системы
    /// </summary>
    public class DialogueManager
    {
        private DialogueData _currentDialogue;
        private string _currentNodeId;
        private bool _isRunning;
        
        public event UnityEvent<DialogueLine> OnLineDisplayed;
        public event UnityEvent<List<DialogueChoice>> OnChoicesDisplayed;
        public event UnityEvent OnDialogueStarted;
        public event UnityEvent OnDialogueEnded;
        public event UnityEvent<string> OnNodeEntered;
        
        public bool IsRunning => _isRunning;
        public DialogueData CurrentDialogue => _currentDialogue;
        
        /// <summary>
        /// Запуск диалога
        /// </summary>
        public void StartDialogue(DialogueData dialogue, string startNodeId = null)
        {
            if (dialogue == null)
            {
                Debug.LogError("Попытка запустить null диалог");
                return;
            }
            
            _currentDialogue = dialogue;
            _currentNodeId = startNodeId ?? dialogue.StartNodeId;
            _isRunning = true;
            
            OnDialogueStarted?.Invoke();
            EventBus.Publish(new GraphStartedEvent 
            { 
                GraphId = dialogue.DialogueId, 
                GraphName = dialogue.Title 
            });
            
            ExecuteCurrentNode().Forget();
        }
        
        /// <summary>
        /// Выполнение текущего узла
        /// </summary>
        private async Task ExecuteCurrentNode()
        {
            if (!_isRunning || _currentDialogue == null) return;
            
            var node = _currentDialogue.GetNode(_currentNodeId);
            if (node == null)
            {
                EndDialogue();
                return;
            }
            
            OnNodeEntered?.Invoke(node.NodeId);
            
            // Публикация строки диалога
            if (!string.IsNullOrEmpty(node.Text))
            {
                var line = new DialogueLine
                {
                    SpeakerId = node.SpeakerId,
                    Text = node.Text,
                    VoiceClip = node.VoiceClip,
                    Duration = node.Duration
                };
                
                OnLineDisplayed?.Invoke(line);
                
                // Ожидание завершения аудио или таймера
                if (line.Duration > 0)
                    await Task.Delay(TimeSpan.FromSeconds(line.Duration));
            }
            
            // Показ выборов
            var choices = new List<DialogueChoice>();
            foreach (var choiceData in node.Choices)
            {
                if (IsChoiceAvailable(choiceData))
                {
                    choices.Add(new DialogueChoice
                    {
                        Text = choiceData.Text,
                        TargetNodeId = choiceData.TargetNodeId
                    });
                }
            }
            
            if (choices.Count > 0)
            {
                OnChoicesDisplayed?.Invoke(choices);
                // Ожидание выбора игрока
                return;
            }
            
            // Переход к следующему узлу
            if (!string.IsNullOrEmpty(node.NextNodeId))
            {
                _currentNodeId = node.NextNodeId;
                await ExecuteCurrentNode();
            }
            else
            {
                EndDialogue();
            }
        }
        
        /// <summary>
        /// Обработка выбора игрока
        /// </summary>
        public void MakeChoice(int choiceIndex)
        {
            if (!_isRunning || _currentDialogue == null) return;
            
            var node = _currentDialogue.GetNode(_currentNodeId);
            if (node == null || choiceIndex >= node.Choices.Count) return;
            
            var choice = node.Choices[choiceIndex];
            
            if (IsChoiceAvailable(choice))
            {
                _currentNodeId = choice.TargetNodeId;
                ExecuteCurrentNode().Forget();
            }
        }
        
        /// <summary>
        /// Проверка доступности выбора
        /// </summary>
        private bool IsChoiceAvailable(DialogueChoiceData choice)
        {
            if (string.IsNullOrEmpty(choice.ConditionVariableId))
                return true;
            
            // Здесь должна быть логика проверки условий через VariableManager
            return true;
        }
        
        /// <summary>
        /// Завершение диалога
        /// </summary>
        public void EndDialogue()
        {
            _isRunning = false;
            
            EventBus.Publish(new GraphCompletedEvent 
            { 
                GraphId = _currentDialogue?.DialogueId 
            });
            
            OnDialogueEnded?.Invoke();
            
            _currentDialogue = null;
            _currentNodeId = null;
        }
        
        /// <summary>
        /// Принудительная остановка диалога
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _currentDialogue = null;
        }
    }
    
    /// <summary>
    /// Данные диалога
    /// </summary>
    [Serializable]
    public class DialogueData
    {
        public string DialogueId;
        public string Title;
        public string StartNodeId;
        public List<DialogueNodeData> Nodes = new();
        
        public DialogueNodeData GetNode(string nodeId)
        {
            return Nodes.Find(n => n.NodeId == nodeId);
        }
    }
    
    /// <summary>
    /// Данные узла диалога
    /// </summary>
    [Serializable]
    public class DialogueNodeData
    {
        public string NodeId;
        public string SpeakerId;
        public string Text;
        public AudioClip VoiceClip;
        public float Duration;
        public string NextNodeId;
        public List<DialogueChoiceData> Choices = new();
        public Vector2 Position;
    }
    
    /// <summary>
    /// Данные выбора в диалоге
    /// </summary>
    [Serializable]
    public class DialogueChoiceData
    {
        public string Text;
        public string TargetNodeId;
        public string ConditionVariableId;
        public object RequiredValue;
    }
    
    /// <summary>
    /// Строка диалога для отображения
    /// </summary>
    public class DialogueLine
    {
        public string SpeakerId;
        public string Text;
        public AudioClip VoiceClip;
        public float Duration;
    }
    
    /// <summary>
    /// Выбор для игрока
    /// </summary>
    public class DialogueChoice
    {
        public string Text;
        public string TargetNodeId;
    }
    
    /// <summary>
    /// MonoBehaviour компонент для запуска диалогов
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] private DialogueData dialogue;
        [SerializeField] private string startNodeId;
        
        private DialogueManager _manager;
        
        private void Awake()
        {
            _manager = new DialogueManager();
            SetupEvents();
        }
        
        private void SetupEvents()
        {
            _manager.OnDialogueStarted += HandleDialogueStarted;
            _manager.OnDialogueEnded += HandleDialogueEnded;
            _manager.OnLineDisplayed += HandleLineDisplayed;
            _manager.OnChoicesDisplayed += HandleChoicesDisplayed;
        }
        
        private void HandleDialogueStarted()
        {
            Debug.Log("Диалог начался");
        }
        
        private void HandleDialogueEnded()
        {
            Debug.Log("Диалог завершен");
        }
        
        private void HandleLineDisplayed(DialogueLine line)
        {
            Debug.Log($"{line.SpeakerId}: {line.Text}");
        }
        
        private void HandleChoicesDisplayed(List<DialogueChoice> choices)
        {
            Debug.Log($"Доступно выборов: {choices.Count}");
        }
        
        public void StartDialogue()
        {
            _manager.StartDialogue(dialogue, startNodeId);
        }
        
        public void MakeChoice(int index)
        {
            _manager.MakeChoice(index);
        }
        
        private void OnDestroy()
        {
            _manager.Stop();
        }
    }
}
