using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ExPlayerInteract : MonoBehaviour
{
    private NPCInteractable currentNPC;
    private PlayerInput playerInput;
    private InputAction interactAction;
    [SerializeField] private GameObject interactButton;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    void Start()
    {
        interactAction = playerInput.actions["Interact"];
        if (interactButton != null)
            interactButton.SetActive(false);
    }

    void Update()
    {
        if (interactAction.WasPerformedThisFrame() && currentNPC != null)
        {
            currentNPC.TriggerDialogue();
        }
    }

    public void Interact()
    {
        if (currentNPC != null)
        {
            currentNPC.TriggerDialogue();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("ชนกับ " + other.name);
        if (other.GetComponent<NPCInteractable>() != null)
        {
            currentNPC = other.GetComponent<NPCInteractable>();
            if (interactButton != null)
                interactButton.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<NPCInteractable>() == currentNPC)
        {
            currentNPC = null;
            if (interactButton != null)
                interactButton.SetActive(false);
        }
    }
}
