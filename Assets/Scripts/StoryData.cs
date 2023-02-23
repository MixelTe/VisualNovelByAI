using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New StoryData", menuName = "StoryData")]
public class StoryData : ScriptableObject
{
    [SerializeField] private List<Dialogue> dialogues;

    public List<Dialogue> Dialogues => dialogues;
}
