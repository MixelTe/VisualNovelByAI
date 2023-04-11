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
    private List<Dialogue> _dialogues;
    private List<DialogNode> _nodes = new();
    private int _maxIndex = 0;
    private string[] Characters = null;
    private string[] Backgrounds = null;
    private string[] Field = null;
    private Vector2 Offset { get => _offset; }

    private Vector2 _drag = Vector2.zero;
    private Vector2 _offset = Vector2.zero;
    private float _zoom = 1;
    private bool _creatingConnection = false;
    private DialogNode _creatingConnection_Node;
    private int _creatingConnection_choice;

    private void Init(StoryData data)
	{
        _data = data;
        _nodes = new List<DialogNode>();
        _dialogues = _data.Dialogues.Select(d => d.Clone()).ToList();
        Characters = _data.Characters.Select(ch => ch.Name).Append("").ToArray();
        Backgrounds = _data.BackgroundImages.Select(i => i.Name).Append(BackgroundImage.Same).ToArray();
        Field = _data.Fields.Prepend("").ToArray();

        _maxIndex = 0;
        _drag = Vector2.zero;
        _offset = Vector2.zero;
        _zoom = 1;

        CreateNodes();
    }

    private void CreateNodes()
	{
        if (!_dialogues.Any(d => d.id == 0))
		{
            _dialogues.Add(new Dialogue());
        }

        var nodes = new Dictionary<int, DialogNode>();
		foreach (var dialogue in _dialogues)
        {
            if (dialogue.id > _maxIndex)
                _maxIndex = dialogue.id;

            var node = new DialogNode(dialogue, this);
            _nodes.Add(node);
            nodes[dialogue.id] = node;
        }

        foreach (var node in _nodes)
        {
            var nextNodes = new List<DialogNode>();
            foreach (var choice in node.Dialog.Choices)
            {
                if (nodes.TryGetValue(choice.NextDialogueId, out var dialog))
                    nextNodes.Add(dialog);
                else
                    choice.NextDialogueId = -1;
            }
            node.Next = nextNodes;
        }
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
        GUILayout.BeginArea(new Rect(4, 4, 64, 24));
        if (GUILayout.Button("Save"))
        {
            SaveChanges();
        }
        GUILayout.EndArea();

        hasUnsavedChanges = true;
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
        GUILayout.Button("Save");
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Screen.width - 100, 4, 96, 24));
        if (GUILayout.Button("Reset zoom"))
            _zoom = 1;
        GUILayout.EndArea();

        if (Event.current.type == EventType.ScrollWheel)
        {
            if (Event.current.delta.y > 0)
                _zoom *= 0.9f;
            else
                _zoom *= 1.1f;
            _zoom = Mathf.Clamp(_zoom, 0.5f, 1.6f);
            Repaint();
        }
        
        wantsMouseMove = _creatingConnection;
        if (_creatingConnection && Event.current.type == EventType.MouseMove)
            Repaint();

        if (mouseEventProcessed) return;

        if (Event.current.type == EventType.MouseDown)
		{
            if (Event.current.button != 0)
			{
                _drag = Event.current.mousePosition - _offset;
			}
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
            if (_creatingConnection && Event.current.button == 0)
			{
                _creatingConnection = false;
                var node = AddNode(Event.current.mousePosition - _offset);
                _creatingConnection_Node.SetConnection(_creatingConnection_choice, node);
            }
        }
    }

    private void AddChoice(DialogNode dialogNode)
    {
        var newDialog = new Dialogue()
        {
            PosInEditor = new Vector2(
                dialogNode.Position.x + DialogNode.Width + 24, 
                dialogNode.Position.y + (dialogNode.OptionHeight + dialogNode.Height - 60) * dialogNode.Next.Count),
            id = ++_maxIndex
        };
        _dialogues.Add(newDialog);
        dialogNode.Dialog.Choices.Add(new Choice()
        {
            NextDialogueId = newDialog.id,
            Text = "Continue"
        });

        var node = new DialogNode(newDialog, this);
        _nodes.Add(node);
        dialogNode.Next.Add(node);
    }

    private DialogNode AddNode(Vector2 pos)
    {
        var newDialog = new Dialogue()
        {
            PosInEditor = pos,
            id = ++_maxIndex
        };
        _dialogues.Add(newDialog);

        var node = new DialogNode(newDialog, this);
        _nodes.Add(node);
        return node;
    }

    private void DeleteNode(DialogNode node)
	{
        _nodes.Remove(node);
        _dialogues.Remove(node.Dialog);
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

    public override void SaveChanges()
    {
        _data.Dialogues = _dialogues.Select(d => d.Clone()).ToList();
        EditorUtility.SetDirty(_data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        base.SaveChanges();
    }

    public override void DiscardChanges()
    {
        base.DiscardChanges();
    }

    private class DialogNode
	{
        public static readonly int Width = 250;
        private static readonly int _heightDialogue = 210;
        private static readonly int _heightSetter = 120;
        private static readonly int _heightSwitch = 90;
        private static readonly int _heightOptionDialogue = 52;
        private static readonly int _heightOptionSetter = 30;
        private static readonly int _heightOptionSwitch = 90;
        private static readonly int _connectionTop = 32;
        private static readonly int _fontSize = 12;
        public int OptionHeight { get => Dialog.Type == DialogueType.Dialogue ? _heightOptionDialogue : Dialog.Type == DialogueType.Setter ? _heightOptionSetter : _heightOptionSwitch; }
        public int Height { get => Dialog.Type == DialogueType.Dialogue ? _heightDialogue : Dialog.Type == DialogueType.Setter ? _heightSetter : _heightSwitch; }
        private float _height { get => Height - (_storyEditor._zoom > 1 ? Height * 0f : Height * 0.4f) * (_storyEditor._zoom - 1); }
        private float _optionHeight { get => OptionHeight - (_storyEditor._zoom > 1 ? OptionHeight * 0.02f : OptionHeight * 0.05f) * (_storyEditor._zoom - 1); }
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

        public Rect Rect => new Rect(Position.x, Position.y, Width, _height + _optionHeight * Dialog.Choices.Count).Mul(_storyEditor._zoom);

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

            var fontSize = Mathf.RoundToInt(_fontSize * _storyEditor._zoom);
            var lineHeight = fontSize * 1.5f;
            _button.fontSize = Mathf.RoundToInt(_fontSize * _storyEditor._zoom);
            _textArea.fontSize = Mathf.RoundToInt(_fontSize * _storyEditor._zoom);

            var editorGUIUtility_labelWidth = EditorGUIUtility.labelWidth;
            var editorStyles_popup_fontSize = EditorStyles.popup.fontSize;
            var editorStyles_popup_fixedHeight = EditorStyles.popup.fixedHeight;
            var editorStyles_label_fontSize = EditorStyles.label.fontSize;
            var editorStyles_label_fixedHeight = EditorStyles.label.fixedHeight;
            var editorStyles_textField_fontSize = EditorStyles.textField.fontSize;
            var editorStyles_textField_fixedHeight = EditorStyles.textField.fixedHeight;
            var editorStyles_toggle_fontSize = EditorStyles.toggle.fontSize;
            var editorStyles_toggle_fixedHeight = EditorStyles.toggle.fixedHeight;
            EditorStyles.popup.fontSize = fontSize;
            EditorStyles.popup.fixedHeight = fontSize * 1.5f;
            EditorStyles.label.fontSize = fontSize;
            EditorStyles.label.fixedHeight = fontSize * 1.5f;
            EditorStyles.textField.fontSize = fontSize;
            EditorStyles.textField.fixedHeight = fontSize * 1.5f;
            EditorStyles.toggle.fontSize = fontSize;
            EditorStyles.toggle.fixedHeight = fontSize * 1.5f;

            GUILayout.BeginArea(new Rect(Rect.position + offset, Rect.size), _nodeStyle);
            if (Dialog.id != 0)
			{
                if (GUI.Button(new Rect(new Vector2(Width - 32, 10) * _storyEditor._zoom, new Vector2(18, 18) * _storyEditor._zoom), "x", _button))
                {
                    if (EditorUtility.DisplayDialog("Delete Dialog Node", "Are you sure?", "Delete", "Cancel"))
                    {
                        _storyEditor.DeleteNode(this);
                    }
                }
            }


            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"ID: {Dialog.id}", new GUILayoutOption[]
			{
                GUILayout.Height(lineHeight),
                GUILayout.Width(40 * _storyEditor._zoom)
            });
            EditorGUIUtility.labelWidth = 36 * _storyEditor._zoom;
            
            GUILayout.BeginHorizontal(GUILayout.Width((Width - 35 - 40 - 20) * _storyEditor._zoom));
            Dialog.Type = (DialogueType)EditorGUILayout.EnumPopup("Type", Dialog.Type, GUILayout.Height(lineHeight));
            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 80 * _storyEditor._zoom;

            if (Dialog.Type == DialogueType.Dialogue)
                DrawDialogue(lineHeight);
            else if (Dialog.Type == DialogueType.Setter)
                DrawSetter(lineHeight);
            else if (Dialog.Type == DialogueType.Switch)
                DrawSwitch(lineHeight);

            GUILayout.EndVertical();
            GUILayout.EndArea();

            EditorGUIUtility.labelWidth = editorGUIUtility_labelWidth;
            EditorStyles.popup.fontSize = editorStyles_popup_fontSize;
            EditorStyles.popup.fixedHeight = editorStyles_popup_fixedHeight;
            EditorStyles.label.fontSize = editorStyles_label_fontSize;
            EditorStyles.label.fixedHeight = editorStyles_label_fixedHeight;
            EditorStyles.textField.fontSize = editorStyles_textField_fontSize;
            EditorStyles.textField.fixedHeight = editorStyles_textField_fixedHeight;
            EditorStyles.toggle.fontSize = editorStyles_toggle_fontSize;
            EditorStyles.toggle.fixedHeight = editorStyles_toggle_fixedHeight;

            if (mouseEventProcessed) return false;

            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button != 0) return false;
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

        public void DrawDialogue(float lineHeight)
		{
            var character = EditorGUILayout.Popup("Character", Array.IndexOf(_storyEditor.Characters, Dialog.Character), _storyEditor.Characters, GUILayout.Height(lineHeight));
            Dialog.Character = _storyEditor.Characters[character];

            Dialog.CharacterEmotion = (Emotions)EditorGUILayout.EnumPopup("Emotion", Dialog.CharacterEmotion, GUILayout.Height(lineHeight));

            EditorStyles.textField.wordWrap = true;
            Dialog.Text = EditorGUILayout.TextArea(Dialog.Text, _textArea, GUILayout.Height(50 * _storyEditor._zoom));

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
                choice.Text = EditorGUILayout.TextArea(choice.Text, _textArea, GUILayout.Height(50 * _storyEditor._zoom));
            }
        }
        
        public void DrawSetter(float lineHeight)
        {
            var field = EditorGUILayout.Popup("Field", Array.IndexOf(_storyEditor.Field, Dialog.Field), _storyEditor.Field, GUILayout.Height(lineHeight));
            field = Mathf.Clamp(field, 0, _storyEditor.Field.Length);
            Dialog.Field = _storyEditor.Field[field];
            Dialog.Value = EditorGUILayout.IntField("Value", Dialog.Value, GUILayout.Height(lineHeight));
            EditorGUIUtility.labelWidth = Width * _storyEditor._zoom - 48;
            Dialog.SetOrChange = EditorGUILayout.Toggle("Set this value (add otherwise)", Dialog.SetOrChange, GUILayout.Height(lineHeight));

            if (Next.Count == 0)
                _storyEditor.AddChoice(this);
        }

        public void DrawSwitch(float lineHeight)
        {
            if (GUILayout.Button("Add", _button))
            {
                _storyEditor.AddChoice(this);
            }

            for (int i = 0; i < Dialog.Choices.Count; i++)
            {
                var choice = Dialog.Choices[i];
                var splited = choice.Text.Split(";");
                var field = splited[0];
                var oper = Choice.Operators[0];
                var value = 0;
                if (splited.Length > 1)
					oper = splited[1];
                if (splited.Length > 2)
                    int.TryParse(splited[2], out value);

                GUILayout.BeginVertical(_nodeStyle, GUILayout.Height(_optionHeight * _storyEditor._zoom));
                var fieldNew = EditorGUILayout.Popup(Array.IndexOf(_storyEditor.Field, field), _storyEditor.Field, GUILayout.Height(lineHeight));
                var operNew = EditorGUILayout.Popup(Array.IndexOf(Choice.Operators, oper), Choice.Operators, GUILayout.Height(lineHeight));
                var valueNew = value;
                if (operNew != 3)
				{
                    valueNew = EditorGUILayout.IntField(value, GUILayout.Height(lineHeight));
				}
                GUILayout.EndVertical();
                fieldNew = Mathf.Clamp(fieldNew, 0, _storyEditor.Field.Length);
                operNew = Mathf.Clamp(operNew, 0, Choice.Operators.Length);

                choice.Text = $"{_storyEditor.Field[fieldNew]};{Choice.Operators[operNew]};{valueNew}";
            }
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
                    p1 = new Vector2(Rect.x + 7 * _storyEditor._zoom, Rect.y + (_height + _optionHeight * i) * _storyEditor._zoom);
                    p2 = new Vector2(choice.Rect.xMax - 7 * _storyEditor._zoom, choice.Rect.y + _connectionTop * _storyEditor._zoom);
                }
                else
				{
                    p1 = new Vector2(Rect.xMax - 4, Rect.y + (_height + _optionHeight * i) * _storyEditor._zoom);
                    p2 = new Vector2(choice.Rect.x + 4 * _storyEditor._zoom, choice.Rect.y + _connectionTop * _storyEditor._zoom);
                }

                if (_storyEditor._creatingConnection && _storyEditor._creatingConnection_Node == this)
				{
                    if (_storyEditor._creatingConnection_choice == i)
					{
                        p2 = Event.current.mousePosition - offset;
                    }
				}

                DrawConnection(p1 + offset, p2 + offset);
            }
        }
        public void DrawConnectionPoints(Vector2 offset)
		{
            var size = 24 * _storyEditor._zoom;
            if (GUI.Button(new Rect(Rect.x + offset.x - size + 7, Rect.y + offset.y + _connectionTop * _storyEditor._zoom - size / 2, size, size), "", _inputPointStyle))
			{
                if (_storyEditor._creatingConnection)
				{
                    _storyEditor._creatingConnection_Node.SetConnection(_storyEditor._creatingConnection_choice, this);
                    _storyEditor._creatingConnection = false;
                }
			}

            var x = Rect.xMax + offset.x - 7;
            for (int i = 0; i < Dialog.Choices.Count; i++)
            {
                if (GUI.Button(new Rect(x, Rect.y + offset.y + (_height + _optionHeight * i) * _storyEditor._zoom - size / 2, size, size), "", _outputPointStyle))
				{
                    _storyEditor._creatingConnection = true;
                    _storyEditor._creatingConnection_Node = this;
                    _storyEditor._creatingConnection_choice = i;
                }
            }
        }
        private void DrawConnection(Vector2 start, Vector2 end)
        {
            var startPos = new Vector3(start.x, start.y, 0);
            var endPos = new Vector3(end.x, end.y, 0);
            var mulX = Mathf.Sign(endPos.x - startPos.x);
            var startTan = startPos + Vector3.right * (50 * mulX * _storyEditor._zoom);
            var endTan = endPos + Vector3.left * (50 * mulX * _storyEditor._zoom);

            Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 5);
            Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.blue, null, 3);
        }

        public void SetConnection(int i, DialogNode next)
		{
            Next[i] = next;
            Dialog.Choices[i].NextDialogueId = next.Dialog.id;
		}

        public bool Contains(Vector2 pos)
		{
            return Rect.Contains(pos);
		}
    }
}
