using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Rect rect;
    private bool isDragged;
    public ConnectionPoint inPoint;
    public ConnectionPoint outPoint;
    public NodeEditor nodeEditor;

    public Node(NodeEditor nodeEditor, Vector2 pos) {
        this.nodeEditor = nodeEditor;
        rect = new Rect(pos.x,pos.y,160,40);
        inPoint = new ConnectionPoint(nodeEditor,this, ConnectPointTp.In);
        outPoint = new ConnectionPoint(nodeEditor,this, ConnectPointTp.Out);
    }

    public void Draw() {
        GUI.Box(rect,"Node");
        inPoint.Draw();
        outPoint.Draw();

       
    }

    public void ProcessDrag(Vector2 delta) {
        rect.position += delta;
    }

    public bool ProcessEvents(Event e) {
        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && rect.Contains(e.mousePosition)) 
                { 
                    isDragged = true;
                    GUI.changed = true;
                }
                break;
            case EventType.MouseUp:
                isDragged = false;
                break;
            case EventType.MouseDrag:
                if (e.button == 0 && isDragged)
                {
                    ProcessDrag(e.delta);
                    e.Use();
                    return true;
                }
                break;
            default:
                break;
        }
        return false;
    }
}
