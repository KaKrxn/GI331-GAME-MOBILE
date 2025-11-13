using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner ทางวิ่งแบบต่อชิ้น รองรับเลี้ยว (socket) + Grid Guard และรองรับ EndTile แบบมีจำนวนชิ้นจำกัด
/// </summary>
public class TileSpawner : MonoBehaviour
{
    [Header("Player & Prefabs")]
    public Transform player;
    public GameObject[] tilePrefabs;

    [Header("Tile Spawn Weights")]
    [Tooltip("น้ำหนักโอกาส spawn ของแต่ละ prefab (ตามลำดับใน tilePrefabs) ใช้เป็นสัดส่วน รวมกันไม่จำเป็นต้องเท่ากับ 1 หรือ 100")]
    public float[] tileSpawnWeights;

    [Header("Straight Fallback")]
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
    [Range(0f, 1f)] public float turnChance = 0.25f;     // โอกาสสุ่มออกทางเลี้ยว
    public bool preventConsecutiveTurns = true;          // กันทางเลี้ยวติดกัน

    [Header("Anchor (turned mode)")]
    public Transform anchor;                              // ให้ entrySocket ของชิ้นใหม่มาทับอันนี้

    [Header("Grid Guard (No-Overlap, No-Physics)")]
    public bool useGridGuard = true;
    [Tooltip("ขนาด 1 เซลล์กริด (ตั้ง ~ เท่าความยาวไทล์มาตรฐาน)")]
    public float cellSize = 10f;
    [Tooltip("จำนวนเซลล์ตามยาวที่ 1 ไทล์กิน (ไทล์ยาวสองเท่าใส่ 2)")]
    public int forwardCellsPerTile = 1;

    // เก็บการจองเซลล์ของแต่ละไทล์ เพื่อคืน/ย้ายตอนรีไซเคิล
    private readonly Dictionary<GameObject, List<Vector3Int>> reservations = new();
    private readonly HashSet<Vector3Int> occupied = new();

    [Header("Spawn Budget")]
    [Tooltip("จำกัดจำนวนสปอว์น/รีไซเคิลต่อ 1 เฟรม เพื่อลดโอกาสค้าง")]
    public int maxSpawnsPerFrame = 2;

    [Header("Finite Track / End Tile")]
    [Tooltip("ถ้าเปิด = สร้างจำนวน Tile ปกติจำกัด แล้วตามด้วย EndTile 1 ชิ้น จากนั้นหยุด spawn เพิ่ม")]
    public bool useFiniteTrack = false;

    [Tooltip("จำนวน Tile ปกติทั้งหมดก่อนถึง EndTile")]
    public int maxRegularTiles = 50;

    [Tooltip("Prefab ของ Tile สุดท้าย (EndTile)")]
    public GameObject endTilePrefab;

    // ---------- internal ----------
    private readonly List<GameObject> activeTiles = new();
    private float nextZ;
    private bool usingTurnMode = false;
    private bool lastWasTurn = false;

    // นับจำนวน Tile ปกติ + flag ว่า EndTile spawn แล้วหรือยัง
    private int regularTilesSpawned = 0;
    private bool endTileSpawned = false;

    void Start()
    {
        if (!player)
        {
            Debug.LogError("[TileSpawner] Assign Player");
            enabled = false;
            return;
        }

        if (tilePrefabs == null || tilePrefabs.Length == 0)
        {
            Debug.LogError("[TileSpawner] Assign tilePrefabs");
            enabled = false;
            return;
        }

        if (!anchor)
        {
            var a = new GameObject("TileAnchor");
            anchor = a.transform;
            anchor.SetPositionAndRotation(transform.position, transform.rotation);
        }

        activeTiles.Clear();
        nextZ = 0f;
        regularTilesSpawned = 0;
        endTileSpawned = false;

        usingTurnMode = allowTurns && AnyPrefabHasSocket();

        // เริ่มต้น spawn ให้ครบ tilesAhead ชิ้น (หรือจนกว่าจะหมด finite track)
        for (int i = 0; i < tilesAhead; i++)
        {
            bool safe = i < safeStartCount;
            if (usingTurnMode)
            {
                if (!SpawnByAnchor(safe)) break;
            }
            else
            {
                if (!SpawnStraight(safe)) break;
            }
        }
    }

