using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using System.Collections;

namespace HelloInputField
{
	[RequireComponent (typeof(LayoutElement))]
	[RequireComponent (typeof(CanvasRenderer))]
	[RequireComponent (typeof(RectTransform))]
	public class DefaultCaret : MonoBehaviour, ICaret
	{
		private Mesh _mesh;
		private UIVertex[] _verts;
		private int _selectionAnchorIndex = 0;
		private int _index = 0;

        private Rect _drawRect = Rect.zero;
        private bool _isVisible;
        private Color _color;

        private Coroutine _blinkCoroutine;

        private void Start()
        {
            GetComponent<LayoutElement>().ignoreLayout = true;
            AlignPosition(transform.parent.GetChild(0).GetComponent<RectTransform>(), GetComponent<RectTransform>());
			CaretRenderer.SetMaterial (Graphic.defaultGraphicMaterial, Texture2D.whiteTexture);
            gameObject.layer = transform.parent.gameObject.layer;
            transform.SetAsFirstSibling();

			_verts = CreateVerts (Color.black);
        }

        private void OnDisable()
        {
            DestroyCaret();
        }

        private void OnEnable()
        {
            _mesh = new Mesh();
            CaretRenderer.SetMaterial(Graphic.defaultGraphicMaterial, Texture2D.whiteTexture);
        }

        public CanvasRenderer CaretRenderer
        {
            get { return GetComponent<CanvasRenderer> (); }
		}

		public void ActivateCaret ()
		{
            _isVisible = true;
            _blinkCoroutine = StartCoroutine(CaretBlink());
        }

		public void DeactivateCaret ()
		{
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
            _isVisible = false;
            Rebuild(_drawRect, _color);
		}

		public void DestroyCaret ()
		{
            CaretRenderer.Clear();
            DestroyImmediate(_mesh);
            _mesh = null;
		}

		private bool HasSelection ()
		{
			return _index != _selectionAnchorIndex;
		}

		public int GetIndex ()
		{
			return _index;
		}

		public void MoveTo (int index, bool withSelection)
		{
			_selectionAnchorIndex = withSelection ? _selectionAnchorIndex : index;
			_index = index;
		}

		private UIVertex[] CreateVerts (Color color)
		{
			UIVertex[] verts = new UIVertex[4];
			for (int i = 0; i < verts.Length; i++) {
				verts [i] = UIVertex.simpleVert;
				verts [i].color = color;
				verts [i].uv0 = Vector2.zero;
			}

			return verts;
		}

		private void SetupCursorVertsPositions (ref UIVertex[] verts, Rect drawRect)
		{
            Assert.IsNotNull (verts);
			Assert.IsTrue (verts.Length >= 4);

			verts [0].position = new Vector3 (drawRect.xMin, drawRect.yMin, 0.0f);
			verts [1].position = new Vector3 (drawRect.xMax, drawRect.yMin, 0.0f);
			verts [2].position = new Vector3 (drawRect.xMax, drawRect.yMax, 0.0f);
			verts [3].position = new Vector3 (drawRect.xMin, drawRect.yMax, 0.0f);
		}

		public bool IsVisible ()
		{
            return _isVisible;
		}

        public void Rebuild(Rect drawRect, Color color)
        {
            _drawRect = drawRect;
            _color = color;

            using (var helper = new VertexHelper())
            {
                if (IsVisible())
                {
                    GenerateCursorOrSelection(helper, ref _verts, drawRect, color);
                }
                helper.FillMesh(_mesh);
            }

            CaretRenderer.SetMesh(_mesh);
        }

        private void GenerateCursorOrSelection(VertexHelper helper, ref UIVertex[] verts, Rect drawRect, Color color)
        {
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i].color = color;
            }

            SetupCursorVertsPositions(ref verts, drawRect);
            helper.AddUIVertexQuad(verts);
        }

        private void AlignPosition(RectTransform textTransform, RectTransform caretTransform)
        {
            Assert.IsNotNull(textTransform);
            Assert.IsNotNull(caretTransform);

            caretTransform.localPosition = textTransform.localPosition;
            caretTransform.localRotation = textTransform.localRotation;
            caretTransform.localScale = textTransform.localScale;
            caretTransform.anchorMin = textTransform.anchorMin;
            caretTransform.anchorMax = textTransform.anchorMax;
            caretTransform.anchoredPosition = textTransform.anchoredPosition;
            caretTransform.sizeDelta = textTransform.sizeDelta;
            caretTransform.pivot = textTransform.pivot;
        }

        private IEnumerator CaretBlink()
        {
            int timer = 0;
            while (true)
            {
                if (!HasSelection())
                {
                    _isVisible = Mathf.Sin(timer++ * 0.03f) < 0;
                    Rebuild(_drawRect, _color);
                }
                else
                {
                    _isVisible = true;
                }
                yield return null;
            }
        }

        public int GetSelectionIndex()
        {
            return _selectionAnchorIndex;
        }
    }
}

