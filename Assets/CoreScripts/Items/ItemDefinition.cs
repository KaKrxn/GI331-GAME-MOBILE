using UnityEngine;

public enum ItemCategory
{
    Quest,
    Misc,
    Currency
}

[CreateAssetMenu(menuName = "Game/Item Definition", fileName = "NewItem")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("ID ที่ใช้เป็นตัวแทนไอเทมนี้ในระบบ (ต้องไม่ซ้ำกัน)")]
    public string itemId;

    public string displayName;
    [TextArea]
    public string description;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Category & Stack")]
    public ItemCategory category = ItemCategory.Misc;

    [Tooltip("สามารถซ้อนจำนวนในช่องเดียวกันได้ไหม")]
    public bool stackable = true;

    [Tooltip("จำนวนสูงสุดที่ซ้อนใน 1 Stack ได้")]
    public int maxStack = 99;

    [Header("Audio")]
    [Tooltip("เสียงตอนเก็บไอเทมนี้ (ถ้าไม่ใส่จะไม่เล่น)")]
    public AudioClip pickupSfx;
}
