using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ShopModule: 负责"商店"能力模块
    /// Phase 7: 管理商店系统，与 MiniShop 集成。
    /// - 调用 MiniShop.OpenFromDialog() 打开商店
    /// - 记录商店解锁到 Memory
    /// - 支持商店台词切换
    /// </summary>
    public class ShopModule : ActorModule
    {
        [Header("Shop Configuration")]
        [Tooltip("商店商品目录（从 NPCDefinition 配置）")]
        public ShopCatalogSO shopCatalog;

        [Header("UI References")]
        [Tooltip("商店 UI 引用（如果留空，会自动查找 MiniShop）")]
        public MiniShop shopUI;

        [Header("Options")]
        [Tooltip("打开商店后是否自动关闭对话框")]
        public bool closeDialogAfterShop = true;

        [Header("Shop Line")]
        [Tooltip("打开商店时显示的台词（如果留空，使用默认台词）")]
        public string shopOpenLine = "欢迎光临！需要点什么？";

        [Header("Debug")]
        [Tooltip("显示调试日志")]
        public bool showDebugLogs = true;

        // 内部状态
        private bool _isShopOpen;

        /// <summary>
        /// Phase 7: 初始化商店模块
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

            // 动态查找 MiniShop
            if (shopUI == null)
            {
                shopUI = FindObjectOfType<MiniShop>();
                if (shopUI != null)
                {
                    if (showDebugLogs) Debug.Log("[ShopModule] 已自动找到 MiniShop");
                }
                else
                {
                    Debug.LogWarning("[ShopModule] 场景中未找到 MiniShop，商店功能将不可用");
                }
            }
        }

        /// <summary>
        /// Phase 7: 打开商店（从 NPC 对话面板）
        /// </summary>
        public void OpenShop()
        {
            if (showDebugLogs) Debug.Log("[ShopModule] OpenShop() 被调用");

            // 查找 MiniShop
            if (shopUI == null)
            {
                shopUI = FindObjectOfType<MiniShop>();
                if (showDebugLogs)
                {
                    if (shopUI != null)
                        Debug.Log("[ShopModule] 运行时找到了 MiniShop");
                    else
                        Debug.LogWarning("[ShopModule] 运行时未找到 MiniShop");
                }
            }

            if (shopUI == null)
            {
                Debug.LogError("[ShopModule] MiniShop 未找到，无法打开商店！请确保场景中有 MiniShop 组件。");
                return;
            }

            // Phase 7: 记录商店解锁到 Memory
            if (_memory != null)
            {
                _memory.RecordEvent(ActorMemory.EventType.ShopOpened);
            }

            // Phase 7: 配置商店目录
            if (shopCatalog != null)
            {
                shopUI.catalog = shopCatalog;
                if (showDebugLogs) Debug.Log($"[ShopModule] 已配置商店目录：{shopCatalog.name}");
            }

            // Phase 7: 调用 MiniShop 的打开方法
            if (!string.IsNullOrEmpty(shopOpenLine))
            {
                shopUI.shopOpenLine = shopOpenLine;
                if (showDebugLogs) Debug.Log($"[ShopModule] 已设置商店台词：{shopOpenLine}");
            }
            
            shopUI.OpenFromDialog();
            _isShopOpen = true;

            if (showDebugLogs) Debug.Log("[ShopModule] 商店已打开");
        }

        /// <summary>
        /// Phase 7: 关闭商店
        /// </summary>
        public void CloseShop()
        {
            if (shopUI == null)
            {
                Debug.LogWarning("[ShopModule] MiniShop 未找到，无法关闭商店");
                return;
            }

            shopUI.Close();
            _isShopOpen = false;

            if (showDebugLogs) Debug.Log("[ShopModule] 商店已关闭");
        }

        /// <summary>
        /// Phase 7: 检查商店是否打开
        /// </summary>
        public bool IsShopOpen()
        {
            return _isShopOpen;
        }

        /// <summary>
        /// Phase 7: 启用模块
        /// </summary>
        public override void Enable()
        {
            base.Enable();
            InitializeShop();
            Debug.Log("[ShopModule] 已启用");
        }

        /// <summary>
        /// Phase 7: 禁用模块
        /// </summary>
        public override void Disable()
        {
            CloseShop();
            base.Disable();
            Debug.Log("[ShopModule] 已禁用");
        }

        /// <summary>
        /// Phase 7: 启用模块时的初始化
        /// </summary>
        protected override void OnEnabled()
        {
            base.OnEnabled();
            
            // Phase 7: 动态查找 MiniShop
            if (shopUI == null)
            {
                shopUI = FindObjectOfType<MiniShop>();
                if (shopUI != null)
                {
                    Debug.Log("[ShopModule] 已自动找到 MiniShop");
                }
                else
                {
                    Debug.LogWarning("[ShopModule] 场景中未找到 MiniShop，商店功能将不可用");
                }
            }
        }

        /// <summary>
        /// Phase 7: 禁用模块时的清理
        /// </summary>
        protected override void OnDisabled()
        {
            shopUI = null;
            _isShopOpen = false;
            base.OnDisabled();
        }
    }
}
