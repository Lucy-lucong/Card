using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaplayerItem
{
    private int nowLayer;
    private int entranceCount;
    private int entranceMaxCount;
    private int ySize = 190;
    private int xSize = 200;
    private int xRandom = 50;
    private int yRandom = 30;
    private ELayerType layerType;

    public  Dictionary<int, MapItemBase> MapItemDic;

    public MaplayerItem(int nowLayer, int entranceCount)
    {
        this.nowLayer = nowLayer;
        this.entranceCount = entranceCount;
        this.entranceMaxCount = entranceCount * 2 ;
    }

    public void InitData(ELayerType layerType, MapItemBase item, Transform rootObj)
    {
        int count = 0;
        this.layerType = layerType;
        switch (layerType)
        {
            case ELayerType.Start:
                count = entranceCount;
                break;
            case ELayerType.Cent:
                count = entranceCount + Random.Range(2 - entranceCount, entranceCount);
                break;
            case ELayerType.End:
                count = 1;
                break;
            default:
                break;
        }
        InitLayerDic(count, item, rootObj);
    }
    public void InitLayerDic(int Count, MapItemBase item, Transform rootObj)
    {
        if (MapItemDic == null)
        {
            MapItemDic = new Dictionary<int, MapItemBase>();
        }
        else
        {
            Clear();
        }
        for (int i = 0; i < Count; i++)
        {
            MapItemBase nowItem = MonoBehaviour.Instantiate(item, rootObj);
            int xR = Random.Range(-xRandom, xRandom);
            int yR = Random.Range(-yRandom, yRandom);
            if (nowLayer == 0)
            {
                yR = 0;
            }
            float xPos = GetPosX(i, Count);
            float addendPosY = ELayerType.End == layerType ? 70 : 0;
            nowItem.transform.localPosition = new Vector3(xPos, nowLayer * ySize + yR+addendPosY, 0);
            nowItem.RefreshUI(nowLayer + "_" + i, nowLayer);
            MapItemDic.Add(i, nowItem);
        }

    }
    private float GetPosX(int nowIndix, float Count)
    {

        float onePos = entranceMaxCount * xSize / (Count + 1);
        float pos = (nowIndix + 1) * onePos;
        int xR = Random.Range(-xRandom, xRandom);
        return pos + xR;
    }


    /// <summary>
    /// 刷新上层数据列表
    /// </summary>
    /// <param name="ComparelayerDic"></param>
    public void RefreshTopLineData(Dictionary<int, MapItemBase> ComparelayerDic)
    {
        if (MapItemDic == null) { return; }
        foreach (var item in MapItemDic)
        {
            MapItemBase nowItem = item.Value;
            nowItem.TopItemList.Clear();
            //获取距离此节点最近的对象
            MapItemBase topItem= nowItem.GetNearestDistanceItem(ComparelayerDic);
            if (topItem != null) {
                topItem.DownItem = nowItem;
                nowItem.TopItemList.Add(topItem);
            }
        }

    }
    /// <summary>
    /// 断路检索
    /// </summary>
    /// <param name="ComparelayerDic"></param>
    public void TopLineAdditional(Dictionary<int, MapItemBase> ComparelayerDic) {
        if (MapItemDic == null) { return; }
        foreach (var item in MapItemDic)
        {
            MapItemBase nowItem = item.Value;
            if (nowItem.DownItem==null)
            {
                //获取距离此节点最近的对象
                MapItemBase downItem = nowItem.GetNearestDistanceItem(ComparelayerDic);
                if (downItem != null)
                {
                    nowItem.DownItem = downItem;
                    downItem.TopItemList.Add(nowItem);
                }
            }
        
        }
    }

    public void Clear()
    {
        foreach (var item in MapItemDic)
        {
            MapItemBase nowItem = item.Value;
            nowItem.Clear();
        }
        MapItemDic.Clear();
    }
}

public enum ELayerType
{
    Start,
    Cent,
    End

}
