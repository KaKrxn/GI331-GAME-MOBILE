using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    public string npcName;
    [TextArea(2, 5)] public string[] dialogueLines;
    public Sprite portrait;

    public void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(this);
    }
}
