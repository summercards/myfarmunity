// Assets/Scripts/Interaction/PlayerInteractor.cs
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 玩家“交互”控制器：
/// - 在半径内搜索最近的 IInteractable，显示提示；按 E 执行交互；
/// - 对话打开时隐藏提示；
/// - 【新增】离开当前对话 NPC 超过 closeDistance 自动关闭对话框。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerInteractor : MonoBehaviour
{
    [Header("Detect")]
    [Tooltip("交互搜索半径（米）。")]
    public float searchRadius = 2.2f;

    [Tooltip("只检测这些层（建议为 NPC/可交互物体单独建一层，如 Interactable）。")]
    public LayerMask interactMask = ~0;

    [Header("UI (Optional)")]
    [Tooltip("用于显示提示字符串（可直接拖你项目里的 PickupHUD）。")]
    public PickupHUD hintHUD;

    [Tooltip("（可选）对话面板引用。若 NPC 未绑定，会回退使用这里的。")]
    public NPCDialogUI sharedDialogUI;

    [Header("Auto Close")]
    [Tooltip("对话打开时，若玩家与该 NPC 距离大于该值则自动关闭。")]
    public bool autoCloseWhenFar = true;
    public float closeDistance = 3.0f;

    // 当前锁定的可交互体
    private IInteractable _current;

    void Update()
    {
        // 允许在对话打开时进行“离开距离自动关闭”
        if (sharedDialogUI && sharedDialogUI.IsOpen && autoCloseWhenFar)
        {
            var npc = sharedDialogUI.CurrentNPC;
            if (npc != null)
            {
                float d = Vector3.Distance(transform.position, npc.transform.position);
                if (d > closeDistance)
                {
                    sharedDialogUI.Close();
                    // 不 return；继续走后续逻辑允许重新搜索交互体
                }
            }
        }

        // 面板打开期间，不再显示锁定提示（交互由面板按钮接管）
        if (sharedDialogUI && sharedDialogUI.IsOpen)
        {
            HideHint();
            return;
        }

        // 1) 查找最近的 IInteractable
        _current = FindClosestInteractable();

        // 2) 显示提示
        if (_current != null)
        {
            string tip = _current.GetInteractPrompt();
            if (!string.IsNullOrEmpty(tip)) ShowHint(tip);
        }
        else
        {
            HideHint();
        }

        // 3) 按键触发
        if (_current != null && PressedInteractKey())
        {
            // 允许 NPC 使用共享的 DialogUI（若其自身未拖拽）
            if (_current is NPCInteractable npc && npc.dialogUI == null && sharedDialogUI != null)
            {
                npc.dialogUI = sharedDialogUI;
            }

            _current.Interact(gameObject);
        }
    }

    private IInteractable FindClosestInteractable()
    {
        Collider[] cols = Physics.OverlapSphere(
            transform.position,
            searchRadius,
            interactMask,
            QueryTriggerInteraction.Collide
        );

        IInteractable best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < cols.Length; i++)
        {
            // 支持物体本体或父节点上实现接口
            var inter = cols[i].GetComponentInParent<IInteractable>();
            if (inter == null) continue;

            float d = Vector3.Distance(transform.position, inter.GetTransform().position);
            if (d < bestDist)
            {
                best = inter;
                bestDist = d;
            }
        }
        return best;
    }

    private bool PressedInteractKey()
    {
#if ENABLE_INPUT_SYSTEM && !UNITY_INPUT_SYSTEM_DISABLE
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
        return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton0);
    }

    private void ShowHint(string text)
    {
        if (hintHUD) hintHUD.Show(text);
    }

    private void HideHint()
    {
        if (hintHUD) hintHUD.Hide();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, searchRadius);
    }
}
