using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using FarmGame.UI; // Added for IDialogSubject

namespace FarmGame.NPCSystem
{
    public class NPCInteractable : MonoBehaviour, IInteractable, IDialogSubject
    {
        [Header("UI 覆盖 (可选)")]
        public NPCDialogUI dialogUI;

        // IDialogSubject Implementation
        public string Name => GetDefinition()?.npcName ?? "Unknown";
        public List<string> DialogLines => GetDefinition()?.dialogLines;
        public string FunctionButtonText => GetDefinition()?.functionButtonText ?? "";
        public Transform SubjectTransform => transform;

        // Internal helper to get definition safely
        private NPCDefinition GetDefinition()
        {
            var fromDef = GetComponent<NPCFromDefinition>();
            return fromDef != null ? fromDef.definition : null;
        }

        // IInteractable implementation
        public string GetInteractPrompt()
        {
            var def = GetDefinition();
            return def != null ? $"按 [E] 对话：{def.npcName}" : "按 [E] 对话";
        }

        public Transform GetTransform() => transform;

        public void Interact(GameObject interactor)
        {
            var def = GetDefinition();
            if (def != null)
            {
                Debug.Log($"Interacting with {def.npcName} ({def.npcId})");
                
                NPCDialogUI ui = dialogUI;
                if (ui == null)
                {
                     ui = FindObjectOfType<NPCDialogUI>();
                }
                
                if (ui == null)
                {
                    Debug.LogWarning("No NPCDialogUI found in scene.");
                    return;
                }

                ui.Open(this);
            }
        }

        public void InvokeFunction()
        {
            var def = GetDefinition();
            if (def == null) return;

            if (def.function == NPCFunction.OpenShop)
            {
                var shopOpener = GetComponent<SimpleShopOpener>();
                if (shopOpener != null)
                {
                    shopOpener.OpenShop();
                }
                else
                {
                    Debug.LogWarning("NPC has OpenShop function but no SimpleShopOpener component.");
                }
            }
            else if (def.function == NPCFunction.Talk)
            {
                Debug.Log("Function button clicked: Talk (Dialog continues or ends)");
            }
        }
    }
}
