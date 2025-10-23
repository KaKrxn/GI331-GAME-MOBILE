using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TurnMarker : MonoBehaviour
{
    [Tooltip("มุมเลี้ยวในองศา: ซ้าย = ลบ, ขวา = บวก (เช่น -90 หรือ +90)")]
    public float turnAngle = 90f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
}
