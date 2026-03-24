using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Ladder : MonoBehaviour
{
    [Header("Orientation")]
    [Tooltip("梯子外侧方向（玩家站的那一面）。通常用 LadderVolume 的 forward。")]
    public Transform ladderTransform; // 不填就用自己

    [Header("Snap")]
    public float snapSpeed = 14f;        // 吸附速度
    public float maxSnapDistance = 0.55f; // 离梯子平面多远还允许吸附

    [Header("Auto Enter")]
    public float enterAngle = 55f;       // 面向梯子的角度阈值（越小越严格）
    public float enterCooldown = 0.15f;  // 防止反复进出抖动
    [Header("Top Enter")]
    public bool enableTopAutoEnter = true;
    public float topEnterBand = 0.8f; // 顶部多少米范围内算“顶端进入区”

    void Reset()
    {
        var c = GetComponent<BoxCollider>();
        c.isTrigger = true;
        ladderTransform = transform;
    }
}
