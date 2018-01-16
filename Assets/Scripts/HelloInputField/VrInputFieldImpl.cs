using UnityEngine;
using UnityEngine.EventSystems;

namespace HelloInputField
{
    public class VrInputFieldImpl : IVrInputField
    {
        private ICaret _caret;
        private IInputEventProcessor _inputEventProcessor;
        private IInputFieldController _controller;
        private int _drawStart, _drawEnd;

        private bool _interactive = false;

        public string TextValue
        {
            get { return _inputEventProcessor.TextValue; }
            set
            {
                _inputEventProcessor.TextValue = value;
                _inputEventProcessor.SelectAll();
                UpdateText();
            }
        }

        public int DrawStart { get { return _drawStart; } }

        public ICaret Caret
        {
            get { return _caret; }
            set
            {
                if (_caret == value)
                {
                    return;
                }

                _caret.DestroyCaret();
                _caret = value;
            }
        }

        public VrInputFieldImpl(ICaret caret,
            IInputEventProcessor inputEventProcessor,
            IInputFieldController controller)
        {
            _controller = controller;
            _caret = caret;
            _inputEventProcessor = inputEventProcessor;
        }

        public void ProcessEvent(Event evt)
        {
            if (!IsInteractive())
            {
                return;
            }

            if (!_inputEventProcessor.ProcessEvent(evt, Caret.GetIndex(), Caret.GetSelectionIndex()))
            {
                DeactivateInputField();
            }
        }

        public void ActivateInputField()
        {
            UpdateText();
            _interactive = true;
            Caret.ActivateCaret();
        }

        public void DeactivateInputField()
        {
            _interactive = false;
            Caret.DeactivateCaret();
        }

        public void FinishInput()
        {
            _controller.OnEndInput(TextValue);
        }

        public void UpdateText()
        {
            _controller.PopulateText(TextValue);

            // truncate text to display within the bounds of text rect.
            Vector2 textRectExtents = _controller.DisplayRectExtents();

            float width = 0;
            if (_caret.GetIndex() > _drawEnd || (_caret.GetIndex() == TextValue.Length && _drawStart > 0))
            {
                _drawEnd = _caret.GetIndex();
                _drawStart = _drawEnd - 1;
                while (width < textRectExtents.x && _drawStart >= 0)
                {
                    width += _controller.GetCharacterWidth(_drawStart--);
                }

                if (width >= textRectExtents.x)
                {
                    _drawStart++;
                }
            }
            else
            {
                if (_caret.GetIndex() < _drawStart)
                {
                    _drawStart = _caret.GetIndex();
                }

                _drawEnd = _drawStart;
                while (width < textRectExtents.x && _drawEnd < TextValue.Length)
                {
                    width += _controller.GetCharacterWidth(_drawEnd++);
                }

                if (width >= textRectExtents.x)
                {
                    _drawEnd--;
                }
            }

            _drawStart = Mathf.Clamp(_drawStart, 0, TextValue.Length);
            _drawEnd = Mathf.Clamp(_drawEnd, 0, TextValue.Length);
            string processed = TextValue.Substring(_drawStart, _drawEnd - _drawStart);
            _controller.UpdateDisplayText(processed);
        }

        public bool IsInteractive()
        {
            return _interactive;
        }

        public void OnSelect(BaseEventData eventData)
        {
            ActivateInputField();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (IsInteractive())
            {
                DeactivateInputField();
            }
        }
    }
}
