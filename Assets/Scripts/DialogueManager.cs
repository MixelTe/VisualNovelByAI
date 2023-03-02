using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private StoryData _storyData;
    [SerializeField] private TMP_Text _characterNameText;
    [SerializeField] private Image _characterImage;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private TMP_Text _dialogueText;
    [SerializeField] private GameObject _choiceButtonPrefab;
    [SerializeField] private Transform _choiceButtonContainer;

    private Dictionary<int, Dialogue> _dialogues;
    private Dictionary<string, Character> _characters;
    private Dictionary<string, BackgroundImage> _backgrounds;

	private void Start()
    {
        _dialogues = _storyData.Dialogues.ToDictionary(d => d.id);
        _characters = _storyData.Characters.ToDictionary(ch => ch.Name);
        _backgrounds = _storyData.BackgroundImages.ToDictionary(bg => bg.Name);

        DisplayDialogue(0);
    }

    private void DisplayDialogue(int id)
    {
        Dialogue dialogue = _dialogues[id];

        if (_characters.TryGetValue(dialogue.Character, out var ch))
            _characterImage.sprite = ch.Portrait;
        else
            Debug.LogError($"[Dialogue #{id}] Wrong CharacterName: {dialogue.Character}");
        _characterNameText.text = dialogue.Character;
        _dialogueText.text = dialogue.Text;

        if (dialogue.Background != null)
        {
            if (_backgrounds.TryGetValue(dialogue.Background, out var bg))
                _backgroundImage.sprite = bg.Image;
            else if (dialogue.Background != BackgroundImage.Same)
                Debug.LogError($"[Dialogue #{id}] Wrong Background name: {dialogue.Background}");
        }

        ClearChoices();
        if (dialogue.Choices.Count > 0)
        {
            DisplayChoices(dialogue);
        }
        else
        {
            Debug.Log("End of Story");
        }
    }

    private void DisplayChoices(Dialogue dialogue)
    {
        foreach (Choice choice in dialogue.Choices)
        {
            GameObject choiceButton = Instantiate(_choiceButtonPrefab, _choiceButtonContainer);
            TMP_Text choiceText = choiceButton.GetComponentInChildren<TMP_Text>();
            choiceText.text = choice.Text;

            Button button = choiceButton.GetComponent<Button>();
            button.onClick.AddListener(() => DisplayDialogue(choice.NextDialogueId));
        }
    }

    private void ClearChoices()
    {
        foreach (Transform child in _choiceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
