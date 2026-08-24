using System.Collections.Generic;
using UnityEngine;

public class AudioMgr
{
    public static AudioMgr Instance { get; private set; }

    private AudioSource audioSource_Bg;
    private AudioSource audioSource_Effect;

    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private readonly List<string> cachedBgPaths = new List<string>() { "Audio/Bgs/Town" };

    public void Init()
    {
        Instance = this;

        GameObject audioRoot = GameObject.Find("Audio");
        if (audioRoot == null)
        {
            Debug.LogError("[AudioMgr] Can't find Audio node in scene!");
            return;
        }

        AudioSource[] sources = audioRoot.GetComponentsInChildren<AudioSource>();
        int count = sources.Length;
        for (int i = 0; i < count; i++)
        {
            var source = sources[i];
            string name = source.gameObject.name;
            if (name == "AudioSource_Bg")
                audioSource_Bg = source;
            else if (name == "AudioSource_Effect")
                audioSource_Effect = source;
        }

        if (audioSource_Bg == null)
            Debug.LogError("[AudioMgr] AudioSource_Bg not found!");
        if (audioSource_Effect == null)
            Debug.LogError("[AudioMgr] AudioSource_Effect not found!");

        PlayBg("Assets/HotRes/Audio/Bgs/Town.ogg");
    }

    private void PreloadBgAudio()
    {
        int count = cachedBgPaths.Count;
        for (int i = 0; i < count; i++)
        {
            string path = cachedBgPaths[i];
            if (!clipCache.ContainsKey(path))
            {
                AppCtrl.Instance.ResMgr.LoadAssetAsync<AudioClip>(path, null, (clip) =>
                {
                    if (clip != null)
                        clipCache[path] = clip;
                });
            }
        }
    }

    public void SwichAudioClip(int audioType, int audioId, string path)
    {
        if (audioSource_Effect == null)
        {
            Debug.LogError("[AudioMgr] AudioSource_Effect is null!");
            return;
        }

        AudioClip clip = null;
        if (clipCache.TryGetValue(path, out clip) && clip != null)
        {
            audioSource_Effect.PlayOneShot(clip);
            return;
        }

        AppCtrl.Instance.ResMgr.LoadAssetAsync<AudioClip>(path, null, (loadedClip) =>
        {
            if (loadedClip != null)
            {
                clipCache[path] = loadedClip;
                if (audioSource_Effect != null)
                    audioSource_Effect.PlayOneShot(loadedClip);
            }
            else
            {
                Debug.LogWarning($"[AudioMgr] Audio clip not found: {path}");
            }
        });
    }

    public void PlayBg(string path)
    {
        if (audioSource_Bg == null)
        {
            Debug.LogError("[AudioMgr] AudioSource_Bg is null!");
            return;
        }

        AudioClip clip = null;
        if (clipCache.TryGetValue(path, out clip) && clip != null)
        {
            audioSource_Bg.clip = clip;
            audioSource_Bg.loop = true;
            audioSource_Bg.Play();
            return;
        }

        AppCtrl.Instance.ResMgr.LoadAssetAsync<AudioClip>(path, null, (loadedClip) =>
        {
            if (loadedClip != null)
            {
                clipCache[path] = loadedClip;
                if (audioSource_Bg != null)
                {
                    audioSource_Bg.clip = loadedClip;
                    audioSource_Bg.loop = true;
                    audioSource_Bg.Play();
                }
            }
            else
            {
                Debug.LogWarning($"[AudioMgr] Bg clip not found: {path}");
            }
        });
    }

    public void StopBg()
    {
        if (audioSource_Bg != null)
            audioSource_Bg.Stop();
    }

    public void SetBgVolume(float volume)
    {
        if (audioSource_Bg != null)
            audioSource_Bg.volume = volume;
    }

    public void SetEffectVolume(float volume)
    {
        if (audioSource_Effect != null)
            audioSource_Effect.volume = volume;
    }

    public void ClearCache()
    {
        clipCache.Clear();
    }
}