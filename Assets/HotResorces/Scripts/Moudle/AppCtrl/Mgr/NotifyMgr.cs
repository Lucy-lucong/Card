using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

// 事件监听/派发管理器（参照 Assets/Editor/AIScripts/GameRoot/System/NotifyMgr文档.txt）
// 注意：文档里的 Lua 版本在 RmoveListenByObj 的 reverseIndex 指向上可能存在表引用问题；
// 这里实现为“reverseIndex 指向同一个 ListenerInfo 实例”，以保证 removed=true 能生效。
public class NotifyMgr
{
    // 支持标记前缀：[regex]xxx
    private const string RegexPrefix = "[regex]";

    public bool bCheck = false; // 性能检查
    public int count = 0; // 监听个数
    public int maxCount = 0;

    // 普通事件名 -> (宿主对象 -> 监听信息)
    private readonly Dictionary<string, Dictionary<object, ListenerInfo>> _listenList =
        new Dictionary<string, Dictionary<object, ListenerInfo>>();

    // 正则事件名(含前缀key) -> (宿主对象 -> 监听信息)
    private readonly Dictionary<string, Dictionary<object, ListenerInfo>> _listenListReg =
        new Dictionary<string, Dictionary<object, ListenerInfo>>();

    // 反向索引：宿主对象 -> (eventName -> 监听信息)
    private readonly Dictionary<object, Dictionary<string, ListenerInfo>> _listenListReverseIndex =
        new Dictionary<object, Dictionary<string, ListenerInfo>>();

    private class ListenerInfo
    {
        public Action<string, object[]> func;
        public bool removed;
    }

    private class PendingCall
    {
        public Action<string, object[]> func;
        public string eventName;
        public object obj;
        public object[] args;
    }

    // 用于 SendEventByUpdate 的延迟派发队列
    private readonly Dictionary<string, PendingCall> _updateCallMap = new Dictionary<string, PendingCall>();
    private readonly List<string> _updateCallKeys = new List<string>();

    private readonly Dictionary<Type, MethodInfo> _isDestroyedMethodCache = new Dictionary<Type, MethodInfo>();

    public void Init()
    {
        // 文档中为空：这里保留接口以便对接
    }

    public void Reset()
    {
        RmoveAll();
    }

    public void Listen(object obj, string[] eventNames, Action<string, object[]> callback)
    {
        if (obj == null)
        {
            Debug.LogError("需要绑定一个非空对象!");
            return;
        }
        if (eventNames == null || eventNames.Length <= 0)
        {
            Debug.LogError("请传入一个table数组!");
            return;
        }
        if (callback == null)
        {
            Debug.LogError("callback 不能为 null");
            return;
        }

        for (int i = 0; i < eventNames.Length; i++)
        {
            RegisterListening(obj, eventNames[i], callback);
        }
    }

    public void UnListen(object obj, string[] eventNames)
    {
        if (eventNames == null) return;
        for (int i = 0; i < eventNames.Length; i++)
        {
            RmoveListen(obj, eventNames[i]);
        }
    }

    // 监听
    public void RegisterListening(object obj, string eventName, Action<string, object[]> callback)
    {
        if (obj == null)
        {
            Debug.LogError("需要绑定一个非空对象!");
            return;
        }
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("eventName 不能为空");
            return;
        }
        if (callback == null)
        {
            Debug.LogError("callback 不能为空");
            return;
        }

        Dictionary<string, Dictionary<object, ListenerInfo>> list =
            IsRexStr(eventName) ? _listenListReg : _listenList;

        if (!list.TryGetValue(eventName, out Dictionary<object, ListenerInfo> hostDict) || hostDict == null)
        {
            hostDict = new Dictionary<object, ListenerInfo>();
            list[eventName] = hostDict;
        }

        ListenerInfo info = new ListenerInfo();
        info.func = callback;
        info.removed = false;
        hostDict[obj] = info;

        if (!_listenListReverseIndex.TryGetValue(obj, out Dictionary<string, ListenerInfo> reverseForHost) || reverseForHost == null)
        {
            reverseForHost = new Dictionary<string, ListenerInfo>();
            _listenListReverseIndex[obj] = reverseForHost;
        }

        if (bCheck)
        {
            if (!reverseForHost.ContainsKey(eventName))
            {
                count++;
                if (maxCount < count)
                {
                    maxCount = count;
                    print("NotifyMgr.maxCount +:" + maxCount);
                }
            }
        }

