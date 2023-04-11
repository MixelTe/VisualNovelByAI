using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(DialogueManager))]
public class DialogueManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var myScript = (DialogueManager)target;

        if (myScript.StoryData == null) return;

        GUILayout.Space(16);

        GUI.enabled = !Application.isPlaying;
        myScript.UseStateDEV = EditorGUILayout.Toggle("Use State DEV", myScript.UseStateDEV);
        GUI.enabled = true;

        if (!myScript.UseStateDEV) return;

        myScript.StateDEV_node = EditorGUILayout.IntField($"Current Node", myScript.StateDEV_node);
        if (Application.isPlaying)
        {
            if (GUILayout.Button("Set Current Node"))
            {
                myScript.SetCurrentNode();
            }
        }

        for (int i = 0; i < myScript.StateDEV_values.Length && i < myScript.StoryData.Fields.Count; i++)
        {
            DrawListItem(myScript, i);
		}

        if (GUILayout.Button("Update Dev State Fields"))
        {
            myScript.UpdateDevStateFields();
        }
    }

    public void DrawListItem(DialogueManager myScript, int i)
	{
        GUILayout.BeginHorizontal();
        GUILayout.Space(16);
        myScript.StateDEV_values[i] = EditorGUILayout.IntField($"{myScript.StoryData.Fields[i]}", myScript.StateDEV_values[i]);
        GUILayout.EndHorizontal();
    }
}
