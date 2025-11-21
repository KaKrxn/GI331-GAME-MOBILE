using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// สุ่ม Spawn Coin เป็นเส้นยาว + Obstacle + Item แบบ 3 เลน บน 1 Tile
/// ใช้ติดกับ Prefab ของ Tile (เช่น Straight)
/// </summary>
public class LaneObjectSpawner : MonoBehaviour
{
    [Header("Common Lane Settings")]
    [Tooltip("ระยะเลนซ้าย/ขวา จากกลาง (เช่น 2 = ซ้าย -2, ขวา +2)")]
    [SerializeField] private float laneOffset = 2f;

    [SerializeField] private bool allowLeftLane = true;
    [SerializeField] private bool allowCenterLane = true;
    [SerializeField] private bool allowRightLane = true;

    [Tooltip("ยกทุกอย่างลอยจากพื้นเพื่อไม่ให้จม")]
    [SerializeField] private float baseHeightOffset = 0.5f;

    [Tooltip("เว้นขอบหัว-ท้าย tile ไม่ให้วัตถุชิดเกินไป")]
    [SerializeField] private float marginZ = 1f;

    [Header("Coin Line Settings")]
    [SerializeField] private GameObject coinPrefab;

    [Tooltip("จำนวนเหรียญต่อเส้นใน 1 Tile")]
    [SerializeField] private int coinsPerTile = 6;

    [Tooltip("โอกาสที่เลนนั้นจะมีเส้นเหรียญ (เฉพาะเลนที่ไม่มี Obstacle)")]
    [Range(0f, 1f)]
    [SerializeField] private float coinLineChancePerLane = 0.7f;

    [Header("Obstacle Settings")]
    [Tooltip("Prefab สิ่งกีดขวาง")]
    [SerializeField] private List<GameObject> obstaclePrefabs;

    [Tooltip("โอกาสที่ Tile นี้จะมี Obstacle อย่างน้อย 1 เลน")]
    [Range(0f, 1f)]
    [SerializeField] private float obstacleChancePerTile = 0.7f;

    [Tooltip("จำนวนเลนที่มี Obstacle ได้สูงสุดต่อ Tile (1–2 แนะนำใช้ 2)")]
    [Range(1, 3)]
    [SerializeField] private int maxObstacleLanesPerTile = 2;

    [Tooltip("ยก Obstacle ลอยจากพื้นเท่าไหร่")]
    [SerializeField] private float obstacleHeightOffset = 0f;

    [Header("Item Settings")]
    [Tooltip("Prefab Item / Power-up แบบ Subway Surfers")]
    [SerializeField] private List<GameObject> itemPrefabs;

    [Tooltip("โอกาสที่ Tile นี้จะมี Item (แค่ 1 อันต่อ Tile)")]
    [Range(0f, 1f)]
    [SerializeField] private float itemChancePerTile = 0.3f;

    [Tooltip("ยก Item ให้สูงกว่า Coin นิดหน่อย")]
    [SerializeField] private float itemHeightOffset = 1.0f;

    // ------------------------------------------------------------

    private struct LaneState
    {
        public bool allowed;
        public float xOffset;
        public bool hasObstacle;
        public bool hasCoinLine;
        public bool hasItem;
    }

    private void Start()
    {
        SpawnAll();
    }

    private void SpawnAll()
    {
        var rend = GetComponent<Renderer>();
        if (rend == null) return;

        // ความยาว tile ตามแกน forward
        float length = rend.bounds.size.z;
        float usableLength = Mathf.Max(0.01f, length - (marginZ * 2f));

        // จุดเริ่มต้น (ด้านท้าย tile) ตาม forward
        Vector3 start = transform.position
                        - transform.forward * (usableLength / 2f)
                        + transform.forward * marginZ;

        // เตรียมข้อมูล 3 เลน
        LaneState[] lanes = new LaneState[3];
        lanes[0] = new LaneState { allowed = allowLeftLane, xOffset = -laneOffset };
        lanes[1] = new LaneState { allowed = allowCenterLane, xOffset = 0f };
        lanes[2] = new LaneState { allowed = allowRightLane, xOffset = laneOffset };

        // ----------------- 1) สุ่ม Obstacle (1–2 เลนต่อ Tile สูงสุด + ใช้ prefab ชนิดเดียว) -----------------
        GameObject chosenObstaclePrefab = null; // obstacle ชนิดเดียวสำหรับ tile นี้

        if (obstaclePrefabs != null && obstaclePrefabs.Count > 0 &&
            Random.value <= obstacleChancePerTile)
        {
            // เลือก obstacle 1 ชนิดสำหรับ tile นี้
            chosenObstaclePrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Count)];

            // เลนที่อนุญาตให้มี Obstacle ได้
            List<int> candidateObstacleLanes = new List<int>();
            for (int i = 0; i < lanes.Length; i++)
            {
                if (lanes[i].allowed)
                    candidateObstacleLanes.Add(i);
            }

