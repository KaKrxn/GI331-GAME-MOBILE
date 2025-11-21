using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ExPlayerInteract : MonoBehaviour
{
    private NPCInteractable currentNPC;
    private NPCQuestUI currentNPCQuestUI;   // เก็บ UI ของ NPC
    private ShopInteractable currentShop;
    private ScenePortalInteractable currentScenePortal; // ✅ เพิ่ม: สำหรับ Load Scene

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

    // ใช้กับปุ่ม UI (OnClick)
    public void Interact()
    {
        HandleInteract();
    }

    // --------------- logic หลักของการกด Interact ---------------
    private void HandleInteract()
    {
        // 1) ถ้ามี NPC + Quest UI → เปิด Panel Interact ก่อน
        if (currentNPCQuestUI != null)
        {
            currentNPCQuestUI.ShowPreTalkPanel();
        }
        // 2) NPC ไม่มี UI เควส → คุยเลย
        else if (currentNPC != null)
        {
            currentNPC.TriggerDialogue();
        }
        // 3) Shop
        else if (currentShop != null)
        {
            currentShop.TriggerShop();
        }
        // 4) Portal โหลด Scene
        else if (currentScenePortal != null)
        {
            currentScenePortal.TriggerSceneLoad();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // ---------- NPC ----------
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null)
        {
            currentNPC = npc;
            currentNPCQuestUI = npc.GetComponent<NPCQuestUI>();
            currentShop = null;
            currentScenePortal = null;

            if (interactButton != null)
                interactButton.SetActive(true);

            return;
        }

        // ---------- Shop ----------
        ShopInteractable shop = other.GetComponent<ShopInteractable>();
        if (shop != null)
        {
            currentShop = shop;
            currentNPC = null;
            currentNPCQuestUI = null;
            currentScenePortal = null;

            if (interactButton != null)
                interactButton.SetActive(true);

            return;
        }

        // ---------- Scene Portal ----------
        ScenePortalInteractable portal = other.GetComponent<ScenePortalInteractable>();
        if (portal != null)
        {
            currentScenePortal = portal;
            currentNPC = null;
            currentNPCQuestUI = null;
            currentShop = null;

            if (interactButton != null)
                interactButton.SetActive(true);

            return;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // ---------- NPC ----------
        NPCInteractable npc = other.GetComponent<NPCInteractable>();
        if (npc != null && npc == currentNPC)
        {
            currentNPC = null;

            if (currentNPCQuestUI != null)
            {
                currentNPCQuestUI.HideAllUI();
                currentNPCQuestUI = null;
            }

            if (currentShop == null && currentScenePortal == null && interactButton != null)
                interactButton.SetActive(false);

            return;
        }

        // ---------- Shop ----------
        ShopInteractable shop = other.GetComponent<ShopInteractable>();
        if (shop != null && shop == currentShop)
        {
            currentShop = null;
            if (currentNPC == null && currentScenePortal == null && interactButton != null)
                interactButton.SetActive(false);

            return;
        }

        // ---------- Scene Portal ----------
        ScenePortalInteractable portal = other.GetComponent<ScenePortalInteractable>();
        if (portal != null && portal == currentScenePortal)
        {
            currentScenePortal = null;

            if (currentNPC == null && currentShop == null && interactButton != null)
                interactButton.SetActive(false);

            return;
        }
    }
}
