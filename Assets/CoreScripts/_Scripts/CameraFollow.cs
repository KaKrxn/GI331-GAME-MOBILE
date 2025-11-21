// CameraFollow.cs  (with anti-clipping)
using UnityEngine;

[ExecuteAlways]
public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform target;                 // ลาก Player มาใส่

    [Header("Position (Relative to Target)")]
    public float height = 7.5f;              // ความสูงของกล้อง
    public float distance = 10f;             // ระยะห่างด้านหลังเป้าหมาย (ตามแกน Z)
    public float xFollowStrength = 0.35f;    // ตามแกน X มากน้อย (0=ไม่ตาม, 1=ตามเต็ม)
    public float xSmoothTime = 0.10f;        // นุ่มนวลในการตามแกน X
    public float ySmoothTime = 0.10f;        // นุ่มนวลแกน Y (ความสูง)
    public float zSmoothTime = 0.12f;        // นุ่มนวลแกน Z (ระยะตามหลัง)

    [Header("Look / Aim")]
    public float lookAheadZ = 6f;            // มองไปข้างหน้าเหนือหัวตัวละคร
    public float tiltDownAngle = 10f;        // ก้มลงเล็กน้อยให้เห็นพื้น
    public bool useWorldForward = true;      // true=กล้องหันไป +Z โลกเสมอ, false=อิงตาม forward ของเป้าหมาย

    [Header("Limits")]
    public bool clampX = true;               // ลิมิตขอบซ้าย/ขวาที่กล้องยอมตาม
    public float minX = -4.5f;
    public float maxX = 4.5f;

    [Header("FOV & Misc")]
    public Camera cam;
    public float fieldOfView = 65f;          // มุมมองกล้อง
    public float fovSmoothTime = 0.2f;       // นุ่มนวลเปลี่ยน FOV (เผื่อปรับตอนสปีดอัพ)

    [Header("Anti-Clipping")]
    public LayerMask collisionMask = ~0;     // เลเยอร์ที่ให้ชน (ตั้งค่า exclude Player/Triggers)
    public float collisionRadius = 0.35f;    // รัศมี SphereCast ป้องกันมุมชนแล้วสั่น
    public float collisionBuffer = 0.15f;    // เผื่อระยะห่างจากผิววัตถุ
    public float collisionSmooth = 0.06f;    // นุ่มนวลตอนย่อ/คลายระยะเพราะชน
    public bool debugRays = false;

    // internals
    float xVel, yVel, zVel, fovVel;
    Vector3 collisionVel; // สำหรับ smooth โพสิชันหลังชน

    void Reset() { cam = GetComponent<Camera>(); }

    void LateUpdate()
    {
        if (!target) return;
        if (!cam) cam = GetComponent<Camera>();

        // --- คำนวณทิศทางหลักของโลก/เป้าหมาย ---
        Vector3 forwardDir = useWorldForward ? Vector3.forward : target.forward;
        forwardDir.y = 0f;
        if (forwardDir.sqrMagnitude < 1e-4f) forwardDir = Vector3.forward;
        forwardDir.Normalize();

        // จุดโฟกัสบนผู้เล่น (หัวไหล่/อก)
        Vector3 focus = target.position + Vector3.up * 1.2f;

        // ตำแหน่งกล้องที่ "อยากได้" (ยังไม่กันชน)
        Vector3 desiredPos =
            focus
            - forwardDir * distance
            + Vector3.up * height;

        // ผูก X ของกล้องเข้าใกล้ X ของผู้เล่นตาม strength ที่กำหนด
        float targetX = Mathf.Lerp(desiredPos.x, target.position.x, Mathf.Clamp01(xFollowStrength));
        if (clampX) targetX = Mathf.Clamp(targetX, minX, maxX);
        desiredPos.x = targetX;

        // ------------------------------------------------------
        //            ANTI-CLIPPING (SphereCast)
        // ------------------------------------------------------
        Vector3 camDir = desiredPos - focus;
        float camDist = camDir.magnitude;
        Vector3 camDirN = (camDist > 1e-4f) ? camDir / camDist : forwardDir * -1f;

        float allowedDist = camDist; // ระยะที่อนุญาตหลังตรวจชน
        if (camDist > 0.001f)
        {
            // ใช้ SphereCast เพื่อเลี่ยงขอบแหลม/เสากวนใจ
            if (Physics.SphereCast(focus, collisionRadius, camDirN, out RaycastHit hit, camDist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                allowedDist = Mathf.Max(0.0f, hit.distance - collisionBuffer);
                if (debugRays)
                {
                    Debug.DrawLine(focus, hit.point, Color.red);
                    Debug.DrawRay(hit.point, hit.normal * 0.5f, Color.yellow);
                }
            }
            else if (debugRays)
            {
                Debug.DrawLine(focus, desiredPos, Color.green);
            }
        }

        Vector3 collisionSafePos = focus + camDirN * allowedDist;

        // นุ่มนวลทีละแกนก่อน แล้วค่อยนุ่มนวลระยะชนอีกขั้น
        Vector3 pos = transform.position;
        float newX = (xSmoothTime > 0f) ? Mathf.SmoothDamp(pos.x, collisionSafePos.x, ref xVel, xSmoothTime) : collisionSafePos.x;
        float newY = (ySmoothTime > 0f) ? Mathf.SmoothDamp(pos.y, collisionSafePos.y, ref yVel, ySmoothTime) : collisionSafePos.y;
        float newZ = (zSmoothTime > 0f) ? Mathf.SmoothDamp(pos.z, collisionSafePos.z, ref zVel, zSmoothTime) : collisionSafePos.z;
        Vector3 smoothedPos = new Vector3(newX, newY, newZ);

        // เพิ่มความนุ่มนวลรวมอีกชั้นตอนเกิดชน/คลายชน
        if (collisionSmooth > 0f)
            transform.position = Vector3.SmoothDamp(transform.position, smoothedPos, ref collisionVel, collisionSmooth);
        else
            transform.position = smoothedPos;

        // --- หมุนกล้องให้มองไปข้างหน้า + tilt ลงเล็กน้อย ---
        Vector3 lookTarget = focus + forwardDir * lookAheadZ; // มองล่วงหน้า
        Vector3 lookDir = (lookTarget - transform.position);
        if (lookDir.sqrMagnitude < 1e-4f) lookDir = forwardDir;

        Quaternion lookRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        lookRot = Quaternion.AngleAxis(tiltDownAngle, transform.right) * lookRot;
        transform.rotation = lookRot;

        // --- ปรับ FOV นุ่มนวล ---
        if (cam != null)
        {
            float newFov = (fovSmoothTime > 0f)
                ? Mathf.SmoothDamp(cam.fieldOfView, fieldOfView, ref fovVel, fovSmoothTime)
                : fieldOfView;
            cam.fieldOfView = newFov;
        }
    }

    // API เผื่อเรียกจากสคริปต์อื่น (เพิ่มเอฟเฟกต์สปีด/บูสต์)
    public void SetTargetFOV(float targetFov, float smooth = 0.2f)
    {
        fieldOfView = targetFov;
        fovSmoothTime = smooth;
    }
}
