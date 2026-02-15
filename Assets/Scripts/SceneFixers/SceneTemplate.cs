using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 场景模板配置
/// 定义标准游戏场景所需的完整配置
/// </summary>
[CreateAssetMenu(fileName = "NewSceneTemplate", menuName = "Scene Fixers/Scene Template")]
public class SceneTemplate : ScriptableObject
{
    [Header("=== 场景基础信息 ===")]
    [Tooltip("场景名称（不含.unity）")]
    public string sceneName = "NewGameScene";

    [Tooltip("场景路径（相对Scenes文件夹）")]
    public string scenePath = "Assets/Scenes/";

    [Header("=== 玩家配置 ===")]
    [Tooltip("玩家对象名称前缀（用于查找）")]
    public string playerNamePrefix = "Player";

    [Tooltip("玩家出生位置")]
    public Vector3 playerSpawnPosition = new Vector3(0, 1, 0);

    [Tooltip("玩家出生旋转")]
    public Vector3 playerSpawnRotation = new Vector3(0, 0, 0);

    [Header("=== 必需组件清单 ===")]
    [Tooltip("是否创建 Player 对象")]
    public bool createPlayer = true;

    [Tooltip("是否添加 PlayerInventoryHolder 组件")]
    public bool addPlayerInventoryHolder = true;

    [Tooltip("是否添加 ActiveItemController 组件")]
    public bool addActiveItemController = true;

    [Tooltip("是否添加 PlayerStats 组件")]
    public bool addPlayerStats = true;

    [Tooltip("是否添加 PlayerBuilder 组件")]
    public bool addPlayerBuilder = true;

    [Tooltip("是否添加 CharacterController 组件")]
    public bool addCharacterController = true;

    [Header("=== 管理器配置 ===")]
    [Tooltip("是否创建 GameManager")]
    public bool createGameManager = true;

    [Tooltip("是否创建 BuildSaveManager")]
    public bool createBuildSaveManager = true;

    [Tooltip("是否创建 SaveManager")]
    public bool createSaveManager = true;

    [Tooltip("是否创建 TimeController")]
    public bool createTimeController = true;

    [Tooltip("是否创建 PortalManager")]
    public bool createPortalManager = true;

    [Header("=== 相机配置 ===")]
    [Tooltip("是否创建主相机")]
    public bool createMainCamera = true;

    [Tooltip("相机类型")]
    public CameraType cameraType = CameraType.ThirdPerson;

    [Tooltip("相机位置")]
    public Vector3 cameraPosition = new Vector3(0, 5, -10);

    [Header("=== 环境配置 ===")]
    [Tooltip("是否创建基础光照")]
    public bool createLighting = true;

    [Tooltip("是否创建地面")]
    public bool createGround = true;

    [Tooltip("地面大小")]
    public Vector3 groundScale = new Vector3(50, 1, 50);

    [Header("=== 资源引用 ===")]
    [Tooltip("BuildCatalogSO 资源（留空则不配置）")]
    public BuildCatalogSO buildCatalog;

    [Tooltip("ItemDatabase 资源（留空则不配置）")]
    public ItemDatabaseSO itemDatabase;

    [Header("=== 场景设置 ===")]
    [Tooltip("场景的天空盒材质")]
    public Material skyboxMaterial;

    [Tooltip("场景的雾效设置")]
    public FogMode fogMode = FogMode.Exponential;

    [Tooltip("雾效颜色")]
    public Color fogColor = new Color(0.5f, 0.6f, 0.7f, 1f);

    [Range(0f, 0.1f)]
    [Tooltip("雾效密度")]
    public float fogDensity = 0.01f;

    [Header("=== 自动清理 ===")]
    [Tooltip("创建前删除同名旧场景")]
    public bool deleteOldScene = true;

    [Tooltip("是否只创建不打开")]
    public bool createOnly = false;

    /// <summary>
    /// 相机类型枚举
    /// </summary>
    public enum CameraType
    {
        ThirdPerson,
        TopDown,
        Isometric,
        FirstPerson
    }
}

#endif
