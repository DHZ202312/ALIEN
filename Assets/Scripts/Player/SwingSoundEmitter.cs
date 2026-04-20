using UnityEngine;

public class SwingSoundEmitter : MonoBehaviour
{
    public enum VariationMode
    {
        SingleClipWithPitch,
        MultiClipNoRepeat
    }

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] swingClips;

    [Header("Variation")]
    public VariationMode variationMode = VariationMode.SingleClipWithPitch;

    [Header("Pitch Variation")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    [Header("Volume Variation")]
    public bool randomizeVolume = true;
    public float minVolume = 0.9f;
    public float maxVolume = 1f;

    [Header("Cooldown")]
    public float minInterval = 0.1f; // 防止一帧多次触发

    private int lastClipIndex = -1;
    private float lastPlayTime;

    private void Awake()
    {
        // 可选自动获取
        if (audioSource == null)
            TryGetComponent(out audioSource);
    }

    // 👉 给外部调用（攻击时调用这个）
    public void PlaySwing()
    {
        if (audioSource == null || swingClips == null || swingClips.Length == 0)
            return;

        if (Time.time - lastPlayTime < minInterval)
            return;

        AudioClip clip = null;

        switch (variationMode)
        {
            case VariationMode.SingleClipWithPitch:
                clip = GetSingleClipWithPitch();
                break;

            case VariationMode.MultiClipNoRepeat:
                clip = GetMultiClipNoRepeat();
                break;
        }

        if (clip == null)
            return;

        float volume = randomizeVolume
            ? Random.Range(minVolume, maxVolume)
            : 1f;

        audioSource.volume = volume;
        audioSource.PlayOneShot(clip);

        lastPlayTime = Time.time;
    }

    private AudioClip GetSingleClipWithPitch()
    {
        if (swingClips[0] == null)
            return null;

        audioSource.pitch = Random.Range(minPitch, maxPitch);
        return swingClips[0];
    }

    private AudioClip GetMultiClipNoRepeat()
    {
        audioSource.pitch = 1f;

        if (swingClips.Length == 1)
        {
            lastClipIndex = 0;
            return swingClips[0];
        }

        int index;
        do
        {
            index = Random.Range(0, swingClips.Length);
        }
        while (index == lastClipIndex);

        lastClipIndex = index;
        return swingClips[index];
    }
}
