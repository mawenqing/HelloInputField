using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace HelloInputField
{
    public class VrInputField :
        Selectable,
        IVrInputField,
        ICanvasElement,
        IUpdateSelectedHandler,
        ICaretNavigator,
        IInputFieldController,
        IDragHandler
    {

#pragma warning disable CS0649

        [SerializeField]
        private Text _textComponent;

        [SerializeField]
        private GameObject _placeHolder;

        [SerializeField]
        private Color _selectionColor = new Color(168f / 255f, 206f / 255f, 255f / 255f, 192f / 255f);

#pragma warning restore CS0649

        private TextGenerator _textGenerator;

        private VrInputFieldImpl Impl { get; set; }

        public string TextValue
        {
            get { return Impl.TextValue; }
            set { Impl.TextValue = value; }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            Impl.OnSelect(eventData);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            Impl.OnDeselect(eventData);
        }

        public void ActivateInputField()
        {
            Impl.ActivateInputField();
            RegisterTextComponentDirtyCallbacks(_textComponent);
        }

        public void DeactivateInputField()
        {
            Impl.DeactivateInputField();
            UnregisterTextComponentDirtyCallbacks(_textComponent);
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
        }

        public bool IsInteractive()
        {
            return Impl.IsInteractive();
        }

        public void FinishInput()
        {
            Impl.FinishInput();
        }

        public void ProcessEvent(Event evt)
        {
            Impl.ProcessEvent(evt);
        }

        protected override void Start()
        {
            base.Start();
            _textGenerator = new TextGenerator();
            Impl = new VrInputFieldImpl(CreateCaret(),
                new UnityTextProcessor(new System.Text.StringBuilder(), this),
                this);
        }

        private ICaret CreateCaret()
        {
            GameObject caret = new GameObject(transform.name + " Caret", typeof(DefaultCaret));
            caret.hideFlags = HideFlags.DontSave;
            caret.transform.SetParent(_textComponent.transform.parent);

            return caret.GetComponent<ICaret>();
        }

        public void RegisterTextComponentDirtyCallbacks(Text textComponent)
        {
            Assert.IsNotNull(textComponent);

            // register for a notification when text component update its vertices.
            // this is happening as text component's or its parent's RectTransform changes.
            // should update the text and caret following the text component.
            textComponent.RegisterDirtyVerticesCallback(MarkGeometryAsDirty);
        }

        public void UnregisterTextComponentDirtyCallbacks(Text textComponent)
        {
            Assert.IsNotNull(textComponent);
            textComponent.UnregisterDirtyVerticesCallback(MarkGeometryAsDirty);
        }

        private void MarkGeometryAsDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying || UnityEditor.PrefabUtility.GetPrefabObject(gameObject) != null)
                return;
#endif
            // request update graphic by adding this ICanvasElement to CanvasUpdateRegistry's graphic rebuilding queue.
            // Rebuild() will be called on next canvas update. 
            CanvasUpdateRegistry.RegisterCanvasElementForGraphicRebuild(this);
        }

        public void Rebuild(CanvasUpdate executing)
        {
            switch (executing)
            {
                case CanvasUpdate.LatePreRender:
                    Color color;
                    Impl.Caret.Rebuild(CalculateCaretDrawRect(RoundedTextPivotLocalPosition(_textComponent), out color), color);
                    break;
            }
        }

        private Rect CalculateCaretDrawRect(Vector2 offset, out Color color)
        {
            int index = Impl.Caret.GetIndex() - Impl.DrawStart;
            int selectionIndex = Mathf.Min(Mathf.Max(Impl.Caret.GetSelectionIndex() - Impl.DrawStart, 0), GetDisplayedTextLength());
            bool hasSelection = index != selectionIndex;
            Vector2 cursorPos = CursorPosition(_textComponent.cachedTextGenerator, index);
            Vector2 selectionPos = CursorPosition(_textComponent.cachedTextGenerator, selectionIndex);

            float height = TextHeight(_textComponent);

            color = hasSelection ? _selectionColor : Color.black;

            if (index > selectionIndex)
            {
                Vector2 temp = selectionPos;
                selectionPos = cursorPos;
                cursorPos = temp;
            }

            return new Rect(cursorPos.x + offset.x,
                cursorPos.y - height + offset.y,
                hasSelection ? Mathf.Abs(selectionPos.x - cursorPos.x) : 1,
                height);
        }

        public void LayoutComplete()
        {
        }

        public void GraphicUpdateComplete()
        {
        }

        public void UpdateText()
        {
            Impl.UpdateText();
        }

        public void PopulateText(string text) {
            _textGenerator.PopulateWithErrors(
                text,
                _textComponent.GetGenerationSettings(_textComponent.rectTransform.rect.size),
                gameObject
                );

            MarkGeometryAsDirty();
        }

        private float TextHeight(Text textComponent)
        {
            return textComponent.cachedTextGenerator.lineCount > 0 ?
                textComponent.cachedTextGenerator.lines[0].height : 0;
        }

        private Vector2 RoundedTextPivotLocalPosition(Text textComponent)
        {
            Rect inputRect = textComponent.rectTransform.rect;
            Vector2 textAnchorPivot = Text.GetTextAnchorPivot(textComponent.alignment);
            Vector2 refPoint = Vector2.zero;
            refPoint.x = Mathf.Lerp(inputRect.xMin, inputRect.xMax, textAnchorPivot.x);
            refPoint.y = Mathf.Lerp(inputRect.yMin, inputRect.yMax, textAnchorPivot.y);

            Vector2 pixelPerfectRefPoint = textComponent.PixelAdjustPoint(refPoint);

            Vector2 rounddedRefPoint = pixelPerfectRefPoint - refPoint + Vector2.Scale(inputRect.size, textAnchorPivot);
            rounddedRefPoint.x = rounddedRefPoint.x - Mathf.Floor(0.5f + rounddedRefPoint.x);
            rounddedRefPoint.y = rounddedRefPoint.y - Mathf.Floor(0.5f + rounddedRefPoint.y);

            return rounddedRefPoint;
        }

        private Vector2 CursorPosition(TextGenerator gen, int index)
        {
            index = Mathf.Clamp(index, 0, gen.characterCount - 1);
            UICharInfo cursorChar = gen.characters[index];
            return new Vector2(cursorChar.cursorPos.x, cursorChar.cursorPos.y);
        }

