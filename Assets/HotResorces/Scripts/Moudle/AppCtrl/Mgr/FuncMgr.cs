using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuncMgr
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    /// 调用此接口方便后续扩展缩写规则
    public string GetNum(int n) {
        return n.ToString();    
    }
}
