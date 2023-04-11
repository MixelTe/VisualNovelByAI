using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New StoryData", menuName = "StoryData")]
public class StoryData : ScriptableObject
{
    [SerializeField] private List<Character> characters;
    [SerializeField] private List<BackgroundImage> backgroundImages;
    [SerializeField] private List<string> fields;
    [FormerlySerializedAs("dialogues")]
    public List<Dialogue> Dialogues;

    public List<Character> Characters => characters;
    public List<BackgroundImage> BackgroundImages => backgroundImages;
    public List<string> Fields => fields;
}


[System.Serializable]
public class Character
{
    public string Name;
    public Sprite Portrait;
    public Sprite PortraitHappy;
    public Sprite PortraitSad;
    public Sprite PortraitAngry;
}


[System.Serializable]
public class BackgroundImage
{
    public static readonly string Same = "<same>";
    public string Name;
    public Sprite Image;
}
