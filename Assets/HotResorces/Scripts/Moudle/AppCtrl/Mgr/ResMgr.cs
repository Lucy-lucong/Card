using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ResourceFrame;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// 资源/场景管理器（先作为 ResMgr 的封装落地）。
/// 目前不接管 UIMgr.Close 的卸载时机：由外部在需要时调用 UnloadAssetAsync。
/// </summary>
public sealed class ResMgr
{
    // 仅允许在程序集内由 GameRoot 创建，外部统一走 GameRoot.Instance.ResMgr 调用。
    internal ResMgr() { }
    private ResourcePackage package;

    private Dictionary<string, ResAutoRefData> _autoRefData = new Dictionary<string, ResAutoRefData>();
    private readonly List<string> _removeHandle = new List<string>();

    public void Init() {
        package = YooAssets.GetPackage("DefaultPackage");
    }

    /// <summary>
    /// 同步加载资源（编辑器和运行时均支持）
    /// 编辑器: 使用 AssetDatabase 直接加载
    /// 运行时: 使用 YooAsset 同步加载
    /// </summary>
    public T LoadAsset<T>(string path, GameObject refObj) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        // 编辑器模式下直接同步加载资源
        T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            Debug.LogError($"资源加载错误LoadAsset:{path}");
            return null;
        }
        
        // 记录引用关系，方便后续管理
        if (!_autoRefData.TryGetValue(path, out ResAutoRefData resAutoRefData))
        {
            resAutoRefData = new ResAutoRefData(null); // 编辑器模式下handle为null
            _autoRefData.Add(path, resAutoRefData);
        }
        resAutoRefData.RefObj.Add(refObj);
        
        return asset;
#else
        // 运行时使用 YooAsset 同步加载
        AssetHandle assetHandle = package.LoadAssetSync<T>(path);
        if (assetHandle.AssetObject == null)
        {
            Debug.LogError($"资源加载错误LoadAsset:{path}");
            return null;
        }

        if (!_autoRefData.TryGetValue(path, out ResAutoRefData resAutoRefData))
        {
            resAutoRefData = new ResAutoRefData(assetHandle);
            _autoRefData.Add(path, resAutoRefData);
        }
        resAutoRefData.RefObj.Add(refObj);
        
        T asset = assetHandle.AssetObject as T;
        return asset;
#endif
    }

    public async void LoadAssetAsync<T>(string path, GameObject refObj, Action<T> call) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        // 编辑器模式下直接加载资源（不使用YooAsset）
        T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            Debug.LogError($"资源加载错误LoadAssetAsync:{path}");
            return;
        }
        
        // 编辑器模式下仍然记录引用关系，方便后续管理
        if (!_autoRefData.TryGetValue(path, out ResAutoRefData resAutoRefData))
        {
            resAutoRefData = new ResAutoRefData(null); // 编辑器模式下handle为null
            _autoRefData.Add(path, resAutoRefData);
        }
        resAutoRefData.RefObj.Add(refObj);
        
        // 立即回调（编辑器模式下同步加载）
        call?.Invoke(asset);
#else
        // 运行时使用YooAsset加载
        AssetHandle assetHandle = package.LoadAssetAsync<T>(path);
        await assetHandle.Task;
        if (assetHandle.AssetObject == null)
        {
            Debug.LogError($"资源加载错误LoadAssetAsync:{path}");
            return;
        }

        if (!_autoRefData.TryGetValue(path, out ResAutoRefData resAutoRefData))
        {
            resAutoRefData = new ResAutoRefData(assetHandle);
            _autoRefData.Add(path, resAutoRefData);
        }
        resAutoRefData.RefObj.Add(refObj);
        T asset = assetHandle.AssetObject as T;
        call?.Invoke(asset);
#endif
    }

    //释放引用计数为0的资源
    public async UniTask UnloadAsset()
    {
        //因为GameObject销毁是在本帧的最后阶段才会消耗，所以要等待帧结束
        await UniTask.WaitForEndOfFrame();
        _removeHandle.Clear();
        foreach (string path in _autoRefData.Keys)
        {
            ResAutoRefData resAutoRef = _autoRefData[path];
            List<GameObject> refObjs = resAutoRef.RefObj;
            int refIndex = 0;
            foreach (GameObject refObj in refObjs)
            {
                if (refObj != null)
                {
                    refIndex++;
                    break;
                }
            }
            if (refIndex == 0)
            {
#if !UNITY_EDITOR
                // 运行时才释放YooAsset资源
                resAutoRef.Handle?.Release();
#endif
                _removeHandle.Add(path);
            }
        }

        //移除资源的加载
        if (_removeHandle.Count != 0)
        {
            foreach (string path in _removeHandle)
            {
                _autoRefData.Remove(path);
            }
        }
#if !UNITY_EDITOR
        UnloadUnusedAssetsOperation unloadUnused = package.UnloadUnusedAssetsAsync();
        await unloadUnused.Task;
#endif
    }

    public async UniTaskVoid LoadSceneAsync(string path, Action<SceneHandle> onCompleted = null, LoadSceneMode mode = LoadSceneMode.Single, LocalPhysicsMode p_mode = LocalPhysicsMode.None, bool suspendLoad = false)
    {
#if UNITY_EDITOR
        // 编辑器模式下直接加载场景
        EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(mode, p_mode));
        Debug.Log($"场景加载成功: {path}");
        // 编辑器模式下onCompleted回调参数为null（因为没有SceneHandle）
        onCompleted?.Invoke(null);
        await UniTask.Yield();
#else
        // 运行时使用YooAsset加载场景
        // 调用YooAsset的场景加载接口
        SceneHandle sceneHandle = package.LoadSceneAsync(path, mode, p_mode, suspendLoad);

        // 等待加载完成（注意：加载完成不代表切换激活，suspendLoad为true时需手动激活）
        await sceneHandle.Task;

        if (sceneHandle.Status != EOperationStatus.Succeed)
        {
            Debug.LogError($"场景加载失败: {path}");
        }

        // 您可以在这里将sceneHandle存入一个专门的字典进行管理，类似_autoRefData
        // 例如：_sceneHandles[path] = sceneHandle;

        Debug.Log($"场景加载成功: {path}");
        onCompleted?.Invoke(sceneHandle);
#endif
    }
}

