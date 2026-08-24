using UnityEngine;
using UnityEngine.UI;

public class AudioItem : MonoBehaviour
{
    public int AudioId = 1;

    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => OnButtonClickPersistent());
        }

        Toggle toggle = GetComponent<Toggle>();
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener((value) => OnToggleValueChangedPersistent(value));
        }
    }

    private void OnButtonClickPersistent()
    {
        PlayAudio();
    }

    private void OnToggleValueChangedPersistent(bool value)
    {
        if (value)
        {
            PlayAudio();
        }
    }

    public void PlayAudio()
    {
        AudioMgr.Instance.SwichAudioClip(2, 1, "Assets/HotRes/Audio/UI/" + AudioId.ToString() + ".mp3");
    }
}