    void Update()
    {
        int budget = Mathf.Max(1, maxSpawnsPerFrame);

        if (!usingTurnMode)
        {
            // โหมดวิ่งตรงธรรมดา
            while (budget > 0 &&
                   (!useFiniteTrack || !endTileSpawned) &&
                   player.position.z + tilesAhead * tileLength > nextZ - tileLength)
            {
                if (!SpawnStraight(false)) break;
                budget--;
            }

            // ถ้าเป็น finite track จะไม่ recycle เพื่อให้เส้นทางอยู่ครบจนจบ
            if (!useFiniteTrack && activeTiles.Count > 0)
            {
                GameObject first = activeTiles[0];
                if (player.position.z - first.transform.position.z > tileLength * 1.5f)
                    RecycleStraight();
            }
        }
        else
        {
            // โหมดมี turn + socket
            while (budget > 0 &&
                   (!useFiniteTrack || !endTileSpawned) &&
                   activeTiles.Count < tilesAhead + 2)
            {
                if (!SpawnByAnchor(false)) break; // ถ้าเฟรมนี้ลงไม่ได้ ให้หยุด—ไม่ลูป
                budget--;
            }

            // finite track: ไม่ recycle เพื่อไม่ไปสร้างทางใหม่หลัง EndTile
            if (!useFiniteTrack && activeTiles.Count > 0)
            {
                var first = activeTiles[0];
                if (Vector3.Distance(player.position, first.transform.position) > tileLength * (tilesAhead + 2))
                    RecycleByAnchor();
            }
        }
    }

    // =================== Straight mode ===================
    private bool SpawnStraight(bool safe)
    {
        // finite track: ถ้า EndTile ถูก spawn แล้ว ไม่ spawn เพิ่ม
        if (useFiniteTrack && endTileSpawned)
            return false;

        GameObject prefab;
        bool spawnEnd = false;

        if (useFiniteTrack && regularTilesSpawned >= maxRegularTiles && !endTileSpawned)
        {
            if (!endTilePrefab)
            {
                Debug.LogWarning("[TileSpawner] useFiniteTrack เปิดอยู่ แต่ยังไม่ได้ตั้ง endTilePrefab");
                return false;
            }

            prefab = endTilePrefab;
            spawnEnd = true;
        }
        else
        {
            // โหมดตรงใช้สุ่มจาก tilePrefabs ปกติ (ใช้ weight ร่วมกันได้เหมือนกัน)
            int idx = WeightedPickIndexForAll();
            prefab = tilePrefabs[idx];
        }

        GameObject tile = Instantiate(prefab, new Vector3(0f, 0f, nextZ), Quaternion.identity, transform);
        var tileComp = tile.GetComponent<Tile>();
        tileComp?.RefreshContents(this, safe);
        activeTiles.Add(tile);
        nextZ += tileLength;

        if (useFiniteTrack)
        {
            if (spawnEnd) endTileSpawned = true;
            else regularTilesSpawned++;
        }

        return true;
    }

    private void RecycleStraight()
    {
        var oldest = activeTiles[0];
        activeTiles.RemoveAt(0);

        oldest.transform.position = new Vector3(0f, 0f, nextZ);
        var tileComp = oldest.GetComponent<Tile>();
        tileComp?.RefreshContents(this, false);
        activeTiles.Add(oldest);
        nextZ += tileLength;
    }

    // =================== Turned mode + Grid Guard ===================
    /// <summary>พยายามสปอว์นใหม่ตาม anchor (ถ้าลงไม่ได้ ให้ return false)</summary>
    private bool SpawnByAnchor(bool safe)
    {
        // finite track: ถ้า EndTile ถูก spawn แล้ว ไม่ spawn เพิ่ม
        if (useFiniteTrack && endTileSpawned)
            return false;

        GameObject prefab;
        bool spawnEnd = false;

        if (useFiniteTrack && regularTilesSpawned >= maxRegularTiles && !endTileSpawned)
        {
            if (!endTilePrefab)
            {
                Debug.LogWarning("[TileSpawner] useFiniteTrack เปิดอยู่ แต่ยังไม่ได้ตั้ง endTilePrefab");
                return false;
            }

            prefab = endTilePrefab;
            spawnEnd = true;
        }
        else
        {
            prefab = ChooseTilePrefab();
        }

        // คำนวณโพสที่ควรจะวาง
        var ghost = Instantiate(prefab);
        ghost.SetActive(false);
        Pose p = ComputePoseFor(ghost);

        var t = ghost.GetComponent<Tile>();

        // ถ้าใช้ GridGuard กับ EndTile: เราเลือกจะ "ไม่เช็ค" เพื่อให้ชิ้นสุดท้ายลงได้แน่นอน
        List<Vector3Int> cells = null;
        if (useGridGuard && !spawnEnd && !TryReserveCells(t, p, out cells))
        {
            // ลองบังคับเป็นตรง 1 ครั้ง (ผ่านง่ายกว่า)
            var straight = PickStraightOnly();
            if (straight != null)
            {
                Destroy(ghost);
                ghost = Instantiate(straight);
                ghost.SetActive(false);
                p = ComputePoseFor(ghost, forceStraight: true);
                t = ghost.GetComponent<Tile>();

                cells = null;
                if (useGridGuard && !TryReserveCells(t, p, out cells))
                {
                    Destroy(ghost);
                    return false; // ไม่มีที่ลง → ข้ามในเฟรมนี้
                }

                lastWasTurn = false;
            }
            else
            {
                Destroy(ghost);
                return false;
            }
        }

        // วางจริง
        ghost.transform.SetPositionAndRotation(p.position, p.rotation);
        ghost.SetActive(true);
        t?.RefreshContents(this, safe);
        activeTiles.Add(ghost);
        AdvanceAnchor(t);

        if (useGridGuard && cells != null)
            reservations[ghost] = cells;

        if (useFiniteTrack)
        {
            if (spawnEnd) endTileSpawned = true;
            else regularTilesSpawned++;
        }

        return true;
    }

