using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Drag the parent modal panel by grabbing the title/header strip (needs a Graphic with raycasts — e.g. TMP with raycastTarget on).
/// </summary>
[DisallowMultipleComponent]
public class UiDraggablePanelHeader : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] RectTransform _panel;

    Canvas _canvas;

    public void Initialize(RectTransform panel)
    {
        _panel = panel;
        CacheCanvas();
    }

    void Awake()
    {
        CacheCanvas();
    }

    void CacheCanvas()
    {
        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_panel == null)
            return;
        CacheCanvas();
        float s = _canvas != null ? _canvas.scaleFactor : 1f;
        if (s < 0.01f)
            s = 1f;
        _panel.anchoredPosition += eventData.delta / s;
    }
}
