using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Forward & Vertical")]
    public float forwardSpeed = 8f;
    public float jumpForce = 7.5f;
    public float gravity = -30f;
    public float slideDuration = 0.6f;
    public float speedRampPerSec = 0.2f;

    [Header("Horizontal Step (X)")]
    public float stepLeft = 2.5f;    // ระยะที่ขยับเมื่อกดซ้าย
    public float stepRight = 2.5f;   // ระยะที่ขยับเมื่อกดขวา
    public float minX = -3.5f;       // ขอบซ้ายสุด
    public float maxX = 3.5f;       // ขอบขวาสุด
    [Tooltip("0 = สแน็ปทันที, ค่าน้อย = เร็ว, ค่าสูง = นุ่มนวลขึ้น")]
    public float xSmoothTime = 0.10f;

    // ภายใน
    float targetX;       // เป้าหมายแกน X
    float xVelocity;     // ความเร็วภายในของ SmoothDamp
    float verticalVel;   // ความเร็วแกน Y

    bool isSliding;
    CharacterController cc;
    Vector3 baseCenter;
    float baseHeight;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        baseCenter = cc.center; baseHeight = cc.height;
        targetX = transform.position.x; // เริ่มที่ตำแหน่งปัจจุบัน
    }

    void Update()
    {
        // เร่งความเร็วไปข้างหน้าเรื่อย ๆ
        //forwardSpeed += speedRampPerSec * Time.deltaTime;

        // ----- Input ซ้าย/ขวา -----
        if (Input.GetKeyDown(KeyCode.LeftArrow) || SwipeLeft())
        {
            MoveHorizontal(-1);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) || SwipeRight())
        {
            MoveHorizontal(+1);
        }

        // ----- กระโดด/สไลด์ -----
        if (IsGrounded())
        {
            if ((Input.GetKeyDown(KeyCode.UpArrow) || SwipeUp()) && !isSliding) Jump();
            if ((Input.GetKeyDown(KeyCode.DownArrow) || SwipeDown()) && !isSliding) StartCoroutine(Slide());
            if (verticalVel < 0) verticalVel = -2f; // ตรึงให้ติดพื้น
        }
        verticalVel += gravity * Time.deltaTime;

        // ----- คำนวณ X แบบ SmoothDamp -----
        float newX;
        if (xSmoothTime <= 0f)
        {
            // สแน็ปทันที
            newX = targetX;
            xVelocity = 0f;
        }
        else
        {
            newX = Mathf.SmoothDamp(transform.position.x, targetX, ref xVelocity, xSmoothTime);
        }

        // เราจะแปลงการย้าย X เป็นความเร็วสำหรับ CharacterController.Move เพียงครั้งเดียวต่อเฟรม
        float deltaX = newX - transform.position.x;

        // ----- รวมเวกเตอร์การเคลื่อนที่ -----
        Vector3 move =
            //Vector3.forward * forwardSpeed +        // ไปข้างหน้า
            Vector3.up * verticalVel +              // แรงดึงโลก/กระโดด
            Vector3.right * (deltaX / Time.deltaTime); // แปลง deltaX เป็นความเร็วต่อวินาที

        cc.Move(move * Time.deltaTime);
    }

    void MoveHorizontal(int dir)
    {
        // dir = -1 ซ้าย, +1 ขวา
        float step = (dir < 0) ? -stepLeft : stepRight;
        float nextTarget = Mathf.Clamp(targetX + step, minX, maxX);
        targetX = nextTarget;
    }

    void Jump()
    {
        verticalVel = jumpForce;
    }

    System.Collections.IEnumerator Slide()
    {
        isSliding = true;
        // ลดความสูง collider ชั่วคราว
        cc.height = baseHeight * 0.5f;
        cc.center = baseCenter + Vector3.down * (baseHeight * 0.25f);

        yield return new WaitForSeconds(slideDuration);

        cc.height = baseHeight;
        cc.center = baseCenter;
        isSliding = false;
    }

    bool IsGrounded()
    {
        return cc.isGrounded;
    }

    // ----- Swipe detection แบบง่าย -----
    Vector2 startPos; bool swiping;
    bool SwipeLeft() { return SwipeDir(Vector2.left); }
    bool SwipeRight() { return SwipeDir(Vector2.right); }
    bool SwipeUp() { return SwipeDir(Vector2.up); }
    bool SwipeDown() { return SwipeDir(Vector2.down); }

    bool SwipeDir(Vector2 dir)
    {
        const float minDist = 50f; // พิกเซล
        if (Input.touchCount == 0) return false;
        var t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began) { startPos = t.position; swiping = true; }
        if (!swiping) return false;
        if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Ended)
        {
            Vector2 delta = t.position - startPos;
            if (delta.magnitude >= minDist)
            {
                swiping = false;
                Vector2 nd = delta.normalized;
                if (Vector2.Dot(nd, dir) > 0.7f) return true;
            }
            if (t.phase == TouchPhase.Ended) swiping = false;
        }
        return false;
    }
}
