using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private Ease ease = Ease.OutBack;

    private Vector3 restScale;
    private Selectable selectable;
    private Tween tween;
    private bool pointerOver;
    private bool selected;

    private void Awake()
    {
        restScale = transform.localScale;
        selectable = GetComponent<Selectable>();
    }

    private void OnDisable()
    {
        tween?.Kill();
        tween = null;
        pointerOver = false;
        selected = false;
        transform.localScale = restScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
        Refresh();
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        Refresh();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        Refresh();
    }

    private void Refresh()
    {
        bool interactable = selectable == null || selectable.IsInteractable();
        Vector3 target = (pointerOver || selected) && interactable ? restScale * hoverScale : restScale;

        tween?.Kill();
        tween = transform.DOScale(target, duration)
            .SetEase(ease)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
