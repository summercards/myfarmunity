// 临时调试脚本，帮助用户找到商店 UI
using UnityEngine;
using UnityEditor;

public class FindShopUI : EditorWindow
{
    [MenuItem("工具/调试/查找商店 UI")]
    static void ShowWindow()
    {
        GetWindow<FindShopUI>("查找商店 UI");
    }

    void OnGUI()
    {
        GUILayout.Label("查找场景中的商店 UI 组件", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("查找 MiniShop"))
        {
            FindComponent<MiniShop>();
        }

        if (GUILayout.Button("查找 SimpleShopUI"))
        {
            FindComponent<SimpleShopUI>();
        }

        if (GUILayout.Button("查找 PlayerWallet"))
        {
            FindComponent<PlayerWallet>();
        }

        if (GUILayout.Button("查找 InventoryBridge"))
        {
            FindComponent<InventoryBridge>();
        }

        if (GUILayout.Button("查找 NPCDialogUI"))
        {
            FindComponent<NPCDialogUI>();
        }
    }

    static void FindComponent<T>() where T : Component
    {
        var obj = FindObjectOfType<T>();
        if (obj != null)
        {
            Debug.Log($"[FindShopUI] 找到 {typeof(T).Name}: {obj.name} at {obj.transform.Path()}");
            Selection.activeGameObject = obj.gameObject;
        }
        else
        {
            Debug.LogWarning($"[FindShopUI] 未找到 {typeof(T).Name}");
        }
    }
}

public static class TransformExtensions
{
    public static string Path(this Transform transform)
    {
        if (transform == null) return "";
        if (transform.parent == null) return transform.name;
        return transform.parent.Path() + "/" + transform.name;
    }
}
