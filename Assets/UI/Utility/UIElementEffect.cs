using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(RectTransform))]
public class UIElementEffect : MonoBehaviour
{
    public enum EffectType
    {
        LineShine,   // line chạy từ trái sang phải
        PulseGlow,
        Flicker,
        ColorShift
    }

    [Header("Effect Settings")]
    public EffectType effectType = EffectType.LineShine;
    public float duration = 1f;
    public float delay = 0f;
    public Ease ease = Ease.InOutSine;

    [Header("Optional Components")]
    public Image targetImage;  // Image chính
    public RectTransform rect;

    [Header("Line Settings")]
    public Image lineImage;    // Image trắng hoặc gradient làm line
    public float lineWidth = 50f;

    private Tween currentTween;
    private Color baseColor;
    private Vector3 basePosition;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        targetImage = GetComponent<Image>();
        if (targetImage != null)
            baseColor = targetImage.color;

        basePosition = rect.anchoredPosition3D;

        if (lineImage != null)
            lineImage.gameObject.SetActive(false); // tắt line lúc đầu
    }

    private void OnEnable()
    {
        PlayEffect();
    }

    private void OnDisable()
    {
        StopEffect();
        ResetEffect();
    }

    public void PlayEffect(Action onComplete = null)
    {
        StopEffect();

        switch (effectType)
        {
            case EffectType.LineShine:
                if (lineImage != null && targetImage != null)
                {
                    lineImage.gameObject.SetActive(true);

                    RectTransform lineRect = lineImage.rectTransform;
                    float parentWidth = rect.rect.width;

                    // đặt bắt đầu ngoài biên trái
                    lineRect.anchoredPosition = new Vector2(-lineWidth, 0);

                    currentTween = DOTween.To(() => lineRect.anchoredPosition,
                                              v => lineRect.anchoredPosition = v,
                                              new Vector2(parentWidth + lineWidth, 0),
                                              duration)
                                           .SetEase(Ease.Linear)
                                           .SetLoops(-1, LoopType.Restart)
                                           .SetDelay(delay);
                }
                break;

            case EffectType.PulseGlow:
                if (targetImage != null)
                {
                    currentTween = targetImage.DOFade(0.6f, duration / 2)
                        .SetEase(ease)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetDelay(delay);
                }
                break;

            case EffectType.Flicker:
                if (targetImage != null)
                {
                    currentTween = DOTween.Sequence()
                        .Append(targetImage.DOFade(0f, 0.05f))
                        .Append(targetImage.DOFade(1f, 0.05f))
                        .SetLoops(-1, LoopType.Restart)
                        .SetDelay(delay);
                }
                break;

            case EffectType.ColorShift:
                if (targetImage != null)
                {
                    currentTween = targetImage.DOColor(UnityEngine.Random.ColorHSV(), duration)
                        .SetEase(Ease.Linear)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetDelay(delay);
                }
                break;
        }

        onComplete?.Invoke();
    }

    public void StopEffect()
    {
        currentTween?.Kill();
        currentTween = null;
    }

    public void ResetEffect()
    {
        if (targetImage != null)
            targetImage.color = baseColor;

        rect.anchoredPosition3D = basePosition;
        rect.localScale = Vector3.one;

        if (lineImage != null)
            lineImage.gameObject.SetActive(false);
    }
}
