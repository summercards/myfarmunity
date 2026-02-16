using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// ShopModule: 负责"商店"能力模块
    /// Phase 7: 管理商店系统，与 ShopCatalogSO 交互。
    /// - 控制商店 UI 的打开/关闭
    /// - 记录商店解锁到 Memory
    /// - 修复编译错误：移除对不存在的 ShopCatalogSO.items 的引用
    /// </summary>
    public class ShopModule : ActorModule
    {
        [Header("Shop Configuration")]
        [Tooltip("商店商品目录（从 NPCDefinition 配置）")]
        public GameObject catalogObject;

        [Header("UI References")]
        [Tooltip("商店 UI 引用（动态查找）")]
        public GameObject shopUIObject;

        [Header("Options")]
        [Tooltip("打开商店后是否自动关闭对话框")]
        public bool closeDialogAfterShop = true;

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
            if (catalogObject == null)
            {
                Debug.LogWarning("[ShopModule] 商店目录对象为空");
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[ShopModule] 商店已初始化，使用对象：{catalogObject.name}");
                }
            }
        }

        /// <summary>
        /// Phase 7: 打开商店
        /// </summary>
        public void OpenShop()
        {
            if (catalogObject == null)
            {
                Debug.LogWarning("[ShopModule] 商店目录未配置，无法打开");
                return;
            }

            // Phase 7: 记录商店解锁到 Memory
            if (_memory != null)
            {
                _memory.RecordEvent(ActorMemory.EventType.ShopOpened);
            }

            // Phase 7: 动态查找并打开商店 UI
            if (shopUIObject == null)
            {
                shopUIObject = GameObject.FindGameObjectWithTag("ShopUI");
                if (shopUIObject != null)
                {
                    shopUIObject.SetActive(true);
                    _isShopOpen = true;
                    if (showDebugLogs) Debug.Log("[ShopModule] 商店已打开（动态查找）");
                }
                else
                {
                    Debug.LogWarning("[ShopModule] 场景中未找到 ShopUI（Tag: ShopUI），商店功能将不可用");
                }
            }
            else
            {
                // 如果有直接引用，直接使用
                shopUIObject.SetActive(true);
                _isShopOpen = true;
                if (showDebugLogs) Debug.Log("[ShopModule] 商店已打开（直接引用）");
            }
        }

        /// <summary>
        /// Phase 7: 关闭商店
        /// </summary>
        public void CloseShop()
        {
            _isShopOpen = false;

            // Phase 7: 动态关闭商店 UI
            if (shopUIObject == null)
            {
                shopUIObject = GameObject.FindGameObjectWithTag("ShopUI");
                if (shopUIObject != null)
                {
                    shopUIObject.SetActive(false);
                    if (showDebugLogs) Debug.Log("[ShopModule] 商店已关闭（动态查找）");
                }
            }
            else
            {
                if (shopUIObject != null) shopUIObject.SetActive(false);
                if (showDebugLogs) Debug.Log("[ShopModule] 商店已关闭（直接引用）");
            }
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
            
            // Phase 7: 动态查找商店 UI
            if (shopUIObject == null)
            {
                shopUIObject = GameObject.FindGameObjectWithTag("ShopUI");
                if (shopUIObject != null)
                {
                    Debug.Log("[ShopModule] 已自动找到 ShopUI");
                }
                else
                {
                    Debug.LogWarning("[ShopModule] 场景中未找到 ShopUI（Tag: ShopUI），商店功能将不可用");
                }
            }
        }

        /// <summary>
        /// Phase 7: 禁用模块时的清理
        /// </summary>
        protected override void OnDisabled()
        {
            shopUIObject = null;
            _isShopOpen = false;
            base.OnDisabled();
        }
    }
}
