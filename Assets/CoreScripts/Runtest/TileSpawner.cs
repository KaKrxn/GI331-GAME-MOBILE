using System.Collections.Generic;
using UnityEngine;

public class TileSpawner : MonoBehaviour
{
    [Header("Player & Prefabs")]
    public Transform player;
    public GameObject[] tilePrefabs;

    [Header("Straight Mode (fallback)")]
    public float tileLength = 10f;
    public int tilesAhead = 8;
    public int safeStartCount = 3;

    [Header("Obstacles & Coins")]
    public GameObject[] obstaclePrefabs;
    [Range(0, 1f)] public float obstacleChance = 0.45f;
    public GameObject coinPrefab;
    [Range(0, 1f)] public float coinChance = 0.35f;

    [Header("Turn Settings")]
    public bool allowTurns = true;
    [Range(0f, 1f)] public float turnChance = 0.25f;  // โอกาสสุ่มทางเลี้ยว
    public bool preventConsecutiveTurns = true;       // กันเลี้ยวติดกัน

    [Header("Anchor (turned mode)")]
    public Transform anchor;

    [Header("Anti-Overlap (OverlapBox)")]
    [Tooltip("เลือกให้ครอบเลเยอร์ TileFootprint เท่านั้น")]
    public LayerMask footprintMask;
    [Tooltip("จำนวนครั้งที่ลองสุ่มใหม่เมื่อทับกัน")]
    public int placeRetry = 8;
    [Tooltip("ถ้าทับกันแล้วไม่มีตัวเลือก ให้ข้ามการวางในรอบนั้นแทนการบังคับยัด")]
    public bool skipIfOverlap = true;
    [Tooltip("ตอนรีไซเคิล: ถ้าทับจะ 'ทำลายชิ้นเก่าและสร้างชิ้นใหม่' แทนการฝืนย้าย")]
    public bool replaceOnRecycleIfOverlap = true;

    // ===== internal =====
    private readonly List<GameObject> activeTiles = new List<GameObject>();
    private float nextZ;
    private bool usingTurnMode = false;
    private bool lastWasTurn = false;

    void Start()
    {
        if (!player) { Debug.LogError("[TileSpawner] Assign Player"); enabled = false; return; }
        if (tilePrefabs == null || tilePrefabs.Length == 0) { Debug.LogError("[TileSpawner] Assign tilePrefabs"); enabled = false; return; }

        if (!anchor)
        {
            var a = new GameObject("TileAnchor");
            anchor = a.transform;
            anchor.SetPositionAndRotation(transform.position, transform.rotation);
        }

        activeTiles.Clear();
        nextZ = 0f;

        usingTurnMode = allowTurns && AnyPrefabHasSocket();

        for (int i = 0; i < tilesAhead; i++)
        {
            if (usingTurnMode) SpawnByAnchor(i < safeStartCount);
            else SpawnStraight(i < safeStartCount);
        }
    }

    void Update()
    {
        if (!usingTurnMode)
        {
            while (player.position.z + tilesAhead * tileLength > nextZ - tileLength)
                SpawnStraight(false);

            if (activeTiles.Count > 0)
            {
                GameObject first = activeTiles[0];
                if (player.position.z - first.transform.position.z > tileLength * 1.5f)
                    RecycleStraight();
            }
        }
        else
        {
            while (activeTiles.Count < tilesAhead + 2)
                SpawnByAnchor(false);

            if (activeTiles.Count > 0)
            {
                var first = activeTiles[0];
                if (Vector3.Distance(player.position, first.transform.position) > tileLength * (tilesAhead + 2))
                    RecycleByAnchor();
            }
        }
    }

    // ===== Straight mode (legacy fallback) =====
    private void SpawnStraight(bool safe)
    {
        GameObject prefab = tilePrefabs[Random.Range(0, tilePrefabs.Length)];
        GameObject tile = Instantiate(prefab, new Vector3(0f, 0f, nextZ), Quaternion.identity, transform);
        var tileComp = tile.GetComponent<Tile>();
        if (tileComp) tileComp.RefreshContents(this, safe);
        activeTiles.Add(tile);
        nextZ += tileLength;
    }

    private void RecycleStraight()
    {
        var oldest = activeTiles[0];
        activeTiles.RemoveAt(0);
        oldest.transform.position = new Vector3(0f, 0f, nextZ);
        var tileComp = oldest.GetComponent<Tile>();
        if (tileComp) tileComp.RefreshContents(this, false);
        activeTiles.Add(oldest);
        nextZ += tileLength;
    }

