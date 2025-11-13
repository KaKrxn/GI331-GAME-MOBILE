using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour
{
    [Header("Item")]
    public ItemDefinition itemDefinition;
    [Min(1)] public int amount = 1;

    [Header("Audio")]
    [Tooltip("ถ้าไม่ใส่ จะใช้ pickupSfx จาก ItemDefinition แทน")]
    public AudioClip overridePickupSfx;
    public float sfxVolume = 1f;

    [Header("Destroy Settings")]
    [Tooltip("ทำลาย GameObject ทันทีหลังเก็บ (ถ้าใช้ AudioSource ติด object แล้วอยากให้เสียงเล่นจบค่อยหาย ค่อยปรับ logic เองเพิ่มได้)")]
    public bool destroyOnPickup = true;

    private void Reset()
    {
        // ให้ collider เป็น trigger โดยอัตโนมัติเวลา Add Component
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (itemDefinition == null)
        {
            Debug.LogWarning("[ItemPickup] ยังไม่ได้ตั้ง ItemDefinition");
            return;
        }

        // หาว่า Player มี InventoryLite อยู่ตรงไหน
        InventoryLite inventory = other.GetComponent<InventoryLite>();
        if (!inventory)
            inventory = other.GetComponentInChildren<InventoryLite>();

        if (!inventory)
        {
            Debug.LogWarning("[ItemPickup] ไม่เจอ InventoryLite บน Player");
            return;
        }

        // เพิ่มไอเทมเข้า inventory
        inventory.AddItem(itemDefinition, amount);

        // เล่นเสียง
        AudioClip clipToPlay = overridePickupSfx != null ? overridePickupSfx : itemDefinition.pickupSfx;
        if (clipToPlay != null)
        {
            AudioSource.PlayClipAtPoint(clipToPlay, transform.position, sfxVolume);
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            // ถ้าไม่ทำลายทันที แนะนำให้ปิด mesh / collider
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;

            var renderer = GetComponentInChildren<MeshRenderer>();
            if (renderer) renderer.enabled = false;
        }
    }
}
