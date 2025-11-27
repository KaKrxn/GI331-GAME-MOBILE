using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Events;
using TempleRun; // ใช้ Tile / TileType
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace TempleRun.Player
{
    /// <summary>
    /// คุมตัว Player:
    /// - วิ่งไปข้างหน้าอัตโนมัติ
    /// - Turn:
    ///     * ถ้าอยู่จุดเลี้ยว -> เลี้ยวแบบ pivot
    ///     * ถ้าไม่อยู่จุดเลี้ยว -> เปลี่ยนเลน (ไม่อิง Tile)
    /// - Jump, Slide, Score, GameOver
    /// - รองรับ Swipe: ซ้าย/ขวา/ปัดขึ้น
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Tooltip("ความเร็วเริ่มต้นของผู้เล่น")]
        private float initialPlayerSpeed = 4f;

        [SerializeField, Tooltip("ความเร็วสูงสุดของผู้เล่น")]
        private float maximumPlayerSpeed = 30f;

        [SerializeField, Tooltip("อัตราเพิ่มความเร็วต่อวินาที")]
        private float playerSpeedIncreaseRate = .1f;

        [SerializeField, Tooltip("ความสูงของการกระโดด")]
        private float jumpHeight = 1.0f;

        [SerializeField, Tooltip("ค่าแรงโน้มถ่วงเริ่มต้น (ค่าลบ)")]
        private float initialGravityValue = -9.81f;

        [SerializeField, Tooltip("Layer ของพื้นสำหรับเช็ค grounded")]
        private LayerMask groundLayer;

        [Header("Turn & Obstacles")]
        [SerializeField, Tooltip("Layer ของ collider จุดเลี้ยว")]
        private LayerMask turnLayer;

        [SerializeField, Tooltip("Layer ของสิ่งกีดขวางที่ชนแล้ว GameOver")]
        private LayerMask obstacleLayer;

        [Header("Lane Settings (ไม่อิง Tile)")]
        [SerializeField, Tooltip("ระยะห่างระหว่างเลน (ซ้าย-กลาง-ขวา)")]
        private float laneDistance = 2.5f;

        [SerializeField, Tooltip("เลนซ้าย/ขวาสุดจากกลาง (1 = 3 เลน)")]
        private int maxLaneOffset = 1; // -1, 0, 1

        [Header("Animation & Score")]
        [SerializeField, Tooltip("Animator ของ Player")]
        private Animator animator;

        [SerializeField, Tooltip("Animation Clip สำหรับสไลด์")]
        private AnimationClip slideAnimationClip;

        [SerializeField, Tooltip("ตัวคูณคะแนนต่อวินาที")]
        private float scoreMultiplier = 10f;

        [Header("Events")]
        [SerializeField, Tooltip("Event ตอนเลี้ยวสำเร็จ (ส่งทิศใหม่ให้ TileSpawner)")]
        private UnityEvent<Vector3> turnEvent;

        [SerializeField, Tooltip("เรียกเมื่อ Game Over (ส่ง score)")]
        private UnityEvent<int> gameOverEvent;

        [SerializeField, Tooltip("เรียกทุกเฟรมเมื่อคะแนนอัปเดต")]
        private UnityEvent<int> scoreUpdateEvent;

        [Header("Debug")]
        [SerializeField, Tooltip("ความเร็วปัจจุบันของผู้เล่น")]
        private float playerSpeed;

        [Header("Mobile Swipe")]
        [SerializeField, Tooltip("เปิด/ปิดระบบปัดหน้าจอ (มือถือ)")]
        private bool enableSwipe = true;

        [SerializeField, Tooltip("ระยะขั้นต่ำของการปัด (pixel)")]
        private float minSwipeDistance = 50f;

        [SerializeField, Tooltip("เวลา max ที่นับเป็นการปัด (วินาที)")]
        private float maxSwipeTime = 0.5f;

        #region Private Variables

        private PlayerInput playerInput;
        private InputAction turnAction;
        private InputAction jumpAction;
        private InputAction slideAction;
        private CharacterController controller;

        private float gravity;
        private Vector3 movementDirection = Vector3.forward;
        private Vector3 playerVelocity;
        private bool sliding = false;
        private float score = 0;
        private int slidingAnimationId;

        // ระบบเลนแบบเดิม: -1 = ซ้าย, 0 = กลาง, 1 = ขวา
        private int laneOffset = 0;

        // Swipe state
        private bool swipeInProgress = false;
        private Vector2 swipeStartPos;
        private float swipeStartTime;
        private int activeFingerId = -1;

        #endregion

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            controller = GetComponent<CharacterController>();

            slidingAnimationId = Animator.StringToHash("Sliding");

            if (playerInput != null && playerInput.actions != null)
            {
                turnAction  = playerInput.actions["Turn"];
                jumpAction  = playerInput.actions["Jump"];
                slideAction = playerInput.actions["Slide"];
            }
        }

        /// <summary>Subscribe input</summary>
        private void OnEnable()
        {
            if (turnAction  != null) turnAction.performed  += PlayerTurn;
            if (slideAction != null) slideAction.performed += PlayerSlide;
            if (jumpAction  != null) jumpAction.performed  += PlayerJump;

            if (enableSwipe)
            {
                EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
                TouchSimulation.Enable(); // ให้ลองปัดด้วยเมาส์ใน Editor ได้
#endif
            }
        }

        /// <summary>Unsubscribe input</summary>
        private void OnDisable()
        {
            if (turnAction  != null) turnAction.performed  -= PlayerTurn;
            if (slideAction != null) slideAction.performed -= PlayerSlide;
            if (jumpAction  != null) jumpAction.performed  -= PlayerJump;

            if (enableSwipe)
            {
#if UNITY_EDITOR
                TouchSimulation.Disable();
#endif
                EnhancedTouchSupport.Disable();
            }
        }

        private void Start()
        {
            playerSpeed = initialPlayerSpeed;
            gravity     = initialGravityValue;
        }

        // ======================
        // TURN + LANE
        // ======================

        /// <summary>
        /// Input ซ้าย/ขวา:
        /// - ถ้าตรงจุดเลี้ยว & ทิศถูก -> เลี้ยว
        /// - ถ้าไม่ใช่จุดเลี้ยว -> เปลี่ยนเลน
        /// </summary>
        private void PlayerTurn(InputAction.CallbackContext context)
        {
            float turnValue = context.ReadValue<float>(); // -1 ซ้าย, 1 ขวา
            HandleTurn(turnValue);
        }

        private void HandleTurn(float turnValue)
        {
            if (Mathf.Approximately(turnValue, 0f))
                return;

            // 1) พยายามเลี้ยวก่อน (ใช้ระบบเลี้ยวแบบเดิม)
            Vector3? turnPosition = CheckTurn(turnValue);
            if (turnPosition.HasValue)
            {
                Vector3 targetDirection =
                    Quaternion.AngleAxis(90 * turnValue, Vector3.up) * movementDirection;

                turnEvent?.Invoke(targetDirection);

                Turn(turnValue, turnPosition.Value);

                // reset เลนกลับกลางหลังเลี้ยว
                laneOffset = 0;
                return;
            }

            // 2) ถ้าไม่สามารถเลี้ยวได้ -> ใช้เป็น "เปลี่ยนเลน" แทน
            int dir       = turnValue < 0 ? -1 : 1;
            int newOffset = Mathf.Clamp(laneOffset + dir, -maxLaneOffset, maxLaneOffset);
            int delta     = newOffset - laneOffset;

            if (delta == 0)
                return;

            laneOffset = newOffset;

            // ใช้ local right ของตัว Player
            Vector3 sideMove = transform.right * laneDistance * delta;
            controller.Move(sideMove);

            // ❌ ไม่ GameOver ถ้ากดเลี้ยวตอนที่ไม่ได้อยู่ point
        }

        /// <summary>
        /// เช็คว่าตอนนี้อยู่จุดเลี้ยวไหม ใช้ระบบเดียวกับไฟล์ตัวอย่าง
        /// </summary>
        private Vector3? CheckTurn(float turnValue)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, .1f, turnLayer);
            if (hitColliders.Length == 0)
                return null;

            Tile tile = hitColliders[0].transform.parent.GetComponent<Tile>();
            if (tile == null) return null;

            TileType type = tile.type;
            bool canTurn =
                (type == TileType.LEFT    && turnValue == -1) ||
                (type == TileType.RIGHT   && turnValue ==  1) ||
                (type == TileType.SIDEWAYS);

            if (!canTurn || tile.pivot == null)
                return null;

            return tile.pivot.position;
        }

        /// <summary>
        /// ระบบเลี้ยว: Snap ไป pivot แล้วหมุน 90 องศา + เปลี่ยน direction
        /// </summary>
        private void Turn(float turnValue, Vector3 turnPosition)
        {
            // Teleport ไป pivot ก่อนเลี้ยว
            Vector3 tempPlayerPosition =
                new Vector3(turnPosition.x, transform.position.y, turnPosition.z);

            controller.enabled = false;
            transform.position = tempPlayerPosition;
            controller.enabled = true;

            // หมุนตัวไปตามทิศใหม่
            Quaternion targetRotation =
                transform.rotation * Quaternion.Euler(0, 90 * turnValue, 0);

            transform.rotation   = targetRotation;
            movementDirection    = transform.forward.normalized;
        }

        // ======================
        // SLIDE
        // ======================

        private void PlayerSlide(InputAction.CallbackContext context)
        {
            if (!sliding && IsGrounded())
            {
                StartCoroutine(Slide());
            }
        }

        private IEnumerator Slide()
        {
            sliding = true;

            // ย่อ collider ลงครึ่งหนึ่ง
            Vector3 originalCenter = controller.center;
            float   originalHeight = controller.height;

            controller.height /= 2f;

            Vector3 newCenter = originalCenter;
            newCenter.y      -= controller.height / 2f;
            controller.center = newCenter;

            // เล่นอนิเมชัน slide
            if (animator != null)
            {
                animator.Play(slidingAnimationId);
            }

            float slideDuration =
                (slideAnimationClip != null && animator != null)
                ? slideAnimationClip.length / animator.speed
                : 1f;

            yield return new WaitForSeconds(slideDuration);

            // รีเซ็ต collider กลับ
            controller.height = originalHeight;
            controller.center = originalCenter;
            sliding           = false;
        }

        // ======================
        // JUMP
        // ======================

        private void PlayerJump(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            HandleJump();
        }

        private void HandleJump()
        {
            if (!IsGrounded())
                return;

            // ใช้สูตรเดิม แค่ย้ายมาแยกเป็นฟังก์ชัน
            playerVelocity.y += Mathf.Sqrt(jumpHeight * gravity * -3f);
            controller.Move(playerVelocity * Time.deltaTime);
        }

        // ======================
        // UPDATE LOOP
        // ======================

        private void Update()
        {
            float dt = Time.deltaTime;

            // อัปเดต input จากการปัดหน้าจอ
            if (enableSwipe)
                UpdateSwipeInput();

            // ถ้าตกฉาก (raycast ยาวๆ) -> GameOver
            if (!IsGrounded(20f))
            {
                GameOver();
                return;
            }

            // อัปเดตคะแนน
            score += scoreMultiplier * dt;
            scoreUpdateEvent?.Invoke((int)score);

            // วิ่งไปด้านหน้าตามทิศของ transform
            controller.Move(transform.forward * playerSpeed * dt);

            bool grounded = IsGrounded();

            // รีเซ็ต velocity y ถ้าแตะพื้น
            if (grounded && playerVelocity.y < 0f)
            {
                playerVelocity.y = 0f;
            }

            // ใส่แรงโน้มถ่วง
            playerVelocity.y += gravity * dt;
            controller.Move(playerVelocity * dt);

            // เพิ่มความเร็ว/ความยากตามเวลา
            if (playerSpeed < maximumPlayerSpeed)
            {
                playerSpeed += dt * playerSpeedIncreaseRate;
                gravity      = initialGravityValue - playerSpeed;

                if (animator != null && animator.speed < 1.25f)
                {
                    animator.speed += (1f / playerSpeed) * dt;
                }
            }
        }

        // ======================
        // SWIPE INPUT (MOBILE)
        // ======================

        private void UpdateSwipeInput()
        {
            var touches = Touch.activeTouches;
            if (touches.Count == 0)
                return;

            foreach (var t in touches)
            {
                // เริ่มปัด
                if (!swipeInProgress && t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    swipeInProgress = true;
                    swipeStartPos   = t.screenPosition;
                    swipeStartTime  = Time.time;
                    activeFingerId  = t.finger.index;
                }
                // จบการปัด (นิ้วเดิม)
                else if (swipeInProgress &&
                         t.finger.index == activeFingerId &&
                         (t.phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                          t.phase == UnityEngine.InputSystem.TouchPhase.Canceled))
                {
                    Vector2 delta    = t.screenPosition - swipeStartPos;
                    float   duration = Time.time - swipeStartTime;

                    swipeInProgress = false;
                    activeFingerId  = -1;

                    // ไม่เข้าเงื่อนไข swipe
                    if (duration > maxSwipeTime)          return;
                    if (delta.magnitude < minSwipeDistance) return;

                    // ตัดสินใจแกนเด่น
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    {
                        // ปัดซ้าย/ขวา -> เลี้ยว/เปลี่ยนเลน
                        float dir = delta.x > 0 ? 1f : -1f;
                        HandleTurn(dir);
                    }
                    else
                    {
                        // ปัดขึ้น -> Jump (ยังไม่ทำปัดลง)
                        if (delta.y > 0)
                        {
                            HandleJump();
                        }
                    }

                    return; // อ่านแค่ swipe แรกพอ
                }
            }
        }

        // ======================
        // HELPERS
        // ======================

        private bool IsGrounded(float length = .2f)
        {
            Vector3 raycastOriginFirst = transform.position;
            raycastOriginFirst.y -= controller.height / 2f;
            raycastOriginFirst.y += .1f;

            Vector3 raycastOriginSecond = raycastOriginFirst;
            raycastOriginFirst  -= transform.forward * .2f;
            raycastOriginSecond += transform.forward * .2f;

            bool hit =
                Physics.Raycast(raycastOriginFirst,  Vector3.down, length, groundLayer) ||
                Physics.Raycast(raycastOriginSecond, Vector3.down, length, groundLayer);

            return hit;
        }

        private void GameOver()
        {
            Debug.Log("Game Over");
            gameOverEvent?.Invoke((int)score);
            gameObject.SetActive(false);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleLayer) != 0)
            {
                GameOver();
            }
        }
    }
}
