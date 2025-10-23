using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class RunnerController : MonoBehaviour
{
    [Header("Forward Move")]
    public float baseSpeed = 10f;
    public float speedGainPerSecond = 0.2f;
    public float maxSpeed = 25f;

    [Header("Lane")]
    public float laneOffset = 3.0f;               // ระยะห่างแต่ละเลน (ซ้าย/กลาง/ขวา)
    [SerializeField] private int currentLane = 1;  // 0=ซ้าย,1=กลาง,2=ขวา
    public float laneSmoothTime = 0.08f;          // ความนุ่มเวลาย้ายเลน
    public float maxLateralSpeed = 25f;           // จำกัดความเร็วเฉือน

    [Header("Jump/Slide")]
    public float jumpForce = 8f;
    public float gravity = -25f;
    public float slideDuration = 0.6f;
    public float slideHeight = 1.0f;

    [Header("Turn")]
    public float turnLerpSpeed = 8f;         // ความเร็วหมุนตอนเลี้ยว
    public bool snapOnFinishTurn = true;     // ปรับมุมให้เป๊ะเมื่อใกล้เป้า
    public bool lockLaneWhileTurning = true; // ล็อกการเปลี่ยนเลนระหว่างเลี้ยว

    [Header("Refs (optional)")]
    public Animator anim;
    public CapsuleCollider capsuleForSlide; // ใช้เมื่อไม่ใช้ CharacterController

    // ===== private =====
    private CharacterController cc;
    private Vector3 velocity;
    private float speed;
    private bool isSliding = false;
    private float originalHeight;
    private Vector3 originalCenter;

    // หมุนเลี้ยว
    private bool isTurning = false;
    private Quaternion targetRotation;

    // ระบบเลน (อ้างอิงจากเส้นกึ่งกลางถนนในโลก)
    private float currentX = 0f;         // ระยะขวางจาก center line (หน่วยเมตร, + = ไปทาง transform.right)
    private float targetX = 0f;          // เป้าหมายของระยะขวาง
    private float xVel = 0f;             // สำหรับ SmoothDamp

    // เส้นกึ่งกลางถนน (world-space) ที่เราอ้างอิง
    private Vector3 centerLineWorld;     // world position ของเส้นกึ่งกลาง ณ ระยะ Z ปัจจุบัน
    private Vector3 preTurnCenterLine;   // center line ที่ “ตรึง” ไว้ตอนกำลังเลี้ยว

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        speed = baseSpeed;

        if (cc != null)
        {
            originalHeight = cc.height;
            originalCenter = cc.center;
        }
        else if (capsuleForSlide != null)
        {
            originalHeight = capsuleForSlide.height;
            originalCenter = capsuleForSlide.center;
        }

        // เริ่มที่เลนปัจจุบัน (กลางเลน = x=0)
        targetX = LaneIndexToX(currentLane);
        currentX = targetX;

        // คำนวณเส้นกึ่งกลางครั้งแรก
        centerLineWorld = transform.position - transform.right * currentX;
    }

    void Update()
    {
        // ความเร็วเดินหน้าเพิ่มตามเวลา
        speed = Mathf.Min(maxSpeed, speed + speedGainPerSecond * Time.deltaTime);

        // ===== Input เปลี่ยนเลน (ระงับถ้าล็อกระหว่างเลี้ยว) =====
        if (!isTurning || !lockLaneWhileTurning)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) ChangeLane(-1);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) ChangeLane(+1);
        }

        // กระโดด/สไลด์
        bool grounded = cc.isGrounded;
        if (grounded && velocity.y < 0) velocity.y = -2f;

        if ((Input.GetKeyDown(KeyCode.Space)) && grounded) Jump();
        if ((Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.S)) && grounded) Slide();

        // Swipe (ถ้ามี)
        var si = SwipeInput.Instance;
        if (si != null && (!isTurning || !lockLaneWhileTurning))
        {
            if (si.SwipedLeft) ChangeLane(-1);
            if (si.SwipedRight) ChangeLane(+1);
            if (si.SwipedUp && grounded) Jump();
            if (si.SwipedDown && grounded) Slide();
        }

        // ===== อัปเดต center line ก่อนคำนวณการเคลื่อนที่ =====
        if (!isTurning)
        {
            // center line ปัจจุบัน = ตำแหน่ง - (ทิศขวา * ระยะขวาง)
            centerLineWorld = transform.position - transform.right * currentX;
        }
        // (ขณะเลี้ยว เรา “ตรึง” center line ไว้ที่ preTurnCenterLine)

        // ===== หมุนเลี้ยว =====
        if (isTurning)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turnLerpSpeed);
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.8f)
            {
                if (snapOnFinishTurn) transform.rotation = targetRotation;
                isTurning = false;

                // ✅ หลังเลี้ยวจบ: snap ให้อยู่กลางเลนที่ใกล้ที่สุดบน “center line ที่ตรึงไว้”
                int nearestLane = Mathf.Clamp(Mathf.RoundToInt(currentX / laneOffset) + 1, 0, 2);
                currentLane = nearestLane;

                targetX = LaneIndexToX(currentLane);
                currentX = targetX;      // snap เข้าตำแหน่งเป๊ะ
                xVel = 0f;

                // ย้ายตำแหน่งผู้เล่นกลับเข้ากลางเลนตามทิศใหม่ (ใช้ center line ที่ตรึงขณะเลี้ยว)
                transform.position = preTurnCenterLine + transform.right * currentX;

                // อัปเดต center line สำหรับเฟรมถัดไป
                centerLineWorld = transform.position - transform.right * currentX;
            }
        }

        // ===== เลื่อนเข้าเลนอย่างนุ่ม (ถ้าไม่ได้ snap ตอนเลี้ยว) =====
        if (!isTurning)
        {
            currentX = Mathf.SmoothDamp(currentX, targetX, ref xVel, laneSmoothTime, maxLateralSpeed, Time.deltaTime);
            currentX = Mathf.Clamp(currentX, -laneOffset, laneOffset);
        }

        // ===== สร้างเวกเตอร์การเคลื่อนที่ =====
        Vector3 move = Vector3.zero;

        // เดินไปข้างหน้าเสมอ (ตามทิศหัว)
        move += transform.forward * speed;

        // ปรับตำแหน่งให้คงระยะจาก center line = currentX (แก้ drift)
        Vector3 desiredPosOnLane = (isTurning ? preTurnCenterLine : centerLineWorld) + transform.right * currentX;
        Vector3 lateralDelta = (desiredPosOnLane - transform.position);
        // เอาเฉพาะส่วนแกนขวา เพื่อหลีกเลี่ยงการแทรกทิศหน้า/สูง
        float lateralAlongRight = Vector3.Dot(lateralDelta, transform.right);
        move += transform.right * (lateralAlongRight / Mathf.Max(Time.deltaTime, 1e-5f));

        // แรงตกลง
        velocity.y += gravity * Time.deltaTime;
        move.y = velocity.y;

        cc.Move(move * Time.deltaTime);

        // อนิเมชัน
        if (anim)
        {
            anim.SetFloat("Speed", speed);
            anim.SetBool("Grounded", grounded);
        }
    }

    // ============ Helper ============

    float LaneIndexToX(int laneIndex) => (Mathf.Clamp(laneIndex, 0, 2) - 1) * laneOffset;

    void ChangeLane(int dir)
    {
        int newLane = Mathf.Clamp(currentLane + dir, 0, 2);
        if (newLane == currentLane) return;
        currentLane = newLane;
        targetX = LaneIndexToX(currentLane);
    }

    void Jump()
    {
        velocity.y = jumpForce;
        if (anim) anim.SetTrigger("Jump");
    }

    void Slide()
    {
        if (isSliding) return;
        StartCoroutine(CoSlide());
        if (anim) anim.SetTrigger("Slide");
    }

    System.Collections.IEnumerator CoSlide()
    {
        isSliding = true;
        if (cc != null)
        {
            cc.height = Mathf.Max(0.5f, originalHeight - slideHeight);
            cc.center = originalCenter - new Vector3(0, slideHeight * 0.5f, 0);
        }
        else if (capsuleForSlide != null)
        {
            capsuleForSlide.height = Mathf.Max(0.5f, originalHeight - slideHeight);
            capsuleForSlide.center = originalCenter - new Vector3(0, slideHeight * 0.5f, 0);
        }

        yield return new WaitForSeconds(slideDuration);

        if (cc != null)
        {
            cc.height = originalHeight;
            cc.center = originalCenter;
        }
        else if (capsuleForSlide != null)
        {
            capsuleForSlide.height = originalHeight;
            capsuleForSlide.center = originalCenter;
        }
        isSliding = false;
    }

    // ======= เลี้ยวเมื่อชน Trigger =======
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TurnLeft")) StartTurn(-90f);
        else if (other.CompareTag("TurnRight")) StartTurn(+90f);

        var marker = other.GetComponent<TurnMarker>();
        if (marker != null) StartTurn(marker.turnAngle);
    }

    private void StartTurn(float deltaYaw)
    {
        float newYaw = transform.eulerAngles.y + deltaYaw;
        targetRotation = Quaternion.Euler(0f, newYaw, 0f);
        isTurning = true;

        // 🔒 ตรึง center line ที่ใช้อ้างอิงระหว่างเลี้ยว (ป้องกัน drift)
        preTurnCenterLine = transform.position - transform.right * currentX;

        // ปิดการเปลี่ยนเลนระหว่างเลี้ยว (ทางเลือก)
        if (lockLaneWhileTurning)
        {
            targetX = LaneIndexToX(currentLane);
            xVel = 0f;
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Obstacle"))
        {
            GameManager.Instance.GameOver();
        }
    }
}
