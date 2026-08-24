using UnityEngine;

public class AppCtrl : MonoBehaviour
{
    private static AppCtrl _Instance = null;
    public static AppCtrl Instance {
        get {
            if (_Instance != null)
            {
                return _Instance;
            }
            return null;
        }
    }

    // ????????????????? NotifyMgr?????
    public NotifyMgr NotifyMgr { get; private set; }
    public ResMgr ResMgr { get; private set; }
    public UIMgr UIMgr { get; private set; }
    public AudioMgr AudioMgr { get; private set; }

    public FuncMgr FuncMgr { get; private set; }

    public DataMgr DataMgr { get; private set; }
    
    public CacheMgr CacheMgr { get; private set; }
    private AppCtrl() //????????? ??????????????
    { 
    }
    private void Awake()
    {
        if (_Instance != null && _Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _Instance = this;
        DontDestroyOnLoad(gameObject);

        if (NotifyMgr == null)
        {
            NotifyMgr = new NotifyMgr();
            NotifyMgr.Init();
        }

        if (ResMgr == null)
        {
            ResMgr = new ResMgr();
            ResMgr.Init();
        }

        if (UIMgr == null)
        {
            UIMgr = new UIMgr();
        }

        /*  if (AudioMgr == null)
          {
              AudioMgr = new AudioMgr();
              AudioMgr.Init();
          }*/
        if (FuncMgr == null)
        {
            FuncMgr = new FuncMgr();
        }

        if (CacheMgr == null)
        {
            CacheMgr = new CacheMgr();
            CacheMgr.Init();
        }

        if (DataMgr == null)
        {
            DataMgr = new DataMgr();
            DataMgr.Init();
        }

       
    }

    private void Start()
    {
        UIMgr.Init();
        
    }

    private void Update()
    {
        NotifyMgr?.Update();
    }

    private void OnDestroy()
    {
        if (_Instance == this)
        {
            _Instance = null;
        }
    }
}
