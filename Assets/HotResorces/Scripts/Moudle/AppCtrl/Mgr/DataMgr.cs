using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataMgr
{
    public GameState gameState;



    public void Init() {
        bool hasGameState = AppCtrl.Instance.CacheMgr.HasKey("gameState");
        if (hasGameState)
        {
            gameState = AppCtrl.Instance.CacheMgr.GetData<GameState>("gameState");
        }
        else {
            gameState.tp = 1;
            gameState.gold = Random.Range(10000,200000);
            gameState.freeTimes = 0;
        }
    }

    /// <summary>
    /// 设置免费次数并持久化
    /// </summary>
    public void SetFreeTimes(int times) {
        gameState.freeTimes = times;
        SaveGameState();
    }

    /// <summary>
    /// 增加免费次数并持久化
    /// </summary>
    public void AddFreeTimes(int times) {
        gameState.freeTimes += times;
        if (gameState.freeTimes <= 0)
        {
            ChangeTp(1);
        }
        else { 
            SaveGameState();
        }
    }

    public void GetGold(int num) {
        gameState.gold += num; 
        SaveGameState();
    }

    public void ChangeTp(int tp) {
        gameState.tp = tp;
        SaveGameState();
    }

    public void SaveGameState() { 
        AppCtrl.Instance.CacheMgr.SetData("gameState",gameState);
    }
}
