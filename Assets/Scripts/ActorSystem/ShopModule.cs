using UnityEngine;
using System;
using System.Reflection;

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

            // 动态查找商店 UI
            if (_shopUI == null)
            {
                var shopUIs = GameObject.FindObjectsOfType<MonoBehaviour>();
                foreach (var ui in shopUIs)
                {
                    if (ui.gameObject.name.Contains("Shop") || ui.GetType().Name.Contains("Shop"))
                    {
                        _shopUI = ui;
                        if (showDebugLogs) Debug.Log($"[ShopModule] 运行时找到商店 UI：{ui.GetType().Name}");
                        break;
                    }
                }
            }

            if (_shopUI == null)
            {
                Debug.LogWarning("[ShopModule] 运行时未找到商店 UI，无法打开商店");
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
                var dialogUI = GameObject.FindObjectOfType<NPCDialogUI>();
                if (dialogUI != null && dialogUI.IsOpen)
                {
                    dialogUI.Close();
                    if (showDebugLogs) Debug.Log("[ShopModule] 已关闭对话面板");
                }
            }

            _isShopOpen = true;

            if (showDebugLogs) Debug.Log("[ShopModule] 商店已打开");
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
