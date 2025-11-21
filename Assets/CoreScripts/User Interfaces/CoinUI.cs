using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("Inventory Source")]
    [SerializeField] private InventoryLite inventory;   // อ้างอิง InventoryLite
    [SerializeField] private string coinItemId = "coin"; // ใส่ itemId ของเหรียญให้ตรงกับ ItemDefinition

    private int lastCoins = int.MinValue;

    private void Start()
    {
        // ถ้ายังไม่เซ็ตใน Inspector ให้ลองหาใน Scene ให้เอง
        if (inventory == null)
        {
            inventory = FindObjectOfType<InventoryLite>();
            if (inventory == null)
            {
                Debug.LogWarning("[CoinUI] ไม่พบ InventoryLite ใน Scene");
            }
        }

        UpdateCoinText();
    }

    private void Update()
    {
        if (inventory == null || string.IsNullOrEmpty(coinItemId))
            return;

        int currentCoins = inventory.GetQuantity(coinItemId);

        // อัปเดตเฉพาะตอนค่ามันเปลี่ยน จะได้ไม่เปลือง
        if (currentCoins != lastCoins)
        {
            lastCoins = currentCoins;
            UpdateCoinText();
        }
    }

    private void UpdateCoinText()
    {
        if (coinText == null)
            return;

        coinText.text = lastCoins.ToString();
    }
}
