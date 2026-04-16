using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlyGame.Runtime.Skills
{
    /// <summary>
    /// Менеджер системы навыков и способностей
    /// </summary>
    public class SkillManager
    {
        private readonly Dictionary<string, SkillData> _skillsDatabase = new();
        private readonly Dictionary<string, PlayerSkill> _playerSkills = new();
        private readonly Dictionary<string, float> _cooldowns = new();
        
        public event Action<PlayerSkill> OnSkillLearned;
        public event Action<PlayerSkill> OnSkillUpgraded;
        public event Action<PlayerSkill> OnSkillUsed;
        public event Action<string, float> OnCooldownStarted;
        public event Action<string> OnCooldownEnded;
        
        /// <summary>
        /// Регистрация навыка в базе данных
        /// </summary>
        public void RegisterSkill(SkillData skill)
        {
            if (skill != null && !string.IsNullOrEmpty(skill.SkillId))
                _skillsDatabase[skill.SkillId] = skill;
        }
        
        /// <summary>
        /// Получить данные навыка
        /// </summary>
        public SkillData GetSkillData(string skillId)
        {
            return _skillsDatabase.TryGetValue(skillId, out var skill) ? skill : null;
        }
        
        /// <summary>
        /// Изучить навык
        /// </summary>
        public bool LearnSkill(string skillId)
        {
            var skillData = GetSkillData(skillId);
            if (skillData == null)
            {
                Debug.LogError($"Навык {skillId} не найден");
                return false;
            }
            
            if (_playerSkills.ContainsKey(skillId))
            {
                Debug.LogWarning($"Навык {skillId} уже изучен");
                return false;
            }
            
            _playerSkills[skillId] = new PlayerSkill
            {
                SkillId = skillId,
                Level = 1,
                Experience = 0
            };
            
            OnSkillLearned?.Invoke(_playerSkills[skillId]);
            return true;
        }
        
        /// <summary>
        /// Улучшить навык
        /// </summary>
        public bool UpgradeSkill(string skillId)
        {
            if (!_playerSkills.TryGetValue(skillId, out var playerSkill))
                return false;
            
            var skillData = GetSkillData(skillId);
            if (playerSkill.Level >= skillData.MaxLevel)
            {
                Debug.LogWarning($"Навык {skillId} достиг максимального уровня");
                return false;
            }
            
            playerSkill.Level++;
            OnSkillUpgraded?.Invoke(playerSkill);
            return true;
        }
        
        /// <summary>
        /// Использовать навык
        /// </summary>
        public bool UseSkill(string skillId, UnityEngine.Object target = null)
        {
            var skillData = GetSkillData(skillId);
            if (skillData == null) return false;
            
            if (!_playerSkills.ContainsKey(skillId))
            {
                Debug.LogWarning($"Навык {skillId} не изучен");
                return false;
            }
            
            if (IsOnCooldown(skillId))
            {
                Debug.LogWarning($"Навык {skillId} на перезарядке");
                return false;
            }
            
            // Проверка стоимости
            // Здесь должна быть проверка маны/ресурсов
            
            // Применение эффекта
            ApplySkillEffect(skillData, target);
            
            // Запуск кулдауна
            if (skillData.Cooldown > 0)
            {
                StartCooldown(skillId, skillData.Cooldown);
            }
            
            OnSkillUsed?.Invoke(_playerSkills[skillId]);
            return true;
        }
        
        /// <summary>
        /// Применение эффекта навыка
        /// </summary>
        private void ApplySkillEffect(SkillData skill, UnityEngine.Object target)
        {
            Debug.Log($"Использован навык: {skill.Name} (уровень {GetSkillLevel(skill.SkillId)})");
            
            // Здесь должна быть логика применения эффектов
            // В зависимости от типа навыка
        }
        
        /// <summary>
        /// Запуск перезарядки
        /// </summary>
        private async void StartCooldown(string skillId, float duration)
        {
            _cooldowns[skillId] = Time.time + duration;
            OnCooldownStarted?.Invoke(skillId, duration);
            
            await new WaitForSeconds(duration);
            
            _cooldowns.Remove(skillId);
            OnCooldownEnded?.Invoke(skillId);
        }
        
        /// <summary>
        /// Проверка нахождения на перезарядке
        /// </summary>
        public bool IsOnCooldown(string skillId)
        {
            if (!_cooldowns.TryGetValue(skillId, out var cooldownEnd))
                return false;
            
            return Time.time < cooldownEnd;
        }
        
        /// <summary>
        /// Получить оставшееся время кулдауна
        /// </summary>
        public float GetRemainingCooldown(string skillId)
        {
            if (!_cooldowns.TryGetValue(skillId, out var cooldownEnd))
                return 0;
            
            return Mathf.Max(0, cooldownEnd - Time.time);
        }
        
        /// <summary>
        /// Получить уровень навыка
        /// </summary>
        public int GetSkillLevel(string skillId)
        {
            return _playerSkills.TryGetValue(skillId, out var skill) ? skill.Level : 0;
        }
        
        /// <summary>
        /// Получить все изученные навыки
        /// </summary>
        public List<PlayerSkill> GetAllSkills()
        {
            return new List<PlayerSkill>(_playerSkills.Values);
        }
        
        /// <summary>
        /// Проверить изучен ли навык
        /// </summary>
        public bool HasSkill(string skillId)
        {
            return _playerSkills.ContainsKey(skillId);
        }
    }
    
    /// <summary>
    /// Данные навыка
    /// </summary>
    [Serializable]
    public class SkillData
    {
        public string SkillId;
        public string Name;
        public string Description;
        public SkillType Type;
        public Sprite Icon;
        public int MaxLevel = 5;
        public float Cooldown;
        public int ManaCost;
        public int StaminaCost;
        public List<SkillEffect> Effects = new();
        public List<string> Prerequisites = new();
        public SkillTargetType TargetType;
        public float Range;
        public float AreaOfEffect;
    }
    
    /// <summary>
    /// Тип навыка
    /// </summary>
    public enum SkillType
    {
        Active,     // Активный навык
        Passive,    // Пассивный навык
        Toggle      // Переключаемый навык
    }
    
    /// <summary>
    /// Тип цели навыка
    /// </summary>
    public enum SkillTargetType
    {
        Self,
        SingleTarget,
        Area,
        Cone,
        Line,
        Location
    }
    
    /// <summary>
    /// Эффект навыка
    /// </summary>
    [Serializable]
    public class SkillEffect
    {
        public EffectType Type;
        public float Value;
        public float Duration;
        public int TickCount;
        public float TickInterval;
        public string VariableId;
    }
    
    /// <summary>
    /// Тип эффекта
    /// </summary>
    public enum EffectType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Spawn,
        Teleport,
        ModifyVariable
    }
    
    /// <summary>
    /// Навык игрока
    /// </summary>
    [Serializable]
    public class PlayerSkill
    {
        public string SkillId;
        public int Level;
        public float Experience;
    }
}
