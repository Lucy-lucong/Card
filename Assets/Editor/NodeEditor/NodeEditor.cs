using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NodeEditor : EditorWindow
{
    public ConnectionPoint selectingPoint;

    private List<Node> nodes;
    public List<ConnectLine> connectLines;
    [MenuItem("NodeMap/NodeEditor")]
    static void OpenWindow() {
        NodeEditor window = GetWindow<NodeEditor>();
       
       /* window.nodes.Add(new Node(new Vector2(0,0)));*/
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("NodeEditor");
        nodes = new List<Node>();
        connectLines = new List<ConnectLine>();
    }

    private void OnGUI()
    {
        DrawNodes();
        DrawConnectLine();
        DrawPendingConnection(Event.current);
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
                    selectingPoint = null;
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
        nodes.Add(new Node(this,pos));
    }
    #endregion

    private void DrawConnectLine() {
        foreach (var v in connectLines)
        {
            v.Draw();
        }
    }

    private void DrawPendingConnection(Event e)
    {
        if (selectingPoint != null)//如果已经选择了一个连接点，则画出待连接的线
        {
            //贝塞尔曲线的起点，根据已选则点的方向做判断：
            Vector3 startPosition = (selectingPoint.tp == ConnectPointTp.In) ? selectingPoint.rect.center : e.mousePosition;
            Vector3 endPosition = (selectingPoint.tp == ConnectPointTp.In) ? e.mousePosition : selectingPoint.rect.center;

            Handles.DrawBezier(     //绘制通过给定切线的起点和终点的纹理化贝塞尔曲线
            startPosition,
            endPosition,
            startPosition + Vector3.left * 50f, //startTangent	贝塞尔曲线的起始切线。
            endPosition - Vector3.left * 50f,   //endTangent	贝塞尔曲线的终点切线。
            Color.white,        //color	    要用于贝塞尔曲线的颜色。
            null,               //texture	要用于绘制贝塞尔曲线的纹理。
            2f                  //width	    贝塞尔曲线的宽度。
            );

            GUI.changed = true;
        }
    }
}