    // ===== Turned mode (with anti-overlap) =====
    private void SpawnByAnchor(bool safe)
    {
        for (int attempt = 0; attempt < Mathf.Max(1, placeRetry); attempt++)
        {
            var prefab = ChooseTilePrefab();
            var ghost = Instantiate(prefab);
            ghost.SetActive(false);

            // คำนวณ pose ที่จะวาง (แต่ยังไม่วางจริง)
            Pose targetPose = ComputePoseFor(ghost);

            var t = ghost.GetComponent<Tile>();
            bool ok = CanPlaceTileAtPose(t, targetPose.position, targetPose.rotation);

            if (ok)
            {
                // วางจริง
                ghost.transform.SetPositionAndRotation(targetPose.position, targetPose.rotation);
                ghost.SetActive(true);

                if (t) t.RefreshContents(this, safe);
                activeTiles.Add(ghost);
                AdvanceAnchor(t);
                lastWasTurn = (t && (t.turnKind == TurnKind.Left90 || t.turnKind == TurnKind.Right90));
                return;
            }

            // ไม่ผ่าน → ทำลายตัวทดลอง
            Destroy(ghost);
        }

        if (!skipIfOverlap)
        {
            // fallback: บังคับวางตรง (โอกาสผ่านสูงกว่า)
            var straight = PickStraightOnly();
            if (straight)
            {
                var go = Instantiate(straight);
                go.SetActive(false);
                Pose p = ComputePoseFor(go, forceStraight: true);
                var tt = go.GetComponent<Tile>();
                if (CanPlaceTileAtPose(tt, p.position, p.rotation))
                {
                    go.transform.SetPositionAndRotation(p.position, p.rotation);
                    go.SetActive(true);
                    if (tt) tt.RefreshContents(this, safe);
                    activeTiles.Add(go);
                    AdvanceAnchor(tt);
                    lastWasTurn = false;
                    return;
                }
                Destroy(go);
            }
        }

        Debug.LogWarning("[TileSpawner] SpawnByAnchor: cannot find non-overlapping placement.");
    }

    private void RecycleByAnchor()
    {
        var oldest = activeTiles[0];
        activeTiles.RemoveAt(0);

        // คำนวณ pose ใหม่ให้กับ 'oldest'
        Pose p = ComputePoseFor(oldest);
        var t = oldest.GetComponent<Tile>();

        if (CanPlaceTileAtPose(t, p.position, p.rotation))
        {
            oldest.transform.SetPositionAndRotation(p.position, p.rotation);
            t?.RefreshContents(this, false);
            activeTiles.Add(oldest);
            AdvanceAnchor(t);
            lastWasTurn = t && (t.turnKind == TurnKind.Left90 || t.turnKind == TurnKind.Right90);
        }
        else if (replaceOnRecycleIfOverlap)
        {
            // ทำลายชิ้นเก่า แล้วหาชิ้นใหม่แทน (ใช้ตรรกะเดียวกับ SpawnByAnchor)
            Destroy(oldest);
            SpawnByAnchor(false);
        }
        else
        {
            // ถ้าไม่ให้ทำลาย ก็พยายามดันไปข้างหน้าตามแกน anchor แบบเดิม (เสี่ยงชน)
            oldest.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            t?.RefreshContents(this, false);
            activeTiles.Add(oldest);
            AdvanceAnchor(t);
        }
    }

    // ====== เลือก Prefab: ตรง/เลี้ยว + กันเลี้ยวติดกัน ======
    private GameObject ChooseTilePrefab()
    {
        bool wantTurn = allowTurns && Random.value < turnChance;
        if (preventConsecutiveTurns && lastWasTurn) wantTurn = false;

        List<GameObject> list = new List<GameObject>();
        foreach (var pf in tilePrefabs)
        {
            var t = pf.GetComponent<Tile>();
            if (!t) continue;
            if (wantTurn)
            {
                if (t.turnKind == TurnKind.Left90 || t.turnKind == TurnKind.Right90) list.Add(pf);
            }
            else
            {
                if (t.turnKind == TurnKind.Straight) list.Add(pf);
            }
        }
        if (list.Count == 0) list.AddRange(tilePrefabs); // fallback

        var chosen = list[Random.Range(0, list.Count)];
        var tile = chosen.GetComponent<Tile>();
        lastWasTurn = (tile.turnKind == TurnKind.Left90 || tile.turnKind == TurnKind.Right90);
        return chosen;
    }