#if UNITY_EDITOR
        Event processingEvent = new Event();
        public void OnUpdateSelected(BaseEventData eventData)
        {
            if (!IsInteractive())
            {
                return;
            }

            while (Event.PopEvent(processingEvent))
            {
                if (processingEvent.rawType == EventType.KeyDown)
                {
                    ProcessEvent(processingEvent);
                    continue;
                }

                switch (processingEvent.type)
                {
                    case EventType.ValidateCommand:
                    case EventType.ExecuteCommand:
                        switch (processingEvent.commandName)
                        {
                            case "SelectAll":
                                processingEvent.keyCode = KeyCode.A;
                                processingEvent.modifiers = EventModifiers.Control;
                                ProcessEvent(processingEvent);
                                break;
                        }
                        break;
                }
            }

            eventData.Use();
        }

#else
       public void OnUpdateSelected(BaseEventData eventData){}
#endif

        public void MoveCaretTo(int index, bool withSelection)
        {
            index = Mathf.Clamp(index, 0, TextValue.Length);
            Impl.Caret.MoveTo(index, withSelection);
            UpdateText();
        }

        public Vector2 DisplayRectExtents()
        {
            return _textComponent.cachedTextGenerator.rectExtents.size;
        }

        public void UpdateDisplayText(string text)
        {
            _textComponent.text = text;
            _placeHolder.SetActive(text.Equals(string.Empty));
        }

        public void OnEndInput(string text)
        {
            // TODO: add event.
        }

        public float GetCharacterWidth(int index)
        {
            Assert.IsTrue(index >= 0 && index < _textGenerator.characters.Count);
            return _textGenerator.characters[index].charWidth;
        }

        public int GetDisplayedTextLength()
        {
            return _textComponent.text.Length;
        }

        public void RegisterTextComponentDirtyCallbacks()
        {
            RegisterTextComponentDirtyCallbacks(_textComponent);
        }

        Vector2 LocalMousePosition(PointerEventData eventData)
        {
            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_textComponent.rectTransform,
                eventData.position, eventData.pressEventCamera, out localMousePos);

            return localMousePos;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!AcceptPointerEvent(eventData))
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            base.OnPointerDown(eventData);

            Vector2 localMousePos = LocalMousePosition(eventData);
            MoveCaretTo(
                GetCharacterIndexFromPosition(
                    localMousePos, _textComponent.cachedTextGenerator, _textComponent) + Impl.DrawStart, false);

            MarkGeometryAsDirty();
            eventData.Use();
        }

        private bool AcceptPointerEvent(PointerEventData eventData)
        {
            return IsActive() &&
                IsInteractable() &&
                eventData.button == PointerEventData.InputButton.Left;
        }

        private int GetCharacterIndexFromPosition(Vector2 pos, TextGenerator gen, Text textComponent) {
            if(gen.lineCount == 0)
            {
                return 0;
            }

            int startCharIndex = gen.lines[0].startCharIdx;
            int endCharIndex = GetLineEndPosition(gen, 0);

            for (int i = startCharIndex; i < endCharIndex; i++)
            {
                if (i >= gen.characterCountVisible)
                    break;

                UICharInfo charInfo = gen.characters[i];
                Vector2 charPos = charInfo.cursorPos / textComponent.pixelsPerUnit;

                float distToCharStart = pos.x - charPos.x;
                float distToCharEnd = charPos.x + (charInfo.charWidth / textComponent.pixelsPerUnit) - pos.x;
                if (distToCharStart < distToCharEnd)
                    return i;
            }

            return endCharIndex;
        }

        private int GetLineEndPosition(TextGenerator gen, int line)
        {
            line = Mathf.Max(line, 0);
            if (line + 1 < gen.lines.Count)
                return gen.lines[line + 1].startCharIdx - 1;
            return gen.characterCountVisible;
        }

#region Drag to highlight

        public void OnDrag(PointerEventData eventData)
        {
            if (!AcceptPointerEvent(eventData))
            {
                return;
            }

            Vector2 localMousePos = LocalMousePosition(eventData);
            Rect rect = _textComponent.rectTransform.rect;
            if (localMousePos.x < rect.xMin)
            {
                MoveCaretTo(Impl.Caret.GetIndex() - 1, true);
                UpdateText();
            }
            else if (localMousePos.x > rect.xMax)
            {
                MoveCaretTo(Impl.Caret.GetIndex() + 1, true);
                UpdateText();
            }
            else
            {
                MoveCaretTo(GetCharacterIndexFromPosition(
                        localMousePos, _textComponent.cachedTextGenerator, _textComponent) + Impl.DrawStart, true);

                MarkGeometryAsDirty();
            }

            eventData.Use();
        }

#endregion
    }
}

