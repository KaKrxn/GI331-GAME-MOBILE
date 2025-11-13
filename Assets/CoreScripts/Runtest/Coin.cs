using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [SerializeField] int amount = 1;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;     // เหรียญควรเป็น Trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        var wallet = other.GetComponent<PlayerWallet>();
        if (wallet != null)
        {
            wallet.AddCoin(amount);
        }
        else
        {
            // ทางเลือก: ถ้าคุณมี GameManager แบบซิงเกิลตัน ให้ลองเรียกตรงนี้แทน
            // GameManager.Instance?.AddCoins(amount);
            Debug.LogWarning("Player ไม่มี PlayerWallet จึงไม่ได้บวกเหรียญ");
        }

        Destroy(gameObject);
    }
}
