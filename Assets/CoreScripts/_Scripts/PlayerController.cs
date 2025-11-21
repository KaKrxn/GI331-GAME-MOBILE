using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using TempleRun; // ใช้ Tile / TileType

namespace TempleRun.Player
{
    /// <summary>
    /// คุมตัว Player:
    /// - วิ่งไปข้างหน้าอัตโนมัติ
    /// - A/D หรือ ซ้าย/ขวา:
    ///     * ถ้าอยู่จุดเลี้ยว -> เลี้ยวแบบ pivot (เหมือนไฟล์ตัวอย่าง)
    ///     * ถ้าไม่อยู่จุดเลี้ยว -> เปลี่ยนเลน (ไม่อิง Tile)
    /// - Jump, Slide, Score, GameOver
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

        #endregion

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            controller = GetComponent<CharacterController>();

            slidingAnimationId = Animator.StringToHash("Sliding");

            if (playerInput != null && playerInput.actions != null)
            {
                turnAction = playerInput.actions["Turn"];
                jumpAction = playerInput.actions["Jump"];
                slideAction = playerInput.actions["Slide"];
            }
        }

        /// <summary>Subscribe input</summary>
        private void OnEnable()
        {
            if (turnAction != null) turnAction.performed += PlayerTurn;
            if (slideAction != null) slideAction.performed += PlayerSlide;
            if (jumpAction != null) jumpAction.performed += PlayerJump;
        }

        /// <summary>Unsubscribe input</summary>
        private void OnDisable()
        {
            if (turnAction != null) turnAction.performed -= PlayerTurn;
            if (slideAction != null) slideAction.performed -= PlayerSlide;
            if (jumpAction != null) jumpAction.performed -= PlayerJump;
        }

        private void Start()
        {
            playerSpeed = initialPlayerSpeed;
            gravity = initialGravityValue;
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
            if (Mathf.Approximately(turnValue, 0f))
                return;

            // 1) พยายามเลี้ยวก่อน (ใช้ระบบเลี้ยวแบบใน PlayerControllerNew.cs)
            Vector3? turnPosition = CheckTurn(turnValue);
            if (turnPosition.HasValue)
            {
                // ถ้าเลี้ยวได้
                Vector3 targetDirection =
                    Quaternion.AngleAxis(90 * turnValue, Vector3.up) * movementDirection;

                if (turnEvent != null)
                    turnEvent.Invoke(targetDirection);

                Turn(turnValue, turnPosition.Value);

                // reset เลนกลับกลางหลังเลี้ยว
                laneOffset = 0;
                return;
            }

            // 2) ถ้าไม่สามารถเลี้ยวได้ -> ใช้เป็น "เปลี่ยนเลน" แทน
            int dir = turnValue < 0 ? -1 : 1;
            int newOffset = Mathf.Clamp(laneOffset + dir, -maxLaneOffset, maxLaneOffset);
            int delta = newOffset - laneOffset;

            if (delta != 0)
            {
                laneOffset = newOffset;

                // ใช้ local right ของตัว Player
                Vector3 sideMove = transform.right * laneDistance * delta;
                controller.Move(sideMove);
            }

            // ❌ ไม่ GameOver ถ้ากดเลี้ยวตอนที่ไม่ได้อยู่ point
        }

        /// <summary>
        /// เช็คว่าตอนนี้อยู่จุดเลี้ยวไหม ใช้ระบบเดียวกับไฟล์ตัวอย่างที่เธอส่งมา
        /// </summary>
        private Vector3? CheckTurn(float turnValue)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, .1f, turnLayer);
            if (hitColliders.Length != 0)
            {
                Tile tile = hitColliders[0].transform.parent.GetComponent<Tile>();
                if (tile == null) return null;

                TileType type = tile.type;
                if ((type == TileType.LEFT && turnValue == -1) ||
                    (type == TileType.RIGHT && turnValue == 1) ||
                    (type == TileType.SIDEWAYS))
                {
                    return tile.pivot != null ? tile.pivot.position : (Vector3?)null;
                }
            }
            return null;
        }

        /// <summary>
        /// ระบบเลี้ยวจากไฟล์ตัวอย่าง: Snap ไป pivot แล้วหมุน 90 องศา + เปลี่ยน direction
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

            transform.rotation = targetRotation;
            movementDirection = transform.forward.normalized;
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
            float originalHeight = controller.height;

            Vector3 newCenter = originalCenter;

            controller.height /= 2;
            newCenter.y -= controller.height / 2;
            controller.center = newCenter;

            // เล่นอนิเมชัน slide
            if (animator != null)
            {
                animator.Play(slidingAnimationId);
            }

            float slideDuration = slideAnimationClip != null && animator != null
                ? slideAnimationClip.length / animator.speed
                : 1f;

            yield return new WaitForSeconds(slideDuration);

            // รีเซ็ต collider กลับ
            controller.height = originalHeight;
            controller.center = originalCenter;
            sliding = false;
        }

        // ======================
        // JUMP
        // ======================

        private void PlayerJump(InputAction.CallbackContext context)
        {
            if (IsGrounded())
            {
                playerVelocity.y += Mathf.Sqrt(jumpHeight * gravity * -3f);
                controller.Move(playerVelocity * Time.deltaTime);
            }
        }

        // ======================
        // UPDATE LOOP
        // ======================

        private void Update()
        {
            // ถ้าตกฉาก (raycast ยาวๆ) -> GameOver
            if (!IsGrounded(20f))
            {
                GameOver();
                return;
            }

            // อัปเดตคะแนน
            score += scoreMultiplier * Time.deltaTime;
            if (scoreUpdateEvent != null)
                scoreUpdateEvent.Invoke((int)score);

            // วิ่งไปด้านหน้าตามทิศของ transform
            controller.Move(transform.forward * playerSpeed * Time.deltaTime);

            // รีเซ็ต velocity y ถ้าแตะพื้น
            if (IsGrounded() && playerVelocity.y < 0)
            {
                playerVelocity.y = 0f;
            }

            // ใส่แรงโน้มถ่วง
            playerVelocity.y += gravity * Time.deltaTime;
            controller.Move(playerVelocity * Time.deltaTime);

            // เพิ่มความเร็ว/ความยากตามเวลา
            if (playerSpeed < maximumPlayerSpeed)
            {
                playerSpeed += Time.deltaTime * playerSpeedIncreaseRate;
                gravity = initialGravityValue - playerSpeed;

                if (animator != null && animator.speed < 1.25f)
                {
                    animator.speed += (1 / playerSpeed) * Time.deltaTime;
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
            raycastOriginFirst -= transform.forward * .2f;
            raycastOriginSecond += transform.forward * .2f;

            if (Physics.Raycast(raycastOriginFirst, Vector3.down, length, groundLayer) ||
                Physics.Raycast(raycastOriginSecond, Vector3.down, length, groundLayer))
            {
                return true;
            }
            return false;
        }

        private void GameOver()
        {
            Debug.Log("Game Over");
            if (gameOverEvent != null)
                gameOverEvent.Invoke((int)score);

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
