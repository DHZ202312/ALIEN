using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MaterialOffsetScroller : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private int materialIndex = 0;

    [Header("Texture Property Name")]
    [Tooltip("常见有 _BaseMap, _MainTex, _DetailAlbedoMap 等。要和你的Shader属性名一致。")]
    [SerializeField] private string textureProperty = "_BaseMap";

    [Header("Scroll Speed")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.1f, 0f);

    [Header("Use Unscaled Time")]
    [SerializeField] private bool useUnscaledTime = false;

    private Material runtimeMaterial;
    private Vector2 currentOffset;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError("MaterialOffsetScroller: 没找到 Renderer。", this);
            enabled = false;
            return;
        }

        var mats = targetRenderer.materials;
        if (materialIndex < 0 || materialIndex >= mats.Length)
        {
            Debug.LogError($"MaterialOffsetScroller: materialIndex 越界，当前材质数 = {mats.Length}", this);
            enabled = false;
            return;
        }

        // 用 materials / material 会实例化一份运行时材质，避免直接改到项目里的原材质
        runtimeMaterial = mats[materialIndex];

        if (runtimeMaterial == null)
        {
            Debug.LogError("MaterialOffsetScroller: 目标材质为空。", this);
            enabled = false;
            return;
        }

        if (!runtimeMaterial.HasProperty(textureProperty))
        {
            Debug.LogError($"MaterialOffsetScroller: 材质没有属性 {textureProperty}", this);
            enabled = false;
            return;
        }

        currentOffset = runtimeMaterial.GetTextureOffset(textureProperty);
    }

    private void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        currentOffset += scrollSpeed * dt;

        // 保持数值不要无限增大
        currentOffset.x = Mathf.Repeat(currentOffset.x, 1f);
        currentOffset.y = Mathf.Repeat(currentOffset.y, 1f);

        runtimeMaterial.SetTextureOffset(textureProperty, currentOffset);
    }
}