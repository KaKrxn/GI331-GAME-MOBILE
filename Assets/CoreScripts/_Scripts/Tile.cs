using UnityEngine;

namespace TempleRun
{
    public enum TileType
    {
        STRAIGHT,
        LEFT,
        RIGHT,
        SIDEWAYS
    }

    /// <summary>
    /// Defines the attributes of a tile.
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [Header("Type")]
        public TileType type;

        [Header("Turn pivot (จุดใช้ตอนเลี้ยวครั้งแรก)")]
        public Transform pivot;

        [Header("Post-turn pivot (ใช้กับ LEFT/RIGHT หรือเป็น default กลาง)")]
        public Transform postTurnPivot;

        [Header("Post-turn pivot สำหรับ SIDEWAYS (ซ้าย/ขวา)")]
        public Transform postTurnPivotLeft;
        public Transform postTurnPivotRight;

        [Header("Lane points บน Tile นี้ (ซ้าย-กลาง-ขวา)")]
        public Transform laneLeft;
        public Transform laneMiddle;
        public Transform laneRight;
    }
}
