using UnityEngine;
using UnityEngine.EventSystems;

namespace HelloInputField
{
    public interface IVrInputField : ISelectHandler, IDeselectHandler
    {
        string TextValue { get; set; }

        void ProcessEvent(Event evt);

        void ActivateInputField();

        void DeactivateInputField();

        void FinishInput();

        void UpdateText();

        bool IsInteractive();
    }
}

