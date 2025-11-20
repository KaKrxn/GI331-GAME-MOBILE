using UnityEngine;

public class ShopInteractable : MonoBehaviour
{
    public void TriggerShop()
    {
        ShopManager.Instance.OpenShop();
    }
}