            if (candidateObstacleLanes.Count > 0)
            {
                // จำนวนเลนที่ใช้ Obstacle จริง (1..maxObstacleLanesPerTile แต่ไม่เกินจำนวนเลนที่มีอยู่)
                int maxLanes = Mathf.Min(maxObstacleLanesPerTile, candidateObstacleLanes.Count);
                int lanesToUse = Random.Range(1, maxLanes + 1);

                // สุ่มลำดับ lane แล้วเลือกตามจำนวนที่ต้องการ
                ShuffleList(candidateObstacleLanes);
                for (int i = 0; i < lanesToUse; i++)
                {
                    int laneIndex = candidateObstacleLanes[i];
                    lanes[laneIndex].hasObstacle = true;
                }
            }
        }

        // ----------------- 2) สุ่ม Coin line (เฉพาะเลนที่ไม่มี Obstacle) --------------
        if (coinPrefab != null)
        {
            for (int i = 0; i < lanes.Length; i++)
            {
                if (!lanes[i].allowed) continue;
                if (lanes[i].hasObstacle) continue;   // ห้าม coin บน lane ที่มี obstacle

                if (Random.value <= coinLineChancePerLane)
                    lanes[i].hasCoinLine = true;
            }
        }

        // ----------------- 3) สุ่ม Item (แค่ 1 อันต่อ Tile และไม่อยู่บน Obstacle) ------
        if (itemPrefabs != null && itemPrefabs.Count > 0 &&
            Random.value <= itemChancePerTile)
        {
            List<int> candidateItemLanes = new List<int>();
            for (int i = 0; i < lanes.Length; i++)
            {
                if (!lanes[i].allowed) continue;
                if (lanes[i].hasObstacle) continue;   // ไม่ spawn item บน lane obstacle

                candidateItemLanes.Add(i);
            }

            if (candidateItemLanes.Count > 0)
            {
                int laneIndex = candidateItemLanes[Random.Range(0, candidateItemLanes.Count)];
                lanes[laneIndex].hasItem = true;
            }
        }

        // ----------------- 4) วาง Obstacle (ใช้ prefab ชนิดเดียวบนทุกเลนของ tile นี้) -------------------
        float obstacleZ = usableLength * 0.5f;
        if (chosenObstaclePrefab != null)
        {
            for (int i = 0; i < lanes.Length; i++)
            {
                if (!lanes[i].allowed || !lanes[i].hasObstacle) continue;

                Vector3 pos = transform.position
                              + transform.right * lanes[i].xOffset
                              + transform.forward * (marginZ + obstacleZ)
                              + Vector3.up * (baseHeightOffset + obstacleHeightOffset);

                SpawnObstacle(chosenObstaclePrefab, pos);
            }
        }

        // ----------------- 5) วาง Coin เป็นเส้นยาว -----------------------------
        if (coinPrefab != null && coinsPerTile > 0)
        {
            float step = (coinsPerTile > 1) ? usableLength / (coinsPerTile - 1) : 0f;

            for (int i = 0; i < coinsPerTile; i++)
            {
                float zOffset = step * i;

                for (int laneIndex = 0; laneIndex < lanes.Length; laneIndex++)
                {
                    if (!lanes[laneIndex].allowed || !lanes[laneIndex].hasCoinLine)
                        continue;

                    Vector3 pos = start
                                  + transform.forward * zOffset
                                  + Vector3.up * baseHeightOffset
                                  + transform.right * lanes[laneIndex].xOffset;

                    SpawnCoin(pos);
                }
            }
        }

        // ----------------- 6) วาง Item (1 อันต่อ Tile) -----------------------------
        for (int i = 0; i < lanes.Length; i++)
        {
            if (!lanes[i].allowed || !lanes[i].hasItem) continue;

            // ถ้ามี Coin line → วาง item กลางเส้น, ถ้าไม่มีก็กลาง Tile
            float zOffset;
            if (lanes[i].hasCoinLine && coinsPerTile > 0)
            {
                int midIndex = coinsPerTile / 2;
                float step = (coinsPerTile > 1) ? usableLength / (coinsPerTile - 1) : 0f;
                zOffset = step * midIndex;
            }
            else
            {
                zOffset = usableLength * 0.5f;
            }

            Vector3 pos = start
                          + transform.forward * zOffset
                          + Vector3.up * (baseHeightOffset + itemHeightOffset)
                          + transform.right * lanes[i].xOffset;

            SpawnRandomFromList(itemPrefabs, pos);
            break; // กันพลาดไม่ให้เกิน 1 ชิ้นต่อ tile
        }
    }

    // ------------------------------------------------------------

    private void SpawnCoin(Vector3 pos)
    {
        Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);
        // ผูกเป็น child ของ Tile
        Instantiate(coinPrefab, pos, rot, transform);
    }

    private void SpawnObstacle(GameObject prefab, Vector3 pos)
    {
        if (prefab == null) return;
        Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);
        Instantiate(prefab, pos, rot, transform);
    }

    private void SpawnRandomFromList(List<GameObject> list, Vector3 pos)
    {
        if (list == null || list.Count == 0) return;

        GameObject prefab = list[Random.Range(0, list.Count)];
        if (prefab == null) return;

        Quaternion rot = Quaternion.LookRotation(transform.forward, Vector3.up);
        // ผูกเป็น child ของ Tile
        Instantiate(prefab, pos, rot, transform);
    }

    // ฟังก์ชันสุ่มสลับลำดับใน List (ใช้ตอนสุ่ม lane obstacle)
    private void ShuffleList(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }
}