    private void RecycleByAnchor()
    {
        var oldest = activeTiles[0];
        activeTiles.RemoveAt(0);

        // คืนเซลล์เดิมแบบปลอดภัย
        if (useGridGuard && reservations.TryGetValue(oldest, out var cellsOld))
        {
            foreach (var c in cellsOld) occupied.Remove(c);
            reservations.Remove(oldest);
        }

        // คำนวณโพสใหม่โดยอิง anchor ปัจจุบัน
        Pose p = ComputePoseFor(oldest);
        var t = oldest.GetComponent<Tile>();

        List<Vector3Int> cellsNew = null;
        bool ok = !useGridGuard || TryReserveCells(t, p, out cellsNew);

        if (ok)
        {
            oldest.transform.SetPositionAndRotation(p.position, p.rotation);
            t?.RefreshContents(this, false);
            activeTiles.Add(oldest);
            AdvanceAnchor(t);

            if (useGridGuard && cellsNew != null)
                reservations[oldest] = cellsNew;

            return;
        }

        Destroy(oldest);
        SpawnByAnchor(false);
    }

    // =================== สุ่ม prefab (ถ่วงน้ำหนัก) ===================
    /// <summary>
    /// เลือก Tile ตาม turn/straight + น้ำหนัก tileSpawnWeights
    /// </summary>
    private GameObject ChooseTilePrefab()
    {
        bool wantTurn = allowTurns && Random.value < turnChance;
        if (preventConsecutiveTurns && lastWasTurn) wantTurn = false;

        List<int> candidateIndices = new();

        for (int i = 0; i < tilePrefabs.Length; i++)
        {
            var pf = tilePrefabs[i];
            if (!pf) continue;

            var t = pf.GetComponent<Tile>();
            if (!t) continue;

            if (wantTurn)
            {
                if (t.turnKind == TurnKind.Left90 || t.turnKind == TurnKind.Right90)
                    candidateIndices.Add(i);
            }
            else
            {
                if (t.turnKind == TurnKind.Straight)
                    candidateIndices.Add(i);
            }
        }

        // ถ้าไม่มี candidate ตามประเภทที่ต้องการ ให้ fallback เป็นทุกอัน
        if (candidateIndices.Count == 0)
        {
            for (int i = 0; i < tilePrefabs.Length; i++)
            {
                if (tilePrefabs[i] != null)
                    candidateIndices.Add(i);
            }
        }

        int pickedIndex = WeightedPickIndex(candidateIndices);
        GameObject chosen = tilePrefabs[pickedIndex];

        var tile = chosen.GetComponent<Tile>();
        lastWasTurn = tile &&
                      (tile.turnKind == TurnKind.Left90 || tile.turnKind == TurnKind.Right90);

        return chosen;
    }

    /// <summary>
    /// ใช้สุ่ม index จาก tilePrefabs ทั้งหมด ตามน้ำหนัก tileSpawnWeights
    /// </summary>
    private int WeightedPickIndexForAll()
    {
        List<int> all = new();
        for (int i = 0; i < tilePrefabs.Length; i++)
        {
            if (tilePrefabs[i] != null)
                all.Add(i);
        }

        if (all.Count == 0) return 0;
        return WeightedPickIndex(all);
    }