    private GameObject PickStraightOnly()
    {
        List<GameObject> list = new List<GameObject>();
        foreach (var pf in tilePrefabs)
        {
            var tt = pf.GetComponent<Tile>();
            if (tt && tt.turnKind == TurnKind.Straight) list.Add(pf);
        }
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    // ====== คำนวณ Pose ที่จะวาง (เหมือน Align แต่ไม่แตะ transform จริง) ======
    private Pose ComputePoseFor(GameObject go, bool forceStraight = false)
    {
        var t = go.GetComponent<Tile>();
        Quaternion rot = go.transform.rotation;
        Vector3 pos = go.transform.position;

        if (t && (t.HasSocketsStraight() || t.HasSocketsSplit()) && !forceStraight)
        {
            var entry = t.entrySocket;
            Quaternion rotDelta = anchor.rotation * Quaternion.Inverse(entry.rotation);
            rot = rotDelta * rot;

            // worldPos = anchor.pos - (rot * (entry.localOffsetFromRoot))
            Vector3 localOffset = entry.position - go.transform.position;
            pos = anchor.position - (rot * localOffset);
        }
        else
        {
            rot = anchor.rotation;
            pos = anchor.position + anchor.forward * (tileLength * 0.5f);
        }

        return new Pose(pos, rot);
    }

    // ====== ตรวจชนด้วย OverlapBox ======
    private bool CanPlaceTileAtPose(Tile t, Vector3 worldPos, Quaternion worldRot)
    {
        if (t == null || t.footprint == null) return true; // ไม่มีกล่อง → ปล่อยผ่าน (แนะนำใส่ทุกอัน)

        // คำนวณ center ใน world: center_world = pos + rot * local_center
        Vector3 centerWorld = worldPos + worldRot * t.footprint.center;

        // half extents ตามขนาดกล่อง (รองรับสเกลโลกของ Tile)
        // ถ้า Tile/Root สเกล 1,1,1 ใช้ size*0.5 ก็พอ
        Vector3 halfExtents = t.footprint.size * 0.5f;

        // หา collider ที่ทับ (นับรวม trigger เพราะ footprint เป็น trigger)
        Collider[] hits = Physics.OverlapBox(centerWorld, halfExtents, worldRot, footprintMask, QueryTriggerInteraction.Collide);

        foreach (var h in hits)
        {
            // อนุญาตชนกับตัวเองเฉพาะกรณีที่เป็น 'go' เดียวกัน (ตอนที่วางจริง ๆ จะไม่มีกรณีนี้ เพราะเราเช็คก่อน SetActive)
            // แต่กันเคสชนกับชิ้นอื่นทั้งหมด
            if (t.footprint != null && h == t.footprint) continue;
            return false;
        }
        return true;
    }

    // ====== ย้าย Anchor ไป exit ของชิ้นที่วาง ======
    private void AdvanceAnchor(Tile tile)
    {
        if (!tile)
        {
            anchor.position += anchor.forward * tileLength;
            return;
        }

        if (tile.HasSocketsSplit())
        {
            // ถ้ามีระบบ auto-choose split ให้เลือกที่นี่ (มีแต่ละโปรเจกต์เลือกเปิด-ปิด)
            return;
        }

        if (tile.HasSocketsStraight())
            anchor.SetPositionAndRotation(tile.exitSocket.position, tile.exitSocket.rotation);
        else
            anchor.position += anchor.forward * tileLength;
    }

    private bool PrefabHasSocket(GameObject prefab)
    {
        var t = prefab.GetComponent<Tile>();
        return t && (t.HasSocketsStraight() || t.HasSocketsSplit());
    }

    private bool AnyPrefabHasSocket()
    {
        foreach (var pf in tilePrefabs)
            if (PrefabHasSocket(pf)) return true;
        return false;
    }

    // ===== API เดิม =====
    public bool TrySpawnObstacle(Transform parent, Transform[] lanePoints)
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return false;
        if (lanePoints == null || lanePoints.Length == 0) return false;
        if (Random.value > obstacleChance) return false;
        int laneIndex = Random.Range(0, lanePoints.Length);
        Transform p = lanePoints[laneIndex];
        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        Instantiate(prefab, p.position, p.rotation, parent);
        return true;
    }

    public bool TrySpawnCoin(Transform parent, Transform[] lanePoints)
    {
        if (coinPrefab == null) return false;
        if (lanePoints == null || lanePoints.Length == 0) return false;
        if (Random.value > coinChance) return false;
        int laneIndex = Random.Range(0, lanePoints.Length);
        Transform p = lanePoints[laneIndex];
        Instantiate(coinPrefab, p.position + Vector3.up * 0.5f, p.rotation, parent);
        return true;
    }

    // ถ้าคุณใช้ SplitChoiceTrigger เรียกอันนี้
    public void ChooseSplitExit(Tile splitTile, bool chooseLeft)
    {
        if (splitTile == null) return;
        Transform next = chooseLeft ? splitTile.exitLeftSocket : splitTile.exitRightSocket;
        if (next != null)
            anchor.SetPositionAndRotation(next.position, next.rotation);
    }
}
