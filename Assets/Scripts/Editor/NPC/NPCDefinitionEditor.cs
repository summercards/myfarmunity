using UnityEngine;
using UnityEditor;
using FarmGame.NPCSystem;

namespace FarmGame.Editor.NPC
{
    [CustomEditor(typeof(NPCDefinition))]
    public class NPCDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 0. 架构切换
            using (new EditorGUILayout.VerticalScope("box"))
            {
                var style = new GUIStyle(EditorStyles.boldLabel);
                style.normal.textColor = new Color(0.2f, 0.7f, 1f); // 醒目的蓝色
                EditorGUILayout.LabelField("【重构阶段】架构设置", style);
                DrawProp("useActorSystem", "使用 Actor 系统 (新架构)");
                if (serializedObject.FindProperty("useActorSystem").boolValue)
                {
                    EditorGUILayout.HelpBox("当前使用 Actor 架构构建。旧的交互组件将被移除，暂时无法对话是正常的。", MessageType.Info);
                }
            }
            EditorGUILayout.Space();

            // 1. 模块开关
            EditorGUILayout.LabelField("模块开关 (功能测试)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("enableVisuals", "启用视觉 (模型/动画)");
                DrawProp("enableCollider", "启用碰撞 (物理/交互层级)");
                DrawProp("enableInteraction", "启用交互 (对话/逻辑)");
                DrawProp("enableShop", "启用商店 (需配合功能类型)");
            }

            EditorGUILayout.Space();

            // 2. 基本信息
            EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("npcId", "NPC ID (唯一标识)");
                DrawProp("npcName", "显示名称");
                DrawProp("dialogLines", "对话内容");
            }

            EditorGUILayout.Space();

            // 3. 功能配置
            EditorGUILayout.LabelField("功能配置", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("function", "功能类型");
                
                // 仅当功能是 OpenShop 时显示商店配置
                var funcProp = serializedObject.FindProperty("function");
                if ((NPCFunction)funcProp.enumValueIndex == NPCFunction.OpenShop)
                {
                    DrawProp("defaultShopCatalog", "商店商品配置");
                }
                
                DrawProp("functionButtonText", "功能按钮文本");
            }

            EditorGUILayout.Space();

            // 4. 模型与动画
            EditorGUILayout.LabelField("模型与动画", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("modelPrefab", "模型预制体");
                DrawProp("animatorController", "动画控制器");
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("模型微调", EditorStyles.miniLabel);
                DrawProp("modelLocalPosition", "位置偏移");
                DrawProp("modelLocalEuler", "旋转偏移");
                DrawProp("modelLocalScale", "缩放");
            }

            EditorGUILayout.Space();

            // 5. 日常作息
            EditorGUILayout.LabelField("日常作息 (24小时制)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("dailyAnimations", "作息时间表");
            }

            EditorGUILayout.Space();

            // 6. 对话表现
            EditorGUILayout.LabelField("对话表现", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("talkStateName", "对话动画状态名");
                DrawProp("talkSpeed", "播放速度");
                DrawProp("talkCrossFade", "过渡时间");
            }

            EditorGUILayout.Space();

            // 7. 碰撞体设置
            EditorGUILayout.LabelField("碰撞体设置 (预制体构建)", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawProp("colliderCenter", "中心点");
                DrawProp("colliderRadius", "半径");
                DrawProp("colliderHeight", "高度");
            }

            EditorGUILayout.Space();
            
            // 8. 调试
            EditorGUILayout.LabelField("编辑器调试", EditorStyles.boldLabel);
            DrawProp("gizmoColor", "辅助线颜色");

            serializedObject.ApplyModifiedProperties();
            
            EditorGUILayout.Space(20);
            if (GUILayout.Button("构建/更新此 NPC 预制体", GUILayout.Height(30)))
            {
                NPCPrefabBuilder.BuildPrefab((NPCDefinition)target);
            }
        }

        private void DrawProp(string propertyName, string label)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, new GUIContent(label));
            }
            else
            {
                // 如果找不到属性（可能是代码改了没同步），显示个错误提示
                EditorGUILayout.HelpBox($"找不到属性: {propertyName}", MessageType.Error);
            }
        }
    }
}
