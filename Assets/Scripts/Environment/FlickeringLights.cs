using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickeringLights : MonoBehaviour
{
    [Header("Base Intensity")]
    public float baseIntensity = 2.0f;

    [Header("Subtle Flicker")]
    public float subtleFlickerStrength = 0.15f;
    public float subtleFlickerSpeed = 8f;

    [Header("Random Flicker Events")]
    public float flickerChancePerSecond = 0.6f;
    public float flickerDurationMin = 0.05f;
    public float flickerDurationMax = 0.25f;

    [Header("Heavy Glitch Burst")]
    public float burstChancePerSecond = 0.15f;
    public float burstDurationMin = 0.3f;
    public float burstDurationMax = 0.8f;
    public float burstIntensityMultiplier = 0.4f;

    [Header("Permanent Failure (Optional)")]
    public bool canBurnOut = false;
    public float burnOutChancePerSecond = 0.02f;

    Light lightComp;

    bool flickering;
    bool bursting;
    bool burnedOut;

    float eventEndTime;
    float nextCheckTime;

    void Awake()
    {
        lightComp = GetComponent<Light>();
        lightComp.intensity = baseIntensity;
    }

    void Update()
    {
        if (burnedOut) return;

        // 基础轻微电压抖动（持续存在）
        float subtle = Mathf.PerlinNoise(Time.time * subtleFlickerSpeed, 0f);
        subtle = (subtle - 0.5f) * subtleFlickerStrength;

        float targetIntensity = baseIntensity + subtle;

        // 随机事件检测（不是每帧）
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + 0.1f;

            TryStartEvents();
        }

        // 处理闪烁
        if (flickering)
        {
            if (Time.time >= eventEndTime)
                flickering = false;
            else
                targetIntensity = 0f;
        }

        // 处理严重抖动
        if (bursting)
        {
            if (Time.time >= eventEndTime)
                bursting = false;
            else
                targetIntensity *= Random.Range(burstIntensityMultiplier, 1f);
        }

        lightComp.intensity = targetIntensity;
    }

    void TryStartEvents()
    {
        if (flickering || bursting) return;

        // 普通闪烁
        if (Random.value < flickerChancePerSecond * 0.1f)
        {
            flickering = true;
            eventEndTime = Time.time + Random.Range(flickerDurationMin, flickerDurationMax);
            return;
        }

        // 严重抖动
        if (Random.value < burstChancePerSecond * 0.1f)
        {
            bursting = true;
            eventEndTime = Time.time + Random.Range(burstDurationMin, burstDurationMax);
            return;
        }

        // 永久损坏
        if (canBurnOut && Random.value < burnOutChancePerSecond * 0.1f)
        {
            burnedOut = true;
            lightComp.enabled = false;
        }
    }
}
