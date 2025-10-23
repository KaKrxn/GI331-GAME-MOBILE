using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TurnMarker : MonoBehaviour
{
    [Tooltip("����������ͧ��: ���� = ź, ��� = �ǡ (�� -90 ���� +90)")]
    public float turnAngle = 90f;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }
}
