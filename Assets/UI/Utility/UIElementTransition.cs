using UnityEngine;
using DG.Tweening;
using System;
using System.Collections.Generic;


[Serializable]
public class UITransition
{
    public enum TransitionType
    {
        Fade,
        Scale,
        Slide
    }
    public TransitionType transition = TransitionType.Fade;
    public float duration = 0.4f;
    public float delay = 0f;
    public Ease ease = Ease.OutBack;

    [ShowIf(nameof(transition), (int)TransitionType.Slide)]
    public Vector3 slideTarget = new Vector3(0, -100, 0);

    [ShowIf(nameof(transition), (int)TransitionType.Scale)]
    public Vector3 scaleFrom = Vector3.zero;

    [ShowIf(nameof(transition), (int)TransitionType.Fade)]
    public float fadeTo = 1f;

    private RectTransform rect;
    private CanvasGroup canvas;

    private Vector3 originalPos;

    public void Initialize(MonoBehaviour owner)
    {
        if (owner == null) return;
        rect = owner.GetComponent<RectTransform>();
        canvas = owner.GetComponent<CanvasGroup>();
        originalPos = rect.anchoredPosition3D;
    }

    public Tween Play(Action onComplete = null)
    {
        if (rect == null) return null;

        Tween tween = null;
        switch (transition)
        {
            case TransitionType.Scale:
                tween = rect.DOScale(scaleFrom, duration)
                           .SetDelay(delay)
                           .SetEase(ease);
                break;

            case TransitionType.Slide:
                tween = rect.DOAnchorPos3D(slideTarget, duration)
                           .SetDelay(delay)
                           .SetEase(ease);
                break;

            case TransitionType.Fade:
                if (canvas != null)
                    tween = canvas.DOFade(fadeTo, duration)
                                  .SetDelay(delay)
                                  .SetEase(ease);
                break;
        }

        tween?.OnComplete(() => onComplete?.Invoke());
        return tween;
    }

    public void Reset()
    {
        if (rect == null || canvas == null) return;
        switch (transition)
        {
            case TransitionType.Scale:
                canvas.alpha = 1f;
                rect.localScale = Vector3.zero;
                rect.anchoredPosition3D = originalPos;
                break;
            case TransitionType.Slide:
                canvas.alpha = 1f;
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = originalPos;
                break;
            case TransitionType.Fade:
                rect.localScale = Vector3.one;
                rect.anchoredPosition3D = originalPos;
                canvas.alpha = 0f;
                break;
        }
    }
}



[Serializable]
public class UIElementGroup
{
    public List<UIElementTransition> elements = new List<UIElementTransition>();

    public float duration = 0.3f;

    public bool playHide = false;

    public void PlayShow()
    {
        foreach (var item in elements)
        {
            if (item != null)
                item.gameObject.SetActive(true);
            item.PlayIn();
        }
    }

    public void PlayHide()
    {
        if (!playHide) return;
        foreach (var item in elements)
        {
            if (item != null)
                item.PlayOut();
        }
    }

    public void HideImmediate()
    {

        foreach (var item in elements)
        {
            if (item != null)
                item.ResetShow();
        }
    }
}


[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class UIElementTransition : MonoBehaviour
{
    public UITransition showTransition = new UITransition();
    public UITransition hideTransition = new UITransition();

    private void Awake()
    {
        showTransition?.Initialize(this);
        hideTransition?.Initialize(this);
    }


    public Tween PlayIn(Action onComplete = null)
    {
        return showTransition.Play(() =>
        {
            var anim = GetComponent<UIElementAnimation>();
            anim?.StartAnimation();
            onComplete?.Invoke();
        });
    }

    public Tween PlayOut(Action onComplete = null)
    {
        var anim = GetComponent<UIElementAnimation>();
        anim?.StopAnimation();
        return hideTransition.Play(onComplete);
    }

    public void ResetShow()
    {
        var anim = GetComponent<UIElementAnimation>();
        anim?.StopAnimation();
        showTransition?.Reset();
    }
}
