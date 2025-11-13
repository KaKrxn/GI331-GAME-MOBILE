using UnityEngine;
using UnityEngine.InputSystem;

public class ExPlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private PlayerInput playerInput;
    private Vector2 moveInput;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        Vector3 move = new Vector3(moveInput.x, moveInput.y, 0);
        transform.position += move * moveSpeed * Time.deltaTime;

        if (move.x > 0)
            spriteRenderer.flipX = false;
        else if (move.x < 0)
            spriteRenderer.flipX = true;
    }
}
