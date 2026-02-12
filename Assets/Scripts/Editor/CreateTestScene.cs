// Assets/Scripts/Editor/CreateTestScene.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

/// <summary>
/// 创建测试场景
/// </summary>
public class CreateTestScene : Editor
{
    [MenuItem("Tools/时间系统/创建测试场景")]
    public static void CreateTestSceneFile()
    {
        // 创建新场景
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject sceneRoot = new GameObject("TestScene");

        // 1. 添加摄像机
        CreateCamera();

        // 2. 添加地面
        CreateGround();

        // 3. 添加光照
        SetupLighting();

        // 4. 保存场景
        string scenePath = "Assets/Scenes/TestScene.unity";
        string folder = "Assets/Scenes";

        if (!System.IO.Directory.Exists(folder))
        {
            System.IO.Directory.CreateDirectory(folder);
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        AssetDatabase.Refresh();

        Debug.Log($"测试场景已创建: {scenePath}");

        // 提示用户运行自动配置
        if (EditorUtility.DisplayDialog("配置完成", "测试场景已创建！\n\n是否现在运行时间系统自动配置？", "是", "否"))
        {
            TimeSystemAutoSetup.ShowWindow();
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObj = new GameObject("Main Camera");
        cameraObj.transform.position = new Vector3(0, 10, -10);
        cameraObj.transform.rotation = Quaternion.Euler(30, 0, 0);

        Camera camera = cameraObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.backgroundColor = new Color(0.5f, 0.7f, 1f);
        camera.farClipPlane = 1000f;

        cameraObj.AddComponent<AudioListener>();
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(10, 1, 10);
    }

    private static void SetupLighting()
    {
        // 场景将由自动配置脚本创建 Directional Light 和 DayNightCycle
        Debug.Log("请运行 'Tools/时间系统/自动配置' 来设置光照系统");
    }
}
#endif
