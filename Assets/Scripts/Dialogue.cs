using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialogue
{
    public int id = 0;
    public string CharacterName;
    public string Text;
    public string BackgroundImage;
    public List<Choice> Choices = new List<Choice>();
}


[System.Serializable]
public class Choice
{
    [SerializeField] private string text;
    [SerializeField] private int nextDialogueId;

    public string Text => text;
    public int NextDialogueId => nextDialogueId;
}
