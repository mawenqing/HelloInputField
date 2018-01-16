using UnityEngine;

namespace HelloInputField
{
    public interface IInputFieldController
    {
        void PopulateText(string text);

        Vector2 DisplayRectExtents();

        void UpdateDisplayText(string text);

        void OnEndInput(string text);

        float GetCharacterWidth(int index);

        int GetDisplayedTextLength();

    }
}
