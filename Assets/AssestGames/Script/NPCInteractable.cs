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
    public void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(this);
    }

    
    public void OnDialogueFinished()
    {
        if (npcQuestUI != null)
        {
            npcQuestUI.ShowPostTalkPanel();   
        }
        else
        {
            Debug.LogWarning($"[NPCInteractable] {name} UnSet npcQuestUI ใน Inspector");
        }
    }
}
