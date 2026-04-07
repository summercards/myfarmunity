// Assets/Scripts/Portal/PortalVisualGenerator.cs
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 传送门视觉效果生成器
/// 在传送门对象上自动创建视觉效果
/// </summary>
[ExecuteInEditMode]
public class PortalVisualGenerator : MonoBehaviour
{
    [Header("视觉效果配置")]
    [Tooltip("传送门颜色")]
    public Color portalColor = new Color(0f, 0.8f, 1f, 0.6f);

    [Tooltip("传送门大小")]
    public float portalSize = 2f;

    [Tooltip("动画速度")]
    public float animationSpeed = 1f;

    [Tooltip("是否旋转")]
    public bool rotateEffect = true;

    [Tooltip("是否有粒子效果")]
    public bool showParticles = true;

    // 内部引用
    private GameObject portalRing;
    private GameObject portalCore;
    private ParticleSystem particles;

    private void Awake()
    {
        GenerateVisuals();
    }

    private void Reset()
    {
        GenerateVisuals();
    }

    /// <summary>
    /// 生成传送门视觉效果
    /// </summary>
    [ContextMenu("生成/更新视觉效果")]
    public void GenerateVisuals()
    {
        // 清理旧的视觉效果
        CleanupVisuals();

        // 创建传送门外环
        portalRing = CreatePortalRing();

        // 创建传送门核心
        portalCore = CreatePortalCore();

        // 创建粒子效果
        if (showParticles)
        {
            particles = CreateParticles();
        }
    }

    /// <summary>
    /// 创建传送门外环
    /// </summary>
    private GameObject CreatePortalRing()
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "PortalRing";
        ring.transform.parent = transform;
        ring.transform.localPosition = Vector3.zero;
        ring.transform.localScale = new Vector3(portalSize * 1.2f, 0.1f, portalSize * 1.2f);

        // 设置材质
        Material ringMat = new Material(Shader.Find("Standard"));
        ringMat.color = new Color(portalColor.r, portalColor.g, portalColor.b, 0.8f);
        ringMat.SetFloat("_Metallic", 0.8f);
        ringMat.SetFloat("_Glossiness", 0.9f);
        ringMat.SetFloat("_Mode", 3f);
        ringMat.SetOverrideTag("RenderType", "Transparent");

        ring.GetComponent<Renderer>().material = ringMat;

        // 删除碰撞体
        DestroyImmediate(ring.GetComponent<Collider>());

        return ring;
    }

    /// <summary>
    /// 创建传送门核心
    /// </summary>
    private GameObject CreatePortalCore()
    {
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "PortalCore";
        core.transform.parent = transform;
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = Vector3.one * portalSize * 0.8f;

        // 设置材质
        Material coreMat = new Material(Shader.Find("Standard"));
        coreMat.color = portalColor;
        coreMat.SetFloat("_Metallic", 0f);
        coreMat.SetFloat("_Glossiness", 0.5f);
        coreMat.SetFloat("_Mode", 3f);
        coreMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        coreMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        coreMat.SetOverrideTag("RenderType", "Transparent");

        core.GetComponent<Renderer>().material = coreMat;
        core.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        core.GetComponent<Renderer>().receiveShadows = false;

        // 删除碰撞体
        DestroyImmediate(core.GetComponent<Collider>());

        return core;
    }

    /// <summary>
    /// 创建粒子效果
    /// </summary>
    private ParticleSystem CreateParticles()
    {
        GameObject particleObj = new GameObject("PortalParticles");
        particleObj.transform.parent = transform;
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();

        // 配置粒子系统
        var main = ps.main;
        main.startLifetime = 2f;
        main.startSpeed = 1f;
        main.startSize = 0.2f;
        main.startColor = portalColor;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = portalSize * 0.5f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = CreateParticleMesh();
        renderer.material = CreateParticleMaterial(portalColor);

        return ps;
    }

    /// <summary>
    /// 创建粒子网格
    /// </summary>
    private Mesh CreateParticleMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.1f, -0.1f, 0f),
            new Vector3(0.1f, -0.1f, 0f),
            new Vector3(0f, 0.1f, 0f)
        };
        mesh.triangles = new int[] { 0, 1, 2 };
        return mesh;
    }

    /// <summary>
    /// 创建粒子材质
    /// </summary>
    private Material CreateParticleMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        mat.color = color;
        return mat;
    }

    /// <summary>
    /// 清理视觉效果
    /// </summary>
    [ContextMenu("清理视觉效果")]
    public void CleanupVisuals()
    {
        if (portalRing != null)
        {
            if (Application.isPlaying)
                Destroy(portalRing);
            else
                DestroyImmediate(portalRing);
        }

        if (portalCore != null)
        {
            if (Application.isPlaying)
                Destroy(portalCore);
            else
                DestroyImmediate(portalCore);
        }

        if (particles != null)
        {
            if (Application.isPlaying)
                Destroy(particles.gameObject);
            else
                DestroyImmediate(particles.gameObject);
        }

        // 清理场景中所有PortalVisual相关的对象
        Transform[] children = GetComponentsInChildren<Transform>();
        foreach (Transform child in children)
        {
            if (child != transform)
            {
                if (child.name.Contains("Portal"))
                {
                    if (Application.isPlaying)
                        Destroy(child.gameObject);
                    else
                        DestroyImmediate(child.gameObject);
                }
            }
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // 动画效果
        float time = Time.time * animationSpeed;

        // 旋转外环
        if (portalRing != null && rotateEffect)
        {
            portalRing.transform.localRotation = Quaternion.Euler(90f, time * 30f, 0f);
        }

        // 呼吸效果
        if (portalCore != null)
        {
            float scale = portalSize * 0.8f + Mathf.Sin(time * 2f) * 0.1f;
            portalCore.transform.localScale = Vector3.one * scale;
        }
    }
}

/// <summary>
/// 传送门编辑器扩展
/// </summary>
#if UNITY_EDITOR
[CustomEditor(typeof(PortalVisualGenerator))]
public class PortalVisualGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PortalVisualGenerator generator = (PortalVisualGenerator)target;

        DrawDefaultInspector();

        EditorGUILayout.Space();
        GUILayout.Label("视觉效果控制", EditorStyles.boldLabel);

        if (GUILayout.Button("生成/更新视觉效果", GUILayout.Height(30)))
        {
            generator.GenerateVisuals();
        }

        if (GUILayout.Button("清理视觉效果", GUILayout.Height(30)))
        {
            generator.CleanupVisuals();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "点击按钮可以生成或清理传送门的视觉效果。\n" +
            "效果包括外环、核心和粒子系统。",
            MessageType.Info
        );
    }
}
#endif
