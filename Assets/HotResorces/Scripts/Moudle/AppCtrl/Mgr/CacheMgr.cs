using UnityEngine;
using System;

public class CacheMgr
{
    public void Init()
    {
        // 初始化可以在这里进行
        Debug.Log("[CacheMgr] Init.");
    }

    /// <summary>
    /// 保存基础数据类型 (Int)
    /// </summary>
    public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

    /// <summary>
    /// 获取基础数据类型 (Int)
    /// </summary>
    public int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);

    /// <summary>
    /// 保存基础数据类型 (Float)
    /// </summary>
    public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);

    /// <summary>
    /// 获取基础数据类型 (Float)
    /// </summary>
    public float GetFloat(string key, float defaultValue = 0f) => PlayerPrefs.GetFloat(key, defaultValue);

    /// <summary>
    /// 保存基础数据类型 (String)
    /// </summary>
    public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);

    /// <summary>
    /// 获取基础数据类型 (String)
    /// </summary>
    public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);

    /// <summary>
    /// 保存复杂对象数据 (通过JSON序列化)
    /// 注意：T必须支持序列化 (添加[System.Serializable]标签)
    /// </summary>
    public void SetData<T>(string key, T data)
    {
        if (data == null) return;
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 获取复杂对象数据 (通过JSON反序列化)
    /// 如果不存在或解析失败，将返回 new T()
    /// </summary>
    public T GetData<T>(string key) where T : new()
    {
        string json = PlayerPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(json))
        {
            return new T();
        }
        try
        {
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CacheMgr] 数据解析失败 Key: {key}, Error: {e.Message}");
            return new T();
        }
    }

    /// <summary>
    /// 检查是否存在某个键
    /// </summary>
    public bool HasKey(string key) => PlayerPrefs.HasKey(key);

    /// <summary>
    /// 删除指定键的数据
    /// </summary>
    public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);

    /// <summary>
    /// 清除所有本地缓存数据 (危险操作，慎用)
    /// </summary>
    public void DeleteAll() => PlayerPrefs.DeleteAll();

    /// <summary>
    /// 手动将数据立即写入磁盘（某些特定情况防止闪退丢失数据）
    /// </summary>
    public void Save() => PlayerPrefs.Save();
}
