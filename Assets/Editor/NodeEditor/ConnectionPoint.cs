using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ConnectPointTp { 
    In,
    Out
}
public class ConnectionPoint
{
    public Rect rect;
    public ConnectPointTp tp;
    public Node node;

    public ConnectionPoint(Node owner,ConnectPointTp tp)
    {
        node = owner;
        this.tp = tp;

        rect = new Rect(0,0,10,20);

    }
    
    public void Draw()
    {
        rect.y = node.rect.y + node.rect.height * 0.5f - rect.height * 0.5f;
        switch (tp)
        {
            case ConnectPointTp.In:
                rect.x = node.rect.x - rect.width;
                break;
            case ConnectPointTp.Out:
                rect.x = node.rect.x + node.rect.width;
                break;
            default:
                break;
        }

        GUI.Button(rect,"");
    }
}
