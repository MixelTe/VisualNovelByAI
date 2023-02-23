using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialogue
{
    [SerializeField] private string characterName;
    [SerializeField] private Sprite characterImage;
    [SerializeField] private string text;
    [SerializeField] private List<Choice> choices;

    public string CharacterName => characterName;
    public Sprite CharacterImage => characterImage;
    public string Text => text;
    public List<Choice> Choices => choices;
}

[System.Serializable]
public class Choice
{
    [SerializeField] private string text;
    [SerializeField] private int nextDialogueIndex;

    public string Text => text;
    public int NextDialogueIndex => nextDialogueIndex;
}
