using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 高性能 UI 管理器：
/// 1) UI 实例按路径缓存，关闭默认仅隐藏；
/// 2) 减少反复 Instantiate/Destroy 带来的 GC 与卡顿；
/// 3) 打开时支持透传 userData；
/// 4) 支持焦点管理：打开新 UI 时自动通知上一个 UI 失去焦点，关闭时通知上一个 UI 获得焦点。
/// </summary>
public sealed class UIMgr
{
    internal UIMgr() { }

    private Camera ui_camera;
    private Canvas root_canvas;

    Dictionary<string, Transform> ui_parents = new Dictionary<string, Transform>();
    Dictionary<UIType, Dictionary<string, int>> firtui_index = new Dictionary<UIType, Dictionary<string, int>>() { 
        [UIType.UI] = new Dictionary<string, int>(),
        [UIType.Tip] = new Dictionary<string, int>(),
    };

    Dictionary<UIType, int> layerIndex = new Dictionary<UIType, int>()
    {
        [UIType.UI] = 2,
        [UIType.Tip] = 5,
    };

    /// <summary>
    /// 按 UI 类型维护的 UI 栈，栈顶为当前最上层 UI
    /// </summary>
    Dictionary<UIType, Stack<UIBase>> allUI = new Dictionary<UIType, Stack<UIBase>>() {
        [UIType.UI] = new Stack<UIBase>(),
        [UIType.Tip] = new Stack<UIBase>(),
    };

    Dictionary<UIType, Dictionary<string,int>> waitList = new Dictionary<UIType, Dictionary<string, int>>()
    {
        [UIType.UI] = new Dictionary<string, int>(),
        [UIType.Tip] = new Dictionary<string, int>(),
    };

    /// <summary>
    /// 缓存已创建的 UI 实例，避免重复实例化
    /// </summary>
    Dictionary<string, UIBase> uiCache = new Dictionary<string, UIBase>();


    public void Init() {
        ui_camera = GameObject.Find("UICamera").GetComponent<Camera>();
        root_canvas = GameObject.Find("UICanvas").GetComponent<Canvas>();

        ui_parents["UI"] = root_canvas.transform.GetChild(0).Find("UI");
        ui_parents["Tip"] = root_canvas.transform.GetChild(0).Find("Tip");
    }


    /// <summary>
    /// 打开 UI
    /// </summary>
    /// <typeparam name="T">UI 类型</typeparam>
    /// <param name="ui_data">UI 数据（包含路径、类型等）</param>
    /// <param name="sent_data">透传给 UI 的数据</param>
    /// <returns>返回打开的 UI 实例</returns>
    public void OpenUI<T>(UIData ui_data, object[] sent_data, Action<UIBase> onCompleted = null) where T : UIBase
    {
        UIBase newUI = null;
        
        // 尝试从缓存获取已创建的 UI
        if (uiCache.TryGetValue(ui_data.path, out newUI))
        {
            OpenUICore(newUI, ui_data, sent_data);
            onCompleted?.Invoke(newUI);
            return;
        }

        // 缓存中没有，需要通过 ResMgr 异步加载
        AppCtrl.Instance.ResMgr.LoadAssetAsync<GameObject>(ui_data.path, null, (uiPrefab) =>
        {
            if (uiPrefab == null)
            {
                Debug.LogError($"[UIMgr] UI 预制体加载失败：{ui_data.path}");
                return;
            }
            
            Transform parent = ui_parents[ui_data.uitp.ToString()];

            // 在 Instantiate 之前写入静态槽，Awake 会同步触发并读取它
            UIBase._pendingSentData = sent_data;
            GameObject uiObj = UnityEngine.Object.Instantiate(uiPrefab, parent);
            // Awake 已执行，静态槽已被清空，此处无需再清

            newUI = uiObj.GetComponent<T>()? uiObj.GetComponent<T>():uiObj.AddComponent<T>();
            
            if (newUI == null)
            {
                Debug.LogError($"[UIMgr] UI 组件类型不匹配：{ui_data.path}");
                UIBase._pendingSentData = null; // 异常时保证清空
                UnityEngine.Object.Destroy(uiObj);
                return;
            }
            
            // 初始化 UI
            uiCache[ui_data.path] = newUI;
            
            OpenUICore(newUI, ui_data, sent_data);
            onCompleted?.Invoke(newUI);
        });
    }
    
