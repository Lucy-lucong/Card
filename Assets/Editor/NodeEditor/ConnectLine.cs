using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ConnectLine
{
    public ConnectionPoint inPoint;
    public ConnectionPoint outPoint;

    public ConnectLine(ConnectionPoint inPoint,ConnectionPoint outPoint) {
        this.inPoint = inPoint;
        this.outPoint = outPoint;
    }

    public void Draw()
    {
        Handles.DrawBezier(inPoint.rect.center,outPoint.rect.center,inPoint.rect.center+Vector2.left*50f,outPoint.rect.center + Vector2.left*50f,Color.white,null,2f);
    }
}
