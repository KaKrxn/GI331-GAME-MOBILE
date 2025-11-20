using UnityEngine;
using UnityEngine.InputSystem;

public class ExPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput = playerInput.actions["Move"].ReadValue<Vector2>();

        // Flip sprite
        if (moveInput.x > 0)
            spriteRenderer.flipX = true;
        else if (moveInput.x < 0)
            spriteRenderer.flipX = false;
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
}
