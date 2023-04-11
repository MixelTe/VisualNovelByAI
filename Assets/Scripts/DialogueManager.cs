using System.Collections;
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
    [Header("Game State DEV")]
    [HideInInspector] public bool UseStateDEV;
    [HideInInspector] public int StateDEV_node;
    [HideInInspector] public int[] StateDEV_values = new int[0];

    private Dictionary<int, Dialogue> _dialogues;
    private Dictionary<string, Character> _characters;
    private Dictionary<string, BackgroundImage> _backgrounds;
    private GameState _state;

    public StoryData StoryData { get => _storyData; }

    private void Start()
    {
        if (UseStateDEV)
            _state = new GameStateDEV(StateDEV_node, StateDEV_values, _storyData);
		else
            _state = GameState.Load();
        
        _dialogues = _storyData.Dialogues.ToDictionary(d => d.id);
        _characters = _storyData.Characters.ToDictionary(ch => ch.Name);
        _backgrounds = _storyData.BackgroundImages.ToDictionary(bg => bg.Name);

        if (_state.Fields == null)
		{
            _state.Fields = _storyData.Fields.ToDictionary(f => f, f => 0);
        }

        StartCoroutine(SaveState());
        DisplayDialogue(_state.NodeId);
    }

    private void DisplayDialogue(int id)
    {
        _state.NodeId = id;
        var dialogue = _dialogues[id];
        
        ClearChoices();

        switch (dialogue.Type)
		{
			case DialogueType.Dialogue:
                Node_Dialogue(id);
                break;
			case DialogueType.Setter:
                Node_Setter(id);
                break;
			case DialogueType.Switch:
                Node_Switch(id);
                break;
			default:
                Debug.LogError($"[Dialogue #{id}] Wrong Type: {dialogue.Type}");
				break;
        }
        if (UseStateDEV)
		{
            StateDEV_values = _state.Fields.Select(f => f.Value).ToArray();
        }
	}
    private void Node_Dialogue(int id)
	{
        var dialogue = _dialogues[id];

        if (_characters.TryGetValue(dialogue.Character, out var ch))
		{
			switch (dialogue.CharacterEmotion)
			{
				case Emotions.Normal:
			        _characterImage.sprite = ch.Portrait;
                    break;
				case Emotions.Happy:
			        _characterImage.sprite = ch.PortraitHappy != null ? ch.PortraitHappy : ch.Portrait;
					break;
                case Emotions.Sad:
			        _characterImage.sprite = ch.PortraitSad != null ? ch.PortraitSad : ch.Portrait;
					break;
                case Emotions.Angry:
			        _characterImage.sprite = ch.PortraitAngry != null ? ch.PortraitAngry : ch.Portrait;
					break;
                default:
			        _characterImage.sprite = ch.Portrait;
                    Debug.LogError($"[Dialogue #{id}] Wrong CharacterEmotion: {dialogue.CharacterEmotion}");
					break;
            }
		}
		else
		{
            Debug.LogError($"[Dialogue #{id}] Wrong CharacterName: {dialogue.Character}");
		}
        _characterNameText.text = dialogue.Character;
        _dialogueText.text = dialogue.Text;

        if (dialogue.Background != null)
        {
            if (_backgrounds.TryGetValue(dialogue.Background, out var bg))
                _backgroundImage.sprite = bg.Image;
            else if (dialogue.Background != BackgroundImage.Same)
                Debug.LogError($"[Dialogue #{id}] Wrong Background name: {dialogue.Background}");
        }

        if (dialogue.Choices.Count > 0)
        {
            DisplayChoices(dialogue);
        }
        else
        {
            Debug.Log("End of Story");
        }
    }
    private void Node_Setter(int id)
	{
        var dialogue = _dialogues[id];
        if (_state.Fields.ContainsKey(dialogue.Field))
		{
            if (dialogue.SetOrChange)
                _state.Fields[dialogue.Field] = dialogue.Value;
            else
                _state.Fields[dialogue.Field] += dialogue.Value;
        }
        else
		{
            Debug.LogError($"[Setter #{id}] Wrong Field name: {dialogue.Field}");
        }

        if (dialogue.Choices.Count > 0)
        {
            DisplayDialogue(dialogue.Choices[0].NextDialogueId);
        }
        else
        {
            Debug.LogError($"[Setter #{id}] Dont has Choice");
        }
    }
    private void Node_Switch(int id)
    {
        var dialogue = _dialogues[id];
        if (dialogue.Choices.Count == 0)
        {
            Debug.LogError($"[Switch #{id}] Dont has Choice");
            return;
        }
		foreach (var choice in dialogue.Choices)
		{
			var splited = choice.Text.Split(";");
            if (splited.Length != 3)
			{
                Debug.LogError($"[Switch #{id}] Wrong Choice #{dialogue.Choices.IndexOf(choice)}: {choice.Text}");
                continue;
            }
            var field = splited[0];
            if (!_state.Fields.ContainsKey(field))
			{
                Debug.LogError($"[Switch #{id}] Wrong Choice Field #{dialogue.Choices.IndexOf(choice)}: {field}");
                continue;
            }
            var oper = splited[1];
            if (!Choice.Operators.Contains(oper))
            {
                Debug.LogError($"[Switch #{id}] Wrong Choice Operator #{dialogue.Choices.IndexOf(choice)}: {oper}");
                continue;
            }
            if (!int.TryParse(splited[2], out var value))
            {
                Debug.LogError($"[Switch #{id}] Wrong Choice Value #{dialogue.Choices.IndexOf(choice)}: {splited[2]}");
                continue;
            }

            var fieldValue = _state.Fields[field];
			switch (oper)
			{
                case ">":
                    if (fieldValue > value)
					{
                        DisplayDialogue(choice.NextDialogueId);
                        return;
                    }
                    break;
                case "<":
                    if (fieldValue < value)
                    {
                        DisplayDialogue(choice.NextDialogueId);
                        return;
                    }
                    break;
                case "==":
                    if (fieldValue == value)
                    {
                        DisplayDialogue(choice.NextDialogueId);
                        return;
                    }
                    break;
                case "Any":
                    DisplayDialogue(choice.NextDialogueId);
                    return;
            }
		}
        DisplayDialogue(dialogue.Choices[0].NextDialogueId);
        Debug.LogError($"[Switch #{id}] None of the choices fit");
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

    private IEnumerator SaveState()
	{
		while (true)
		{
            yield return new WaitForSeconds(2);
            _state.Save();
		}
	}

    public void UpdateDevStateFields()
	{
        if (Application.isPlaying)
		{
			for (int i = 0; i < StateDEV_values.Length; i++)
			{
                var field = _storyData.Fields[i];
                _state.Fields[field] = StateDEV_values[i];
            }
		}
		else
		{
            StateDEV_values = new int[_storyData.Fields.Count];
        }
    }

    public void SetCurrentNode()
	{
        if (Application.isPlaying)
		{
            _state.NodeId = StateDEV_node;
            DisplayDialogue(_state.NodeId);
		}
    }
}
