using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DoorSoundController : MonoBehaviour
{
    [Header("Refs")]
    public DoorController doorController;
    public AudioSource audioSource;

    [Header("Single Door Clips")]
    public AudioClip singleDoorOpenClip;
    public AudioClip singleDoorCloseClip;

    [Header("Double Door Clips")]
    public AudioClip doubleDoorOpenClip;
    public AudioClip doubleDoorCloseClip;

    [Header("Locked Feedback")]
    public AudioClip lockedClip;
    public bool playLockedSoundOnEnter = true;
    public float lockedSoundCooldown = 1.5f; // 防止反复触发
    private float lastLockedPlayTime = -999f;

    [Header("Pitch Variation")]
    public bool randomizePitch = true;
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    [Header("Volume Variation")]
    public bool randomizeVolume = false;
    public float minVolume = 0.95f;
    public float maxVolume = 1f;

    [Header("Optional")]
    public float minPlayInterval = 0.05f; // 防止极短时间重复触发

    private float lastPlayTime = -999f;

    private void Awake()
    {
        if (doorController == null)
            doorController = GetComponent<DoorController>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    public void PlayLockedSound()
    {
        if (audioSource == null || lockedClip == null)
            return;

        if (Time.time - lastLockedPlayTime < lockedSoundCooldown)
            return;

        audioSource.pitch = 1f; // ❗固定，不随机
        audioSource.volume = 1f;

        audioSource.PlayOneShot(lockedClip);

        lastLockedPlayTime = Time.time;
    }

    public void PlayOpenSound()
    {
        if (doorController == null || audioSource == null)
            return;

        AudioClip clip = doorController.isDoubleDoor ? doubleDoorOpenClip : singleDoorOpenClip;
        PlayClip(clip);
    }

    public void PlayCloseSound()
    {
        if (doorController == null || audioSource == null)
            return;

        AudioClip clip = doorController.isDoubleDoor ? doubleDoorCloseClip : singleDoorCloseClip;
        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
            return;

        if (Time.time - lastPlayTime < minPlayInterval)
            return;

        audioSource.pitch = randomizePitch
            ? Random.Range(minPitch, maxPitch)
            : 1f;

        if (randomizeVolume)
            audioSource.volume = Random.Range(minVolume, maxVolume);
        else
            audioSource.volume = 1f;

        audioSource.PlayOneShot(clip);
        lastPlayTime = Time.time;
    }
}
