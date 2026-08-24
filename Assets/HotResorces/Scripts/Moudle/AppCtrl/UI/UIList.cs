using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class UIList
{
    public static Dictionary<string, UIData> UI = new Dictionary<string, UIData>()
    {
        ["Start"] = new UIData("Assets/HotRes/UI/Start/Prefabs/Start.prefab", UIType.UI, false),
        ["StartPanel"] = new UIData("Assets/HotRes/Main/StartPanel/StartPanel.prefab", UIType.UI, false),
        ["SlotsView"] = new UIData("Assets/HotRes/Main/SlotsView/SlotsView.prefab", UIType.UI, false),
        ["MaitreyaBuddha_BasePanel"] = new UIData("Assets/HotRes/UI/SlotController/Prefabs/MaitreyaBuddha_BasePanel.prefab", UIType.UI, false),
        ["MaitreyaBuddha_FreePanel"] = new UIData("Assets/HotRes/UI/SlotController/Prefabs/MaitreyaBuddha_FreePanel.prefab", UIType.UI, false),
        ["HappyBuddhaFreeSpinStartView"] = new UIData("Assets/HotRes/UI/SlotController/Prefabs/HappyBuddhaFreeSpinStartView.prefab", UIType.UI, false),
        ["HappyBuddhaFreeSpinSettleView"] = new UIData("Assets/HotRes/UI/SlotController/Prefabs/HappyBuddhaFreeSpinSettleView.prefab", UIType.UI, false),
    };
}
