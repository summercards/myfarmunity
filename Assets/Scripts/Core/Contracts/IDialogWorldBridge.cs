using UnityEngine;

namespace FarmGame.Core.Contracts
{
    public interface IDialogWorldBridge
    {
        void Bind(Transform anchor);
        void BindToSubject(IDialogSubject subject);
        void ShowStandalone(Transform npcRootOrAnchor, string line);
        void EndStandalone();
    }
}
