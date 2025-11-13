using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Button nextButton;
    [SerializeField] private float typingSpeed = 0.03f;

    private string[] lines;
    private int index;
    private bool isTyping;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
        nextButton.onClick.AddListener(NextLine);
    }

    public void StartDialogue(NPCInteractable npc)
    {
        lines = npc.dialogueLines;
        index = 0;
        npcNameText.text = npc.npcName;
        portraitImage.sprite = npc.portrait;
        dialoguePanel.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in lines[index].ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        isTyping = false;
    }

    public void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = lines[index];
            isTyping = false;
            return;
        }

        if (index < lines.Length - 1)
        {
            index++;
            StopAllCoroutines();
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
