using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MapItemBase : MonoBehaviour
{
    public Text text;
    public Image bgImage;
    public List<Sprite> Sprites;
    public List<Sprite> BossSprites;
    /// <summary>
    /// 名称
    /// </summary>
    public string name;
    /// <summary>
    /// 所在层
    /// </summary>
    public int layer;
   
    /// <summary>
    /// 下层对象
    /// </summary>
    public MapItemBase DownItem;
    private List<MapItemBase> topItemList;
    /// <summary>
    /// 上层对象列表
    /// </summary>
    public List<MapItemBase> TopItemList
    {
        get
        {
            if (topItemList == null) topItemList = new List<MapItemBase>();
            return topItemList;
        }
        set => topItemList = value;
    }

    /// <summary>
    /// 刷新UI展示
    /// </summary>
    /// <param name="name"></param>
    /// <param name="layer"></param>
    public void RefreshUI(string name, int layer)
    {
        this.name = name;
        this.layer = layer;
        if (text != null)
        {
            text.text = name;
        }
        gameObject.name = name;
        RefreshBgImage();
        bgImage.SetNativeSize();
    }
    private  void RefreshBgImage()
    {
        if (Sprites==null ||Sprites.Count<=0||layer==0)
        {
            return;
        }

        if (layer==AppCtrl.Instance.MapMgr.MaxLayer-2)
        {
            bgImage.sprite = Sprites[3];
            return;
        }
        if (layer==(AppCtrl.Instance.MapMgr.MaxLayer-1)&&BossSprites.Count>0)
        {
            int randomBoss = Random.Range(0, BossSprites.Count);
            bgImage.sprite = BossSprites[randomBoss];
            return;
        }
        int random = Random.Range(0, 30);
        if (random<20)
        {
            bgImage.sprite = Sprites[0];
            return;
        }
        random = Random.Range(0, Sprites.Count);
        bgImage.sprite = Sprites[random];
        
    }
    /// <summary>
    /// 得到距离当前对象最近的对象
    /// </summary>
    /// <param name="ComparelayerDic"></param>
    /// <returns></returns>
    public MapItemBase GetNearestDistanceItem(Dictionary<int, MapItemBase> ComparelayerDic)
    {
        if (ComparelayerDic == null) { return null; }
        float dis = -1;
        bool isStart = false;
        MapItemBase topItem = null;
        foreach (var item in ComparelayerDic)
        {
            MapItemBase targetItem = item.Value;
            if (isStart == false)
            {
                isStart = true;
                dis = Vector3.Distance(transform.position, targetItem.transform.position);
                topItem = targetItem;
            }
            else
            {
                float nowDis = Vector3.Distance(transform.position, targetItem.transform.position);
                if (nowDis < dis)
                {
                    dis = nowDis;
                    topItem = targetItem;

                }
            }
        }

        return topItem;

    }

    public void Clear()
    {
        Destroy(this.gameObject);
    }
}