    /// <summary>
    /// สุ่ม index จาก list ของ index โดยใช้ tileSpawnWeights เป็นน้ำหนัก
    /// ถ้า weight ว่าง หรือรวมแล้ว <= 0 จะ fallback เป็น random เท่า ๆ กัน
    /// </summary>
    private int WeightedPickIndex(List<int> indices)
    {
        if (indices == null || indices.Count == 0)
            return 0;

        bool hasWeights = tileSpawnWeights != null && tileSpawnWeights.Length > 0;

        float total = 0f;
        foreach (int idx in indices)
        {
            float w = 1f;
            if (hasWeights && idx < tileSpawnWeights.Length)
                w = Mathf.Max(0f, tileSpawnWeights[idx]); // กันค่าติดลบ

            total += w;
        }

        // ถ้า total <= 0 แปลว่าไม่มีน้ำหนักใช้งานได้เลย → random เท่า ๆ กัน
        if (total <= 0f)
            return indices[Random.Range(0, indices.Count)];

        float r = Random.value * total;
        foreach (int idx in indices)
        {
            float w = 1f;
            if (hasWeights && idx < tileSpawnWeights.Length)
                w = Mathf.Max(0f, tileSpawnWeights[idx]);

            if (r < w)
                return idx;

            r -= w;
        }

        return indices[indices.Count - 1];
    }

    private GameObject PickStraightOnly()
    {
        List<int> indices = new();
        for (int i = 0; i < tilePrefabs.Length; i++)
        {
            var pf = tilePrefabs[i];
            if (!pf) continue;

            var tt = pf.GetComponent<Tile>();
            if (tt && tt.turnKind == TurnKind.Straight)
                indices.Add(i);
        }

        if (indices.Count == 0)
            return null;

        int picked = WeightedPickIndex(indices);
        return tilePrefabs[picked];
    }

    // =================== คำนวณ Pose (ให้ entrySocket ซ้อน anchor) ===================
    private Pose ComputePoseFor(GameObject go, bool forceStraight = false)
    {
        var t = go.GetComponent<Tile>();
        Quaternion rot = go.transform.rotation;
        Vector3 pos = go.transform.position;

        if (t && (t.HasSocketsStraight() || t.HasSocketsSplit()) && !forceStraight)
        {
            // ให้ entrySocket ซ้อน anchor ทั้งหมุนและตำแหน่ง
            var entry = t.entrySocket;
            Quaternion rotDelta = anchor.rotation * Quaternion.Inverse(entry.rotation);
            rot = rotDelta * rot;

            Vector3 localOffset = entry.position - go.transform.position; // world offset ก่อนหมุนใหม่
            pos = anchor.position - (rot * localOffset);
        }
        else
        {
            // โหมดตรง (fallback)
            rot = anchor.rotation;
            pos = anchor.position + anchor.forward * (tileLength * 0.5f);
        }

        return new Pose(pos, rot);
    }

    // =================== Grid Guard ===================
    private bool TryReserveCells(Tile t, Pose p, out List<Vector3Int> cells)
    {
        cells = new List<Vector3Int>();
        if (!useGridGuard) return true;

        // ใช้ตำแหน่ง p เป็น center, เดินไปตาม forward โดยความยาว tileLength
        Vector3 forward = (p.rotation * Vector3.forward).normalized;
        Vector3 start = p.position;

        int steps = Mathf.Max(1, forwardCellsPerTile);
        for (int i = 0; i < steps; i++)
        {
            Vector3 sample = start + forward * (i * cellSize);
            Vector3Int c = CellOf(sample);
            if (occupied.Contains(c)) return false;
            cells.Add(c);
        }

        foreach (var c in cells) occupied.Add(c);
        return true;
    }

    private Vector3Int CellOf(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / cellSize);
        int y = Mathf.RoundToInt(worldPos.y / cellSize);
        int z = Mathf.RoundToInt(worldPos.z / cellSize);
        return new Vector3Int(x, y, z);
    }

    // =================== Advance Anchor ===================
    private void AdvanceAnchor(Tile tile)
    {
        if (!tile)
        {
            anchor.position += anchor.forward * tileLength;
            return;
        }

        if (tile.HasSocketsSplit())
        {
            // กรณี split (ออกได้หลายทาง) ปกติจะให้ TurnTrigger ภายนอกเรียก ChooseSplitExit
            return;
        }

        var exit = tile.GetExitSocket();
        if (exit)
            anchor.SetPositionAndRotation(exit.position, exit.rotation);
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

    // =================== API เสริม Obstacle / Coin / Split ===================
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

    public void ChooseSplitExit(Tile splitTile, bool chooseLeft)
    {
        if (splitTile == null) return;
        Transform next = chooseLeft ? splitTile.exitLeftSocket : splitTile.exitRightSocket;
        if (next != null)
            anchor.SetPositionAndRotation(next.position, next.rotation);
    }
}