    private void OpenUICore(UIBase newUI, UIData ui_data, object[] sent_data)
    {
        // 如果当前栈顶有 UI，通知它失去焦点
        if (allUI[ui_data.uitp].Count > 0)
        {
            UIBase topUI = allUI[ui_data.uitp].Peek();
            if (topUI.IsOpen)
            {
                topUI.OnFocus(false);
            }
        }
        
        // 设置 UI 层级顺序
        SetUIOrder(newUI);
        
        // 缓存命中时（Awake 不会再次执行），主动调用 OnOpen 刷新数据
        newUI.OnOpen(sent_data);
        
        // 压入栈
        allUI[ui_data.uitp].Push(newUI);
        
        // 通知新 UI 获得焦点
        newUI.OnFocus(true);
    }

    public bool TryAddToWaitList(UIData ui_data) {
        
        if (waitList[ui_data.uitp].ContainsKey(ui_data.path))
        {
            return false;
        }
        waitList[ui_data.uitp][ui_data.path] = 1; 
        return true;
    }

    public void SetUIOrder(UIBase ub) {
        ub.gameObject.GetComponent<Canvas>().sortingOrder = layerIndex[ub.uitp] * 1000 + 10 * allUI[ub.uitp].Count;
    }


    /// <summary>
    /// 关闭 UI
    /// </summary>
    /// <typeparam name="T">UI 类型</typeparam>
    /// <param name="ub">要关闭的 UI 实例</param>
    public void CloseUI<T>(UIBase ub) where T : UIBase
    {
        if (ub == null)
        {
            Debug.LogWarning("[UIMgr] 尝试关闭空 UI");
            return;
        }
        
        UIType uiType = ub.uitp;
        Stack<UIBase> stack = allUI[uiType];
        
        // 确保要关闭的 UI 在栈顶
        if (stack.Count == 0 || stack.Peek() != ub)
        {
            Debug.LogWarning($"[UIMgr] 尝试关闭非栈顶 UI：{ub.name}");
        }
        
        // 从栈中弹出
        if (stack.Count > 0 && stack.Peek() == ub)
        {
            stack.Pop();
        }

        // 从缓存中移除
        RemoveFromCache(ub);
        
        // 如果栈中还有 UI，通知新的栈顶 UI 获得焦点
        if (stack.Count > 0)
        {
            UIBase newTopUI = stack.Peek();
            if (newTopUI.IsOpen)
            {
                newTopUI.OnFocus(true);
            }
        }

        GameObject.DestroyImmediate(ub.gameObject);
    }
    
    /// <summary>
    /// 获取当前最上层的 UI
    /// </summary>
    /// <param name="uiType">UI 类型</param>
    /// <returns>栈顶 UI，如果栈为空则返回 null</returns>
    public UIBase GetTopUI(UIType uiType)
    {
        if (allUI[uiType].Count > 0)
        {
            return allUI[uiType].Peek();
        }
        return null;
    }
    
    /// <summary>
    /// 关闭当前最上层 UI
    /// </summary>
    /// <param name="uiType">UI 类型</param>
    public void CloseTopUI(UIType uiType)
    {
        if (allUI[uiType].Count > 0)
        {
            UIBase topUI = allUI[uiType].Pop();

            // 从缓存中移除
            RemoveFromCache(topUI);
            GameObject.DestroyImmediate(topUI.gameObject);

            // 通知新的栈顶 UI 获得焦点
            if (allUI[uiType].Count > 0)
            {
                UIBase newTopUI = allUI[uiType].Peek();
                if (newTopUI.IsOpen)
                {
                    newTopUI.OnFocus(true);
                }
            }
        }
    }

    private void RemoveFromCache(UIBase ub)
    {
        foreach (var kv in uiCache)
        {
            if (kv.Value == ub)
            {
                uiCache.Remove(kv.Key);
                return;
            }
        }
    }
}
