#if UNITY_EDITOR
using UnityEngine;

public class TileSocketGizmo : MonoBehaviour
{
    public Color entryColor = Color.cyan;
    public Color exitColor = Color.green;
    public float len = 1.5f;

    void OnDrawGizmos()
    {
        var tile = GetComponent<Tile>();
        if (!tile) return;

        if (tile.entrySocket)
        {
            Gizmos.color = entryColor;
            Gizmos.DrawLine(tile.entrySocket.position, tile.entrySocket.position + tile.entrySocket.forward * len);
            Gizmos.DrawSphere(tile.entrySocket.position, 0.1f);
        }
        if (tile.exitSocket)
        {
            Gizmos.color = exitColor;
            Gizmos.DrawLine(tile.exitSocket.position, tile.exitSocket.position + tile.exitSocket.forward * len);
            Gizmos.DrawCube(tile.exitSocket.position, Vector3.one * 0.15f);
        }
        if (tile.exitLeftSocket)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(tile.exitLeftSocket.position, tile.exitLeftSocket.position + tile.exitLeftSocket.forward * len);
        }
        if (tile.exitRightSocket)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(tile.exitRightSocket.position, tile.exitRightSocket.position + tile.exitRightSocket.forward * len);
        }
    }
}
#endif
