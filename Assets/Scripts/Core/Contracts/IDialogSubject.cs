using System.Collections.Generic;
using UnityEngine;

namespace FarmGame.Core.Contracts
{
    public interface IDialogSubject
    {
        string Name { get; }
        List<string> DialogLines { get; }
        string FunctionButtonText { get; }
        void InvokeFunction();
        Transform SubjectTransform { get; }
    }
}
