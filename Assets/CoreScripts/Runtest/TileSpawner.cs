using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner แบบทางวิ่งต่อชิ้น รองรับเลี้ยว (socket), กันซ้อนทับด้วย Grid Guard (ไม่ใช้ฟิสิกส์),
/// มีงบสปอว์นต่อเฟรมกันค้าง, เลือกโอกาสเลี้ยว และกันเลี้ยวติดกัน
/// </summary>
public class TileSpawner : MonoBehaviour
{
    [Header("Player & Prefabs")]
    public Transform player;
    public GameObject[] tilePrefabs;

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

    // ---------- Grid Guard (No-Physics) ----------
    [Header("Grid Guard (No-Overlap, No-Physics)")]
    public bool useGridGuard = true;
    [Tooltip("ขนาด 1 เซลล์กริด (ตั้ง ~ เท่าความยาวไทล์มาตรฐาน)")]
    public float cellSize = 10f;
    [Tooltip("จำนวนเซลล์ตามยาวที่ 1 ไทล์กิน (ไทล์ยาวสองเท้าใส่ 2)")]
    public int forwardCellsPerTile = 1;

    // เก็บการจองเซลล์ของแต่ละไทล์ เพื่อคืน/ย้ายตอนรีไซเคิล
    private readonly Dictionary<GameObject, List<Vector3Int>> reservations = new();
    private readonly HashSet<Vector3Int> occupied = new();

    [Header("Spawn Budget")]
    [Tooltip("จำกัดจำนวนสปอว์น/รีไซเคิลต่อ 1 เฟรม เพื่อลดโอกาสค้าง")]
    public int maxSpawnsPerFrame = 2;

    // ---------- internal ----------
    private readonly List<GameObject> activeTiles = new();
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
        int budget = Mathf.Max(1, maxSpawnsPerFrame);

        if (!usingTurnMode)
        {
            while (budget > 0 && player.position.z + tilesAhead * tileLength > nextZ - tileLength)
            {
                SpawnStraight(false);
                budget--;
            }

            if (activeTiles.Count > 0)
            {
                GameObject first = activeTiles[0];
                if (player.position.z - first.transform.position.z > tileLength * 1.5f)
                    RecycleStraight();
            }
        }
        else
        {
            while (budget > 0 && activeTiles.Count < tilesAhead + 2)
            {
                if (!SpawnByAnchor(false)) break; // ถ้าเฟรมนี้ลงไม่ได้ ให้หยุด—ไม่ลูป
                budget--;
            }

            if (activeTiles.Count > 0)
            {
                var first = activeTiles[0];
                if (Vector3.Distance(player.position, first.transform.position) > tileLength * (tilesAhead + 2))
                    RecycleByAnchor();
            }
        }
    }

    // =================== Straight mode (เดิม) ===================
    private void SpawnStraight(bool safe)
    {
        GameObject prefab = tilePrefabs[Random.Range(0, tilePrefabs.Length)];
        GameObject tile = Instantiate(prefab, new Vector3(0f, 0f, nextZ), Quaternion.identity, transform);
        var tileComp = tile.GetComponent<Tile>();
        tileComp?.RefreshContents(this, safe);
        activeTiles.Add(tile);
        nextZ += tileLength;
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
    /// <summary>สปอว์นชิ้นใหม่ด้านหน้า (คืนค่า false = เฟรมนี้ไม่มีที่ลง/ข้าม)</summary>
    private bool SpawnByAnchor(bool safe)
    {
        var prefab = ChooseTilePrefab();

        // คำนวณโพสที่ควรจะวาง
        var ghost = Instantiate(prefab);
        ghost.SetActive(false);
        Pose p = ComputePoseFor(ghost);

        var t = ghost.GetComponent<Tile>();

        // ประกาศ cells ให้ชัดเจนกัน CS0165
        List<Vector3Int> cells = null;

        // ถ้าเปิด GridGuard: ต้องจองเซลล์ให้ได้ก่อน
        if (useGridGuard && !TryReserveCells(t, p, out cells))
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

                cells = null; // รีเซ็ตก่อนจองใหม่
                if (useGridGuard && !TryReserveCells(t, p, out cells))
                {
                    Destroy(ghost);
                    return false; // ไม่มีที่ลง → ข้ามในเฟรมนี้ (กันค้าง)
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

        Pose p = ComputePoseFor(oldest);
        var t = oldest.GetComponent<Tile>();

        // จองเซลล์ใหม่ให้สำเร็จก่อนค่อยย้าย
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

        // ถ้าย้ายไม่ได้จริง ๆ → ทำลายแล้วสปอว์นใหม่แทน (เฟรมถัดไปจะลองต่อ)
        Destroy(oldest);
        SpawnByAnchor(false);
    }

    // =================== เลือก Prefab ===================
    private GameObject ChooseTilePrefab()
    {
        bool wantTurn = allowTurns && Random.value < turnChance;
        if (preventConsecutiveTurns && lastWasTurn) wantTurn = false;

        List<GameObject> list = new();
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
        List<GameObject> list = new();
        foreach (var pf in tilePrefabs)
        {
            var tt = pf.GetComponent<Tile>();
            if (tt && tt.turnKind == TurnKind.Straight) list.Add(pf);
        }
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    // =================== คำนวณ Pose (เหมือน Align แต่ไม่แตะ Transform จริง) ===================
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
    /// <summary>พยายามจองเซลล์สำหรับไทล์ ณ โพสที่กำหนด (สำเร็จ = true)</summary>
    private bool TryReserveCells(Tile t, Pose p, out List<Vector3Int> cells)
    {
        cells = new List<Vector3Int>();
        if (!useGridGuard) return true;

        Vector3 origin = p.position;
        Vector3 forward = (p.rotation * Vector3.forward).normalized;

        int steps = Mathf.Max(1, forwardCellsPerTile);
        for (int i = 0; i < steps; i++)
        {
            // ใช้ 0.9f กันกรณีอยู่บนรอยต่อ cell พอดี
            Vector3 sample = origin + forward * (i * cellSize * 0.9f);
            var c = CellOf(sample);
            if (occupied.Contains(c)) return false;
            cells.Add(c);
        }

        foreach (var c in cells) occupied.Add(c);
        return true;
    }

    private Vector3Int CellOf(Vector3 worldPos)
    {
        int cx = Mathf.RoundToInt(worldPos.x / cellSize);
        int cy = Mathf.RoundToInt(worldPos.y / cellSize); // ปกติ y ~ 0
        int cz = Mathf.RoundToInt(worldPos.z / cellSize);
        return new Vector3Int(cx, cy, cz);
    }

    // =================== Anchor ต่อเส้น ===================
    private void AdvanceAnchor(Tile tile)
    {
        if (!tile)
        {
            anchor.position += anchor.forward * tileLength;
            return;
        }

        if (tile.HasSocketsSplit())
        {
            // ถ้าใช้ระบบเลือกซ้าย/ขวาแบบอัตโนมัติ ให้ใส่ที่นี่ (เวอร์ชันนี้รอ Trigger)
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

    // =================== API เดิม (Obstacle/Coin/Split) ===================
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
