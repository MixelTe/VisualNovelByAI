using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
}


[System.Serializable]
public class Choice
{
    public string Text;
    public int NextDialogueId;
}

public enum Emotions
{
    Normal,
    Happy,
    Sad,
    Angry,
}