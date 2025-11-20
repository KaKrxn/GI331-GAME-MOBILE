using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button closeButton;

    void Awake()
    {
        Instance = this;
        shopPanel.SetActive(false);
        closeButton.onClick.AddListener(CloseShop);
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        Time.timeScale = 0f; // หยุดเกมระหว่างเปิดร้าน (ถ้าไม่อยากหยุด ลบได้)
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}
