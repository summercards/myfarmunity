// Assets/Scripts/Interaction/IInteractable.cs
using UnityEngine;

/// <summary>
/// 可交互物体的统一接口：用于在玩家附近显示提示与执行交互。
/// </summary>
public interface IInteractable
{
    /// <summary> 交互时显示在 HUD 上的提示文本，例如："按 [E] 对话：阿婆" </summary>
    string GetInteractPrompt();

    /// <summary> 执行交互（由玩家触发）。interactor 一般传玩家 GameObject。 </summary>
    void Interact(GameObject interactor);

    /// <summary> 该交互体的中心位置（用于计算距离/指示）。可返回 transform。 </summary>
    Transform GetTransform();
}
