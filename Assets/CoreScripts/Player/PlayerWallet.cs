using UnityEngine;

public class PlayerWallet : MonoBehaviour
{
    [SerializeField] int coins;
    public int Coins => coins;

    public void AddCoin(int amount = 1)
    {
        coins += Mathf.Max(0, amount);
        // TODO: อัปเดต UI ที่นี่ถ้ามี เช่น CoinText.text = coins.ToString();
        // Debug.Log($"Coins = {coins}");
    }
}
