using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UIList
{
    public static Dictionary<string, UIData> UI = new Dictionary<string, UIData>()
    {
        ["StartGameView"] = new UIData("Assets/HotResorces/Base/Prefabs/StartGame/StartGameView.prefab", UIType.UI, false),
        ["MapView"] = new UIData("Assets/HotResorces/Base/Prefabs/Map/MapView.prefab", UIType.UI, false),
    };
}
