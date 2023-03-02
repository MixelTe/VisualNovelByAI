using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class StoryEditorWindow : EditorWindow
{
    private List<DialogueNode> nodes = new List<DialogueNode>();
    private List<Connection> connections = new List<Connection>();
    private DialogueNode selectedNode = null;
    private const float nodeWidth = 200;
    private const float nodeHeight = 100;
    private GUIStyle nodeStyle;
    private GUIStyle selectedNodeStyle;
    private GUIStyle inputPointStyle;
    private GUIStyle outputPointStyle;
	private Vector2 drag;
	private List<DialogueNode> selectedNodes = new List<DialogueNode>();
    private Dictionary<int, Rect> nodeRects = new Dictionary<int, Rect>();

    public const int MARGIN = 20;
    public const int CHARACTER_IMAGE_WIDTH = 80;
    public const int CHARACTER_IMAGE_HEIGHT = 80;
    public const int CHARACTER_NAME_HEIGHT = 20;
    public const int TEXT_HEIGHT = 60;
    public const int BACKGROUND_IMAGE_WIDTH = 200;
    public const int BACKGROUND_IMAGE_HEIGHT = 120;
    public const int CHOICE_HEIGHT = 20;
    public const int CHOICE_CONNECTION_WIDTH = 15;
    public const int CHOICE_CONNECTION_HEIGHT = 25;

    private void OnEnable()
	{
        nodeStyle = new GUIStyle();
        nodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1.png") as Texture2D;
        nodeStyle.border = new RectOffset(12, 12, 12, 12);

        selectedNodeStyle = new GUIStyle();
        selectedNodeStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/node1 on.png") as Texture2D;
        selectedNodeStyle.border = new RectOffset(12, 12, 12, 12);

        inputPointStyle = new GUIStyle();
        inputPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left.png") as Texture2D;
        inputPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn left on.png") as Texture2D;
        inputPointStyle.border = new RectOffset(4, 4, 12, 12);

        outputPointStyle = new GUIStyle();
        outputPointStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right.png") as Texture2D;
        outputPointStyle.active.background = EditorGUIUtility.Load("builtin skins/darkskin/images/btn right on.png") as Texture2D;
        outputPointStyle.border = new RectOffset(4, 4, 12, 12);
    }

	private void OnGUI()
    {
        DrawNodes();
		DrawConnections();
        CreateNodeRects();

        ProcessEvents(Event.current);
        ProcessNodeEvents(Event.current);
        if (GUI.changed) Repaint();
    }

    private void DrawNodes()
    {
        foreach (DialogueNode node in nodes)
        {
            DrawNode(node);
        }
    }
    private void DrawNode(DialogueNode node)
    {

        // Create input and output points
        Vector2 inputPoint = new Vector2(node.rect.x + MARGIN, node.rect.y + (node.rect.height / 2f));
        Vector2 outputPoint = new Vector2(node.rect.x + node.rect.width - MARGIN, node.rect.y + (node.rect.height / 2f));

        // Draw input and output points
        GUI.Box(new Rect(inputPoint.x - 5f, inputPoint.y - 5f, 10f, 10f), "", inputPointStyle);
        GUI.Box(new Rect(outputPoint.x - 5f, outputPoint.y - 5f, 10f, 10f), "", outputPointStyle);

        // Draw node box
        GUILayout.BeginArea(node.rect, nodeStyle);
        EditorGUIUtility.labelWidth = 100f;

        // Draw character name input
        node.characterName = EditorGUILayout.TextField("Character Name", node.characterName);

        // Draw character image input
        node.characterImage = (Sprite)EditorGUILayout.ObjectField("Character Image", node.characterImage, typeof(Sprite), false, GUILayout.Width(CHARACTER_IMAGE_WIDTH), GUILayout.Height(CHARACTER_IMAGE_HEIGHT));

        // Draw text input
        node.text = EditorGUILayout.TextField("Text", node.text, GUILayout.Height(TEXT_HEIGHT));

        // Draw background image input
        node.backgroundImage = (Sprite)EditorGUILayout.ObjectField("Background Image", node.backgroundImage, typeof(Sprite), false, GUILayout.Width(BACKGROUND_IMAGE_WIDTH), GUILayout.Height(BACKGROUND_IMAGE_HEIGHT));

        // Draw choices list
        GUILayout.Label("Choices:");
        for (int i = 0; i < node.choices.Count; i++)
        {
            GUILayout.BeginHorizontal();

            // Draw choice text input
            node.choices[i].text = EditorGUILayout.TextField("Choice " + (i + 1) + " Text", node.choices[i].text);
            Rect connectionRect = new Rect(node.rect.xMax - (CHOICE_CONNECTION_WIDTH / 2), CHOICE_CONNECTION_HEIGHT * i, CHOICE_CONNECTION_WIDTH, CHOICE_CONNECTION_HEIGHT);

            // Create choice input and output points
            Vector2 choiceInputPoint = new Vector2(node.rect.x + (node.rect.width / 2f), node.rect.y + node.rect.height + MARGIN + (i * (CHOICE_HEIGHT + CHOICE_CONNECTION_HEIGHT)));
            Vector2 choiceOutputPoint = new Vector2(choiceInputPoint.x, choiceInputPoint.y + CHOICE_HEIGHT + CHOICE_CONNECTION_HEIGHT);

            // Draw choice input and output points
            GUI.Box(new Rect(choiceInputPoint.x - 5f, choiceInputPoint.y - 5f, 10f, 10f), "", inputPointStyle);
            GUI.Box(new Rect(choiceOutputPoint.x - 5f, choiceOutputPoint.y - 5f, 10f, 10f), "", outputPointStyle);

            // Draw choice label
            GUI.Label(new Rect(choiceInputPoint.x - 30f, choiceInputPoint.y - 30f, 60f, 20f), "Choice " + (i + 1));

            // Draw choice connections
            DrawConnectionPoint(choiceInputPoint, true, node.choices[i]);
            DrawConnection(connectionRect, node.rect, node.choices[i]);

            GUILayout.EndHorizontal();
        }

        // Draw add choice button
        if (GUILayout.Button("Add Choice"))
        {
            AddChoice(node);
        }

        GUILayout.EndArea();

	}

	// Draws a connection point for a given connection and choice
	private void DrawConnectionPoint(Vector2 position, bool isInput, Choice choice)
    {
        Rect rect = new Rect(position.x - (CHOICE_CONNECTION_WIDTH * 0.5f), position.y - (CHOICE_CONNECTION_HEIGHT * 0.5f), CHOICE_CONNECTION_WIDTH, CHOICE_CONNECTION_HEIGHT);
        GUIStyle style = isInput ? inputPointStyle : outputPointStyle;

        // Draw the connection point with the corresponding style
        GUI.Box(rect, "", style);

        // If the connection point is an output point and it has a connection, draw the connection
        if (!isInput && choice.destinationNodeId != -1)
        {
            DrawConnection(rect, nodeRects[choice.destinationNodeId], choice);
        }
    }
    private void CreateNodeRects()
    {
        nodeRects.Clear();
        foreach (var node in nodes)
        {
            nodeRects[node.id] = node.rect;
        }
    }

    // Draws a connection between two nodes with a given choice
    private void DrawConnection(Rect start, Rect end, Choice choice)
    {
        Vector3 startPos = new Vector3(start.x + start.width, start.y + start.height / 2, 0);
        Vector3 endPos = new Vector3(end.x, end.y + end.height / 2, 0);
        Vector3 startTan = startPos + Vector3.right * 50;
        Vector3 endTan = endPos + Vector3.left * 50;
        Color shadowCol = new Color(0, 0, 0, 0.06f);

        Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.black, null, 5);
        Handles.DrawBezier(startPos, endPos, startTan, endTan, Color.blue, null, 3);

        // Check if the mouse is over the connection
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            if (ContainsPoint(Event.current.mousePosition, startPos, endPos))
            {
                //selectedConnection = choice;
                Repaint();
            }
        }
    }
    private bool ContainsPoint(Vector2 p, Vector2 start, Vector2 end)
    {
        float distanceFromPointToLine = HandleUtility.DistancePointLine(p, start, end);
        return distanceFromPointToLine < 10f; // adjust distance threshold as needed
    }



    // Processes the context menu for a given node
    private void ProcessContextMenu(Vector2 mousePosition, DialogueNode node)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Add Choice"), false, () => { AddChoice(node); });
        menu.AddItem(new GUIContent("Delete Node"), false, () => { OnRemoveNode(node); });
        menu.ShowAsContext();
        Event.current.Use();
    }
    private void OnRemoveNode(DialogueNode node)
    {
        if (selectedNode == node)
        {
            selectedNode = null;
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].choices.RemoveAll(choice => choice.destinationNodeId == node.id);
        }

        nodes.Remove(node);
    }

    private void AddChoice(DialogueNode node)
    {
        int newId = GetUniqueNodeId();

        Choice newChoice = new Choice
        {
            text = "New Choice",
            destinationNodeId = newId
        };

        node.choices.Add(newChoice);

        DialogueNode destinationNode = new DialogueNode
        {
            id = newId,
            characterName = "",
            characterImage = null,
            text = "",
            backgroundImage = null,
            choices = new List<Choice>(),
            rect = new Rect(node.rect.x + 250, node.rect.y, node.rect.width, node.rect.height)
        };

        nodes.Add(destinationNode);
    }
    private int GetUniqueNodeId()
    {
        int nodeId = 0;
        while (nodeRects.ContainsKey(nodeId))
        {
            nodeId++;
        }
        return nodeId;
    }


    private void DrawConnections()
    {
        foreach (Connection connection in connections)
        {
            DialogueNode sourceNode = connection.startNode;
            DialogueNode targetNode = connection.endNode;

            Vector2 sourcePos = new Vector2(sourceNode.rect.x + nodeWidth, sourceNode.rect.y + nodeHeight / 2);
            Vector2 targetPos = new Vector2(targetNode.rect.x, targetNode.rect.y + nodeHeight / 2);

            Vector2 sourceTangent = sourcePos + Vector2.right * 50;
            Vector2 targetTangent = targetPos - Vector2.right * 50;

            Handles.DrawBezier(sourcePos, targetPos, sourceTangent, targetTangent, Color.white, null, 4);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                if (HandleUtility.DistanceToLine(sourcePos, targetPos) < 10)
                {
                    ProcessContextMenu(Event.current.mousePosition);
                    Event.current.Use();
                }
            }
        }
    }
    private void ProcessNodeEvents(Event e)
    {
        foreach (var node in nodes)
        {
            node.ProcessEvents(e);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (node.rect.Contains(e.mousePosition))
                {
                    if (!selectedNodes.Contains(node))
                    {
                        selectedNodes.Clear();
                        selectedNodes.Add(node);
                    }

                    //if (selectedConnection != null)
                    //{
                    //    selectedConnection = null;
                    //}
                    //else
                    //{
                    //    // Loop through all connections and select the one that was clicked on, if any
                    //    foreach (var connection in connections)
                    //    {
                    //        if (connection.ContainsPoint(e.mousePosition))
                    //        {
                    //            selectedConnection = connection;
                    //            break;
                    //        }
                    //    }
                    //}

                }
                else
                {
                    selectedNodes.Clear();
                }
            }
        }
    }


    private void ProcessEvents(Event e)
    {
        drag = Vector2.zero;

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    ClearConnectionSelection();
                }

                if (e.button == 1)
                {
                    ProcessContextMenu(e.mousePosition);
                }
                break;

            case EventType.MouseDrag:
                if (e.button == 0)
                {
                    OnDrag(e.delta);
                }
                break;
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
        {
            DeleteSelectedNodes();
        }
    }

    private void ClearConnectionSelection()
    {
        //selectedConnection = null;
    }

    private void OnDrag(Vector2 delta)
    {
        drag = delta;

        foreach (var node in selectedNodes)
        {
            node.rect.position += drag;
        }

        GUI.changed = true;
    }

    private void DeleteSelectedNodes()
    {
        foreach (var node in selectedNodes)
        {
            // First, we need to delete any connections attached to this node
            List<Connection> connectionsToRemove = new List<Connection>();
            foreach (var connection in connections)
            {
                if (connection.startNode == node || connection.endNode == node)
                {
                    connectionsToRemove.Add(connection);
                }
            }
            foreach (var connection in connectionsToRemove)
            {
                connections.Remove(connection);
            }

            // Now, we can remove the node itself
            nodes.Remove(node);
        }

        // Finally, we clear the selection
        selectedNodes.Clear();
    }



    private void ProcessContextMenu(Vector2 mousePosition)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Add Node"), false, () => AddNode(mousePosition));

        menu.ShowAsContext();
    }

    private void DeleteConnection(Connection connection)
    {
        connections.Remove(connection);
    }

    private void AddNode(Vector2 position)
    {
        DialogueNode newNode = new DialogueNode();
        newNode.rect = new Rect(position.x, position.y, nodeWidth, nodeHeight);
        newNode.id = nodes.Count;
        nodes.Add(newNode);
    }


    [MenuItem("Window/Story Editor_")]
    public static void ShowEditor()
    {
        GetWindow<StoryEditorWindow>("Story Editor");
    }

    public class DialogueNode
    {
        public string characterName = "";
        public Sprite characterImage;
        public string text = "";
        public Sprite backgroundImage;
        public List<Choice> choices = new List<Choice>();
        public int id;
        public Rect rect;
		private bool isDragged;

        public void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        if (rect.Contains(e.mousePosition))
                        {
                            isDragged = true;
                            GUI.changed = true;
                            EditorGUI.FocusTextInControl(null);
                        }
                        else
                        {
                            GUI.changed = true;
                        }
                    }
                    break;

                case EventType.MouseUp:
                    isDragged = false;
                    break;

                case EventType.MouseDrag:
                    if (e.button == 0 && isDragged)
                    {
                        Drag(e.delta);
                        e.Use();
                        return;
                    }
                    break;

                case EventType.Repaint:
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    GUI.skin.label.normal.textColor = Color.white;
                    GUI.Label(rect, characterName + "\n" + text);
                    break;
            }
        }

        public void Drag(Vector2 delta)
        {
            rect.position += delta;
        }
    }


    public class Choice
    {
        public string text;
        public int destinationNodeId;
    }

    public class Connection
    {
        public DialogueNode startNode;
        public DialogueNode endNode;
        private float selectionRadius = 10f;
        public bool ContainsPoint(Vector2 point)
        {
            // Calculate the distance from the point to the line segment connecting the two nodes
            float distance = HandleUtility.DistancePointLine(point, startNode.rect.center, endNode.rect.center);

            // If the distance is less than the selection radius, the point is considered to be "inside" the connection
            return distance < selectionRadius;
        }
    }
}
