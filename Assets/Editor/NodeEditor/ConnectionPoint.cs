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
    NodeEditor nodeEditor;
    public ConnectionPoint(NodeEditor nodeEditor, Node owner,ConnectPointTp tp)
    {
        this.nodeEditor = nodeEditor;
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

        if (GUI.Button(rect, ""))
        {
            if (nodeEditor.selectingPoint == null)
            {
                nodeEditor.selectingPoint = this;
            }
            else {
                if (nodeEditor.selectingPoint.tp != this.tp)
                {
                    //根据自己的类型来决定创建连接时参数的顺序
                    if (this.tp == ConnectPointTp.In)
                        nodeEditor.connectLines.Add(new ConnectLine(this, nodeEditor.selectingPoint));
                    else
                        nodeEditor.connectLines.Add(new ConnectLine(nodeEditor.selectingPoint, this));

                    //连接创建结束后将SelectingPoint置为空
                    nodeEditor.selectingPoint = null;
                }

            }
        }
    }
}
