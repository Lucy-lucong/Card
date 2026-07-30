using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NodeEditor : EditorWindow
{
    List<Node> nodes;
    List<ConnectLine> connectLines;
    [MenuItem("NodeMap/NodeEditor")]
    static void OpenWindow() {
        NodeEditor window = GetWindow<NodeEditor>();
        window.titleContent = new GUIContent("NodeEditor");
        window.nodes = new List<Node>();
        window.connectLines = new List<ConnectLine>();
       /* window.nodes.Add(new Node(new Vector2(0,0)));*/
    }

    private void OnGUI()
    {
        DrawNodes();
        DrawConnectLine();
        ProcessEvents(Event.current);
        if (GUI.changed)
        {
            Repaint();
        }
    }
    #region 节点
    /// <summary>
    /// 绘制节点
    /// </summary>
    private void DrawNodes()
    {
        foreach (var v in nodes)
        {
            v.Draw();
        }
    }

    private void ProcessEvents(Event e) {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 1)
                {
                    RightMouseMenu(e.mousePosition);
                }
                else if (e.button == 0)
                {

                }
                break;
            default:
                break;
        }

        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            var node = nodes[i];
            bool DragHappend = node.ProcessEvents(e);
            if (DragHappend)
            {
                GUI.changed = true;
                break;
            }
        }
    }
    #endregion

    #region 添加节点
    private void RightMouseMenu(Vector2 pos) {
        GenericMenu genericMenu = new GenericMenu();
        genericMenu.AddItem(new GUIContent("Add Node"),false,()=> ProcessAddNode(pos));
        genericMenu.ShowAsContext();
    }
    private void ProcessAddNode(Vector2 pos) {
        nodes.Add(new Node(pos));
    }
    #endregion

    private void DrawConnectLine() {
        foreach (var v in connectLines)
        {
            v.Draw();
        }
    }
}
