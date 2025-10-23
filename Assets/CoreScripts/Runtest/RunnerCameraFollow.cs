using UnityEngine;

public class RunnerCameraFollow : MonoBehaviour
{
    public Transform target;        // ใส่ Player
    public Vector3 offset = new Vector3(0f, 6f, -8f);
    public float moveSmooth = 8f;   // ความนุ่มของการตาม
    public float rotSmooth = 8f;    // ความนุ่มของการหมุนตาม

    void LateUpdate()
    {
        if (!target) return;

        // ตำแหน่งเป้าหมาย = ตำแหน่งผู้เล่น + ออฟเซ็ตใน local space ของผู้เล่น
        Vector3 desiredPos = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * moveSmooth);

        // หมุนกล้องให้หันตามผู้เล่น (แต่ยังมองไปข้างหน้าของผู้เล่น)
        Quaternion desiredRot = Quaternion.LookRotation(target.forward, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, desiredRot, Time.deltaTime * rotSmooth);
    }
}
