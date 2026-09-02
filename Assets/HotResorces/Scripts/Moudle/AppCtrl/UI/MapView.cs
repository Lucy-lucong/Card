using UnityEngine;

public sealed class MapView : UIBase
{
    [Header("Map References")]
    public Transform rootObj;
    public DrawLineManager drawLine;

    protected override void Awake()
    {
        base.Awake();

        if (AppCtrl.Instance == null || AppCtrl.Instance.MapMgr == null)
        {
            Debug.LogError("[MapView] MapMgr is not initialized.");
            return;
        }

        AppCtrl.Instance.MapMgr.SetData(rootObj, drawLine);
    }

    private void OnDestroy()
    {
        if (AppCtrl.Instance == null || AppCtrl.Instance.MapMgr == null)
        {
            return;
        }

        AppCtrl.Instance.MapMgr.ClearData();
    }
}
