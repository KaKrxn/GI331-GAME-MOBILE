using UnityEngine;

public class CinematicRunnerCamera : MonoBehaviour
{
    public enum Side { RightOfTrack, LeftOfTrack }

    [Header("Target")]
    public Transform target;                // ลาก Player มาใส่
    public Transform lookTargetOverride;    // ถ้าอยากล็อกสายตาที่หัว/อกของโมเดล

    [Header("Side View")]
    public Side side = Side.RightOfTrack;   // วางกล้องไว้ "ขวา" หรือ "ซ้าย" ของลู่วิ่ง
    public float sideDistance = 8f;         // ระยะห่างด้านข้างจากแกนกลางลู่วิ่ง
    public float height = 7f;               // ความสูงกล้อง
    public float distanceBack = 0f;         // ดึงถอยหลังตามทิศวิ่งของ player

    [Header("Follow (ลื่นตามความสูง / ระยะตามแนววิ่ง)")]
    [Tooltip("ค่ามาก = ตามความสูงของผู้เล่นลื่นขึ้น")]
    public float ySmooth = 0.12f;
    [Tooltip("ค่ามาก = ตามตำแหน่งระยะไกลหน้า/หลัง 'ตามแนววิ่ง' ลื่นขึ้น")]
    public float zSmooth = 0.14f;

    [Header("Look / Aim")]
    public float lookAheadZ = 8f;           // มองไปข้างหน้าในทิศวิ่งของ player
    public float sideYawBias = 12f;         // หันเฉียงตามทิศวิ่งเล็กน้อย (เพิ่มมิติ)
    public float tiltDownAngle = 10f;       // ก้มกล้องลงเล็กน้อย

    [Header("Clamp (ตามแนววิ่ง)")]
    public bool clampZ = false;             // ถ้าอยากล็อกช่วงระยะตามแนววิ่ง
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

    void Reset()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (!target) return;
        if (!cam) cam = GetComponent<Camera>();

        // ทิศวิ่งของ player บนพื้น (ignore Y)
        Vector3 trackForward = target.forward;
        trackForward.y = 0f;
        if (trackForward.sqrMagnitude < 0.0001f)
            trackForward = Vector3.forward;
        trackForward.Normalize();

        // ด้านข้างของลู่วิ่ง (ตั้งฉากกับทิศวิ่ง)
        Vector3 trackRight = Vector3.Cross(Vector3.up, trackForward);

        // จุดโฟกัสบนตัวผู้เล่น
        Vector3 focus = lookTargetOverride
            ? lookTargetOverride.position
            : target.position + Vector3.up * 1.2f;

        float sideSign = (side == Side.RightOfTrack) ? +1f : -1f;
        Vector3 sideDir = trackRight * sideSign;

        // ตำแหน่งเป้าหมายของกล้อง (ด้านข้าง + ถอยหลังตามทิศวิ่ง)
        Vector3 desiredPos =
            focus
            + sideDir * sideDistance
            - trackForward * distanceBack;

        float desiredY = focus.y + (height - 1.2f); // ความสูงตามที่ตั้ง
        float newY = (ySmooth > 0f)
            ? Mathf.SmoothDamp(transform.position.y, desiredY, ref yVel, ySmooth)
            : desiredY;

        // ระยะตามแนววิ่ง (project position onto trackForward)
        float curAlong = Vector3.Dot(transform.position, trackForward);
        float desiredAlong = Vector3.Dot(desiredPos, trackForward);

        if (clampZ)
            desiredAlong = Mathf.Clamp(desiredAlong, minZ, maxZ);

        float newAlong = (zSmooth > 0f)
            ? Mathf.SmoothDamp(curAlong, desiredAlong, ref zVel, zSmooth)
            : desiredAlong;

        // สร้างตำแหน่งสุดท้าย: เริ่มจาก desiredPos แล้วเลื่อนตามแนววิ่งด้วยค่า smooth
        float offsetAlong = newAlong - desiredAlong;
        Vector3 finalPos = desiredPos + trackForward * offsetAlong;
        finalPos.y = newY;

        transform.position = finalPos;

        // ================== หมุนกล้อง ==================

        // มองล่วงหน้าตามทิศวิ่งของ player
        Vector3 lookAhead = trackForward * lookAheadZ;
        Vector3 lookPoint = focus + lookAhead;

        Vector3 lookDir = (lookPoint - transform.position);
        if (lookDir.sqrMagnitude < 0.0001f)
            lookDir = -sideDir; // กันศูนย์

        Quaternion lookRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        // เพิ่มหันเฉียงตามทิศวิ่ง (ทำให้เห็นด้านหน้าเส้นทางมากขึ้น)
        lookRot = Quaternion.AngleAxis(sideYawBias * sideSign, Vector3.up) * lookRot;

        // ก้มลงเล็กน้อย
        lookRot = Quaternion.AngleAxis(tiltDownAngle, transform.right) * lookRot;

        transform.rotation = lookRot;

        // ================== FOV ตามสปีด ==================
        float tSpeed = Mathf.InverseLerp(fovAtSpeedMin, fovAtSpeedMax, speedForFOV);
        float targetFov = Mathf.Lerp(baseFOV, maxFOV, Mathf.Clamp01(tSpeed));
        cam.fieldOfView = Mathf.SmoothDamp(cam.fieldOfView, targetFov, ref fovVel, fovSmooth);
    }

    // ให้สคริปต์ผู้เล่นส่งความเร็วมาได้
    public void SetSpeedForFOV(float speed) { speedForFOV = speed; }
}
