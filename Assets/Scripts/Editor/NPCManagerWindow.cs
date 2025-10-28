#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using FarmGame.NPCSystem;

public class NPCManagerWindow : EditorWindow
{
    private List<NPCDefinition> _defs = new List<NPCDefinition>();
    private NPCDefinition _selected;
    private Vector2 _leftScroll, _rightScroll;

    private ReorderableList _linesList;
    private ReorderableList _animList;

    [MenuItem("Tools/Farm/NPC 管理器")]
    public static void Open()
    {
        GetWindow<NPCManagerWindow>("NPC 管理器");
    }

    private void OnEnable()
    {
        RefreshList();
        if (_selected != null)
        {
            BuildLinesList();
            BuildAnimList();
        }
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
        GUILayout.Label("NPC 数据（ScriptableObjects）", EditorStyles.boldLabel);
        if (GUILayout.Button("刷新", GUILayout.Width(60)))
            RefreshList();
        EditorGUILayout.EndHorizontal();

        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
        foreach (var def in _defs)
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                if (GUILayout.Button(def.name, GUILayout.Width(160)))
                {
                    _selected = def;
                    BuildLinesList();
                    BuildAnimList();
                    EditorGUIUtility.PingObject(def);
                }
                if (GUILayout.Button("选中", GUILayout.Width(50)))
                    Selection.activeObject = def;
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("新建 NPC 数据"))
            {
                CreateDefinition();
            }
            if (_selected != null && GUILayout.Button("复制"))
            {
                var path = AssetDatabase.GetAssetPath(_selected);
                var newPath = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CopyAsset(path, newPath);
                AssetDatabase.Refresh();
                RefreshList();
            }
            if (_selected != null && GUILayout.Button("删除"))
            {
                if (EditorUtility.DisplayDialog("删除确认", $"确定删除 {_selected.name} ?", "删除", "取消"))
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
            EditorGUILayout.HelpBox("左侧选择一个 NPC 数据，或点击“新建 NPC 数据”。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

        SerializedObject so = new SerializedObject(_selected);
        so.Update();

        // 基础
        EditorGUILayout.PropertyField(so.FindProperty("npcId"));
        EditorGUILayout.PropertyField(so.FindProperty("npcName"));

        EditorGUILayout.Space();
        GUILayout.Label("对话台词", EditorStyles.boldLabel);
        _linesList?.DoLayoutList();

        EditorGUILayout.Space();
        GUILayout.Label("功能按钮", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("function"));
        var funcProp = so.FindProperty("function");
        if ((NPCFunctionType)funcProp.enumValueIndex == NPCFunctionType.OpenShop)
        {
            EditorGUILayout.PropertyField(so.FindProperty("defaultShopCatalog"));
        }
        EditorGUILayout.PropertyField(so.FindProperty("functionButtonText"));

        EditorGUILayout.Space(10);
        GUILayout.Label("模型与动作", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("modelPrefab"));
        EditorGUILayout.PropertyField(so.FindProperty("animatorController"));

        EditorGUILayout.PropertyField(so.FindProperty("modelLocalPosition"));
        EditorGUILayout.PropertyField(so.FindProperty("modelLocalEuler"));
        EditorGUILayout.PropertyField(so.FindProperty("modelLocalScale"));

        EditorGUILayout.Space(4);
        _animList?.DoLayoutList();

        EditorGUILayout.Space(10);
        GUILayout.Label("生成选项", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("defaultColliderCenter"));
        EditorGUILayout.PropertyField(so.FindProperty("defaultColliderSize"));
        EditorGUILayout.PropertyField(so.FindProperty("addCapsuleColliderInstead"));
        EditorGUILayout.PropertyField(so.FindProperty("gizmoColor"));

        so.ApplyModifiedProperties();

        EditorGUILayout.Space(10);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("→ 在场景创建 NPC", GUILayout.Height(28)))
            {
                CreateNPCInScene(_selected);
            }
            if (GUILayout.Button("→ 应用到选中物体", GUILayout.Height(28)))
            {
                ApplyToSelection(_selected);
            }
            if (GUILayout.Button("→ 选中物体做成 Prefab", GUILayout.Height(28)))
            {
                MakePrefabFromSelection();
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void BuildLinesList()
    {
        if (_selected == null) return;
        SerializedObject so = new SerializedObject(_selected);
        var linesProp = so.FindProperty("dialogLines");

        _linesList = new ReorderableList(so, linesProp, true, true, true, true);
        _linesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "台词（按回车添加行，顺序可拖动）");
        _linesList.drawElementCallback = (rect, index, active, focused) =>
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            var elem = linesProp.GetArrayElementAtIndex(index);
            elem.stringValue = EditorGUI.TextField(rect, $"[{index}]", elem.stringValue);
        };
        _linesList.onAddCallback = list =>
        {
            linesProp.arraySize++;
            linesProp.GetArrayElementAtIndex(linesProp.arraySize - 1).stringValue = "";
        };
    }

    private void BuildAnimList()
    {
        if (_selected == null) return;
        SerializedObject so = new SerializedObject(_selected);
        var animProp = so.FindProperty("dailyAnimations");

        _animList = new ReorderableList(so, animProp, true, true, true, true);
        _animList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "时间段动作（含起不含止，0-24h）");
        _animList.elementHeight = EditorGUIUtility.singleLineHeight * 2.2f;

