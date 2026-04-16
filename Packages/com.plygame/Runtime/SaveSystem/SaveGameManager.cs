using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PlyGame.Runtime.Core.Serialization;
using UnityEngine;

namespace PlyGame.Runtime.SaveSystem
{
    /// <summary>
    /// Менеджер системы сохранений игры
    /// </summary>
    public class SaveGameManager
    {
        private const string SAVE_FOLDER = "SaveGames";
        private const string METADATA_FILE = "save_metadata.json";
        
        private readonly string _savePath;
        private SaveMetadata _metadata;
        
        public event Action<string> OnSaveCreated;
        public event Action<string> OnSaveLoaded;
        public event Action<string> OnSaveDeleted;
        
        public SaveGameManager()
        {
            _savePath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            if (!Directory.Exists(_savePath))
                Directory.CreateDirectory(_savePath);
            
            LoadMetadata();
        }
        
        /// <summary>
        /// Загрузка метаданных сохранений
        /// </summary>
        private async void LoadMetadata()
        {
            var metadataPath = Path.Combine(_savePath, METADATA_FILE);
            _metadata = await SerializationService.LoadFromFileAsync<SaveMetadata>(metadataPath);
            
            if (_metadata == null)
                _metadata = new SaveMetadata();
        }
        
        /// <summary>
        /// Сохранение метаданных
        /// </summary>
        private async Task SaveMetadataAsync()
        {
            var metadataPath = Path.Combine(_savePath, METADATA_FILE);
            await SerializationService.SaveToFileAsync(_metadata, metadataPath);
        }
        
        /// <summary>
        /// Создание нового сохранения
        /// </summary>
        public async Task<bool> SaveGameAsync(string slotName, GameSaveData data)
        {
            try
            {
                var filePath = GetSlotFilePath(slotName);
                await SerializationService.SaveToFileAsync(data, filePath);
                
                // Обновление метаданных
                var slotInfo = _metadata.Slots.Find(s => s.SlotName == slotName) ?? new SaveSlotInfo { SlotName = slotName };
                slotInfo.LastModified = DateTime.Now;
                slotInfo.PlayTime = data.TotalPlayTime;
                slotInfo.Description = data.Description;
                
                if (!_metadata.Slots.Exists(s => s.SlotName == slotName))
                    _metadata.Slots.Add(slotInfo);
                
                await SaveMetadataAsync();
                
                Debug.Log($"Игра сохранена в слот: {slotName}");
                OnSaveCreated?.Invoke(slotName);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка сохранения: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Загрузка сохранения
        /// </summary>
        public async Task<GameSaveData> LoadGameAsync(string slotName)
        {
            try
            {
                var filePath = GetSlotFilePath(slotName);
                
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"Сохранение не найдено: {slotName}");
                    return null;
                }
                
                var data = await SerializationService.LoadFromFileAsync<GameSaveData>(filePath);
                
                // Обновление метаданных
                var slotInfo = _metadata.Slots.Find(s => s.SlotName == slotName);
                if (slotInfo != null)
                {
                    slotInfo.LastLoaded = DateTime.Now;
                    await SaveMetadataAsync();
                }
                
                Debug.Log($"Игра загружена из слота: {slotName}");
                OnSaveLoaded?.Invoke(slotName);
                
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка загрузки: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Удаление сохранения
        /// </summary>
        public bool DeleteSave(string slotName)
        {
            try
            {
                var filePath = GetSlotFilePath(slotName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    
                    _metadata.Slots.RemoveAll(s => s.SlotName == slotName);
                    SaveMetadataAsync().Forget();
                    
                    Debug.Log($"Сохранение удалено: {slotName}");
                    OnSaveDeleted?.Invoke(slotName);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Ошибка удаления: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Проверка существования сохранения
        /// </summary>
        public bool HasSave(string slotName)
        {
            return File.Exists(GetSlotFilePath(slotName));
        }
        
        /// <summary>
        /// Получить список всех сохранений
        /// </summary>
        public List<SaveSlotInfo> GetAllSaveSlots()
        {
            return new List<SaveSlotInfo>(_metadata.Slots);
        }
        
        /// <summary>
        /// Получить путь к файлу сохранения
        /// </summary>
        private string GetSlotFilePath(string slotName)
        {
            var safeName = string.Join("_", slotName.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_savePath, $"{safeName}.json");
        }
    }
    
    /// <summary>
    /// Данные сохранения игры
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public string SaveVersion = "1.0";
        public DateTime SaveTime;
        public float TotalPlayTime;
        public string Description;
        public string SceneName;
        public Dictionary<string, object> Variables;
        public Dictionary<string, object> CustomData;
        public PlayerSaveData PlayerData;
        public List<QuestSaveData> Quests;
        public List<ItemSaveData> Inventory;
        
        public GameSaveData()
        {
            SaveTime = DateTime.Now;
            Variables = new Dictionary<string, object>();
            CustomData = new Dictionary<string, object>();
            Quests = new List<QuestSaveData>();
            Inventory = new List<ItemSaveData>();
        }
    }
    
    /// <summary>
    /// Данные игрока для сохранения
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        public string PlayerName;
        public int Level;
        public float Experience;
        public int Health;
        public int MaxHealth;
        public int Mana;
        public int MaxMana;
        public Vector3 Position;
        public Quaternion Rotation;
        public Dictionary<string, int> Stats;
        public List<string> Skills;
        
        public PlayerSaveData()
        {
            Stats = new Dictionary<string, int>();
            Skills = new List<string>();
        }
    }
    
    /// <summary>
    /// Данные квеста для сохранения
    /// </summary>
    [Serializable]
    public class QuestSaveData
    {
        public string QuestId;
        public string QuestName;
        public QuestStatus Status;
        public Dictionary<string, bool> Objectives;
        public DateTime StartTime;
        public DateTime? CompletionTime;
    }
    
    /// <summary>
    /// Данные предмета для сохранения
    /// </summary>
    [Serializable]
    public class ItemSaveData
    {
        public string ItemId;
        public string ItemName;
        public int Quantity;
        public int Durability;
        public Dictionary<string, object> Modifiers;
    }
    
    /// <summary>
    /// Метаданные сохранений
    /// </summary>
    [Serializable]
    public class SaveMetadata
    {
        public List<SaveSlotInfo> Slots = new();
    }
    
    /// <summary>
    /// Информация о слоте сохранения
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        public string SlotName;
        public string Description;
        public DateTime LastModified;
        public DateTime? LastLoaded;
        public float PlayTime;
    }
    
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
    /// Extension method для忘记 Task
    /// </summary>
    public static class TaskExtensions
    {
        public static async void Forget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Background task error: {ex.Message}");
            }
        }
    }
}
