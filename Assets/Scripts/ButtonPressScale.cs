using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Adds a "real" pressed feel to UI buttons via a small scale-down on pointer down.
/// </summary>
public class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, ICancelHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField] private float selectedScale = 1.02f;
    [SerializeField] private Transform scaleTarget;

    private Vector3 _originalScale;
    private bool _isPointerDown;

    public void SetScaleTarget(Transform target)
    {
        if (target == null)
            return;
        scaleTarget = target;
        _originalScale = scaleTarget.localScale;
    }

    /// <summary>
    /// Tile / block-map cells: use <paramref name="selected"/> = 1 so focus does not enlarge the rect (1.02 caused visible overlap on neighbors).
    /// </summary>
    public void SetScalePresets(float pressed, float selected)
    {
        pressedScale = pressed;
        selectedScale = selected;
        if (scaleTarget == null)
            scaleTarget = transform;
        _originalScale = scaleTarget.localScale;
    }

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform;
        _originalScale = scaleTarget.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
        scaleTarget.localScale = _originalScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        scaleTarget.localScale = _originalScale;
    }

    public void OnCancel(BaseEventData eventData)
    {
        _isPointerDown = false;
        scaleTarget.localScale = _originalScale;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (_isPointerDown)
            return;
        scaleTarget.localScale = _originalScale * selectedScale;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        scaleTarget.localScale = _originalScale;
    }
}

