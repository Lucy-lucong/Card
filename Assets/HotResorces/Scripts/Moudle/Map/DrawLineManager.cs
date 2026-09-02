using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class DrawLineManager : MonoBehaviour
{
    public float stepSize = 10;
    public GameObject Item;
    private List<LineInfo> points;
    private List<PointInfo> PointInfos;
    private List<GameObject> ItemList;

    public void DrawLine(List<PointInfo> PointInfos)
    {
        this.PointInfos = PointInfos;
        if (points == null) points = new List<LineInfo>();
        points.Clear();
        for (int i = 0; i < PointInfos.Count; i++)
        {
            RefreshData(PointInfos[i]); 
        }
        RefreshItem();

    }
    private void RefreshData(PointInfo info)
    {
        
        if (points == null) points = new List<LineInfo>();
        Vector3 direction = info.endPoint - info.startPoint;
        float distance = direction.magnitude;
        int numSteps = Mathf.CeilToInt(distance / stepSize);
        Vector3 step = direction.normalized * stepSize;
        for (int i = 1; i < numSteps-1; i++)
        {
            Vector3 point =  info.startPoint + step * i;
            Vector3 point2 =  info.startPoint + step * (i+1);
            
            float SignedAngle = Vector2.SignedAngle(Vector2.up,point - point2);
            Vector3 euler = new Vector3(0, 0, SignedAngle);
            Quaternion rotation = Quaternion.Euler(euler);
            LineInfo inf = new LineInfo(point,rotation);
            points.Add(inf);
        }
    }

    private void RefreshItem()
    {
        if (points == null) return;
        if (ItemList == null)
        {
            ItemList = new List<GameObject>();
        }
        for (int i = 0; i < ItemList.Count; i++)
        {
            ItemList[i].gameObject.SetActive(false);
        }
        for (int i = 1; i < points.Count; i++)
        {
            GameObject nowObj;
            if (i >= ItemList.Count)
            {
                nowObj = Instantiate(Item, transform);
                ItemList.Add(nowObj);
            }
            else
            {
                nowObj = ItemList[i];
            }
           
            nowObj.transform.localPosition = points[i].point;
            nowObj.transform.rotation = points[i].rotation;
            
            // float SignedAngle = Vector2.SignedAngle(Vector2.up,nowObj.transform.localPosition - points[i + 1]);
            // Vector3 euler = new Vector3(0, 0, SignedAngle);
            // nowObj.transform.rotation = Quaternion.Euler(euler);
            nowObj.gameObject.SetActive(true);
        }

    }


}

public struct PointInfo
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public PointInfo(Vector3 startPoint,Vector3 endPoint)
    {
        this.startPoint = startPoint;
        this.endPoint = endPoint;
    }
}

public struct LineInfo
{
    public Vector3 point;
    public Quaternion rotation;
    public LineInfo(Vector3 point,Quaternion rotation)
    {
        this.point = point;
        this.rotation = rotation;
    }
}
