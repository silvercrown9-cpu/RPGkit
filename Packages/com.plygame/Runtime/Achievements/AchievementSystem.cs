using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlyGame.Achievements
{
    /// <summary>
    /// Менеджер системы достижений
    /// </summary>
    public class AchievementManager
    {
        private readonly Dictionary<string, AchievementData> _achievementsDatabase = new();
        private readonly Dictionary<string, PlayerAchievement> _playerAchievements = new();
        
        public event Action<PlayerAchievement> OnAchievementUnlocked;
        public event Action<PlayerAchievement, float> OnAchievementProgressUpdated;
        
        /// <summary>
        /// Регистрация достижения в базе данных
        /// </summary>
        public void RegisterAchievement(AchievementData achievement)
        {
            if (achievement != null && !string.IsNullOrEmpty(achievement.AchievementId))
                _achievementsDatabase[achievement.AchievementId] = achievement;
        }
        
        /// <summary>
        /// Получить данные достижения
        /// </summary>
        public AchievementData GetAchievementData(string achievementId)
        {
            return _achievementsDatabase.TryGetValue(achievementId, out var achievement) ? achievement : null;
        }
        
        /// <summary>
        /// Обновить прогресс достижения
        /// </summary>
        public bool UpdateProgress(string achievementId, float progress)
        {
            if (!_achievementsDatabase.TryGetValue(achievementId, out var achievementData))
                return false;
            
            // Если достижение уже разблокировано, не обновляем
            if (_playerAchievements.TryGetValue(achievementId, out var playerAchievement) && 
                playerAchievement.IsUnlocked)
                return false;
            
            if (!_playerAchievements.ContainsKey(achievementId))
            {
                _playerAchievements[achievementId] = new PlayerAchievement
                {
                    AchievementId = achievementId,
                    Progress = 0,
                    IsUnlocked = false
                };
            }
            
            var pa = _playerAchievements[achievementId];
            pa.Progress = Mathf.Min(progress, achievementData.TargetAmount);
            
            OnAchievementProgressUpdated?.Invoke(pa, pa.Progress / achievementData.TargetAmount * 100f);
            
            // Проверка разблокировки
            if (pa.Progress >= achievementData.TargetAmount)
            {
                UnlockAchievement(achievementId);
            }
            
            return true;
        }
        
        /// <summary>
        /// Увеличить прогресс достижения
        /// </summary>
        public bool IncrementProgress(string achievementId, float amount = 1)
        {
            if (!_playerAchievements.TryGetValue(achievementId, out var pa))
            {
                pa = new PlayerAchievement
                {
                    AchievementId = achievementId,
                    Progress = 0,
                    IsUnlocked = false
                };
                _playerAchievements[achievementId] = pa;
            }
            
            return UpdateProgress(achievementId, pa.Progress + amount);
        }
        
        /// <summary>
        /// Разблокировать достижение
        /// </summary>
        private void UnlockAchievement(string achievementId)
        {
            if (!_playerAchievements.ContainsKey(achievementId))
            {
                _playerAchievements[achievementId] = new PlayerAchievement
                {
                    AchievementId = achievementId,
                    Progress = 0,
                    IsUnlocked = false
                };
            }
            
            var pa = _playerAchievements[achievementId];
            
            if (!pa.IsUnlocked)
            {
                pa.IsUnlocked = true;
                pa.UnlockDate = DateTime.Now;
                
                Debug.Log($"Достижение разблокировано: {GetAchievementData(achievementId)?.Name}");
                OnAchievementUnlocked?.Invoke(pa);
            }
        }
        
        /// <summary>
        /// Получить прогресс достижения
        /// </summary>
        public float GetProgress(string achievementId)
        {
            return _playerAchievements.TryGetValue(achievementId, out var pa) ? pa.Progress : 0;
        }
        
        /// <summary>
        /// Проверить разблокировано ли достижение
        /// </summary>
        public bool IsUnlocked(string achievementId)
        {
            return _playerAchievements.TryGetValue(achievementId, out var pa) && pa.IsUnlocked;
        }
        
        /// <summary>
        /// Получить все разблокированные достижения
        /// </summary>
        public List<PlayerAchievement> GetUnlockedAchievements()
        {
            return _playerAchievements.Values.FindAll(pa => pa.IsUnlocked);
        }
        
        /// <summary>
        /// Получить все заблокированные достижения
        /// </summary>
        public List<PlayerAchievement> GetLockedAchievements()
        {
            return _playerAchievements.Values.FindAll(pa => !pa.IsUnlocked);
        }
        
        /// <summary>
        /// Получить процент разблокированных достижений
        /// </summary>
        public float GetCompletionPercentage()
        {
            if (_achievementsDatabase.Count == 0) return 0;
            
            var unlockedCount = _playerAchievements.Values.Count(pa => pa.IsUnlocked);
            return (float)unlockedCount / _achievementsDatabase.Count * 100f;
        }
    }
    
    /// <summary>
    /// Данные достижения
    /// </summary>
    [Serializable]
    public class AchievementData
    {
        public string AchievementId;
        public string Name;
        public string Description;
        public Sprite Icon;
        public AchievementType Type;
        public float TargetAmount;
        public int Points;
        public bool IsHidden;
        public List<string> Prerequisites = new();
        public string Category;
    }
    
    /// <summary>
    /// Тип достижения
    /// </summary>
    public enum AchievementType
    {
        KillCount,          // Убийство врагов
        CollectItems,       // Сбор предметов
        CompleteQuests,     // Выполнение квестов
        ReachLevel,         // Достижение уровня
        DealDamage,         // Нанесение урона
        HealAmount,         // Лечение
        ExploreAreas,       // Исследование локаций
        CraftItems,         // Создание предметов
        WinBattles,         // Победы в боях
        Custom              // Пользовательский тип
    }
    
    /// <summary>
    /// Достижение игрока
    /// </summary>
    [Serializable]
    public class PlayerAchievement
    {
        public string AchievementId;
        public float Progress;
        public bool IsUnlocked;
        public DateTime? UnlockDate;
    }
}
