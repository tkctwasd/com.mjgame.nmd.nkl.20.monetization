using UnityEngine;
using DG.Tweening;
using System;

[Serializable]
public class UIAnimation
{
    public enum AnimationType
    {
        Scale,
        Rotate,
        Move,
        Glitch
    }

    public AnimationType _animation = AnimationType.Scale;

    public Ease ease = Ease.InOutSine;
    public LoopType loopType = LoopType.Yoyo;
    public float duration = 1f;
    public float delay = 0f;

    [ShowIf(nameof(_animation), (int)AnimationType.Scale)]
    public Vector3 scaleTo = Vector3.one * 1.1f;

    [ShowIf(nameof(_animation), (int)AnimationType.Rotate)]
    public RotateMode rotateMode = RotateMode.FastBeyond360;
    [ShowIf(nameof(_animation), (int)AnimationType.Rotate)]
    public Vector3 rotationTo = new Vector3(0, 0, 15f);

    [ShowIf(nameof(_animation), (int)AnimationType.Move)]
    public Vector3 moveFrom;

    [ShowIf(nameof(_animation), (int)AnimationType.Glitch)]
    public float glitchStrength = 10f;
    [ShowIf(nameof(_animation), (int)AnimationType.Glitch)]
    public int glitchVibrato = 20;
    [ShowIf(nameof(_animation), (int)AnimationType.Glitch)]
    public float glitchRandomness = 90f;

    private Vector3 scaleFrom = Vector3.one;
    private Vector3 rotationFrom = Vector3.zero;
    private Vector3 originalPos;
    private RectTransform rect;

    public void Initialize(MonoBehaviour owner)
    {
        if (owner == null) return;
        rect = owner.GetComponent<RectTransform>();

        scaleFrom = rect.localScale;
        rotationFrom = rect.localEulerAngles;
        originalPos = rect.anchoredPosition3D;
    }

    public Tween Play(Action onComplete = null)
    {
        if (rect == null) return null;

        Tween tween = null;

        switch (_animation)
        {
            case AnimationType.Scale:
                tween = rect.DOScale(scaleTo, duration)
                           .SetLoops(-1, loopType)
                           .SetEase(ease)
                           .SetDelay(delay);
                break;

            case AnimationType.Rotate:
                tween = rect.DOLocalRotate(rotationTo, duration, rotateMode)
                           .SetLoops(-1, loopType)
                           .SetEase(ease)
                           .SetDelay(delay);
                break;

            case AnimationType.Move:
                tween = rect.DOAnchorPos3D(moveFrom, duration)
                           .SetLoops(-1, loopType)
                           .SetEase(ease)
                           .SetDelay(delay);
                break;

            case AnimationType.Glitch:
                tween = rect.DOShakeAnchorPos(duration, glitchStrength, glitchVibrato, glitchRandomness, false, true)
                           .SetLoops(-1, loopType)
                           .SetEase(ease)
                           .SetDelay(delay);
                break;
        }

        tween?.OnComplete(() => onComplete?.Invoke());
        return tween;
    }

    public void Reset()
    {
        if (rect == null) return;

        switch (_animation)
        {
            case AnimationType.Scale:
                rect.localScale = scaleFrom;
                break;
            case AnimationType.Rotate:
                rect.localEulerAngles = rotationFrom;
                break;
            case AnimationType.Move:
            case AnimationType.Glitch:
                rect.anchoredPosition3D = originalPos;
                break;
        }
    }
}

[RequireComponent(typeof(RectTransform))]
public class UIElementAnimation : MonoBehaviour
{
    public UIAnimation dataAnimation = new UIAnimation();
    private Tween currentTween;

    private void Awake()
    {
        dataAnimation?.Initialize(this);
    }

    private void OnEnable()
    {
        if (GetComponent<UIElementTransition>() != null)
            return;
        StartAnimation();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    public void StartAnimation()
    {
        StopAnimation();
        if (dataAnimation != null)
            currentTween = dataAnimation.Play();
    }

    public void StopAnimation()
    {
        currentTween?.Kill();
        currentTween = null;
        dataAnimation?.Reset();
    }
}
