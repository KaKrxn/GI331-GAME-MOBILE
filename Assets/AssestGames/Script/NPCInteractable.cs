using System.Collections.Generic;
using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [System.Serializable]
    public class DialogueLine
    {
        public bool isPlayer;
        public string characterName;
        public Sprite portrait;
        [TextArea] public string text;
    }

    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    [Header("Quest / Interaction UI")]
    [SerializeField] private NPCQuestUI npcQuestUI;

    [Header("หลังคุยจบให้เปิด GameObject นี้")]
    [SerializeField] private GameObject objectToActivateOnDialogueEnd;

    public void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(this);
    }

    public void OnDialogueFinished()
    {
        // 1) Logic เดิม: เปิดหน้าต่าง Quest / Exit
        if (npcQuestUI != null)
        {
            npcQuestUI.ShowPostTalkPanel();
        }
        else
        {
            Debug.LogWarning($"[NPCInteractable] {name} UnSet npcQuestUI ใน Inspector");
        }

        // 2) Logic ใหม่: เปิด GameObject ที่กำหนดไว้
        if (objectToActivateOnDialogueEnd != null)
        {
            objectToActivateOnDialogueEnd.SetActive(true);
        }
    }
}
