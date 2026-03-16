using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(RectTransform))]
public class UIElementInteractive : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Vector3 originScale = Vector3.one * 0.9f;
    protected bool isSelected = false;
    protected bool interactable = true;
    protected Tween scaleTween;
    protected Tween rotateTween;

    protected virtual void OnEnable()
    {
        ResetVisual();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (isSelected || !interactable) return;
        scaleTween?.Kill();
        rotateTween?.Kill();

        scaleTween = transform.DOScale(originScale * 1.1f, 0.15f).SetEase(Ease.OutBack);
        rotateTween = transform.DORotate(new Vector3(0, 0, 3f), 0.15f)
                               .SetLoops(2, LoopType.Yoyo)
                               .SetEase(Ease.InOutSine);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected || !interactable) return;
        scaleTween?.Kill();
        rotateTween?.Kill();

        scaleTween = transform.DOScale(originScale, 0.15f).SetEase(Ease.InOutBack);
        rotateTween = transform.DORotate(Vector3.zero, 0.1f).SetEase(Ease.InOutSine);

    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        if (isSelected || !interactable) return;
        isSelected = true;
        scaleTween?.Kill();
        rotateTween?.Kill();

        scaleTween = transform.DOScale(originScale * 1.15f, 0.25f)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                OnClickAction();
                ResetVisual();
            });
    }

    public virtual void ResetVisual()
    {
        scaleTween?.Kill();
        rotateTween?.Kill();
        isSelected = false;
        transform.localScale = originScale;
        transform.localEulerAngles = Vector3.zero;
    }

    protected virtual void OnClickAction() { }
}