        _animList.drawElementCallback = (rect, index, active, focused) =>
        {
            var elem = animProp.GetArrayElementAtIndex(index);
            var startHour = elem.FindPropertyRelative("startHour");
            var endHour = elem.FindPropertyRelative("endHour");
            var stateName = elem.FindPropertyRelative("stateName");
            var speed = elem.FindPropertyRelative("speed");

            var line1 = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);
            var line2 = new Rect(rect.x, rect.y + 4 + EditorGUIUtility.singleLineHeight, rect.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            EditorGUI.IntSlider(new Rect(line1.x, line1.y, line1.width * 0.48f, line1.height), startHour, 0, 23, "Start");
            EditorGUI.IntSlider(new Rect(line1.x + line1.width * 0.52f, line1.y, line1.width * 0.48f, line1.height), endHour, 0, 24, "End");

            EditorGUI.PropertyField(new Rect(line2.x, line2.y, line2.width * 0.6f, line2.height), stateName, GUIContent.none);
            EditorGUI.Slider(new Rect(line2.x + line2.width * 0.62f, line2.y, line2.width * 0.36f, line2.height), speed, 0.1f, 3f, "Speed");
            if (EditorGUI.EndChangeCheck())
            {
                startHour.intValue = Mathf.Clamp(startHour.intValue, 0, 23);
                endHour.intValue = Mathf.Clamp(endHour.intValue, 0, 24);
            }
        };

        _animList.onAddCallback = list =>
        {
            animProp.arraySize++;
            var e = animProp.GetArrayElementAtIndex(animProp.arraySize - 1);
            e.FindPropertyRelative("startHour").intValue = 8;
            e.FindPropertyRelative("endHour").intValue = 18;
            e.FindPropertyRelative("stateName").stringValue = "Idle";
            e.FindPropertyRelative("speed").floatValue = 1f;
        };
    }

    private void RefreshList()
    {
        _defs = AssetDatabase.FindAssets("t:FarmGame.NPCSystem.NPCDefinition")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<NPCDefinition>)
            .Where(a => a != null)
            .OrderBy(a => a.npcId)
            .ToList();
        Repaint();
    }

    private void CreateDefinition()
    {
        var path = EditorUtility.SaveFilePanelInProject("新建 NPC 数据", "NPC_New", "asset", "保存到项目");
        if (string.IsNullOrEmpty(path)) return;
        var def = ScriptableObject.CreateInstance<NPCDefinition>();
        AssetDatabase.CreateAsset(def, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        _selected = def;
        RefreshList();
        BuildLinesList();
        BuildAnimList();
        Selection.activeObject = def;
    }

    private void CreateNPCInScene(NPCDefinition def)
    {
        var go = new GameObject(def.npcName);
        Undo.RegisterCreatedObjectUndo(go, "Create NPC");

        // 层：优先放 Interactable
        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer != -1) go.layer = interactableLayer;

        // Collider
        if (def.addCapsuleColliderInstead)
        {
            var cap = go.AddComponent<CapsuleCollider>();
            cap.center = def.defaultColliderCenter;
            cap.height = def.defaultColliderSize.y;
            cap.radius = Mathf.Max(def.defaultColliderSize.x, def.defaultColliderSize.z) * 0.5f;
            cap.isTrigger = true;
        }
        else
        {
            var box = go.AddComponent<BoxCollider>();
            box.center = def.defaultColliderCenter;
            box.size = def.defaultColliderSize;
            box.isTrigger = true;
        }

        // NPCInteractable（模糊匹配）
        var interactable = go.GetComponent<MonoBehaviour>();
        if (interactable == null)
        {
            var npcType = FindTypeContains("NPCInteractable");
            if (npcType != null) interactable = (MonoBehaviour)go.AddComponent(npcType);
        }

        // 绑定器
        var binder = go.AddComponent<NPCFromDefinition>();
        binder.definition = def;
        binder.TryAutoFill();
        binder.ApplyDefinitionToTarget();

        // 可视控制（模型+动作）
        var vis = go.AddComponent<NPCVisualController>();
        vis.definition = def;

        // 放到场景视图中心
        var sv = SceneView.lastActiveSceneView;
        if (sv != null) go.transform.position = sv.pivot;

        Selection.activeGameObject = go;
    }

    private void ApplyToSelection(NPCDefinition def)
    {
        foreach (var go in Selection.gameObjects)
        {
            var binder = go.GetComponent<NPCFromDefinition>();
            if (binder == null) binder = Undo.AddComponent<NPCFromDefinition>(go);
            binder.definition = def;
            binder.TryAutoFill();
            binder.ApplyDefinitionToTarget();

            var vis = go.GetComponent<NPCVisualController>();
            if (vis == null) vis = Undo.AddComponent<NPCVisualController>(go);
            vis.definition = def;

            EditorUtility.SetDirty(go);
        }
        Debug.Log($"已将 NPC 数据 [{def.npcName}] 应用到 {Selection.gameObjects.Length} 个物体。");
    }

    private void MakePrefabFromSelection()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("提示", "请先在层级里选中一个 NPC 物体。", "好的");
            return;
        }
        var path = EditorUtility.SaveFilePanelInProject("保存为 Prefab", go.name, "prefab", "选择保存位置");

        if (string.IsNullOrEmpty(path)) return;
#if UNITY_2021_3_OR_NEWER
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
#else
        var prefab = PrefabUtility.CreatePrefab(path, go);
#endif
        if (prefab != null)
        {
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("Prefab 已创建：" + path);
        }
    }

    private System.Type FindTypeContains(string namePart)
    {
        namePart = namePart.ToLower();
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var types = asm.GetTypes();
            foreach (var t in types)
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(t) && t.Name.ToLower().Contains(namePart))
                    return t;
            }
        }
        return null;
    }
}
#endif
