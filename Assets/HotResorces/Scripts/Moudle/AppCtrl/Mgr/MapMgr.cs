using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapMgr
{
    private const string MapItemPrefabPath = "Assets/HotResorces/Base/Prefabs/Map/MapItem.prefab";

    /// <summary>
    /// 最大层数
    /// </summary>
    public int MaxLayer = 15;
    /// <summary>
    /// 起点数量
    /// </summary>
    public int entranceCount = 3;
    /// <summary>
    /// 道具节点
    /// </summary>
    public MapItemBase itemBase;
    /// <summary>
    /// 生成根路径
    /// </summary>
    public Transform rootObj;
    /// <summary>
    /// 层字典
    /// </summary>
    public Dictionary<int, MaplayerItem> layerDic;
    /// <summary>
    /// 层字典
    /// </summary>
    public List<MaplayerItem> layerList;
    /// <summary>
    /// 绘制地图连线
    /// </summary>
    public List<PointInfo> PointInfoList;

    public DrawLineManager drawLine;

    /*private static MapManager instance;

    public static MapManager Instance
    {
        get => instance;
        set => instance = value;
    }*/

    public void Init()
    {
        /*InitData();
        RefreshLineData();
        DrawLine();*/
    }

    // Update is called once per frame
    /* void Update()
     {
         if (Input.GetKeyDown(KeyCode.Space))
         {
             Clear();
             Init();
         }
     }*/

    public void SetData(Transform rootObj, DrawLineManager drawLine)
    {
        if (rootObj == null || drawLine == null)
        {
            Debug.LogError("[MapMgr] Map root or line manager is not assigned.");
            return;
        }

        this.rootObj = rootObj;
        this.drawLine = drawLine;
        AppCtrl.Instance.ResMgr.LoadAssetAsync<GameObject>(MapItemPrefabPath, rootObj.gameObject, mapItemPrefab =>
        {
            if (this.rootObj != rootObj || this.drawLine != drawLine)
            {
                return;
            }

            itemBase = mapItemPrefab != null ? mapItemPrefab.GetComponent<MapItemBase>() : null;
            if (itemBase == null)
            {
                Debug.LogError($"[MapMgr] MapItemBase is missing on prefab: {MapItemPrefabPath}");
                return;
            }

            Clear();
            InitData();
            RefreshLineData();
            DrawLine();
        });
    }

    public void ClearData()
    {
        Clear();
        itemBase = null;
        rootObj = null;
        drawLine = null;
        PointInfoList?.Clear();
    }

    public void InitData()
    {
     
        if (layerDic == null)
        {
            layerDic = new Dictionary<int, MaplayerItem>();
        }
        if (layerList == null)
        {
            layerList = new List<MaplayerItem>();
        }
        for (int i = 0; i < MaxLayer; i++)
        {
            MaplayerItem item = new MaplayerItem(i, entranceCount);
            ELayerType eLayerType = i == 0 ? ELayerType.Start : i == MaxLayer - 1 ? ELayerType.End : ELayerType.Cent;
            item.InitData(eLayerType, itemBase, rootObj);
            layerDic.Add(i, item);
            layerList.Add(item);

        }
    }
    /// <summary>
    /// 刷新地图路径数据
    /// </summary>
    public void RefreshLineData()
    {
        if (layerList == null)
        {
            return;
        }
        int MaxCount = layerList.Count;
        for (int i = 0; i < layerList.Count; i++)
        {

            //上层
            MaplayerItem TopItem = i == MaxCount - 1 ? null : layerList[i + 1];
            //当前层
            MaplayerItem nowItem = layerList[i];
            nowItem.RefreshTopLineData(TopItem == null ? null : TopItem.MapItemDic);
        }
        for (int i = 0; i < layerList.Count; i++)
        {
            //当前层
            MaplayerItem nowItem = layerList[i];
            ////下层
            MaplayerItem DownItem = i == 0 ? null : layerList[i - 1];
            if (DownItem != null)
            {
                nowItem.TopLineAdditional(DownItem.MapItemDic);
            }
        }


    }

    public void DrawLine()
    {
        if (layerList == null)
        {
            return;
        }

        if (PointInfoList==null)
        {
            PointInfoList=new  List<PointInfo>();
        }
        PointInfoList.Clear();
        for (int i = 0; i < layerList.Count; i++)
        {
            if ( layerList[i].MapItemDic!=null)
            {
                foreach (var VARIABLE in   layerList[i].MapItemDic)
                {
                    MapItemBase item = VARIABLE.Value;
                    if (item!=null&&item.TopItemList!=null)
                    {
                        for (int j = 0; j < item.TopItemList.Count; j++)
                        {
                            PointInfo info=new  PointInfo(item.transform.localPosition,item.TopItemList[j].transform.localPosition);
                            PointInfoList.Add(info);
                        }
                    }
                    
                }
            }
        }
        drawLine.DrawLine(PointInfoList);
    }

    public void Clear()
    {
        if (layerList != null)
        {
            for (int i = 0; i < layerList.Count; i++)
            {
                layerList[i].Clear();
            }

            layerList.Clear();
        }

        layerDic?.Clear();
    }
}
