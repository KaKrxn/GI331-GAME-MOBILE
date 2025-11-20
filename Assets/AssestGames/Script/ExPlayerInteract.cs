using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ExPlayerInteract : MonoBehaviour
{
    private NPCInteractable currentNPC;
    private ShopInteractable currentShop;
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
        if (interactAction.WasPerformedThisFrame())
        {
            if (currentNPC != null)
            {
                currentNPC.TriggerDialogue();
            }
            else if (currentShop != null)
            {
                currentShop.TriggerShop();
            }
        }
    }

    public void Interact()
    {
        if (currentNPC != null)
        {
            currentNPC.TriggerDialogue();
        }
        else if (currentShop != null)
        {
            currentShop.TriggerShop();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<NPCInteractable>() != null)
        {
            currentNPC = other.GetComponent<NPCInteractable>();
            currentShop = null;
            if (interactButton != null)
                interactButton.SetActive(true);
        }
        else if (other.GetComponent<ShopInteractable>() != null)
        {
            currentShop = other.GetComponent<ShopInteractable>();
            currentNPC = null;
            if (interactButton != null)
                interactButton.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<NPCInteractable>() == currentNPC)
        {
            currentNPC = null;
            if (currentShop == null && interactButton != null)
                interactButton.SetActive(false);
        }
        else if (other.GetComponent<ShopInteractable>() == currentShop)
        {
            currentShop = null;
            if (currentNPC == null && interactButton != null)
                interactButton.SetActive(false);
        }
    }
}
