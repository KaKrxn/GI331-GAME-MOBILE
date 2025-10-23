using UnityEngine;

public class SplitChoiceTrigger : MonoBehaviour
{
    public TileSpawner spawner;
    public Tile splitTile;     // อ้าง Tile ที่เป็น SplitLR
    public bool chooseLeft = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        spawner.ChooseSplitExit(splitTile, chooseLeft);
        // (ออปชัน) เพิ่มสคริปต์หมุน Player/กล้องให้หันตามทางใหม่แบบนิ่ม ๆ ได้ที่นี่
    }
}
