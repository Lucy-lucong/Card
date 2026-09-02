using UnityEngine;
using UnityEngine.UI;

public sealed class StartGameView : UIBase
{
    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button quitButton;

    protected override void Awake()
    {
        base.Awake();

        startGameButton?.onClick.AddListener(OnStartGameClicked);
        loadGameButton?.onClick.AddListener(OnLoadGameClicked);
        settingButton?.onClick.AddListener(OnSettingClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);
    }

    private void OnDestroy()
    {
        startGameButton?.onClick.RemoveListener(OnStartGameClicked);
        loadGameButton?.onClick.RemoveListener(OnLoadGameClicked);
        settingButton?.onClick.RemoveListener(OnSettingClicked);
        quitButton?.onClick.RemoveListener(OnQuitClicked);
    }

    private void OnStartGameClicked()
    {
        Debug.Log("[StartGameView] Start Game clicked.");
        AppCtrl.Instance.UIMgr.OpenUI<MapView>(UIList.UI["MapView"],null);
        Cancel();
    }

    private void OnLoadGameClicked()
    {
        Debug.Log("[StartGameView] Load Game clicked.");
    }

    private void OnSettingClicked()
    {
        Debug.Log("[StartGameView] Setting clicked.");
    }

    private void OnQuitClicked()
    {
        Debug.Log("[StartGameView] Quit clicked.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
