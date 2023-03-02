using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class StoryEditor : EditorWindow
{
    private StoryData _data;
    private List<DialogNode> _nodes = new List<DialogNode>();
    private DialogNode _firstNode;
    private int _maxIndex = 0;
    public string[] Characters = null;
    public string[] Backgrounds = null;
    public string[] Emotions = Enum.GetNames(typeof(Emotions));

    private Vector2 _drag = Vector2.zero;
    private Vector2 _offsetPast = Vector2.zero;
    private Vector2 _offset = Vector2.zero;

    private void Init(StoryData data)
	{
        _data = data;
        _nodes = new List<DialogNode>();
        var dialogs = _data.Dialogues.ToDictionary(i => i.id);
        Characters = _data.Characters.Select(ch => ch.Name).Append("").ToArray();
        Backgrounds = _data.BackgroundImages.Select(i => i.Name).Append(BackgroundImage.Same).ToArray();



        if (!dialogs.TryGetValue(0, out var firstDialog))
		{
            firstDialog = new Dialogue();
            _data.Dialogues.Add(firstDialog);
        }
        _firstNode = CreateNodes(dialogs, firstDialog);
        _firstNode.UpdatePosition();
    }

    private DialogNode CreateNodes(Dictionary<int, Dialogue> dialogs, Dialogue dialogue, DialogNode prev = null)
	{
        if (dialogue.id > _maxIndex)
            _maxIndex = dialogue.id;

        var node = new DialogNode(dialogue, prev, this);
        _nodes.Add(node);

        var nextNodes = new List<Connection>();
        foreach (var choice in dialogue.Choices)
		{
            if (dialogs.TryGetValue(choice.NextDialogueId, out var dialog))
			{
                var alreadyCreated = _nodes.Find(node => node.Dialog.id == choice.NextDialogueId);
                if (alreadyCreated == null)
                    nextNodes.Add(new Connection(CreateNodes(dialogs, dialog, node)));
                else
                    nextNodes.Add(new Connection(alreadyCreated, true));
			}
        }
        node.Next = nextNodes;
        return node;
    }

    [MenuItem("Assets/Edit StoryData")]
    public static void ShowEditor()
    {
        var editor = GetWindow<StoryEditor>("Story Editor");
        var data = Selection.activeObject as StoryData;
        editor.Init(data);
    }

    private void OnGUI()
	{
        foreach (DialogNode node in _nodes)
        {
            node.DrawConnections(_offset);
        }
        foreach (DialogNode node in _nodes)
        {
            node.Draw(_offset);
        }

        GUILayout.BeginArea(new Rect(4, 4, 64, 24));
        if (GUILayout.Button("Save"))
		{
            EditorUtility.SetDirty(_data);
        }
        GUILayout.EndArea();

        if (Event.current.type == EventType.MouseDown)
		{
            var cursor = Event.current.mousePosition - _offset;
			if (!_nodes.Any(node => node.Contains(cursor)))
			{
                _offsetPast = _offset;
                _drag = cursor;
            }
        }
        else if (Event.current.type == EventType.MouseDrag)
        {
            var cursor = Event.current.mousePosition - _offsetPast;
            if (_drag != Vector2.zero)
            {
                Repaint();
                _offset = cursor - _drag + _offsetPast;
            }
        }
        else if (Event.current.type == EventType.MouseUp)
        {
            _drag = Vector2.zero;
        }
    }

    private void AddChoice(DialogNode dialogNode)
    {
        var newDialog = new Dialogue()
		{
            id = ++_maxIndex
        };
        _data.Dialogues.Add(newDialog);
        dialogNode.Dialog.Choices.Add(new Choice()
        {
            NextDialogueId = newDialog.id,
            Text = "Continue"
        });

        var node = new DialogNode(newDialog, dialogNode, this);
        _nodes.Add(node);
        dialogNode.Next.Add(new Connection(node));
        _firstNode.UpdatePosition();
    }

    private class DialogNode
	{
        static private int _margin = 20;
        static private int _width = 200;
        static private int _height = 200;
        static private int _optionHeight = 40;
        static private GUIStyle nodeStyle;
        static private GUIStyle inputStyle;

        public Dialogue Dialog;
        public List<Connection> Next = new();
        private int _fullHeight = _height;
        private Vector2 _position = Vector2.zero;
        private DialogNode _prev;
        private int _prevI;
        private StoryEditor _storyEditor;

        private Rect _rect => new Rect(_position.x, _position.y, _width, _height + _optionHeight * Dialog.Choices.Count);

        public DialogNode(Dialogue dialog, DialogNode prev, StoryEditor storyEditor)
		{
			Dialog = dialog;
            _prev = prev;
            if (_prev != null)
			{
                _prevI = _prev.Dialog.Choices.FindIndex(el => el.NextDialogueId == Dialog.id);
            }
            _storyEditor = storyEditor;

            InitStyles();
        }

        private void InitStyles()
		{
            if (nodeStyle == null)
			{
                nodeStyle = new GUIStyle();
                nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
                nodeStyle.border = new RectOffset(12, 12, 12, 12);
                nodeStyle.padding = new RectOffset(16, 16, 16, 16);
            }
            if (inputStyle == null)
            {
                inputStyle = new GUIStyle();
                inputStyle.fixedWidth = 50;
            }
        }

        public void UpdatePosition()
		{
            if (_prev != null)
                _position = _prev._position + new Vector2(_width + _margin, (_height + _optionHeight) * _prevI);

			foreach (var node in Next)
			{
                if (!node.recurse) node.node.UpdatePosition();
			}
		}


        public void Draw(Vector2 offset)
        {
            GUILayout.BeginArea(new Rect(_rect.position + offset, _rect.size), nodeStyle);
            EditorGUIUtility.labelWidth = 80;
            EditorGUILayout.LabelField($"ID: {Dialog.id}");

            var character = EditorGUILayout.Popup("Character", Array.IndexOf(_storyEditor.Characters, Dialog.Character), _storyEditor.Characters);
			Dialog.Character = _storyEditor.Characters[character];

            var emotion = EditorGUILayout.Popup("Emotion", Array.IndexOf(_storyEditor.Emotions, Enum.GetName(typeof(Emotions), Dialog.CharacterEmotion)), _storyEditor.Emotions);
            Dialog.CharacterEmotion = Enum.Parse<Emotions>(_storyEditor.Emotions[emotion]);

            Dialog.Text = EditorGUILayout.TextField("Text", Dialog.Text);

            var background = EditorGUILayout.Popup("Background", Array.IndexOf(_storyEditor.Backgrounds, Dialog.Background), _storyEditor.Backgrounds);
            Dialog.Background = _storyEditor.Backgrounds[background];

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField($"Choices:");
            if (GUILayout.Button("Add"))
            {
                _storyEditor.AddChoice(this);
            }

            for (int i = 0; i < Dialog.Choices.Count; i++)
			{
                var choice = Dialog.Choices[i];
				choice.Text = EditorGUILayout.TextField("Text", choice.Text);
            }
			GUILayout.EndArea();
        }
        public void DrawConnections(Vector2 offset)
        {
            for (int i = 0; i < Dialog.Choices.Count; i++)
            {
                if (i >= Next.Count)
                    Debug.Log("i >= Next.Count");
                var choice = Next[i];
                Vector2 p1;
                if (choice.recurse)
                    p1 = new Vector2(_position.x + 12, _position.y + Height + OptionHeight * i);
                else
                    p1 = new Vector2(_rect.xMax - 12, _position.y + Height + OptionHeight * i);

                var p2 = new Vector2(choice.node._position.x + 12, choice.node._position.y + 32);
                DrawConnection(p1 + offset, p2 + offset);
            }
        }
        private void DrawConnection(Vector2 start, Vector2 end)
        {
            Vector3 startPos = new Vector3(start.x, start.y, 0);
            Vector3 endPos = new Vector3(end.x, end.y, 0);
            Vector3 startTan = startPos + Vector3.right * 50;
            Vector3 endTan = endPos + Vector3.left * 50;

            Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 5);
            Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.blue, null, 3);
        }
        public bool Contains(Vector2 pos)
		{
            return _rect.Contains(pos);
		}
    }

	private class Connection
	{
        public DialogNode node;
        public bool recurse;

		public Connection(DialogNode node, bool recurse = false)
		{
			this.recurse = recurse;
			this.node = node;
		}
	}
}
