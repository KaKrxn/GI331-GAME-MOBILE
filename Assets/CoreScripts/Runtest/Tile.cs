using UnityEngine;

public enum TurnKind { Straight, Left90, Right90, SplitLR }

public class Tile : MonoBehaviour
{
    [Header("Spawn Points")]
    public Transform spawnedRoot;
    public Transform obstaclePointsRoot;

    [Header("Turn Sockets (optional)")]
    public Transform entrySocket;
    public Transform exitSocket;
    public Transform exitLeftSocket;
    public Transform exitRightSocket;
    public TurnKind turnKind = TurnKind.Straight;

    [Header("Anti-Overlap")]
    [Tooltip("BoxCollider (IsTrigger) ครอบพื้นที่พื้นของชิ้นนี้ ใช้เป็นรอยเท้าสำหรับตรวจชน")]
    public BoxCollider footprint;

    private Transform[] lanePoints;

    void Awake()
    {
        if (spawnedRoot == null) spawnedRoot = transform.Find("Spawned");
        if (obstaclePointsRoot == null) obstaclePointsRoot = transform.Find("ObstaclePoints");
        if (footprint == null) footprint = GetComponent<BoxCollider>(); // เผื่อไม่ได้ลากใน Inspector

        if (obstaclePointsRoot != null)
        {
            lanePoints = new Transform[obstaclePointsRoot.childCount];
            for (int i = 0; i < lanePoints.Length; i++)
                lanePoints[i] = obstaclePointsRoot.GetChild(i);
        }
        else lanePoints = new Transform[0];
    }

    public void RefreshContents(TileSpawner spawner, bool safe)
    {
        Transform root = spawnedRoot ? spawnedRoot : transform;

        for (int i = root.childCount - 1; i >= 0; i--)
            Object.Destroy(root.GetChild(i).gameObject);

        if (safe) return;

        spawner.TrySpawnObstacle(root, lanePoints);
        spawner.TrySpawnCoin(root, lanePoints);
    }

    public bool HasSocketsStraight() =>
        entrySocket && exitSocket &&
        (turnKind == TurnKind.Straight || turnKind == TurnKind.Left90 || turnKind == TurnKind.Right90);

    public bool HasSocketsSplit() =>
        entrySocket && exitLeftSocket && exitRightSocket && turnKind == TurnKind.SplitLR;
}
