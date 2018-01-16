using UnityEngine;

namespace HelloInputField
{
    public interface IInputEventProcessor
    {
        bool ProcessEvent(Event keyEvent, int caretIndex, int selectionIndex);

        string TextValue { set; get; }

        void SelectAll();
    }
}
