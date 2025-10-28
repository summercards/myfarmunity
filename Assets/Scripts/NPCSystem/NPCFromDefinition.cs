using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmGame.NPCSystem
{
    /// <summary>
    /// 把 ScriptableObject(NPCDefinition) 的数据应用到场景里的 NPCInteractable。
    /// - 运行时安全（不依赖 UnityEditor）
    /// - 在编辑器里（UNITY_EDITOR）额外做持久化监听器写入/清理
    /// </summary>
    [DisallowMultipleComponent]
    public class NPCFromDefinition : MonoBehaviour
    {
        [Header("数据来源")]
        public NPCDefinition definition;

        [Header("目标组件（自动获取）")]
        public MonoBehaviour npcInteractable;   // 你的 NPCInteractable（不强类型绑定，靠名字兼容）
        [Tooltip("可选：场景里的 SimpleShopOpener；为空时会自动查找")]
        public MonoBehaviour simpleShopOpener; // 你的 SimpleShopOpener
        [Tooltip("仅供查看，不强制引用")]
        public Component npcDialogUI;

        // 常见字段名（大小写忽略）
        private static readonly string[] IdNames = { "npcId", "id" };
        private static readonly string[] NameNames = { "npcName", "name" };
        private static readonly string[] LinesNames = { "dialogLines", "lines", "dialogs", "dialogues" };
        private static readonly string[] EventNames = { "onFunction", "OnFunction", "functionEvent" };
        private static readonly string[] FuncTextNames = { "functionButtonText", "functionText", "functionLabel" };

        private void Reset()
        {
            TryAutoFill();
        }

        private void Awake()
        {
            // 运行时也可以应用（不会用到 UnityEditor）
            ApplyDefinitionToTarget();
        }

        public void TryAutoFill()
        {
            if (npcInteractable == null)
            {
                npcInteractable = GetComponents<MonoBehaviour>()
                    .FirstOrDefault(mb => mb != null && mb.GetType().Name.ToLower().Contains("npcinteractable"));
            }

            if (simpleShopOpener == null)
            {
                // 在场景里找名中含 SimpleShopOpener 的组件
                var all = FindObjectsOfType<MonoBehaviour>(includeInactive: true);
                simpleShopOpener = all.FirstOrDefault(mb => mb != null && mb.GetType().Name.ToLower().Contains("simpleshopopener"));
            }
        }

        [ContextMenu("应用 Definition 到 NPCInteractable")]
        public void ApplyDefinitionToTarget()
        {
            if (definition == null || npcInteractable == null) return;

            var target = npcInteractable;
            var tType = target.GetType();

            // 写入 npcId
            TrySetStringField(tType, target, IdNames, definition.npcId);
            // 写入 npcName
            TrySetStringField(tType, target, NameNames, definition.npcName);
            // 写入 dialogLines
            TrySetLinesField(tType, target, LinesNames, definition.dialogLines);

            // 写入功能按钮文本（如果你的 NPCInteractable 有类似字段）
            if (!string.IsNullOrEmpty(definition.functionButtonText))
                TrySetStringField(tType, target, FuncTextNames, definition.functionButtonText);

            // 功能按钮事件（UnityEvent）
            SetupFunctionEvent(tType, target);

#if UNITY_EDITOR
            // 标记目标组件已修改（可序列化保存）
            UnityEditor.EditorUtility.SetDirty(target);
#endif
        }

        private void SetupFunctionEvent(System.Type tType, object target)
        {
            // 找 UnityEvent 字段
            var evtField = tType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                                .FirstOrDefault(f => typeof(UnityEvent).IsAssignableFrom(f.FieldType) &&
                                                     EventNames.Any(n => string.Equals(f.Name, n, System.StringComparison.OrdinalIgnoreCase)));

            if (evtField == null) return;

            var ue = evtField.GetValue(target) as UnityEvent;
            if (ue == null) return;

#if UNITY_EDITOR
            // 清理旧的持久化监听器，避免重复
            var count = ue.GetPersistentEventCount();
            for (int i = count - 1; i >= 0; i--)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(ue, i);
            }
#endif

            if (definition.function == NPCFunctionType.None) return;

            if (definition.function == NPCFunctionType.OpenShop)
            {
                if (simpleShopOpener == null)
                {
                    var all = FindObjectsOfType<MonoBehaviour>(includeInactive: true);
                    simpleShopOpener = all.FirstOrDefault(mb => mb != null && mb.GetType().Name.ToLower().Contains("simpleshopopener"));
                }

                if (simpleShopOpener == null)
                {
                    Debug.LogWarning("[NPCFromDefinition] 场景里未找到 SimpleShopOpener，功能按钮将无效果。");
                    return;
                }

                var method = simpleShopOpener.GetType().GetMethod("Open",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                if (method == null)
                {
                    Debug.LogWarning("[NPCFromDefinition] SimpleShopOpener 未找到 Open() 方法。");
                    return;
                }

#if UNITY_EDITOR
                // 在编辑器里：添加【持久化】监听器（可被序列化保存）
                UnityEditor.Events.UnityEventTools.AddPersistentListener(ue, () =>
                {
                    method.Invoke(simpleShopOpener, null);
                });
#else
                // 运行时：添加普通监听器（不持久化）
                ue.AddListener(() =>
                {
                    method.Invoke(simpleShopOpener, null);
                });
#endif
            }
        }

        // —— 反射写入：字符串字段 —— //
        private bool TrySetStringField(System.Type tType, object target, string[] names, string value)
        {
            var f = tType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                         .FirstOrDefault(fi => names.Any(n => string.Equals(fi.Name, n, System.StringComparison.OrdinalIgnoreCase))
                                             && fi.FieldType == typeof(string));
            if (f != null)
            {
                f.SetValue(target, value ?? "");
                return true;
            }
            return false;
        }

        // —— 反射写入：台词数组/列表 —— //
        private bool TrySetLinesField(System.Type tType, object target, string[] names, List<string> lines)
        {
            var f = tType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                         .FirstOrDefault(fi => names.Any(n => string.Equals(fi.Name, n, System.StringComparison.OrdinalIgnoreCase)));
            if (f == null) return false;

            if (f.FieldType == typeof(string[]))
            {
                f.SetValue(target, (lines ?? new List<string>()).ToArray());
                return true;
            }
            if (f.FieldType == typeof(List<string>))
            {
                f.SetValue(target, new List<string>(lines ?? new List<string>()));
                return true;
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;
            Gizmos.color = definition.gizmoColor;
            Gizmos.DrawCube(transform.position + definition.defaultColliderCenter, definition.defaultColliderSize);
        }
#endif
    }
}
