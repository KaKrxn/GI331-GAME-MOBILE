using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public Transform player;
    public Text distanceText;
    public Text coinsText;

    float startZ;
    int coins;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        startZ = player.position.z;
        UpdateUI();
    }

    void Update()
    {
        float dist = Mathf.Max(0f, player.position.z - startZ);
        if (distanceText) distanceText.text = Mathf.FloorToInt(dist).ToString();
    }

    public void AddCoins(int c)
    {
        coins += c;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (coinsText) coinsText.text = coins.ToString();
    }
}
