using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlyGame.Runtime.Inventory
{
    /// <summary>
    /// Менеджер системы инвентаря
    /// </summary>
    public class InventoryManager
    {
        private readonly Dictionary<string, ItemData> _itemsDatabase = new();
        private readonly List<InventorySlot> _slots = new();
        private readonly int _maxSlots;
        
        public event Action<InventorySlot> OnSlotChanged;
        public event Action<InventorySlot, int> OnItemAdded;
        public event Action<InventorySlot, int> OnItemRemoved;
        public event Action OnInventoryUpdated;
        
        public int MaxSlots => _maxSlots;
        public IReadOnlyList<InventorySlot> Slots => _slots.AsReadOnly();
        
        public InventoryManager(int maxSlots = 30)
        {
            _maxSlots = maxSlots;
            
            for (int i = 0; i < maxSlots; i++)
            {
                _slots.Add(new InventorySlot(i));
            }
        }
        
        /// <summary>
        /// Регистрация предмета в базе данных
        /// </summary>
        public void RegisterItem(ItemData item)
        {
            if (item != null && !string.IsNullOrEmpty(item.ItemId))
                _itemsDatabase[item.ItemId] = item;
        }
        
        /// <summary>
        /// Получить предмет из базы данных
        /// </summary>
        public ItemData GetItemData(string itemId)
        {
            return _itemsDatabase.TryGetValue(itemId, out var item) ? item : null;
        }
        
        /// <summary>
        /// Добавить предмет в инвентарь
        /// </summary>
        public bool AddItem(string itemId, int quantity = 1)
        {
            var itemData = GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogError($"Предмет {itemId} не найден в базе данных");
                return false;
            }
            
            if (itemData.IsStackable)
            {
                // Попытка стакнуть с существующими
                foreach (var slot in _slots.Where(s => s.ItemId == itemId && s.Quantity < itemData.MaxStackSize))
                {
                    var canAdd = Mathf.Min(quantity, itemData.MaxStackSize - slot.Quantity);
                    slot.Quantity += canAdd;
                    quantity -= canAdd;
                    
                    OnSlotChanged?.Invoke(slot);
                    OnItemAdded?.Invoke(slot, canAdd);
                    
                    if (quantity <= 0)
                    {
                        OnInventoryUpdated?.Invoke();
                        return true;
                    }
                }
            }
            
            // Поиск пустых слотов
            while (quantity > 0)
            {
                var emptySlot = _slots.FirstOrDefault(s => string.IsNullOrEmpty(s.ItemId));
                if (emptySlot == null)
                {
                    Debug.LogWarning("Инвентарь полон");
                    return false;
                }
                
                var addQuantity = itemData.IsStackable 
                    ? Mathf.Min(quantity, itemData.MaxStackSize) 
                    : 1;
                
                emptySlot.ItemId = itemId;
                emptySlot.Quantity = addQuantity;
                quantity -= addQuantity;
                
                OnSlotChanged?.Invoke(emptySlot);
                OnItemAdded?.Invoke(emptySlot, addQuantity);
            }
            
            OnInventoryUpdated?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Удалить предмет из инвентаря
        /// </summary>
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var totalRemoved = 0;
            
            foreach (var slot in _slots.Where(s => s.ItemId == itemId).ToList())
            {
                var removeAmount = Mathf.Min(quantity - totalRemoved, slot.Quantity);
                slot.Quantity -= removeAmount;
                totalRemoved += removeAmount;
                
                OnSlotChanged?.Invoke(slot);
                OnItemRemoved?.Invoke(slot, removeAmount);
                
                if (slot.Quantity <= 0)
                {
                    slot.ItemId = null;
                    slot.Quantity = 0;
                }
                
                if (totalRemoved >= quantity)
                    break;
            }
            
            if (totalRemoved < quantity)
            {
                Debug.LogWarning($"Не удалось удалить {quantity - totalRemoved} предметов {itemId}");
                return false;
            }
            
            OnInventoryUpdated?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Проверить наличие предмета
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            return _slots.Where(s => s.ItemId == itemId).Sum(s => s.Quantity) >= quantity;
        }
        
        /// <summary>
        /// Получить количество предмета
        /// </summary>
        public int GetItemCount(string itemId)
        {
            return _slots.Where(s => s.ItemId == itemId).Sum(s => s.Quantity);
        }
        
        /// <summary>
        /// Переместить предмет между слотами
        /// </summary>
        public bool MoveItem(int fromSlotIndex, int toSlotIndex, int quantity = -1)
        {
            if (fromSlotIndex < 0 || fromSlotIndex >= _maxSlots ||
                toSlotIndex < 0 || toSlotIndex >= _maxSlots)
                return false;
            
            var fromSlot = _slots[fromSlotIndex];
            var toSlot = _slots[toSlotIndex];
            
            if (string.IsNullOrEmpty(fromSlot.ItemId))
                return false;
            
            var itemData = GetItemData(fromSlot.ItemId);
            if (itemData == null) return false;
            
            var moveQuantity = quantity == -1 ? fromSlot.Quantity : Mathf.Min(quantity, fromSlot.Quantity);
            
            // Если целевой слот пустой или содержит тот же предмет
            if (string.IsNullOrEmpty(toSlot.ItemId) || toSlot.ItemId == fromSlot.ItemId)
            {
                if (!string.IsNullOrEmpty(toSlot.ItemId))
                {
                    // Проверка максимального стака
                    if (toSlot.Quantity + moveQuantity > itemData.MaxStackSize)
                        return false;
                    
                    toSlot.Quantity += moveQuantity;
                }
                else
                {
                    toSlot.ItemId = fromSlot.ItemId;
                    toSlot.Quantity = moveQuantity;
                }
                
                fromSlot.Quantity -= moveQuantity;
                if (fromSlot.Quantity <= 0)
                {
                    fromSlot.ItemId = null;
                }
                
                OnSlotChanged?.Invoke(fromSlot);
                OnSlotChanged?.Invoke(toSlot);
                OnInventoryUpdated?.Invoke();
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Очистить инвентарь
        /// </summary>
        public void Clear()
        {
            foreach (var slot in _slots)
            {
                slot.ItemId = null;
                slot.Quantity = 0;
            }
            OnInventoryUpdated?.Invoke();
        }
        
        /// <summary>
        /// Сохранение инвентаря
        /// </summary>
        public List<ItemSaveData> Save()
        {
            return _slots
                .Where(s => !string.IsNullOrEmpty(s.ItemId))
                .Select(s => new ItemSaveData
                {
                    ItemId = s.ItemId,
                    Quantity = s.Quantity,
                    SlotIndex = s.Index
                })
                .ToList();
        }
        
        /// <summary>
        /// Загрузка инвентаря
        /// </summary>
        public void Load(List<ItemSaveData> data)
        {
            Clear();
            
            if (data == null) return;
            
            foreach (var itemData in data)
            {
                if (itemData.SlotIndex >= 0 && itemData.SlotIndex < _maxSlots)
                {
                    var slot = _slots[itemData.SlotIndex];
                    slot.ItemId = itemData.ItemId;
                    slot.Quantity = itemData.Quantity;
                }
            }
            
            OnInventoryUpdated?.Invoke();
        }
    }
    
    /// <summary>
    /// Слот инвентаря
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public int Index;
        public string ItemId;
        public int Quantity;
        
        public InventorySlot(int index)
        {
            Index = index;
        }
        
        public bool IsEmpty => string.IsNullOrEmpty(ItemId);
    }
    
    /// <summary>
    /// Данные предмета
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public string ItemId;
        public string Name;
        public string Description;
        public Sprite Icon;
        public ItemType Type;
        public Rarity Rarity;
        public int Value;
        public int Weight;
        public bool IsStackable;
        public int MaxStackSize = 99;
        public int Durability;
        public int MaxDurability;
        public Dictionary<string, object> Properties = new();
    }
    
    /// <summary>
    /// Тип предмета
    /// </summary>
    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        Material,
        Quest,
        Key,
        Misc
    }
    
    /// <summary>
    /// Редкость предмета
    /// </summary>
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Artifact
    }
}
