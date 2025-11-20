using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestItemRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI statusText;

    /// <summary>
    /// เซ็ตข้อมูลไอเทมในแถวนี้
    /// </summary>
    public void Setup(ItemDefinition def, bool hasItem)
    {
        if (def == null)
        {
            if (nameText) nameText.text = "(Missing ItemDefinition)";
            if (statusText) statusText.text = "-";
            return;
        }

        if (nameText)
            nameText.text = def.displayName;

        if (iconImage)
            iconImage.sprite = def.icon;

        if (statusText)
        {
            if (hasItem)
            {
                statusText.text = "เก็บแล้ว";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "ยังไม่ได้เก็บ";
                statusText.color = Color.red;
            }
        }
    }
}
