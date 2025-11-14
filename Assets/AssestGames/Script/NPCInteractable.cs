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

    public void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(this);
    }
}
