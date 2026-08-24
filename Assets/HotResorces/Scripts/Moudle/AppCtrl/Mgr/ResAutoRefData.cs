using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace ResourceFrame
{
    public struct ResAutoRefData
    {
        public AssetHandle Handle { get; private set; }
        public List<GameObject> RefObj { get; private set; }
        public ResAutoRefData(AssetHandle assetHandle)
        {
            this.Handle = assetHandle;
            RefObj = new List<GameObject>();
        }
    }
}