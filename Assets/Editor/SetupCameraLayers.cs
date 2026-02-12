#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class SetupCameraLayers
{
    [MenuItem("Tools/Setup Camera Collision Layers")]
    public static void SetupLayers()
    {
        // 查找 PlayerCamera
        GameObject cameraObj = GameObject.Find("PlayerCamera");
        
        if (cameraObj == null)
        {
            EditorUtility.DisplayDialog("未找到摄像机", "场景中没有找到 PlayerCamera！\n\n请先运行游戏让 PlayerCamera 生成。", "确定");
            return;
        }
        
        TPSOrbitCamera orbitCam = cameraObj.GetComponent<TPSOrbitCamera>();
        
        if (orbitCam == null)
        {
            EditorUtility.DisplayDialog("未找到组件", "PlayerCamera 上没有 TPSOrbitCamera 组件！", "确定");
            return;
        }
        
        // 获取层级
        int terrainLayer = LayerMask.NameToLayer("Terrain");
        int buildingLayer = LayerMask.NameToLayer("Building");
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        
        // 设置碰撞层（只检测这些层，忽略其他）
        orbitCam.useSpecificLayers = true;
        
        LayerMask mask = 0;
        if (terrainLayer != -1) mask |= 1 << terrainLayer;
        if (buildingLayer != -1) mask |= 1 << buildingLayer;
        if (obstacleLayer != -1) mask |= 1 << obstacleLayer;
        
        orbitCam.collisionMask = mask;
        
        Debug.Log($"[SetupCameraLayers] 碰撞层已设置: {mask}");
        Debug.Log($"[SetupCameraLayers] 包含: Terrain, Building, Obstacle");
        
        EditorUtility.SetDirty(cameraObj);
        EditorUtility.DisplayDialog("设置完成", 
            "摄像机碰撞层已设置！\n\n摄像机会在以下层上检测碰撞：\n- Terrain (地形/地板)\n- Building (建筑)\n- Obstacle (障碍物)\n\n其他层（包括 Player）将被忽略，避免隐形问题。", 
            "确定");
    }
}
#endif