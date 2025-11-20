using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ExPlayerInteract : MonoBehaviour
{
    private NPCInteractable currentNPC;
    private NPCQuestUI currentNPCQuestUI;   // เพิ่ม: เก็บ UI ของ NPC
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
            HandleInteract();
        }
    }

    
    public void Interact()
    {
        HandleInteract();
    }

    // --------------- logic หลักของการกด Interact ---------------
    private void HandleInteract()
    {
        
        if (currentNPCQuestUI != null)
        {
            currentNPCQuestUI.ShowPreTalkPanel();
        }
     
        else if (currentNPC != null)
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
       
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null)
        {
            currentNPC = npc;
            currentNPCQuestUI = npc.GetComponent<NPCQuestUI>();
            currentShop = null;

            if (interactButton != null)
                interactButton.SetActive(true);

            return;
        }

       
        ShopInteractable shop = other.GetComponent<ShopInteractable>();
        if (shop != null)
        {
            currentShop = shop;
            currentNPC = null;
            currentNPCQuestUI = null;

            if (interactButton != null)
                interactButton.SetActive(true);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null && npc == currentNPC)
        {
            currentNPC = null;

            if (currentNPCQuestUI != null)
            {
                currentNPCQuestUI.HideAllUI(); 
                currentNPCQuestUI = null;
            }

            if (currentShop == null && interactButton != null)
                interactButton.SetActive(false);

            return;
        }

        
        ShopInteractable shop = other.GetComponent<ShopInteractable>();
        if (shop != null && shop == currentShop)
        {
            currentShop = null;
            if (currentNPC == null && interactButton != null)
                interactButton.SetActive(false);
        }
    }
}
