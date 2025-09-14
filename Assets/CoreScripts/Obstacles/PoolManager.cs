using UnityEngine;
using System.Collections.Generic;

public class PooledIdentity : MonoBehaviour
{
    public string poolId;
    public int prefabIndex; // index ในอาร์เรย์ของพูลนี้
}

public class PoolManager : MonoBehaviour
{
    public static PoolManager I;

    [System.Serializable]
    public class Pool
    {
        public string id;                 // เช่น "Normal", "Special"
        public GameObject[] prefabs;      // พรีแฟบในพูลนี้
        public int preloadEach = 0;       // จำนวนสร้างล่วงหน้าต่อพรีแฟบ (0=ไม่ preload)
    }

    [Header("Pools")]
    public Pool[] pools;

    [Header("Random Controls")]
    public bool avoidImmediateRepeat = true;   // กันสุ่มซ้ำติดกัน
    [Tooltip("ห้ามซ้ำภายใน N ครั้งล่าสุด (0=ปิด)")]
    public int noRepeatWindow = 0;

    [Header("Spawn/End Anchors (กำหนดระดับซีน)")]
    public Transform normalSpawnRef;           // เกิดปกติ
    public Transform specialSpawnRef;          // เกิดพิเศษ
    public Transform globalEndPoint;           // EndPoint สำหรับของทุกชิ้น

    [Header("Special Switch")]
    public float specialAfterSeconds = 30f;    // เมื่อเวลาถึง → ใช้พูลพิเศษ
    public string normalPoolId = "Normal";
    public string specialPoolId = "Special";

    // -------- runtime --------
    class Runtime
    {
        public Pool conf;
        public List<Queue<GameObject>> queues = new(); // 1 queue ต่อ prefab
        public Queue<int> recent = new();              // กันซ้ำ index ล่าสุด
    }
    readonly Dictionary<string, Runtime> map = new();
    static float gameStartTime = -1f;
    const int SafetyTries = 32;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        if (gameStartTime < 0f) gameStartTime = Time.time;

        // สร้าง runtime + preload
        foreach (var p in pools)
        {
            if (p == null || string.IsNullOrEmpty(p.id) || p.prefabs == null || p.prefabs.Length == 0) continue;

            var rt = new Runtime { conf = p };
            for (int i = 0; i < p.prefabs.Length; i++)
            {
                var q = new Queue<GameObject>();
                rt.queues.Add(q);

                for (int k = 0; k < p.preloadEach; k++)
                {
                    var go = Instantiate(p.prefabs[i]);
                    SetupIdentity(go, p.id, i);
                    go.SetActive(false);
                    q.Enqueue(go);
                }
            }
            map[p.id] = rt;
        }
    }

    // ---------- Public high-level API ----------
    public void SpawnNext()
    {
        bool useSpecial = (Time.time - gameStartTime) >= specialAfterSeconds && HasPool(specialPoolId);
        if (useSpecial)
        {
            if (specialSpawnRef) Acquire(specialPoolId, specialSpawnRef.position, specialSpawnRef.rotation);
            else Debug.LogWarning("[PoolManager] specialSpawnRef ยังว่าง");
        }
        else
        {
            if (normalSpawnRef) Acquire(normalPoolId, normalSpawnRef.position, normalSpawnRef.rotation);
            else Debug.LogWarning("[PoolManager] normalSpawnRef ยังว่าง");
        }
    }

    public GameObject Acquire(string poolId, Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (!map.TryGetValue(poolId, out var rt))
        {
            Debug.LogWarning($"[PoolManager] ไม่พบพูล '{poolId}'");
            return null;
        }

        int idx = PickRandomIndex(rt);
        var q = rt.queues[idx];

        GameObject go = null;
        if (q.Count > 0)
        {
            go = q.Dequeue();
            if (!go) return Acquire(poolId, pos, rot, parent);
        }
        else
        {
            var prefab = rt.conf.prefabs[idx];
            go = Instantiate(prefab);
            SetupIdentity(go, poolId, idx);
        }

        if (parent) go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);

        // ให้ PoolAgent รู้จัก EndPoint + ผู้จัดการ
        var agent = go.GetComponent<PoolAgent>();
        if (agent) agent.Configure(this, poolId, globalEndPoint);

        PushRecent(rt, idx);
        return go;
    }

    public void Return(GameObject go)
    {
        if (!go) return;
        var id = go.GetComponent<PooledIdentity>();
        if (id == null)
        {
            Debug.LogWarning("[PoolManager] Object ไม่มี PooledIdentity");
            return;
        }
        if (!map.TryGetValue(id.poolId, out var rt)) return;

        go.SetActive(false);
        go.transform.SetParent(transform, false);
        int i = Mathf.Clamp(id.prefabIndex, 0, rt.queues.Count - 1);
        rt.queues[i].Enqueue(go);

        // หลังคืน → spawn ตัวใหม่ตามกติกา
        SpawnNext();
    }

    // ---------- helpers ----------
    void SetupIdentity(GameObject go, string poolId, int prefabIndex)
    {
        var id = go.GetComponent<PooledIdentity>() ?? go.AddComponent<PooledIdentity>();
        id.poolId = poolId;
        id.prefabIndex = prefabIndex;

        // แน่ใจว่ามี PoolAgent
        var agent = go.GetComponent<PoolAgent>() ?? go.AddComponent<PoolAgent>();
        agent.Configure(this, poolId, globalEndPoint);
    }

    bool HasPool(string id) => !string.IsNullOrEmpty(id) && map.ContainsKey(id);

    int PickRandomIndex(Runtime rt)
    {
        int n = rt.conf.prefabs.Length;
        if (n == 1) return 0;

        int tries = SafetyTries;
        while (tries-- > 0)
        {
            int idx = Random.Range(0, n);
            if (IsAllowed(rt, idx)) return idx;
        }
        return Random.Range(0, n);
    }

    bool IsAllowed(Runtime rt, int idx)
    {
        if (!avoidImmediateRepeat && noRepeatWindow <= 0) return true;
        foreach (var r in rt.recent) if (r == idx) return false;
        return true;
    }

    void PushRecent(Runtime rt, int idx)
    {
        int maxKeep = Mathf.Max(avoidImmediateRepeat ? 1 : 0, noRepeatWindow);
        if (maxKeep <= 0) return;
        rt.recent.Enqueue(idx);
        while (rt.recent.Count > maxKeep) rt.recent.Dequeue();
    }
}
