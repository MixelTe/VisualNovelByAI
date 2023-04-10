using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class StoryEditor : EditorWindow
{
    private StoryData _data;
    private List<DialogNode> _nodes = new();
    private int _maxIndex = 0;
    public string[] Characters = null;
    public string[] Backgrounds = null;
    public string[] Emotions = Enum.GetNames(typeof(Emotions));
    public Vector2 Offset { get => _offset; }

    private Vector2 _drag = Vector2.zero;
    private Vector2 _offset = Vector2.zero;
    public float Zoom = 1;

    private void Init(StoryData data)
	{
        _data = data;
        _nodes = new List<DialogNode>();
        var dialogs = _data.Dialogues.ToDictionary(i => i.id);
        Characters = _data.Characters.Select(ch => ch.Name).Append("").ToArray();
        Backgrounds = _data.BackgroundImages.Select(i => i.Name).Append(BackgroundImage.Same).ToArray();

        _maxIndex = 0;
        _drag = Vector2.zero;
        _offset = Vector2.zero;
        Zoom = 1;

        if (!dialogs.TryGetValue(0, out var firstDialog))
		{
            firstDialog = new Dialogue();
            _data.Dialogues.Add(firstDialog);
        }
        CreateNodes(dialogs, firstDialog);
    }

    private DialogNode CreateNodes(Dictionary<int, Dialogue> dialogs, Dialogue dialogue)
	{
        if (dialogue.id > _maxIndex)
            _maxIndex = dialogue.id;

        var node = new DialogNode(dialogue, this);
        _nodes.Add(node);

        var nextNodes = new List<DialogNode>();
        foreach (var choice in dialogue.Choices)
		{
            if (dialogs.TryGetValue(choice.NextDialogueId, out var dialog))
			{
                var alreadyCreated = _nodes.Find(node => node.Dialog.id == choice.NextDialogueId);
                if (alreadyCreated == null)
                    nextNodes.Add(CreateNodes(dialogs, dialog));
                else
                    nextNodes.Add(alreadyCreated);
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
        var mouseEventProcessed = false;
		for (int i = 0; i < _nodes.Count; i++)
        {
			mouseEventProcessed = _nodes[i].Draw(_offset, mouseEventProcessed) || mouseEventProcessed;
        }
        foreach (DialogNode node in _nodes)
        {
            node.DrawConnections(_offset);
        }
        foreach (DialogNode node in _nodes)
        {
            node.DrawConnectionPoints(_offset);
        }

        GUILayout.BeginArea(new Rect(4, 4, 64, 24));
        if (GUILayout.Button("Save"))
            EditorUtility.SetDirty(_data);
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Screen.width - 100, 4, 96, 24));
        if (GUILayout.Button("Reset zoom"))
            Zoom = 1;
        GUILayout.EndArea();

        if (Event.current.type == EventType.ScrollWheel)
        {
            if (Event.current.delta.y > 0)
                Zoom *= 0.9f;
            else
                Zoom *= 1.1f;
            Zoom = Mathf.Clamp(Zoom, 0.5f, 1.6f);
            Repaint();
        }

        if (mouseEventProcessed) return;

        if (Event.current.type == EventType.MouseDown)
		{
            _drag = Event.current.mousePosition - _offset;
        }
        else if (Event.current.type == EventType.MouseDrag)
        {
            if (_drag != Vector2.zero)
            {
                _offset = Event.current.mousePosition - _drag;
                Repaint();
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
            PosInEditor = new Vector2(
                dialogNode.Rect.xMax + 8, 
                dialogNode.Position.y + (DialogNode.OptionHeight + DialogNode.Height - 60) * dialogNode.Next.Count),
            id = ++_maxIndex
        };
        _data.Dialogues.Add(newDialog);
        dialogNode.Dialog.Choices.Add(new Choice()
        {
            NextDialogueId = newDialog.id,
            Text = "Continue"
        });

        var node = new DialogNode(newDialog, this);
        _nodes.Add(node);
        dialogNode.Next.Add(node);
    }

    private void DeleteNode(DialogNode node)
	{
        _nodes.Remove(node);
        _data.Dialogues.Remove(node.Dialog);
		foreach (var n in _nodes)
		{
			for (int i = n.Next.Count - 1; i >= 0; i--)
			{
                var choice = n.Next[i];
                if (choice.Dialog.id == node.Dialog.id)
				{
                    n.Next.RemoveAt(i);
                    n.Dialog.Choices.RemoveAt(i);
                }
            }
        }
        Repaint();
    }

    private class DialogNode
	{
        private static readonly int _width = 250;
        public static readonly int Height = 210;
        private static readonly int _connectionTop = 32;
        public static readonly int OptionHeight = 52;
        private static readonly int _fontSize = 12;
        private float _height { get => Height - (_storyEditor.Zoom > 1 ? Height * 0f : Height * 0.4f) * (_storyEditor.Zoom - 1); }
        private float _optionHeight { get => OptionHeight - (_storyEditor.Zoom > 1 ? OptionHeight * 0.02f : OptionHeight * 0.05f) * (_storyEditor.Zoom - 1); }
        static private GUIStyle _nodeStyle;
        static private GUIStyle _inputPointStyle;
        static private GUIStyle _outputPointStyle;
        static private GUIStyle _button;
        static private GUIStyle _textArea;

        public Dialogue Dialog;
        public List<DialogNode> Next = new();
        public Vector2 Position { get => Dialog.PosInEditor; set => Dialog.PosInEditor = value; }
        private readonly StoryEditor _storyEditor;
        private Vector2 _drag;
        private Vector2 _pastPos;

        public Rect Rect => new Rect(Position.x, Position.y, _width, _height + _optionHeight * Dialog.Choices.Count).Mul(_storyEditor.Zoom);

        public DialogNode(Dialogue dialog, StoryEditor storyEditor)
		{
			Dialog = dialog;
            _storyEditor = storyEditor;
        }

        private void InitStyles()
		{
            if (_nodeStyle == null)
			{
                _nodeStyle = new GUIStyle();
                _nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
                _nodeStyle.border = new RectOffset(12, 12, 12, 12);
                _nodeStyle.padding = new RectOffset(16, 16, 16, 16);
            }
            if (_inputPointStyle == null)
			{
                _inputPointStyle = new GUIStyle();
                _inputPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
                _inputPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
                _inputPointStyle.border = new RectOffset(4, 4, 12, 12);
                _inputPointStyle.fontSize = 1;
            }
            if (_outputPointStyle == null)
			{
                _outputPointStyle = new GUIStyle();
                _outputPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
                _outputPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
                _outputPointStyle.border = new RectOffset(4, 4, 12, 12);
                _outputPointStyle.fontSize = 1;
            }
            if (_button == null)
			{
                _button = new GUIStyle(GUI.skin.button);
                _button.fontSize = _fontSize;
            }
            if (_textArea == null)
            {
                _textArea = new GUIStyle(GUI.skin.textArea);
                _textArea.fontSize = _fontSize;
            }
        }

        public bool Draw(Vector2 offset, bool mouseEventProcessed)
        {
            InitStyles();

            var fontSize = Mathf.RoundToInt(_fontSize * _storyEditor.Zoom);
            var lineHeight = fontSize * 1.5f;
            _button.fontSize = Mathf.RoundToInt(_fontSize * _storyEditor.Zoom);
            _textArea.fontSize = Mathf.RoundToInt(_fontSize * _storyEditor.Zoom);

            var editorStyles_popup_fontSize = EditorStyles.popup.fontSize;
            var editorStyles_popup_fixedHeight = EditorStyles.popup.fixedHeight;
            var editorStyles_label_fontSize = EditorStyles.label.fontSize;
            var editorStyles_label_fixedHeight = EditorStyles.label.fixedHeight;
            EditorStyles.popup.fontSize = fontSize;
            EditorStyles.popup.fixedHeight = fontSize * 1.5f;
            EditorStyles.label.fontSize = fontSize;
            EditorStyles.label.fixedHeight = fontSize * 1.5f;

            GUILayout.BeginArea(new Rect(Rect.position + offset, Rect.size), _nodeStyle);
            if (GUI.Button(new Rect(new Vector2(_width - 32, 10) * _storyEditor.Zoom, new Vector2(18, 18) * _storyEditor.Zoom), "x", _button))
            {
                if (EditorUtility.DisplayDialog("Delete Dialog Node", "Are you sure?", "Delete", "Cancel"))
                {
                    _storyEditor.DeleteNode(this);
                }
            }

            GUILayout.BeginVertical();

            EditorGUILayout.LabelField($"ID: {Dialog.id}", GUILayout.Height(lineHeight));
            EditorGUIUtility.labelWidth = 80 * _storyEditor.Zoom;

            var character = EditorGUILayout.Popup("Character", Array.IndexOf(_storyEditor.Characters, Dialog.Character), _storyEditor.Characters, GUILayout.Height(lineHeight));
			Dialog.Character = _storyEditor.Characters[character];

            var emotion = EditorGUILayout.Popup("Emotion", Array.IndexOf(_storyEditor.Emotions, Enum.GetName(typeof(Emotions), Dialog.CharacterEmotion)), _storyEditor.Emotions, GUILayout.Height(lineHeight));
            Dialog.CharacterEmotion = Enum.Parse<Emotions>(_storyEditor.Emotions[emotion]);

            EditorStyles.textField.wordWrap = true;
            Dialog.Text = EditorGUILayout.TextArea(Dialog.Text, _textArea, GUILayout.Height(50 * _storyEditor.Zoom));

            var background = EditorGUILayout.Popup("Background", Array.IndexOf(_storyEditor.Backgrounds, Dialog.Background), _storyEditor.Backgrounds, GUILayout.Height(lineHeight));
            Dialog.Background = _storyEditor.Backgrounds[background];
            
            EditorGUILayout.LabelField($"Choices:", GUILayout.Height(lineHeight));
            if (GUILayout.Button("Add", _button))
            {
                _storyEditor.AddChoice(this);
            }

            for (int i = 0; i < Dialog.Choices.Count; i++)
			{
                var choice = Dialog.Choices[i];
                choice.Text = EditorGUILayout.TextArea(choice.Text, _textArea, GUILayout.Height(50 * _storyEditor.Zoom));
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            EditorStyles.popup.fontSize = editorStyles_popup_fontSize;
            EditorStyles.popup.fixedHeight = editorStyles_popup_fixedHeight;
            EditorStyles.label.fontSize = editorStyles_label_fontSize;
            EditorStyles.label.fixedHeight = editorStyles_label_fixedHeight;

            if (mouseEventProcessed) return false;

            if (Event.current.type == EventType.MouseDown)
            {
                var cursor = Event.current.mousePosition - _storyEditor.Offset;
                if (!Contains(cursor)) return false;
                _drag = cursor;
                _pastPos = Position;
                return true;
            }
            else if (Event.current.type == EventType.MouseDrag)
            {
                if (_drag != Vector2.zero)
                {
                    Position = Event.current.mousePosition - _storyEditor.Offset - _drag + _pastPos;
                    _storyEditor.Repaint();
                    return true;
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                _drag = Vector2.zero;
            }
            return false;
        }
        public void DrawConnections(Vector2 offset)
        {
            for (int i = 0; i < Dialog.Choices.Count; i++)
            {
                if (i >= Next.Count)
                    Debug.Log("i >= Next.Count");
                var choice = Next[i];
                Vector2 p1, p2;
                if (choice.Position.x < Position.x)
				{
                    p1 = new Vector2(Rect.x + 7 * _storyEditor.Zoom, Rect.y + (_height + _optionHeight * i) * _storyEditor.Zoom);
                    p2 = new Vector2(choice.Rect.xMax - 7 * _storyEditor.Zoom, choice.Rect.y + _connectionTop * _storyEditor.Zoom);
                }
                else
				{
                    p1 = new Vector2(Rect.xMax - 4, Rect.y + (_height + _optionHeight * i) * _storyEditor.Zoom);
                    p2 = new Vector2(choice.Rect.x + 4 * _storyEditor.Zoom, choice.Rect.y + _connectionTop * _storyEditor.Zoom);
                }

                DrawConnection(p1 + offset, p2 + offset);
            }
        }
        public void DrawConnectionPoints(Vector2 offset)
		{
            var size = 24 * _storyEditor.Zoom;
            GUI.Button(new Rect(Rect.x + offset.x - size + 7, Rect.y + offset.y + _connectionTop * _storyEditor.Zoom - size / 2, size, size), "", _inputPointStyle);

            var x = Rect.xMax + offset.x - 7;
            for (int i = 0; i < Dialog.Choices.Count; i++)
            {
                var choice = Next[i];
                GUI.Button(new Rect(x, Rect.y + offset.y + (_height + _optionHeight * i) * _storyEditor.Zoom - size / 2, size, size), "", _outputPointStyle);
            }
        }
        private void DrawConnection(Vector2 start, Vector2 end)
        {
            var startPos = new Vector3(start.x, start.y, 0);
            var endPos = new Vector3(end.x, end.y, 0);
            var mulX = Mathf.Sign(endPos.x - startPos.x);
            var startTan = startPos + Vector3.right * (50 * mulX * _storyEditor.Zoom);
            var endTan = endPos + Vector3.left * (50 * mulX * _storyEditor.Zoom);

            Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 5);
            Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.blue, null, 3);
        }
        public bool Contains(Vector2 pos)
		{
            return Rect.Contains(pos);
		}
    }
}