        // 关键：让 reverseIndex 指向同一份 ListenerInfo，removed 才能生效
        reverseForHost[eventName] = info;
    }

    // 移除监听（文档里的拼写：RmoveListen）
    public void RmoveListen(object obj, string eventName)
    {
        if (obj == null || string.IsNullOrEmpty(eventName)) return;

        Dictionary<string, Dictionary<object, ListenerInfo>> list =
            IsRexStr(eventName) ? _listenListReg : _listenList;

        bool existedForCheck = false;
        if (_listenListReverseIndex.TryGetValue(obj, out Dictionary<string, ListenerInfo> reverseForHostCheck) &&
            reverseForHostCheck != null)
        {
            existedForCheck = reverseForHostCheck.ContainsKey(eventName);
        }

        if (list.TryGetValue(eventName, out Dictionary<object, ListenerInfo> hostDict) && hostDict != null)
        {
            if (hostDict.ContainsKey(obj))
            {
                hostDict.Remove(obj);
            }
        }

        if (_listenListReverseIndex.TryGetValue(obj, out Dictionary<string, ListenerInfo> reverseForHost) &&
            reverseForHost != null)
        {
            if (reverseForHost.ContainsKey(eventName))
            {
                reverseForHost.Remove(eventName);
            }

            if (reverseForHost.Count == 0)
            {
                _listenListReverseIndex.Remove(obj);
            }
        }

        if (bCheck && existedForCheck)
        {
            count--;
        }
    }

    public void RmoveListenByObj(object obj)
    {
        if (obj == null) return;
        if (!_listenListReverseIndex.TryGetValue(obj, out Dictionary<string, ListenerInfo> reverseForHost) || reverseForHost == null)
        {
            return;
        }

        foreach (KeyValuePair<string, ListenerInfo> kv in reverseForHost)
        {
            if (kv.Value != null)
            {
                kv.Value.removed = true;
            }
        }

        if (bCheck)
        {
            foreach (KeyValuePair<string, ListenerInfo> kv in reverseForHost)
            {
                RmoveListen(obj, kv.Key);
            }
        }

        _listenListReverseIndex.Remove(obj);
    }

    public void RmoveAll()
    {
        _listenList.Clear();
        _listenListReg.Clear();
        _updateCallMap.Clear();
        _updateCallKeys.Clear();
        _listenListReverseIndex.Clear();

        if (bCheck)
        {
            count = 0;
            maxCount = 0;
        }
    }

    public void GC()
    {
        // 遍历所有宿主对象的反向索引，清理已销毁对象
        // 注意：遍历期间可能删除键，这里用复制列表
        List<object> hosts = new List<object>(_listenListReverseIndex.Keys);
        for (int i = 0; i < hosts.Count; i++)
        {
            object host = hosts[i];
            if (IsHostDestroyed(host))
            {
                RmoveListenByObj(host);
            }
        }
    }

    public void SendEvent(string eventName, params object[] args)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("NotifyMgr.SendEvent: eventName 不能为空");
            return;
        }

        Dictionary<object, ListenerInfo> callbacks = null;

        // 正则匹配：找到第一个匹配的正则表并使用（文档里没有 break，这里为了避免不确定性改为 break）
        foreach (KeyValuePair<string, Dictionary<object, ListenerInfo>> kv in _listenListReg)
        {
            string regexKey = kv.Key;
            string pattern = ExtractRegexPattern(regexKey);
            if (!string.IsNullOrEmpty(pattern) && RegexIsMatchSafe(eventName, pattern))
            {
                callbacks = kv.Value;
                break;
            }
        }

        // 普通事件名优先覆盖（文档行为：如果 ListenList[eventName] 存在，则直接覆盖 callbacks）
        if (_listenList.TryGetValue(eventName, out Dictionary<object, ListenerInfo> normalCallbacks) && normalCallbacks != null)
        {
            callbacks = normalCallbacks;
        }

        if (callbacks == null || callbacks.Count == 0)
        {
            throw new Exception("NotifyMgr.SendEvent error: no listeners, eventName=" + eventName);
        }

        // 避免遍历期间修改字典
        List<object> hosts = new List<object>(callbacks.Keys);
        for (int i = 0; i < hosts.Count; i++)
        {
            object host = hosts[i];
            if (!callbacks.TryGetValue(host, out ListenerInfo callInfo) || callInfo == null) continue;

            if (callInfo.removed || IsHostDestroyed(host))
            {
                RmoveListen(host, eventName);
            }
            else
            {
                callInfo.func?.Invoke(eventName, args);
            }
        }
    }

    public void SendEventByUpdate(string eventName, params object[] args)
    {
        if (string.IsNullOrEmpty(eventName))
        {
            Debug.LogError("NotifyMgr.SendEventByUpdate: eventName 不能为空");
            return;
        }

        Dictionary<object, ListenerInfo> callbacks = null;

        foreach (KeyValuePair<string, Dictionary<object, ListenerInfo>> kv in _listenListReg)
        {
            string regexKey = kv.Key;
            string pattern = ExtractRegexPattern(regexKey);
            if (!string.IsNullOrEmpty(pattern) && RegexIsMatchSafe(eventName, pattern))
            {
                callbacks = kv.Value;
                break;
            }
        }

        if (_listenList.TryGetValue(eventName, out Dictionary<object, ListenerInfo> normalCallbacks) && normalCallbacks != null)
        {
            callbacks = normalCallbacks;
        }

        if (callbacks == null || callbacks.Count == 0)
        {
            throw new Exception("NotifyMgr.SendEventByUpdate error: no listeners, eventName=" + eventName);
        }

        List<object> hosts = new List<object>(callbacks.Keys);
        for (int i = 0; i < hosts.Count; i++)
        {
            object host = hosts[i];
            if (!callbacks.TryGetValue(host, out ListenerInfo callInfo) || callInfo == null) continue;

            if (callInfo.removed)
            {
                RmoveListen(host, eventName);
            }
            else
            {
                string updateKey = BuildPendingKey(host, callInfo.func, eventName);
                if (!_updateCallMap.ContainsKey(updateKey))
                {
                    _updateCallKeys.Add(updateKey);
                }
                _updateCallMap[updateKey] = new PendingCall()
                {
                    func = callInfo.func,
                    eventName = eventName,
                    obj = host,
                    args = args
                };
            }
        }
    }

    public void Update()
    {
        float tBegin = Time.realtimeSinceStartup;

        // 以列表推进，支持时间切片 break
        for (int i = 0; i < _updateCallKeys.Count;)
        {
            string key = _updateCallKeys[i];
            if (!_updateCallMap.TryGetValue(key, out PendingCall call) || call == null)
            {
                _updateCallKeys.RemoveAt(i);
                continue;
            }

            if (IsHostDestroyed(call.obj))
            {
                RmoveListen(call.obj, call.eventName);
            }
            else
            {
                call.func?.Invoke(call.eventName, call.args);
            }

            _updateCallMap.Remove(key);
            _updateCallKeys.RemoveAt(i);

            if (Time.realtimeSinceStartup - tBegin > 0.010f)
            {
                break;
            }
        }
    }

    private bool IsRexStr(string str)
    {
        return !string.IsNullOrEmpty(str) && str.StartsWith(RegexPrefix, StringComparison.Ordinal);
    }

    private string ExtractRegexPattern(string eventName)
    {
        if (!IsRexStr(eventName)) return null;
        return eventName.Substring(RegexPrefix.Length);
    }

    private bool RegexIsMatchSafe(string input, string pattern)
    {
        try
        {
            // 直接使用 .NET Regex 解释 pattern；pattern 的语法由你传入的字符串决定
            return Regex.IsMatch(input, pattern);
        }
        catch (Exception e)
        {
            Debug.LogError("NotifyMgr regex parse/match error, pattern=" + pattern + ", err=" + e.Message);
            return false;
        }
    }

    private bool IsHostDestroyed(object host)
    {
        if (host == null) return true;

        // UnityEngine.Object：Unity 的“已销毁”会让 == null 成立
        if (host is UnityEngine.Object uo)
        {
            return uo == null;
        }

        Type t = host.GetType();
        if (_isDestroyedMethodCache.TryGetValue(t, out MethodInfo cached))
        {
            return cached != null && InvokeIsDestroyed(host, cached);
        }

        MethodInfo mi = t.GetMethod("IsDestroyed",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null);

        if (mi != null && mi.ReturnType == typeof(bool))
        {
            _isDestroyedMethodCache[t] = mi;
            return InvokeIsDestroyed(host, mi);
        }

        _isDestroyedMethodCache[t] = null;
        return false;
    }

    private bool InvokeIsDestroyed(object host, MethodInfo mi)
    {
        try
        {
            object ret = mi.Invoke(host, null);
            if (ret is bool b) return b;
        }
        catch
        {
            // 忽略反射错误：认为未销毁
        }
        return false;
    }

    private string BuildPendingKey(object host, Action<string, object[]> func, string eventName)
    {
        // 为了避免“同一 callback 不同宿主被覆盖”，这里把 host 也纳入 key
        int hostId;
        if (host is UnityEngine.Object uo)
        {
            hostId = uo.GetInstanceID();
        }
        else
        {
            hostId = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(host);
        }

        string funcKey;
        if (func == null)
        {
            funcKey = "null";
        }
        else
        {
            // Method + Target 区分委托来源
            funcKey = func.Method.DeclaringType.FullName + "." + func.Method.Name + ":" + (func.Target != null ? func.Target.GetHashCode().ToString() : "null");
        }

        return hostId + "|" + funcKey + "|" + eventName;
    }

    // Debug 日志缩写（保持和其它脚本一致的风格）
    private void print(object msg)
    {
        Debug.Log(msg);
    }
}

