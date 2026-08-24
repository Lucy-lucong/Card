using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIData 
{
    public string path;
    public UIType uitp;
    public bool hide_last_ui;//是否自动隐藏上一个ui 同一层级

    public UIData(string path,UIType uiType = UIType.UI, bool hide = false)
    {
        this.path = path;
        hide_last_ui = hide;
        uitp = uiType;

    }
}
