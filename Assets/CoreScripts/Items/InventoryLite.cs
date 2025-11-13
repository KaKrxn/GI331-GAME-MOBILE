using System.Collections.Generic;
using UnityEngine;

public class InventoryLite : MonoBehaviour
{
    [System.Serializable]
    public class ItemStack
    {
        public string itemId;
        public ItemDefinition definition;
        public int quantity;

        public string DisplayName => definition ? definition.displayName : itemId;
    }

    [Header("Items (Debug / Inspector View)")]
    [SerializeField] private List<ItemStack> items = new List<ItemStack>();

    private Dictionary<string, ItemStack> lookup = new Dictionary<string, ItemStack>();

    private void Awake()
    {
        RebuildLookup();
    }

    private void RebuildLookup()
    {
        lookup.Clear();
        foreach (var stack in items)
        {
            if (stack == null) continue;
            if (string.IsNullOrEmpty(stack.itemId)) continue;

            if (!lookup.ContainsKey(stack.itemId))
                lookup.Add(stack.itemId, stack);
        }
    }

    public bool HasItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;
        return lookup.ContainsKey(itemId) && lookup[itemId].quantity > 0;
    }

    public int GetQuantity(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return 0;
        if (lookup.TryGetValue(itemId, out var stack))
            return stack.quantity;
        return 0;
    }

    public void AddItem(ItemDefinition def, int amount = 1)
    {
        if (!def) return;
        if (amount <= 0) return;

        string id = def.itemId;
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning($"[InventoryLite] ItemDefinition {def.name} ยังไม่ได้ตั้ง itemId");
            return;
        }

        if (!lookup.TryGetValue(id, out var stack))
        {
            stack = new ItemStack
            {
                itemId = id,
                definition = def,
                quantity = 0
            };
            items.Add(stack);
            lookup.Add(id, stack);
        }
        else if (!stack.definition)
        {
            stack.definition = def;
        }

        if (def.stackable)
        {
            stack.quantity += amount;
            if (def.maxStack > 0)
                stack.quantity = Mathf.Min(stack.quantity, def.maxStack);
        }
        else
        {
            // ไม่ซ้อน stack → บวกทีละ 1 แต่เกิน 1 ก็ถือว่าเป็นจำนวนชิ้นอยู่ดี
            stack.quantity += amount;
        }

        // แจ้งให้ GameData รู้ว่ามีไอเทมนี้แล้ว (ใช้กับระบบ Quest / Npc)
        if (GameData.Instance != null)
        {
            GameData.Instance.RegisterItemCollected(id);
        }
    }

    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
        if (!lookup.TryGetValue(itemId, out var stack)) return false;

        if (stack.quantity < amount) return false;

        stack.quantity -= amount;
        if (stack.quantity <= 0)
        {
            stack.quantity = 0;
            // ถ้าอยากให้หายจาก list เลยก็ได้ แต่ตอนนี้ขอเก็บไว้เผื่อ UI ใช้
        }

        return true;
    }

    // ถ้าอยากดึง list ทั้งหมดไปใช้กับ UI
    public IReadOnlyList<ItemStack> GetAllStacks()
    {
        return items;
    }
}
