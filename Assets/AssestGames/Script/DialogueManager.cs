using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image playerPortrait;
    [SerializeField] private Image npcPortrait;
    [SerializeField] private Button nextButton;
    [SerializeField] private float typingSpeed = 0.03f;

    private List<NPCInteractable.DialogueLine> lines;
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
        dialoguePanel.SetActive(true);
        StopAllCoroutines();
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        var line = lines[index];
        dialogueText.text = "";
        StopAllCoroutines();
        StartCoroutine(TypeLine(line.text));

        if(line.isPlayer)
        {
            playerPortrait.sprite = line.portrait;
            playerPortrait.gameObject.SetActive(true);
            npcPortrait.gameObject.SetActive(false);
        }
        else
        {
            npcPortrait.sprite = line.portrait;
            npcPortrait.gameObject.SetActive(true);
            playerPortrait.gameObject.SetActive(false);
        }

        characterNameText.text = line.characterName;
    }



    IEnumerator TypeLine(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in text.ToCharArray())
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
            dialogueText.text = lines[index].text;
            isTyping = false;
            return;
        }

        if (index < lines.Count - 1)
        {
            index++;
            ShowCurrentLine();
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
