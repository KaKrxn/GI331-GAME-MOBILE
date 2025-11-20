using UnityEngine;

public class GroundTile : MonoBehaviour
{
    private GroundSpawner groundSpawner;

    private void Start()
    {
        groundSpawner = FindObjectOfType<GroundSpawner>();
    }

    // ให้ใช้กับ EndTrigger (Collider isTrigger)
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // สร้าง Tile ใหม่ต่อท้าย
        groundSpawner.SpawnTile();

        // ลบ Tile นี้ทิ้งหลังจากผู้เล่นวิ่งผ่านไปแล้วสักพัก
        Destroy(gameObject, 2f);
    }
}
