using UnityEngine;

/// <summary>
/// 所有 UI 的基类：统一生命周期，默认走「创建一次，反复开关」以减少实例化开销。
/// </summary>
public abstract class UIBase : MonoBehaviour
{
    public bool IsCreated { get; private set; }
    public bool IsOpen { get; private set; }

    public UIType uitp;

    /// <summary>
    /// UIMgr 在 Instantiate 之前写入，Awake 中读取后清空，确保首次创建时 Awake 能拿到数据。
    /// </summary>
    internal static object[] _pendingSentData;

    /// <summary>
    /// 本次打开时透传的数据，在 Awake（首次创建）或 OnOpen（缓存复用）中写入。
    /// </summary>
    public object[] SentData { get; private set; }

    protected virtual void Awake()
    {
        // 首次 Instantiate 时，Awake 同步触发，此时静态槽里存着本次的数据
        SentData = _pendingSentData;
        _pendingSentData = null;        // 取走后立即清空，避免污染下一次
    }

    /// <summary>
    /// 缓存复用时（UI 已存在，再次 Open）由 UIMgr 调用，用于刷新数据。
    /// </summary>
    public virtual void OnOpen(object[] data)
    {
        SentData = data;
    }

    /// <summary>
    /// 焦点切换回调：当 UI 获得或失去焦点时调用
    /// </summary>
    /// <param name="hasFocus">true=获得焦点，false=失去焦点</param>
    public virtual void OnFocus(bool hasFocus) { }
}
