using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PlyGame.Runtime.SaveSystem
{
    /// <summary>
    /// Данные сохранения игры
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public string saveName;
        public long timestamp;
        public int playTimeSeconds;
        public string sceneName;
        public Dictionary<string, object> variables;
        public Dictionary<string, object> customData;
        
        public SaveData()
        {
            variables = new Dictionary<string, object>();
            customData = new Dictionary<string, object>();
        }
        
        public SaveData(string name) : this()
        {
            saveName = name;
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    /// <summary>
    /// Менеджер сохранений
    /// </summary>
    public class SaveGameManager
    {
        private readonly string saveDirectory;
        private readonly SerializationService serializationService;
        private SaveData currentSave;
        
        public event Action<SaveData> OnSaveCreated;
        public event Action<SaveData> OnSaveLoaded;
        public event Action<string> OnSaveDeleted;
        
        public SaveData CurrentSave => currentSave;
        public bool HasActiveSave => currentSave != null;
        
        public SaveGameManager(string directory = "Saves")
        {
            saveDirectory = System.IO.Path.Combine(Application.persistentDataPath, directory);
            serializationService = new SerializationService();
            
            if (!System.IO.Directory.Exists(saveDirectory))
            {
                System.IO.Directory.CreateDirectory(saveDirectory);
            }
        }
        
        /// <summary>
        /// Создать новое сохранение
        /// </summary>
        public async Task<bool> CreateNewSave(string saveName, CancellationToken cancellationToken = default)
        {
            try
            {
                currentSave = new SaveData(saveName);
                currentSave.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                currentSave.playTimeSeconds = 0;
                
                await SaveToFile(cancellationToken);
                OnSaveCreated?.Invoke(currentSave);
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create save: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Сохранить текущее состояние
        /// </summary>
        public async Task<bool> Save(CancellationToken cancellationToken = default)
        {
            if (currentSave == null)
            {
                Debug.LogError("No active save to update");
                return false;
            }
            
            try
            {
                // Обновляем время игры и сцену
                currentSave.playTimeSeconds += GetDeltaTime();
                currentSave.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                currentSave.timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                
                await SaveToFile(cancellationToken);
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Загрузить сохранение по имени
        /// </summary>
        public async Task<SaveData> LoadSave(string saveName, CancellationToken cancellationToken = default)
        {
            try
            {
                var filePath = GetSaveFilePath(saveName);
                
                if (!System.IO.File.Exists(filePath))
                {
                    Debug.LogWarning($"Save file '{saveName}' not found");
                    return null;
                }
                
                var json = await serializationService.ReadJsonAsync(filePath, cancellationToken);
                currentSave = serializationService.Deserialize<SaveData>(json);
                
                OnSaveLoaded?.Invoke(currentSave);
                
                return currentSave;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load save: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Удалить сохранение
        /// </summary>
        public bool DeleteSave(string saveName)
        {
            try
            {
                var filePath = GetSaveFilePath(saveName);
                
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    OnSaveDeleted?.Invoke(saveName);
                    return true;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete save: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Получить список всех сохранений
        /// </summary>
        public List<string> GetAllSaveNames()
        {
            var saves = new List<string>();
            
            if (System.IO.Directory.Exists(saveDirectory))
            {
                var files = System.IO.Directory.GetFiles(saveDirectory, "*.json");
                foreach (var file in files)
                {
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                    saves.Add(fileName);
                }
            }
            
            return saves;
        }
        
        /// <summary>
        /// Проверить существование сохранения
        /// </summary>
        public bool SaveExists(string saveName)
        {
            return System.IO.File.Exists(GetSaveFilePath(saveName));
        }
        
        /// <summary>
        /// Установить переменную в сохранении
        /// </summary>
        public void SetVariable(string name, object value)
        {
            if (currentSave == null)
            {
                Debug.LogError("No active save");
                return;
            }
            
            currentSave.variables[name] = value;
        }
        
        /// <summary>
        /// Получить переменную из сохранения
        /// </summary>
        public T GetVariable<T>(string name, T defaultValue = default)
        {
            if (currentSave == null || !currentSave.variables.ContainsKey(name))
            {
                return defaultValue;
            }
            
            try
            {
                return (T)Convert.ChangeType(currentSave.variables[name], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        /// <summary>
        /// Установить пользовательские данные
        /// </summary>
        public void SetCustomData(string key, object value)
        {
            if (currentSave == null) return;
            currentSave.customData[key] = value;
        }
        
        /// <summary>
        /// Получить пользовательские данные
        /// </summary>
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (currentSave == null || !currentSave.customData.ContainsKey(key))
            {
                return defaultValue;
            }
            
            try
            {
                return (T)Convert.ChangeType(currentSave.customData[key], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        private async Task SaveToFile(CancellationToken cancellationToken)
        {
            var filePath = GetSaveFilePath(currentSave.saveName);
            var json = serializationService.Serialize(currentSave);
            await serializationService.WriteJsonAsync(filePath, json, cancellationToken);
        }
        
        private string GetSaveFilePath(string saveName)
        {
            return System.IO.Path.Combine(saveDirectory, $"{saveName}.json");
        }
        
        private int GetDeltaTime()
        {
            // Заглушка - в реальном проекте использовать Time.time или аналог
            return 0;
        }
    }
}
