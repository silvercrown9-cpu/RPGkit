using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlyGame.Runtime.Dialogue
{
    /// <summary>
    /// Данные диалоговой реплики
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        public string speakerId;
        public string text;
        public string audioClipId;
        public float duration = 3f;
        public Dictionary<string, object> metadata;
        
        public DialogueLine()
        {
            metadata = new Dictionary<string, object>();
        }
        
        public DialogueLine(string speaker, string dialogueText) : this()
        {
            speakerId = speaker;
            text = dialogueText;
        }
    }

    /// <summary>
    /// Данные диалогового узла
    /// </summary>
    [Serializable]
    public class DialogueNodeData
    {
        public string nodeId;
        public List<DialogueLine> lines;
        public List<DialogueChoice> choices;
        public string nextNodeId;
        public bool isEndNode;
        
        public DialogueNodeData()
        {
            lines = new List<DialogueLine>();
            choices = new List<DialogueChoice>();
        }
    }

    /// <summary>
    /// Вариант ответа в диалоге
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        public string text;
        public string targetNodeId;
        public string conditionVariable;
        public object requiredValue;
        public bool hasCondition;
        
        public DialogueChoice() { }
        
        public DialogueChoice(string choiceText, string targetNode)
        {
            text = choiceText;
            targetNodeId = targetNode;
            hasCondition = false;
        }
        
        public DialogueChoice WithCondition(string variable, object value)
        {
            conditionVariable = variable;
            requiredValue = value;
            hasCondition = true;
            return this;
        }
    }

    /// <summary>
    /// Менеджер диалогов
    /// </summary>
    public class DialogueManager
    {
        private readonly Dictionary<string, DialogueNodeData> dialogueNodes = new();
        private DialogueNodeData currentNode;
        private int currentLineIndex;
        
        public event Action<DialogueLine> OnLineStarted;
        public event Action<DialogueLine> OnLineEnded;
        public event Action<List<DialogueChoice>> OnChoicesAvailable;
        public event Action OnDialogueEnded;
        
        public bool IsDialogueActive => currentNode != null;
        public DialogueNodeData CurrentNode => currentNode;
        
        /// <summary>
        /// Загрузить диалог из графа
        /// </summary>
        public void LoadDialogue(GraphAsset graphAsset)
        {
            dialogueNodes.Clear();
            
            foreach (var nodeData in graphAsset.Nodes)
            {
                if (nodeData.Type == "DialogueNode" || nodeData.Type == "Dialogue")
                {
                    var dialogueData = CreateDialogueDataFromNode(nodeData);
                    dialogueNodes[dialogueData.nodeId] = dialogueData;
                }
            }
        }
        
        /// <summary>
        /// Начать диалог с указанного узла
        /// </summary>
        public void StartDialogue(string startNodeId)
        {
            if (!dialogueNodes.ContainsKey(startNodeId))
            {
                Debug.LogError($"Dialogue node '{startNodeId}' not found");
                return;
            }
            
            currentNode = dialogueNodes[startNodeId];
            currentLineIndex = 0;
            
            PlayCurrentLine();
        }
        
        /// <summary>
        /// Продолжить диалог
        /// </summary>
        public void Continue()
        {
            if (currentNode == null) return;
            
            currentLineIndex++;
            
            if (currentLineIndex >= currentNode.lines.Count)
            {
                // Показываем выборы или переходим к следующему узлу
                if (currentNode.choices.Count > 0)
                {
                    ShowChoices();
                }
                else if (!string.IsNullOrEmpty(currentNode.nextNodeId))
                {
                    GoToNode(currentNode.nextNodeId);
                }
                else if (currentNode.isEndNode)
                {
                    EndDialogue();
                }
            }
            else
            {
                PlayCurrentLine();
            }
        }
        
        /// <summary>
        /// Выбрать вариант ответа
        /// </summary>
        public void SelectChoice(int choiceIndex)
        {
            if (currentNode == null || choiceIndex >= currentNode.choices.Count)
            {
                Debug.LogError("Invalid choice selection");
                return;
            }
            
            var choice = currentNode.choices[choiceIndex];
            GoToNode(choice.targetNodeId);
        }
        
        /// <summary>
        /// Проверить доступность выбора
        /// </summary>
        public bool IsChoiceAvailable(DialogueChoice choice, Variables.VariableManager variableManager)
        {
            if (!choice.hasCondition) return true;
            
            if (variableManager == null || !variableManager.HasVariable(choice.conditionVariable))
            {
                return false;
            }
            
            var currentValue = variableManager.GetValue<object>(choice.conditionVariable);
            return Equals(currentValue, choice.requiredValue);
        }
        
        private void PlayCurrentLine()
        {
            if (currentNode == null || currentLineIndex >= currentNode.lines.Count)
            {
                EndDialogue();
                return;
            }
            
            var line = currentNode.lines[currentLineIndex];
            OnLineStarted?.Invoke(line);
            
            // Здесь должна быть логика воспроизведения аудио и тайминга
            UnityEngine.MonoBehaviour.StartCoroutine(WaitForLine(line));
        }
        
        private UnityEngine.Coroutine WaitForLine(DialogueLine line)
        {
            var yield = new UnityEngine.WaitForSeconds(line.duration);
            var routine = UnityEngine.Object.FindObjectOfType<DialogueRunner>();
            if (routine != null)
            {
                return routine.StartCoroutine(WaitAndContinue(yield, line));
            }
            return null;
        }
        
        private System.Collections.IEnumerator WaitAndContinue(UnityEngine.YieldInstruction wait, DialogueLine line)
        {
            yield return wait;
            OnLineEnded?.Invoke(line);
            Continue();
        }
        
        private void ShowChoices()
        {
            var availableChoices = new List<DialogueChoice>();
            foreach (var choice in currentNode.choices)
            {
                // Упрощённая проверка - в реальном проекте использовать VariableManager
                availableChoices.Add(choice);
            }
            
            OnChoicesAvailable?.Invoke(availableChoices);
        }
        
        private void GoToNode(string nodeId)
        {
            if (!dialogueNodes.ContainsKey(nodeId))
            {
                Debug.LogError($"Dialogue node '{nodeId}' not found");
                EndDialogue();
                return;
            }
            
            currentNode = dialogueNodes[nodeId];
            currentLineIndex = 0;
            PlayCurrentLine();
        }
        
        private void EndDialogue()
        {
            currentNode = null;
            currentLineIndex = 0;
            OnDialogueEnded?.Invoke();
        }
        
        private DialogueNodeData CreateDialogueDataFromNode(GraphAsset.NodeData nodeData)
        {
            var dialogueData = new DialogueNodeData
            {
                nodeId = nodeData.Id,
                nextNodeId = nodeData.Connections.Count > 0 ? nodeData.Connections[0] : null,
                isEndNode = nodeData.Connections.Count == 0
            };
            
            // Парсим данные из JSON сериализованных полей узла
            if (!string.IsNullOrEmpty(nodeData.SerializedData))
            {
                try
                {
                    var data = UnityEngine.JsonUtility.FromJson<DialogueNodeSerialized>(nodeData.SerializedData);
                    
                    if (data != null)
                    {
                        foreach (var lineData in data.lines)
                        {
                            dialogueData.lines.Add(new DialogueLine(lineData.speaker, lineData.text)
                            {
                                duration = lineData.duration
                            });
                        }
                        
                        foreach (var choiceData in data.choices)
                        {
                            dialogueData.choices.Add(new DialogueChoice(choiceData.text, choiceData.targetNodeId));
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse dialogue data: {e.Message}");
                }
            }
            
            return dialogueData;
        }
        
        [Serializable]
        private class DialogueNodeSerialized
        {
            public LineData[] lines;
            public ChoiceData[] choices;
        }
        
        [Serializable]
        private class LineData
        {
            public string speaker;
            public string text;
            public float duration;
        }
        
        [Serializable]
        private class ChoiceData
        {
            public string text;
            public string targetNodeId;
        }
    }
    
    /// <summary>
    /// Компонент-раннер для диалогов
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        private DialogueManager dialogueManager;
        
        private void Awake()
        {
            dialogueManager = new DialogueManager();
        }
        
        public void StartDialogueFromGraph(GraphAsset graphAsset, string startNodeId = null)
        {
            dialogueManager.LoadDialogue(graphAsset);
            
            if (string.IsNullOrEmpty(startNodeId))
            {
                // Найти первый узел диалога
                foreach (var node in graphAsset.Nodes)
                {
                    if (node.Type == "DialogueNode" || node.Type == "Dialogue")
                    {
                        startNodeId = node.Id;
                        break;
                    }
                }
            }
            
            if (!string.IsNullOrEmpty(startNodeId))
            {
                dialogueManager.StartDialogue(startNodeId);
            }
        }
        
        public void ContinueDialogue()
        {
            dialogueManager.Continue();
        }
        
        public void SelectDialogueChoice(int index)
        {
            dialogueManager.SelectChoice(index);
        }
    }
}
