using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class GameState
{
	private static readonly string _fileName = "GameData.json";

	public int NodeId = 0;
    public Dictionary<string, int> Fields;

	public void Save()
    {
		var path = FilePath();
		using var writer = new StreamWriter(path);

		var dataToWrite = JsonUtility.ToJson(this);

		writer.Write(dataToWrite);
	}

    public static GameState Load()
    {
		var path = FilePath();
		if (!File.Exists(path)) return new GameState();

		using var reader = new StreamReader(path);
		
		string dataToLoad = reader.ReadToEnd();

		return JsonUtility.FromJson<GameState>(dataToLoad);
	}

	public static string FilePath()
	{
		return Path.Combine(Application.persistentDataPath, _fileName);
	}
}

public class GameStateDEV : GameState
{
	public GameStateDEV(int node, int[] values, StoryData storyData)
	{
		NodeId = node;
		Fields = new();

		for (int i = 0; i < storyData.Fields.Count; i++)
		{
			var field = storyData.Fields[i];
			Fields[field] = values[i];
		}
	}

	public new void Save() {}
}
