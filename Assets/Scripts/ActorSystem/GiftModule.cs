using UnityEngine;

namespace FarmGame.ActorSystem
{
    /// <summary>
    /// GiftModule: 负责"礼物"能力模块
    /// Phase 7: 段理礼物系统，与 ActorMemory 交互。
    /// - 接收玩家赠送的物品
    /// - 更新好感度
    /// - 记录礼物历史
    /// </summary>
    public class GiftModule : ActorModule
    {
        [Header("Gift Configuration")]
        [Tooltip("可接收的礼物类型（未来可扩展）")]
        public string[] acceptedItemTypes = new string[] { "Common", "Rare", "Legendary" };

        [Header("Friendship Settings")]
        [Tooltip("每次赠送增加的好感度")]
        [Range(1, 20)] public int friendshipGainPerGift = 5;

        [Header("UI References")]
        [Tooltip("礼物通知 UI（可选）")]
        public GameObject giftNotificationUI;

        // 内部状态
        private int _totalGiftsReceived;

        /// <summary>
        /// Phase 7: 初始化礼物系统
        /// </summary>
        public void InitializeGiftSystem()
        {
            _totalGiftsReceived = 0;
            Debug.Log($"[GiftModule] 礼物系统已初始化，可接受的类型：{string.Join(", ", acceptedItemTypes)}");
        }

        /// <summary>
        /// Phase 7: 接收玩家赠送的礼物
        /// </summary>
        public void ReceiveGift(string itemType, string itemName, int quantity = 1)
        {
            // Phase 7: 检查物品类型是否可接受
            bool isAccepted = false;
            foreach (string type in acceptedItemTypes)
            {
                if (itemType == type)
                {
                    isAccepted = true;
                    break;
                }
            }

            if (!isAccepted)
            {
                Debug.LogWarning($"[GiftModule] 物品类型 '{itemType}' 不被接受");
                return;
            }

            // Phase 7: 计算总好感度增益
            int totalGain = friendshipGainPerGift * quantity;

            // Phase 7: 更新记忆
            if (_memory != null)
            {
                _memory.AddFriendship(totalGain);
                _totalGiftsReceived += quantity;

                Debug.Log($"[GiftModule] 收到礼物：{itemName} x{quantity}，好感度 +{totalGain}");
            }

            // Phase 7: 通知 UI
            ShowGiftNotification(itemName, quantity);
        }

        /// <summary>
        /// Phase 7: 获取收到礼物总数
        /// </summary>
        public int GetTotalGiftsReceived()
        {
            return _totalGiftsReceived;
        }

        /// <summary>
        /// Phase 7: 启用模块
        /// </summary>
        public override void Enable()
        {
            base.Enable();
            InitializeGiftSystem();
        }

        /// <summary>
        /// Phase 7: 禁用模块
        /// </summary>
        public override void Disable()
        {
            base.Disable();
            Debug.Log("[GiftModule] 礼物模块已禁用");
        }

        /// <summary>
        /// Phase 7: 内部通知方法
        /// </summary>
        private void ShowGiftNotification(string itemName, int quantity)
        {
            if (giftNotificationUI != null)
            {
                // Phase 7: 显示礼物通知
                // TODO: 实现通知逻辑
                Debug.Log($"[GiftModule] 礼物通知：{itemName} x{quantity}");
            }
            else
            {
                Debug.Log("[GiftModule] 礼物通知 UI 未设置");
            }
        }

        /// <summary>
        /// Phase 7: 启用模块时的初始化
        /// </summary>
        protected override void OnEnabled()
        {
            base.OnEnabled();
        }

        /// <summary>
        /// Phase 7: 禁用模块时的清理
        /// </summary>
        protected override void OnDisabled()
        {
            giftNotificationUI = null;
        }
    }
}
