using UnityEngine;

public class GroundSpawner : MonoBehaviour
{
    public GameObject groundTilePrefab;

    private Vector3 nextSpawnPoint;

    private void Start()
    {
        // สร้างพื้นเริ่มต้นก่อนสัก 10 ชิ้น
        for (int i = 0; i < 10; i++)
        {
            SpawnTile();
        }
    }

    public void SpawnTile()
    {
        GameObject tile = Instantiate(groundTilePrefab, nextSpawnPoint, Quaternion.identity);

        // child ที่ชื่อ NextSpawnPoint หรือ index 0/1 แล้วแต่จัด
        Transform nextPoint = tile.transform.Find("NextSpawnPoint");
        if (nextPoint != null)
        {
            nextSpawnPoint = nextPoint.position;
        }
        else
        {
            Debug.LogWarning("NextSpawnPoint not found on tile!");
        }
    }
}
