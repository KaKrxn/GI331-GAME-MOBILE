using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float forwardSpeed = 7f;    // ความเร็ววิ่งไปข้างหน้า
    public float laneDistance = 3f;    // ระยะห่างระหว่างเลน (ซ้าย-กลาง-ขวา)
    public float laneChangeSpeed = 10f;

    public float jumpForce = 7f;
    public float gravity = -20f;

    private CharacterController controller;
    private int currentLane = 1; // 0 = ซ้าย, 1 = กลาง, 2 = ขวา
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1) วิ่งไปข้างหน้าเสมอ
        Vector3 move = Vector3.forward * forwardSpeed;

        // 2) กดเลื่อนเลน
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentLane = Mathf.Max(0, currentLane - 1);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentLane = Mathf.Min(2, currentLane + 1);
        }

        // 3) คำนวณตำแหน่ง X ของเลนเป้าหมาย
        float targetX = (currentLane - 1) * laneDistance; // เลนกลาง = 0, ซ้าย = -laneDistance, ขวา = +laneDistance
        float newX = Mathf.Lerp(transform.position.x, targetX, laneChangeSpeed * Time.deltaTime);

        // แปลงการเลื่อนเลนให้กลายเป็นความเร็วในแกน X
        move.x = (newX - transform.position.x) / Time.deltaTime;

        // 4) กระโดด + แรงโน้มถ่วง
        if (controller.isGrounded)
        {
            velocity.y = -1f;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = jumpForce;
            }
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        move.y = velocity.y;

        // 5) สั่ง CharacterController เคลื่อนที่
        controller.Move(move * Time.deltaTime);
    }
}
