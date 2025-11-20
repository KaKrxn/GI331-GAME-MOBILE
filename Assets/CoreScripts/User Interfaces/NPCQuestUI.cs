using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NPCQuestUI : MonoBehaviour
{
    [Header("Quest Items สำหรับ NPC ตัวนี้")]
    [SerializeField] private List<ItemDefinition> requiredQuestItems = new List<ItemDefinition>();

    [Header("UI Roots")]
    [SerializeField] private GameObject interactionPanelRoot; // NPC_Panel_Interaction
    [SerializeField] private GameObject questWindowRoot;      // หน้าต่างที่แสดง Items

    [Header("Buttons")]
    [SerializeField] private Button talkButton;        // ปุ่ม Talk
    [SerializeField] private Button openQuestButton;   // ปุ่มเปิดหน้าต่าง Quest (หลังคุยจบ)
    [SerializeField] private Button closeQuestButton;  // ปุ่มปิดหน้าต่าง Quest
    [SerializeField] private Button closeAllButton;    // ปิดทุก UI (Exit)

    [Header("Quest List UI")]
    [SerializeField] private Transform questItemListParent;   // Content ของ ScrollView
    [SerializeField] private GameObject questItemRowPrefab;   // Prefab ที่มี QuestItemRowUI

    [Header("ระยะปิด UI อัตโนมัติ")]
    [SerializeField] private float autoCloseDistance = 4f;

    [Header("อ้างอิง NPC")]
    [SerializeField] private NPCInteractable npcInteractable;

    private Transform playerTransform;
    private bool isAnyUIOpen = false;
    private bool hasTalkedOnce = false;

    private void Awake()
    {
        if (talkButton) talkButton.onClick.AddListener(OnTalkButtonClicked);
        if (openQuestButton) openQuestButton.onClick.AddListener(OnOpenQuestClicked);
        if (closeQuestButton) closeQuestButton.onClick.AddListener(OnCloseQuestClicked);
        if (closeAllButton) closeAllButton.onClick.AddListener(HideAllUI);

        if (interactionPanelRoot) interactionPanelRoot.SetActive(false);
        if (questWindowRoot) questWindowRoot.SetActive(false);
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    private void Update()
    {
        if (!isAnyUIOpen || playerTransform == null) return;

        float dist = Vector3.Distance(playerTransform.position, transform.position);
        if (dist > autoCloseDistance)
        {
            HideAllUI();
        }
    }

    // ---------- เรียกจาก ExPlayerInteract ตอนกด Interact ครั้งแรก ----------
    public void ShowPreTalkPanel()
    {
        if (interactionPanelRoot) interactionPanelRoot.SetActive(true);
        if (questWindowRoot) questWindowRoot.SetActive(false);

        if (!hasTalkedOnce)
        {
            // ✅ Interact ครั้งแรก → มีแค่ Talk
            SetButtonVisible(talkButton, true);
            SetButtonVisible(openQuestButton, false);
            SetButtonVisible(closeAllButton, false);
        }
        else
        {
            // ✅ Interact รอบที่ 2+ → มี Talk + Quest + Exit เลย
            SetButtonVisible(talkButton, true);
            SetButtonVisible(openQuestButton, true);
            SetButtonVisible(closeAllButton, true);
        }

        isAnyUIOpen = true;
    }

    // ---------- เรียกตอนคุยจบจาก NPCInteractable.OnDialogueFinished ----------
    public void ShowPostTalkPanel()
    {
        hasTalkedOnce = true;

        if (interactionPanelRoot) interactionPanelRoot.SetActive(true);
        if (questWindowRoot) questWindowRoot.SetActive(false);

        SetButtonVisible(talkButton, false);
        SetButtonVisible(openQuestButton, true);
        SetButtonVisible(closeAllButton, true);

        RefreshQuestListUI();
        isAnyUIOpen = true;
    }

    

    private void OnTalkButtonClicked()
    {
        if (npcInteractable != null)
        {
            npcInteractable.TriggerDialogue();
        }

        // ระหว่างคุย ซ่อน Panel Interact ไว้ก่อน
        if (interactionPanelRoot) interactionPanelRoot.SetActive(false);
    }

    private void OnOpenQuestClicked()
    {
        if (questWindowRoot) questWindowRoot.SetActive(true);
        RefreshQuestListUI();
    }

    private void OnCloseQuestClicked()
    {
        if (questWindowRoot) questWindowRoot.SetActive(false);
    }

    public void HideAllUI()
    {
        if (interactionPanelRoot) interactionPanelRoot.SetActive(false);
        if (questWindowRoot) questWindowRoot.SetActive(false);
        isAnyUIOpen = false;
    }

    // ---------- สร้างรายการ Items เควสต์ ----------

    private void RefreshQuestListUI()
    {
        if (questItemListParent == null || questItemRowPrefab == null)
            return;

        // ล้างของเก่า
        for (int i = questItemListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(questItemListParent.GetChild(i).gameObject);
        }

        GameData gameData = GameData.Instance;
        bool hasGameData = (gameData != null);

        foreach (var def in requiredQuestItems)
        {
            if (def == null) continue;

            GameObject rowObj = Instantiate(questItemRowPrefab, questItemListParent);
            QuestItemRowUI rowUI = rowObj.GetComponent<QuestItemRowUI>();

            bool hasItem = false;
            if (hasGameData && !string.IsNullOrEmpty(def.itemId))
            {
                hasItem = gameData.HasItem(def.itemId);
            }

            if (rowUI != null)
            {
                rowUI.Setup(def, hasItem);
            }
        }
    }

    private void SetButtonVisible(Button button, bool visible)
    {
        if (button != null)
            button.gameObject.SetActive(visible);
    }
}
