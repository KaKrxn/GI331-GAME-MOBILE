using UnityEngine;

public enum TurnKind { Straight, Left90, Right90, SplitLR }

[ExecuteAlways]
public class Tile : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform spawnedRoot;
    public Transform obstaclePointsRoot;

    [Header("Turn Sockets (optional)")]
    public Transform entrySocket;       // จุดหัวชิ้น (StartAnchor)
    public Transform exitSocket;        // จุดท้ายชิ้น (EndAnchor สำหรับตรง/เลี้ยวเดี่ยว)
    public Transform exitLeftSocket;    // จุดท้ายชิ้นเมื่อเลี้ยวซ้าย
    public Transform exitRightSocket;   // จุดท้ายชิ้นเมื่อเลี้ยวขวา
    public TurnKind turnKind = TurnKind.Straight;

    [Header("Anti-Overlap")]
    [Tooltip("BoxCollider (IsTrigger) ครอบพื้นที่พื้นของชิ้นนี้ ใช้ตรวจเขต/กันซ้อน")]
    public BoxCollider footprint;

    private Transform[] lanePoints;

    void Awake()
    {
        if (spawnedRoot == null) spawnedRoot = transform.Find("Spawned");
        if (obstaclePointsRoot == null) obstaclePointsRoot = transform.Find("ObstaclePoints");
        if (footprint == null) footprint = GetComponent<BoxCollider>();

        if (obstaclePointsRoot != null)
        {
            lanePoints = new Transform[obstaclePointsRoot.childCount];
            for (int i = 0; i < lanePoints.Length; i++)
                lanePoints[i] = obstaclePointsRoot.GetChild(i);
        }
        else lanePoints = new Transform[0];
    }

    void OnValidate()
    {
        if (footprint != null)
        {
            footprint.isTrigger = true;
            if (footprint.size.sqrMagnitude < 0.001f)
                Debug.LogWarning($"[Tile] '{name}' footprint size เล็กผิดปกติ", this);
        }
        else
        {
            var bc = GetComponent<BoxCollider>();
            if (bc) { footprint = bc; footprint.isTrigger = true; }
        }

        if (turnKind == TurnKind.Straight || turnKind == TurnKind.Left90 || turnKind == TurnKind.Right90)
        {
            if (!entrySocket || !exitSocket)
                Debug.LogWarning($"[Tile] '{name}' ต้องมี entrySocket และ exitSocket", this);
        }
        else if (turnKind == TurnKind.SplitLR)
        {
            if (!entrySocket || !exitLeftSocket || !exitRightSocket)
                Debug.LogWarning($"[Tile] '{name}' ต้องมี entrySocket/exitLeftSocket/exitRightSocket", this);
        }
    }

    public Transform GetExitSocket(bool? left = null)
    {
        if (turnKind == TurnKind.SplitLR)
        {
            if (left == true) return exitLeftSocket;
            if (left == false) return exitRightSocket;
            return null;
        }
        return exitSocket;
    }

    public void RefreshContents(TileSpawner spawner, bool safe)
    {
        Transform root = spawnedRoot ? spawnedRoot : transform;

        for (int i = root.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.GetChild(i).gameObject);

        if (safe) return;

        spawner.TrySpawnObstacle(root, lanePoints);
        spawner.TrySpawnCoin(root, lanePoints);
    }

    public bool HasSocketsStraight() =>
        entrySocket && exitSocket &&
        (turnKind == TurnKind.Straight || turnKind == TurnKind.Left90 || turnKind == TurnKind.Right90);

    public bool HasSocketsSplit() =>
        entrySocket && exitLeftSocket && exitRightSocket && turnKind == TurnKind.SplitLR;

    // ------- Gizmos ช่วยจัดวาง -------
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Gizmos.DrawRay(transform.position + Vector3.up * 0.02f, transform.forward * 1.5f);

        if (entrySocket)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(entrySocket.position, Vector3.one * 0.2f);
            DrawArrow(entrySocket.position, entrySocket.forward, 0.75f);
        }
        if (exitSocket)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(exitSocket.position, Vector3.one * 0.2f);
            DrawArrow(exitSocket.position, exitSocket.forward, 0.75f);
        }
        if (exitLeftSocket)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(exitLeftSocket.position, Vector3.one * 0.2f);
            DrawArrow(exitLeftSocket.position, exitLeftSocket.forward, 0.75f);
        }
        if (exitRightSocket)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(exitRightSocket.position, Vector3.one * 0.2f);
            DrawArrow(exitRightSocket.position, exitRightSocket.forward, 0.75f);
        }

        if (footprint)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Matrix4x4 m = Matrix4x4.TRS(footprint.bounds.center, Quaternion.identity, footprint.bounds.size);
            Gizmos.matrix = m;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    static void DrawArrow(Vector3 pos, Vector3 dir, float len)
    {
        Vector3 a = pos;
        Vector3 b = pos + dir.normalized * len;
        Gizmos.DrawLine(a, b);
        Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 - 25, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180 + 25, 0) * Vector3.forward;
        Gizmos.DrawLine(b, b + right * 0.25f);
        Gizmos.DrawLine(b, b + left * 0.25f);
    }
}
