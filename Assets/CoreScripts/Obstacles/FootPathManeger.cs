// PathChainSpawner.cs  (with non-repeating + weighted random)
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FootPathManeger : MonoBehaviour
{
    [Header("Prefabs (ต้องมี SegmentConnector)")]
    public GameObject[] segmentPrefabs;

    [Header("Random Settings")]
    public bool avoidImmediateRepeat = true;   // กันซ้ำตัวล่าสุด
    [Tooltip("ห้ามซ้ำภายใน N ชิ้นล่าสุด (0 = ปิด)")]
    public int noRepeatWindow = 1;             // เช่น 2 = ห้ามซ้ำ 2 อันวางล่าสุด
    public bool useWeights = false;            // ใช้ถ่วงน้ำหนักไหม
    [Tooltip("ความยาวเท่ากับจำนวน Prefab หากไม่ได้ตั้งหรือไม่เท่ากันจะถือว่า weight=1 ทุกตัว")]
    public float[] weights;                    // นน.สุ่มของแต่ละ prefab

    [Header("Chain Settings")]
    public Transform chainRoot;
    public Transform startReference;
    public int initialSegments = 3;
    public int maintainSegmentsAhead = 3;
    public bool alignRotationToLast = true;

    [Header("Cleanup (กรณีไม่ได้ให้ชิ้นทำลายตัวเอง)")]
    public Transform playerOrCamera;
    public float destroyBehindDistance = 40f;
    public bool autoDestroyBehind = false;

    private readonly LinkedList<GameObject> chain = new LinkedList<GameObject>();
    private Transform _chainRoot;

    // เก็บประวัติล่าสุดไว้กันซ้ำ
    private readonly Queue<int> recentIndices = new Queue<int>();
    private const int MaxSafety = 50;

    void Awake() { _chainRoot = chainRoot ? chainRoot : transform; }

    void Start()
    {
        Vector3 basePos = startReference ? startReference.position : transform.position;
        Quaternion baseRot = startReference ? startReference.rotation : transform.rotation;

        GameObject first = SpawnSegment(null, basePos, baseRot, forceAtStart: true);
        var last = first;

        for (int i = 1; i < initialSegments; i++)
            last = SpawnSegment(last, Vector3.zero, Quaternion.identity);
    }

    void Update()
    {
        while (chain.Count < maintainSegmentsAhead)
        {
            GameObject last = chain.Last != null ? chain.Last.Value : null;
            SpawnSegment(last, Vector3.zero, Quaternion.identity);
        }

        if (autoDestroyBehind && playerOrCamera)
        {
            var node = chain.First;
            while (node != null)
            {
                var next = node.Next;
                if (IsFarBehind(node.Value.transform)) RemoveSegment(node.Value);
                node = next;
            }
        }
    }

    public void OnSegmentDestroyed(GameObject seg)
    {
        var node = chain.Find(seg);
        if (node != null) chain.Remove(node);

        while (chain.Count < maintainSegmentsAhead)
        {
            GameObject last = chain.Last != null ? chain.Last.Value : null;
            SpawnSegment(last, Vector3.zero, Quaternion.identity);
        }
    }

    GameObject SpawnSegment(GameObject lastSegment,
                            Vector3 startPosOverride,
                            Quaternion startRotOverride,
                            bool forceAtStart = false)
    {
        if (segmentPrefabs == null || segmentPrefabs.Length == 0) return null;

        int pick = PickIndex();
        GameObject prefab = segmentPrefabs[pick];
        GameObject seg = Instantiate(prefab, Vector3.zero, Quaternion.identity, _chainRoot);

        var conn = seg.GetComponent<SegmentConnector>();
        if (!conn)
        {
            Debug.LogWarning($"[{name}] Segment prefab '{prefab.name}' ไม่มี SegmentConnector!");
            Destroy(seg);
            return null;
        }

        if (lastSegment == null || forceAtStart)
        {
            Vector3 basePos = startReference ? startReference.position : transform.position;
            Quaternion baseRot = startReference ? startReference.rotation : transform.rotation;

            if (conn.startAnchor)
            {
                Vector3 offset = basePos - conn.startAnchor.position;
                seg.transform.position += offset;
            }
            else seg.transform.position = basePos;

            seg.transform.rotation = baseRot;
        }
        else
        {
            var lastConn = lastSegment.GetComponent<SegmentConnector>();

            Vector3 lastEndPos = lastConn && lastConn.endAnchor
                ? lastConn.endAnchor.position
                : lastSegment.transform.position + lastSegment.transform.forward * (lastConn ? lastConn.approxLength : 10f);

            Vector3 lastDir = lastConn ? lastConn.GetForward() : lastSegment.transform.forward;

            if (alignRotationToLast)
                seg.transform.rotation = Quaternion.LookRotation(lastDir, Vector3.up);

            if (conn.startAnchor)
            {
                Vector3 offset = lastEndPos - conn.startAnchor.position;
                seg.transform.position += offset;
            }
            else seg.transform.position = lastEndPos;
        }

        // life hook
        var life = seg.GetComponent<SegmentLifetime>();
        if (!life) life = seg.AddComponent<SegmentLifetime>();
        life.owner = this;

        chain.AddLast(seg);

        // อัปเดตหน้าต่างกันซ้ำ
        if (noRepeatWindow > 0 || avoidImmediateRepeat)
        {
            recentIndices.Enqueue(pick);
            int maxKeep = Mathf.Max(avoidImmediateRepeat ? 1 : 0, noRepeatWindow);
            while (recentIndices.Count > maxKeep) recentIndices.Dequeue();
        }

        return seg;
    }

    // ---------- RANDOM PICKER ----------
    int PickIndex()
    {
        int n = segmentPrefabs.Length;
        if (!useWeights || weights == null || weights.Length != n)
        {
            // สุ่มธรรมดา แต่กันซ้ำตามกติกา
            for (int safety = 0; safety < MaxSafety; safety++)
            {
                int idx = Random.Range(0, n);
                if (IsAllowed(idx)) return idx;
            }
            return Random.Range(0, n); // เผื่อสุดทาง
        }
        else
        {
            // สุ่มแบบถ่วงน้ำหนัก + กันซ้ำ
            for (int safety = 0; safety < MaxSafety; safety++)
            {
                int idx = WeightedPick(weights);
                if (IsAllowed(idx)) return idx;
            }
            return WeightedPick(weights);
        }
    }

    bool IsAllowed(int idx)
    {
        if (!avoidImmediateRepeat && noRepeatWindow <= 0) return true;
        // ห้ามซ้ำในคิวล่าสุด
        return !recentIndices.Contains(idx);
    }

    static int WeightedPick(float[] w)
    {
        float sum = 0f;
        for (int i = 0; i < w.Length; i++) sum += Mathf.Max(0f, w[i]);
        if (sum <= 0f) return Random.Range(0, w.Length);

        float r = Random.value * sum;
        float acc = 0f;
        for (int i = 0; i < w.Length; i++)
        {
            acc += Mathf.Max(0f, w[i]);
            if (r <= acc) return i;
        }
        return w.Length - 1;
    }

    bool IsFarBehind(Transform t)
    {
        if (!playerOrCamera) return false;
        float dz = playerOrCamera.position.z - t.position.z; // วิ่งตาม +Z
        return dz > destroyBehindDistance;
    }

    void RemoveSegment(GameObject seg)
    {
        chain.Remove(seg);
        if (seg) Destroy(seg);
    }
}

// ----------------------------------------------------------
// แจ้ง Spawner เวลา Segment โดน Destroy
// ----------------------------------------------------------
public class SegmentLifetime : MonoBehaviour
{
    [HideInInspector] public FootPathManeger owner;
    void OnDestroy()
    {
        if (!Application.isPlaying) return;
        if (owner) owner.OnSegmentDestroyed(gameObject);
    }
}
