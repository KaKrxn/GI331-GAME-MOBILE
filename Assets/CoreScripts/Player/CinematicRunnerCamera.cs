using UnityEngine;

public class CinematicRunnerCamera : MonoBehaviour
{
    public enum Side { RightOfTrack, LeftOfTrack }

    [Header("Target")]
    public Transform target;                // ลาก Player มาใส่
    public Transform lookTargetOverride;    // ถ้าอยากล็อกสายตาที่หัว/อกของโมเดล

    [Header("Side View")]
    public Side side = Side.RightOfTrack;   // วางกล้องไว้ "ขวา" หรือ "ซ้าย" ของลู่วิ่ง
    public float sideDistance = 8f;         // ระยะห่างด้านข้างจากแกนกลางลู่วิ่ง (แกน X)
    public float height = 7f;               // ความสูงกล้อง
    public float distanceBack = 0f;         // ดึงถอยหลังเล็กน้อยถ้าต้องการ (ส่วนใหญ่ 0 ก็พอ)

    [Header("Follow (ไม่ตามด้านข้าง เพื่อคงมุมด้านข้างจริงๆ)")]
    [Tooltip("ค่ามาก=ตามความสูงของผู้เล่นมากขึ้น")]
    public float ySmooth = 0.12f;
    [Tooltip("ค่ามาก=ตามตำแหน่งระยะไกลหน้า/หลัง (Z) ลื่นขึ้น")]
    public float zSmooth = 0.14f;

    [Header("Look / Aim")]
    public float lookAheadZ = 8f;           // มองไปข้างหน้าในทิศวิ่ง (แกน Z)
    public float sideYawBias = 12f;         // หันเฉียงตามทิศวิ่งเล็กน้อย (เพิ่มมิติ)
    public float tiltDownAngle = 10f;       // ก้มกล้องลงเล็กน้อย

    [Header("Clamp")]
    public bool clampZ = false;             // ถ้าอยากล็อกช่วง Z ที่กล้องวิ่งตาม
    public float minZ = -10f;
    public float maxZ = 9999f;

    [Header("FOV (ทางเลือก)")]
    public Camera cam;
    public float baseFOV = 65f;
    public float maxFOV = 78f;
    public float fovSmooth = 0.25f;
    [Tooltip("ส่งความเร็ววิ่งของผู้เล่นมา เพื่อขยาย FOV ตามสปีด")]
    public float speedForFOV = 0f;
    public float fovAtSpeedMin = 8f;
    public float fovAtSpeedMax = 20f;

    // internals
    float yVel, zVel, fovVel;

    void Reset() { cam = GetComponent<Camera>(); }

    void LateUpdate()
    {
        if (!target) return;
        if (!cam) cam = GetComponent<Camera>();

        // จุดโฟกัสบนตัวผู้เล่น
        Vector3 focus = lookTargetOverride ? lookTargetOverride.position : target.position + Vector3.up * 1.2f;

        // วางกล้องด้านข้าง "คงระยะ X" ตายตัว (ไม่ตามซ้าย/ขวาของผู้เล่น เพื่อให้เป็นมุมด้านข้างจริง ๆ)
        float sideSign = (side == Side.RightOfTrack) ? +1f : -1f;
        float desiredX = sideSign * sideDistance;

        // ตำแหน่งเป้าหมายของกล้อง (X คงที่, Y/Z ตามแบบลื่น ๆ)
        float desiredY = focus.y + (height - 1.2f); // ให้ความสูงเป็นไปตามค่าที่ตั้ง
        float desiredZ = focus.z - distanceBack;    // โดยปกติ 0 = เคียงข้างพอดี

        if (clampZ) desiredZ = Mathf.Clamp(desiredZ, minZ, maxZ);

        float newY = (ySmooth > 0f) ? Mathf.SmoothDamp(transform.position.y, desiredY, ref yVel, ySmooth) : desiredY;
        float newZ = (zSmooth > 0f) ? Mathf.SmoothDamp(transform.position.z, desiredZ, ref zVel, zSmooth) : desiredZ;

        transform.position = new Vector3(desiredX, newY, newZ);

        // หมุนกล้อง: มองเข้าหาผู้เล่น + มองล่วงหน้าในทิศวิ่งเล็กน้อย + tilt ลง + bias yaw
        Vector3 lookAhead = Vector3.forward * lookAheadZ; // ทิศวิ่ง = +Z
        Vector3 lookPoint = new Vector3(focus.x, focus.y, focus.z) + lookAhead;

        // ทิศมองพื้นฐาน
        Vector3 lookDir = (lookPoint - transform.position);
        if (lookDir.sqrMagnitude < 0.0001f) lookDir = Vector3.left * sideSign * -1f; // กันศูนย์

        Quaternion lookRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        // เพิ่มหันเฉียงตามทิศวิ่ง (ทำให้เห็นด้านหน้าเส้นทางมากขึ้น)
        lookRot = Quaternion.AngleAxis(sideYawBias * sideSign, Vector3.up) * lookRot;

        // ก้มลงเล็กน้อย
        lookRot = Quaternion.AngleAxis(tiltDownAngle, transform.right) * lookRot;

        transform.rotation = lookRot;

        // FOV ตามสปีด (ถ้าอยากได้เอฟเฟกต์เร็วแล้วภาพกว้าง)
        float tSpeed = Mathf.InverseLerp(fovAtSpeedMin, fovAtSpeedMax, speedForFOV);
        float targetFov = Mathf.Lerp(baseFOV, maxFOV, Mathf.Clamp01(tSpeed));
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovVel, fovSmooth);
    }

    // ให้สคริปต์ผู้เล่นส่งความเร็วมาได้
    public void SetSpeedForFOV(float speed) { speedForFOV = speed; }
}
