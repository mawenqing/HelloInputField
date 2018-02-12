﻿using UnityEngine;
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
        IInputFieldController,
        IDragHandler
    {
        enum LineType
        {
            SingleLine,
            MultiLine,
        }

#pragma warning disable CS0649

        [SerializeField]
        private Text _textComponent;

        [SerializeField]
        private GameObject _placeHolder;

        [SerializeField]
        private Color _selectionColor = new Color(168f / 255f, 206f / 255f, 255f / 255f, 192f / 255f);

        [SerializeField]
        private LineType _lineType = LineType.SingleLine;

#pragma warning restore CS0649

        private AbstractInputField Impl { get; set; }

        private ITextComponentWrapper _editableText;

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
            if (Impl == null)
            {
                return false;
            }

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
            _editableText = new EditableText(_textComponent, new TextGenerator());

            var caret = CreateCaret();

            if (_lineType.Equals(LineType.SingleLine))
            {
                Impl = new SingleLineInputField(caret,
                    new SingleLineInputProcessor(new System.Text.StringBuilder(), new CaretNavigator(caret, _editableText, this)),
                    this,
                    _editableText);
            }
            else
            {
                Impl = new MultiLineInputField(caret,
                    new BaseTextProcessor(new System.Text.StringBuilder(), new CaretNavigator(caret, _editableText, this)),
                    this,
                    _editableText);
            }
        }

        private ICaret CreateCaret()
        {
            GameObject caretObj = new GameObject(transform.name + " Caret", typeof(DefaultCaret));
            caretObj.hideFlags = HideFlags.DontSave;
            caretObj.transform.SetParent(_textComponent.transform.parent);
            ICaret caret = caretObj.GetComponent<ICaret>();
            caret.InputFieldController = this;
            return caret;
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

        public void MarkGeometryAsDirty()
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
                    Impl.DrawCaretOrSelection(_editableText);
                    break;
            }
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

        public void PopulateText(string text)
        {
            _editableText.Populate(text, gameObject);
            MarkGeometryAsDirty();
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

        public void UpdateDisplayText(string text)
        {
            _editableText.UpdateDisplayText(text);
            _placeHolder.SetActive(text.Equals(string.Empty));
        }

        public void OnEndInput(string text)
        {
            // TODO: add event.
        }

        public void RegisterTextComponentDirtyCallbacks()
        {
            RegisterTextComponentDirtyCallbacks(_textComponent);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!AcceptPointerEvent(eventData))
            {
                return;
            }

            base.OnPointerDown(eventData);
            Impl.OnPointerDown(eventData);
            MarkGeometryAsDirty();
        }

        // prevent pressed transition animation.
        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            if (state.Equals(SelectionState.Pressed) && IsInteractive())
            {
                state = SelectionState.Highlighted;
            }
            base.DoStateTransition(state, instant);
        }

        private bool AcceptPointerEvent(PointerEventData eventData)
        {
            return IsActive() &&
                IsInteractable() &&
                eventData.button == PointerEventData.InputButton.Left;
        }

        #region Drag to highlight

        public void OnDrag(PointerEventData eventData)
        {
            if (!AcceptPointerEvent(eventData))
            {
                return;
            }

            Impl.OnDrag(eventData);
        }

        #endregion
    }
}
