#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using FarmGame.NPCSystem;
using FarmGame.Editor.NPC; // Import the Builder namespace

public class NPCManagerWindow : EditorWindow
{
    private List<NPCDefinition> _defs = new List<NPCDefinition>();
    private NPCDefinition _selected;
    private Vector2 _leftScroll, _rightScroll;

    private ReorderableList _linesList;
    private ReorderableList _animList;

    [MenuItem("工具/NPC/NPC 管理器")]
    public static void Open() => GetWindow<NPCManagerWindow>("NPC 管理器");

    private void OnEnable()
    {
        RefreshList();
        if (_selected != null) { BuildLinesList(); BuildAnimList(); }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        DrawLeft();
        DrawRight();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeft()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(260));
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("NPC 数据列表", EditorStyles.boldLabel);
        if (GUILayout.Button("刷新", GUILayout.Width(60))) RefreshList();
        EditorGUILayout.EndHorizontal();

        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
        foreach (var def in _defs)
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                if (GUILayout.Button(def.name, GUILayout.Width(160)))
                {
                    _selected = def;
                    BuildLinesList(); BuildAnimList();
                    EditorGUIUtility.PingObject(def);
                }
                if (GUILayout.Button("选择", GUILayout.Width(50)))
                    Selection.activeObject = def;
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("新建数据")) CreateDefinition();
            if (_selected != null && GUILayout.Button("复制"))
            {
                var path = AssetDatabase.GetAssetPath(_selected);
                var newPath = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CopyAsset(path, newPath);
                AssetDatabase.Refresh(); RefreshList();
            }
            if (_selected != null && GUILayout.Button("删除"))
            {
                if (EditorUtility.DisplayDialog("确认删除", $"确定要删除 {_selected.name} 吗?", "删除", "取消"))
                {
                    var path = AssetDatabase.GetAssetPath(_selected);
                    AssetDatabase.DeleteAsset(path);
                    _selected = null;
                    RefreshList();
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRight()
    {
        EditorGUILayout.BeginVertical();
        EditorGUILayout.Space();

        if (_selected == null)
        {
            EditorGUILayout.HelpBox("请在左侧选择一个 NPC 数据进行编辑。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        var so = new SerializedObject(_selected);
        so.Update();

        var scroll = EditorGUILayout.BeginScrollView(_rightScroll);

        EditorGUILayout.PropertyField(so.FindProperty("npcId"));
        EditorGUILayout.PropertyField(so.FindProperty("npcName"));

        EditorGUILayout.Space();
        GUILayout.Label("对话内容", EditorStyles.boldLabel);
        _linesList?.DoLayoutList();

        EditorGUILayout.Space();
        GUILayout.Label("功能配置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("function"));
        var funcProp = so.FindProperty("function");
        if ((NPCFunction)funcProp.enumValueIndex == NPCFunction.OpenShop)
        {
            EditorGUILayout.PropertyField(so.FindProperty("defaultShopCatalog"));
        }
        EditorGUILayout.PropertyField(so.FindProperty("functionButtonText"));


        EditorGUILayout.Space(10);
        GUILayout.Label("模型与动画", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("modelPrefab"));
        EditorGUILayout.PropertyField(so.FindProperty("animatorController"));
        EditorGUILayout.PropertyField(so.FindProperty("modelLocalPosition"));
        EditorGUILayout.PropertyField(so.FindProperty("modelLocalEuler"));
        EditorGUILayout.PropertyField(so.FindProperty("modelLocalScale"));

        EditorGUILayout.Space(4);
        _animList?.DoLayoutList();

        EditorGUILayout.Space(6);
        GUILayout.Label("对话设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("talkStateName"));
        EditorGUILayout.PropertyField(so.FindProperty("talkSpeed"));
        EditorGUILayout.PropertyField(so.FindProperty("talkCrossFade"));

        EditorGUILayout.Space(10);
        GUILayout.Label("碰撞体设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("colliderCenter"));
        EditorGUILayout.PropertyField(so.FindProperty("colliderRadius"));
        EditorGUILayout.PropertyField(so.FindProperty("colliderHeight"));
        EditorGUILayout.PropertyField(so.FindProperty("gizmoColor"));

        so.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("在场景中生成", GUILayout.Height(28))) CreateNPCInScene(_selected);
            if (GUILayout.Button("重建预制体", GUILayout.Height(28))) NPCPrefabBuilder.BuildPrefab(_selected);
        }

        EditorGUILayout.EndScrollView();
        _rightScroll = scroll;
        EditorGUILayout.EndVertical();
    }

    private void BuildLinesList()
    {
        var so = new SerializedObject(_selected);
        var linesProp = so.FindProperty("dialogLines");
        _linesList = new ReorderableList(so, linesProp, true, true, true, true);
        _linesList.drawHeaderCallback = r => EditorGUI.LabelField(r, "对话行");
        _linesList.drawElementCallback = (rect, i, a, f) =>
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            var elem = linesProp.GetArrayElementAtIndex(i);
            EditorGUI.LabelField(new Rect(rect.x, rect.y, 24, rect.height), $"[{i}]");
            elem.stringValue = EditorGUI.TextField(new Rect(rect.x + 28, rect.y, rect.width - 28, rect.height), elem.stringValue);
        };
        _linesList.onAddCallback = l =>
        {
            linesProp.arraySize++;
            linesProp.GetArrayElementAtIndex(linesProp.arraySize - 1).stringValue = "";
        };
    }

    private void BuildAnimList()
    {
        var so = new SerializedObject(_selected);
        var animProp = so.FindProperty("dailyAnimations");

        _animList = new ReorderableList(so, animProp, true, true, true, true);
        _animList.drawHeaderCallback = r => EditorGUI.LabelField(r, "日常行程 (24小时制)");
        _animList.elementHeight = EditorGUIUtility.singleLineHeight * 2.6f;

        _animList.drawElementCallback = (rect, index, active, focused) =>
        {
            var elem = animProp.GetArrayElementAtIndex(index);
            var startHour = elem.FindPropertyRelative("startHour");
            var endHour = elem.FindPropertyRelative("endHour");
            var stateName = elem.FindPropertyRelative("stateName");
            var speed = elem.FindPropertyRelative("speed");
            var cond = elem.FindPropertyRelative("conditionTag");

            float lh = EditorGUIUtility.singleLineHeight;
            var r1 = new Rect(rect.x, rect.y + 2, rect.width, lh);
            var r2 = new Rect(rect.x, rect.y + 6 + lh, rect.width, lh);

            float col = r1.width / 3f;
            EditorGUI.LabelField(new Rect(r1.x, r1.y, 40, lh), "开始");
            startHour.intValue = EditorGUI.IntSlider(new Rect(r1.x + 42, r1.y, col - 48, lh), startHour.intValue, 0, 23);
            EditorGUI.LabelField(new Rect(r1.x + col, r1.y, 32, lh), "结束");
            endHour.intValue = EditorGUI.IntSlider(new Rect(r1.x + col + 34, r1.y, col - 40, lh), endHour.intValue, 0, 24);
            speed.floatValue = EditorGUI.Slider(new Rect(r1.x + 2 * col + 6, r1.y, col - 8, lh), "速度", speed.floatValue, 0.1f, 3f);

            EditorGUI.LabelField(new Rect(r2.x, r2.y, 52, lh), "状态名");
            stateName.stringValue = EditorGUI.TextField(new Rect(r2.x + 54, r2.y, r2.width * 0.6f - 60, lh), stateName.stringValue);
            EditorGUI.LabelField(new Rect(r2.x + r2.width * 0.62f, r2.y, 70, lh), "条件");
            cond.stringValue = EditorGUI.TextField(new Rect(r2.x + r2.width * 0.62f + 72, r2.y, r2.width * 0.38f - 74, lh), cond.stringValue);
        };

        _animList.onAddCallback = list =>
        {
            animProp.arraySize++;
            var e = animProp.GetArrayElementAtIndex(animProp.arraySize - 1);
            e.FindPropertyRelative("startHour").intValue = 8;
            e.FindPropertyRelative("endHour").intValue = 18;
            e.FindPropertyRelative("stateName").stringValue = "Idle";
            e.FindPropertyRelative("speed").floatValue = 1f;
            e.FindPropertyRelative("conditionTag").stringValue = "";
        };
    }

    private void RefreshList()
    {
        _defs = AssetDatabase.FindAssets("t:FarmGame.NPCSystem.NPCDefinition")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<NPCDefinition>)
            .Where(a => a != null)
            .OrderBy(a => a.npcId).ToList();
        Repaint();
    }

    private void CreateDefinition()
    {
        var path = EditorUtility.SaveFilePanelInProject("新建 NPC 数据", "NPC_New", "asset", "保存");
        if (string.IsNullOrEmpty(path)) return;
        var def = ScriptableObject.CreateInstance<NPCDefinition>();
        AssetDatabase.CreateAsset(def, path);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        _selected = def; RefreshList(); BuildLinesList(); BuildAnimList();
        Selection.activeObject = def;
    }

    private void CreateNPCInScene(NPCDefinition def)
    {
        // Use the Builder to get/ensure the prefab exists
        GameObject prefab = NPCPrefabBuilder.BuildPrefab(def);
        if (prefab == null) return;

        // Instantiate the prefab
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = def.npcName;
        Undo.RegisterCreatedObjectUndo(instance, "Spawn NPC");

        // Move to scene view pivot
        var sv = SceneView.lastActiveSceneView;
        if (sv != null) instance.transform.position = sv.pivot;

        Selection.activeGameObject = instance;
    }
}
#endif
