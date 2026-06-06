using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;
    [Range(0f, 1f)] public float bgmVolume = 0.3f;
    [Range(0f, 1f)] public float seVolume = 0.5f;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (GetComponent<AudioListener>() != null)
            Destroy(GetComponent<AudioListener>());

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        seSource = gameObject.AddComponent<AudioSource>();
        seSource.loop = false;
        seSource.playOnAwake = false;
        seSource.volume = seVolume;
    }

    public void PlayBGM(string clipName)
    {
        var clip = Resources.Load<AudioClip>($"Audio/BGM/{clipName}");
        if (clip == null) { Debug.LogWarning($"[Audio] BGM clip not found: {clipName}"); return; }
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        bgmSource.clip = clip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void PlayRandomBGM(params string[] clipNames)
    {
        if (clipNames == null || clipNames.Length == 0) return;
        var selected = clipNames[Random.Range(0, clipNames.Length)];
        PlayBGM(selected);
    }

    public void StopBGM()
    {
        if (bgmSource.isPlaying) bgmSource.Stop();
    }

    public void PlaySE(string clipName)
    {
        var clip = Resources.Load<AudioClip>($"Audio/SE/{clipName}");
        if (clip == null) { Debug.LogWarning($"[Audio] SE clip not found: {clipName}"); return; }
        seSource.volume = seVolume;
        seSource.PlayOneShot(clip);
    }

    public void SetBGMVolume(float vol)
    {
        bgmVolume = Mathf.Clamp01(vol);
        bgmSource.volume = bgmVolume;
    }

    public void SetSEVolume(float vol)
    {
        seVolume = Mathf.Clamp01(vol);
    }
}
