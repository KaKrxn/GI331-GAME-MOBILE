// SegmentConnector.cs
using UnityEngine;

public class SegmentConnector : MonoBehaviour
{
    public Transform startAnchor;
    public Transform endAnchor;

    [Tooltip("ความยาวโดยประมาณ (สำรองใช้เมื่อไม่มี Anchor)")]
    public float approxLength = 10f;

    public Vector3 GetForward()
    {
        if (startAnchor && endAnchor)
        {
            Vector3 dir = (endAnchor.position - startAnchor.position);
            dir.y = 0f;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        }
        return Vector3.forward;
    }
}
