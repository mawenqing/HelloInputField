using UnityEngine;

namespace HelloInputField
{
    public interface ICaret
    {
        void ActivateCaret();

        void DeactivateCaret();

        void Rebuild(Rect drawRect, Color color);

        int GetIndex();

        int GetSelectionIndex();

        void DestroyCaret();

        bool IsVisible();

        void MoveTo(int index, bool withSelection);
    }
}
