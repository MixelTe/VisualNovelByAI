using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;

[System.Serializable]
public class Dialogue
{
    public int id = 0;
    public Vector2 PosInEditor;
    public string Character = "";
    public Emotions CharacterEmotion = Emotions.Normal;
    public string Text = "";
    public string Background = BackgroundImage.Same;
    public List<Choice> Choices = new();

    public Dialogue Clone()
	{
        return new Dialogue()
        {
            id = id,
            PosInEditor = PosInEditor,
            Character = Character,
            CharacterEmotion = CharacterEmotion,
            Text = Text,
            Background = Background,
            Choices = Choices.Select(ch => ch.Clone()).ToList(),
        };
	}
}


[System.Serializable]
public class Choice
{
    public string Text;
    public int NextDialogueId;


    public Choice Clone()
    {
        return new Choice()
        {
            Text = Text,
            NextDialogueId = NextDialogueId,
        };
    }
}

public enum Emotions
{
    Normal,
    Happy,
    Sad,
    Angry,
}