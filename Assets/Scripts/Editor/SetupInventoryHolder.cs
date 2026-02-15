// Assets/Scripts/Editor/SetupInventoryHolder.cs
using UnityEngine;
using UnityEditor;

/// <summary>
/// 编辑器工具：快速设置 PlayerInventoryHolder 预制体
/// </summary>
public class SetupInventoryHolder : EditorWindow
{
    [MenuItem("Tools/游戏工具/设置背包系统")]
    public static void ShowWindow()
    {
        GetWindow<SetupInventoryHolder>("背包系统设置");
    }

    private ItemDatabaseSO itemDB;
    private int capacity = 24;
    private string prefabPath = "Assets/Prefabs/Player/PlayerInventoryHolder.prefab";

    void OnGUI()
    {
        GUILayout.Label("背包系统设置", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("配置参数", EditorStyles.label);
        itemDB = (ItemDatabaseSO)EditorGUILayout.ObjectField("Item Database", itemDB, typeof(ItemDatabaseSO), false);
        capacity = EditorGUILayout.IntField("背包容量", capacity);
        prefabPath = EditorGUILayout.TextField("预制体路径", prefabPath);

        GUILayout.Space(20);

        if (GUILayout.Button("创建背包预制体", GUILayout.Height(40)))
        {
            CreateInventoryHolder();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "这将创建一个带有 PlayerInventoryHolder 组件的预制体。\n\n" +
            "创建后：\n" +
            "1. 在 main.unity 场景中拖入此预制体\n" +
            "2. 在其他场景（game.unity、couldcity.scene）中删除现有的 PlayerInventoryHolder 对象",
            MessageType.Info
        );
    }

    void CreateInventoryHolder()
    {
        // 确保目录存在
        string directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        // 检查是否已存在
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog("确认", "预制体已存在，是否覆盖？", "覆盖", "取消"))
            {
                return;
            }
        }

        // 创建空游戏对象
        GameObject holderGO = new GameObject("PlayerInventoryHolder");

        // 添加组件
        PlayerInventoryHolder holder = holderGO.AddComponent<PlayerInventoryHolder>();
        holder.itemDB = itemDB;
        holder.capacity = capacity;

        // 保存为预制体
        PrefabUtility.SaveAsPrefabAsset(holderGO, prefabPath);
        Object.DestroyImmediate(holderGO);

        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset(prefabPath);

        EditorUtility.DisplayDialog("成功", $"背包预制体已创建：\n{prefabPath}", "确定");

        Debug.Log($"[SetupInventoryHolder] 背包预制体已创建：{prefabPath}");
    }
}
