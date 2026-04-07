using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.Serialization;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ShopModule: 负责"商店"能力模块
    /// 集成 Panel_Shop（商店 UI）
    /// </summary>
    public class ShopModule : ActorModule
    {
        [Header("Shop Configuration")]
        [Tooltip("商店商品目录（从 NPCDefinition 配置）")]
        public ShopCatalogSO shopCatalog;

        [Header("Options")]
        [Tooltip("打开商店后是否自动关闭对话框")]
        public bool closeDialogAfterShop = true;

        [Header("UI Refs (Optional)")]
        [Tooltip("优先使用商店 UI（可选，未填写时会走 RuntimeRefs）。")]
        [FormerlySerializedAs("simpleShopUI")]
        public MonoBehaviour primaryShopUI;
        [Tooltip("作为回退使用的商店 UI（可选，未填写时会走 RuntimeRefs）。")]
        [FormerlySerializedAs("miniShopUI")]
        public MonoBehaviour fallbackShopUI;

        [Header("Debug")]
        [Tooltip("显示调试日志")]
        public bool showDebugLogs = true;

        // 内部状态
        private bool _isShopOpen;
        private MonoBehaviour _shopUI;

        /// <summary>
        /// 初始化商店模块
        /// </summary>
        public void InitializeShop()
        {
            if (shopCatalog == null)
            {
                Debug.LogWarning("[ShopModule] 商店目录未配置");
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[ShopModule] 商店已初始化，目录：{shopCatalog.name}");
                }
            }
        }

        /// <summary>
        /// 打开商店
        /// </summary>
        public void OpenShop()
        {
            if (showDebugLogs) Debug.Log("[ShopModule] OpenShop() 被调用");

            if (!TryResolveShopUI())
            {
                Debug.LogWarning("[ShopModule] 未绑定商店 UI，无法打开商店");
                return;
            }

            // 记录到 Memory
            if (_memory != null)
            {
                _memory.RecordEvent(ActorMemory.EventType.ShopOpened);
            }

            // 配置商店目录（通过反射）
            if (shopCatalog != null)
            {
                var catalogField = _shopUI.GetType().GetField("catalog", BindingFlags.Public | BindingFlags.Instance);
                if (catalogField != null)
                {
                    catalogField.SetValue(_shopUI, shopCatalog);
                    if (showDebugLogs) Debug.Log($"[ShopModule] 已配置商店目录：{shopCatalog.name}");
                }
            }

            // 调用 Open 方法（通过反射）
            var openMethod = _shopUI.GetType().GetMethod("Open", BindingFlags.Public | BindingFlags.Instance);
            if (openMethod != null)
            {
                openMethod.Invoke(_shopUI, null);
                if (showDebugLogs) Debug.Log($"[ShopModule] 调用 {_shopUI.GetType().Name}.Open()");
            }
            else
            {
                Debug.LogError($"[ShopModule] {_shopUI.GetType().Name} 没有 Open() 方法");
                return;
            }

            // 关闭对话框（如果需要）
            if (closeDialogAfterShop)
            {
                var dialogUI = RuntimeRefs.DialogUIContract;
                if (dialogUI != null && dialogUI.IsOpen)
                {
                    dialogUI.Close();
                    if (showDebugLogs) Debug.Log("[ShopModule] 已关闭对话面板");
                }
            }

            _isShopOpen = true;

            if (showDebugLogs) Debug.Log("[ShopModule] 商店已打开");
        }

        private bool TryResolveShopUI()
        {
            if (_shopUI != null)
            {
                if (IsRuntimeSceneBehaviour(_shopUI))
                {
                    return true;
                }

                if (showDebugLogs)
                {
                    Debug.LogWarning("[ShopModule] 缓存的商店 UI 指向 Prefab 资源，已忽略并重新解析。");
                }

                _shopUI = null;
            }

            if (primaryShopUI != null && !IsRuntimeSceneBehaviour(primaryShopUI))
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning("[ShopModule] primaryShopUI 指向 Prefab 资源，运行时将改用场景实例。");
                }

                primaryShopUI = null;
            }

            if (primaryShopUI == null)
            {
                primaryShopUI = RuntimeRefs.SimpleShopUI;
            }

            if (primaryShopUI != null)
            {
                _shopUI = primaryShopUI;
                if (showDebugLogs) Debug.Log($"[ShopModule] 使用主商店 UI: {_shopUI.GetType().Name}");
                return true;
            }

            if (fallbackShopUI != null && !IsRuntimeSceneBehaviour(fallbackShopUI))
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning("[ShopModule] fallbackShopUI 指向 Prefab 资源，运行时将改用场景实例。");
                }

                fallbackShopUI = null;
            }

            if (fallbackShopUI == null)
            {
                fallbackShopUI = RuntimeRefs.MiniShopUI;
            }

            if (fallbackShopUI != null)
            {
                _shopUI = fallbackShopUI;
                if (showDebugLogs) Debug.Log($"[ShopModule] 使用回退商店 UI: {_shopUI.GetType().Name}");
                return true;
            }

            return false;
        }

        private static bool IsRuntimeSceneBehaviour(MonoBehaviour behaviour)
        {
            return behaviour != null && behaviour.gameObject.scene.IsValid();
        }

        /// <summary>
        /// 关闭商店
        /// </summary>
        public void CloseShop()
        {
            if (_shopUI == null)
            {
                Debug.LogWarning("[ShopModule] 商店 UI 未找到，无法关闭商店");
                return;
            }

            // 调用 Close 方法（通过反射）
            var closeMethod = _shopUI.GetType().GetMethod("Close", BindingFlags.Public | BindingFlags.Instance);
            if (closeMethod != null)
            {
                closeMethod.Invoke(_shopUI, null);
                if (showDebugLogs) Debug.Log($"[ShopModule] 调用 {_shopUI.GetType().Name}.Close()");
            }

            _isShopOpen = false;

            if (showDebugLogs) Debug.Log("[ShopModule] 商店已关闭");
        }

        /// <summary>
        /// 检查商店是否打开
        /// </summary>
        public bool IsShopOpen()
        {
            return _isShopOpen;
        }

        /// <summary>
        /// 启用模块
        /// </summary>
        public override void Enable()
        {
            base.Enable();
            InitializeShop();
            Debug.Log("[ShopModule] 已启用");
        }

        /// <summary>
        /// 禁用模块
        /// </summary>
        public override void Disable()
        {
            CloseShop();
            base.Disable();
            Debug.Log("[ShopModule] 已禁用");
        }

        /// <summary>
        /// 启用模块时的初始化
        /// </summary>
        protected override void OnEnabled()
        {
            base.OnEnabled();
        }

        /// <summary>
        /// 禁用模块时的清理
        /// </summary>
        protected override void OnDisabled()
        {
            _shopUI = null;
            _isShopOpen = false;
            base.OnDisabled();
        }
    }
}
