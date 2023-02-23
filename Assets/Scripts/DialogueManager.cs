using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private StoryData storyData;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Transform choiceButtonContainer;

    private int currentDialogueIndex = 0;

    private void Start()
    {
        DisplayDialogue(currentDialogueIndex);
    }

    private void DisplayDialogue(int index)
    {
        Dialogue dialogue = storyData.Dialogues[index];

        characterNameText.text = dialogue.CharacterName;
        characterImage.sprite = dialogue.CharacterImage;
        dialogueText.text = dialogue.Text;

        if (dialogue.Choices.Count > 0)
        {
            DisplayChoices(dialogue);
        }
        else
        {
            DisplayContinueButton();
        }
    }

    private void DisplayChoices(Dialogue dialogue)
    {
        ClearChoices();

        foreach (Choice choice in dialogue.Choices)
        {
            GameObject choiceButton = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            TMP_Text choiceText = choiceButton.GetComponentInChildren<TMP_Text>();
            choiceText.text = choice.Text;

            Button button = choiceButton.GetComponent<Button>();
            button.onClick.AddListener(() => SelectChoice(choice.NextDialogueIndex));
        }
    }

    private void DisplayContinueButton()
    {
        ClearChoices();

        GameObject choiceButton = Instantiate(choiceButtonPrefab, choiceButtonContainer);
        TMP_Text choiceText = choiceButton.GetComponentInChildren<TMP_Text>();
        choiceText.text = "Continue";

        Button button = choiceButton.GetComponent<Button>();
        button.onClick.RemoveAllListeners(); // Remove previous listeners
        button.onClick.AddListener(() =>
        {
            currentDialogueIndex++;
            if (currentDialogueIndex < storyData.Dialogues.Count)
            {
                DisplayDialogue(currentDialogueIndex);
            }
            else
            {
                Debug.Log("End of dialogue.");
            }
        });
    }


    private void ClearChoices()
    {
        foreach (Transform child in choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void SelectChoice(int choiceIndex)
    {
        currentDialogueIndex = choiceIndex;
        DisplayDialogue(currentDialogueIndex);
    }
}
