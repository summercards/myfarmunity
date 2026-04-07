using System.Collections.Generic;
using System.Text;
using FarmGame.NPCSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmGame.Editor.NPC
{
    /// <summary>
    /// Phase 04 工具：
    /// 扫描并清理 NPC prefab 中遗留的 NPCInteractable 组件。
    /// </summary>
    public static class Phase04LegacyNpcCleanup
    {
        private const string NpcPrefabFolder = "Assets/Prefabs/NPC";
        private const string SceneFolder = "Assets/Scenes";

        [MenuItem("工具/NPC/Phase04/扫描 Legacy NPCInteractable")]
        public static void ScanLegacyNpcInteractable()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { NpcPrefabFolder });
            List<string> hits = new List<string>();
            int totalLegacyCount = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

                try
                {
                    int count = CountLegacyComponents(root);
                    if (count > 0)
                    {
                        totalLegacyCount += count;
                        hits.Add($"{prefabPath} => {count}");
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Phase04LegacyNpcCleanup] 扫描完成");
            sb.AppendLine($"Prefabs Checked: {prefabGuids.Length}");
            sb.AppendLine($"Legacy NPCInteractable Count: {totalLegacyCount}");

            if (hits.Count == 0)
            {
                sb.AppendLine("Result: PASS (未发现遗留旧组件)");
            }
            else
            {
                sb.AppendLine("Result: FOUND");
                for (int i = 0; i < hits.Count; i++)
                {
                    sb.AppendLine(hits[i]);
                }
            }

            Debug.Log(sb.ToString());
        }

        [MenuItem("工具/NPC/Phase04/清理 NPC Prefabs 中 Legacy NPCInteractable")]
        public static void CleanupLegacyNpcInteractable()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { NpcPrefabFolder });
            int touchedPrefabs = 0;
            int removedCount = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

                try
                {
                    int currentRemoved = RemoveLegacyComponents(root);
                    if (currentRemoved <= 0)
                    {
                        continue;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    touchedPrefabs++;
                    removedCount += currentRemoved;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Phase04LegacyNpcCleanup] 清理完成。Prefabs Updated: {touchedPrefabs}, Legacy Components Removed: {removedCount}");
        }

        [MenuItem("工具/NPC/Phase04/扫描 Scenes 中 Legacy NPCInteractable")]
        public static void ScanLegacyNpcInteractableInScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder });
            string originalScenePath = SceneManager.GetActiveScene().path;
            List<string> hits = new List<string>();
            int totalLegacyCount = 0;

            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int currentCount = CountLegacyComponentsInScene();
                if (currentCount > 0)
                {
                    totalLegacyCount += currentCount;
                    hits.Add($"{scene.path} => {currentCount}");
                }
            }

            RestoreScene(originalScenePath);
            PrintSceneScanResult(sceneGuids.Length, totalLegacyCount, hits);
        }

        [MenuItem("工具/NPC/Phase04/清理 Scenes 中 Legacy NPCInteractable")]
        public static void CleanupLegacyNpcInteractableInScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { SceneFolder });
            string originalScenePath = SceneManager.GetActiveScene().path;
            int touchedScenes = 0;
            int removedCount = 0;

            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int currentRemoved = RemoveLegacyComponentsInScene();
                if (currentRemoved <= 0)
                {
                    continue;
                }

                EditorSceneManager.SaveScene(scene);
                touchedScenes++;
                removedCount += currentRemoved;
            }

            RestoreScene(originalScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Phase04LegacyNpcCleanup] Scene 清理完成。Scenes Updated: {touchedScenes}, Legacy Components Removed: {removedCount}");
        }

        private static int CountLegacyComponents(GameObject root)
        {
#pragma warning disable 0618
            NPCInteractable[] legacyComponents = root.GetComponentsInChildren<NPCInteractable>(true);
#pragma warning restore 0618
            return legacyComponents != null ? legacyComponents.Length : 0;
        }

        private static int RemoveLegacyComponents(GameObject root)
        {
#pragma warning disable 0618
            NPCInteractable[] legacyComponents = root.GetComponentsInChildren<NPCInteractable>(true);
#pragma warning restore 0618
            int removedCount = 0;

            for (int i = 0; i < legacyComponents.Length; i++)
            {
                var legacy = legacyComponents[i];
                if (legacy == null)
                {
                    continue;
                }

                Object.DestroyImmediate(legacy);
                removedCount++;
            }

            return removedCount;
        }

        private static int CountLegacyComponentsInScene()
        {
#pragma warning disable 0618
            NPCInteractable[] legacyComponents = Object.FindObjectsOfType<NPCInteractable>(true);
#pragma warning restore 0618
            return legacyComponents != null ? legacyComponents.Length : 0;
        }

        private static int RemoveLegacyComponentsInScene()
        {
#pragma warning disable 0618
            NPCInteractable[] legacyComponents = Object.FindObjectsOfType<NPCInteractable>(true);
#pragma warning restore 0618

            int removedCount = 0;
            for (int i = 0; i < legacyComponents.Length; i++)
            {
                var legacy = legacyComponents[i];
                if (legacy == null)
                {
                    continue;
                }

                Object.DestroyImmediate(legacy);
                removedCount++;
            }

            return removedCount;
        }

        private static void RestoreScene(string originalScenePath)
        {
            if (!string.IsNullOrEmpty(originalScenePath))
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }

        private static void PrintSceneScanResult(int checkedCount, int totalLegacyCount, List<string> hits)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Phase04LegacyNpcCleanup] Scene 扫描完成");
            sb.AppendLine($"Scenes Checked: {checkedCount}");
            sb.AppendLine($"Legacy NPCInteractable Count: {totalLegacyCount}");

            if (hits.Count == 0)
            {
                sb.AppendLine("Result: PASS (未发现遗留旧组件)");
            }
            else
            {
                sb.AppendLine("Result: FOUND");
                for (int i = 0; i < hits.Count; i++)
                {
                    sb.AppendLine(hits[i]);
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
