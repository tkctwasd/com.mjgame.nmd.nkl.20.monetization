using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class UIPanel : MonoBehaviour
{
    [SerializeField] private string panelId;
    [SerializeField] private List<UIElementGroup> elementGroups = new();

    public string PanelId => panelId;
    public event Action<UIPanel> OnPanelShown;
    public event Action<UIPanel> OnPanelHidden;

    public bool IsShown => gameObject.activeSelf;
    public CanvasGroup canvasGroup;

    private void Reset() => canvasGroup = GetComponent<CanvasGroup>();

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.Log("Null+" + panelId);
        }
        if (string.IsNullOrEmpty(panelId))
            panelId = GetType().Name;
    }

    protected virtual void OnValidate()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (string.IsNullOrEmpty(panelId))
            panelId = GetType().Name;
    }

    public void SetOpacity(float alpha)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = alpha;
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (elementGroups.Count > 0)
        {
            HideAllElementsImmediate();
            StartCoroutine(ShowElementsSequence(() => OnPanelShown?.Invoke(this)));
        }
        else
        {
            OnPanelShown?.Invoke(this);
        }
    }

    public virtual void Hide()
    {
        if (!IsShown) return;

        StopAllCoroutines();

        if (elementGroups.Count > 0)
            StartCoroutine(HideElementsSequence(() =>
            {
                gameObject.SetActive(false);
                OnPanelHidden?.Invoke(this);
            }));
        else
        {
            gameObject.SetActive(false);
            OnPanelHidden?.Invoke(this);
        }
    }

    private IEnumerator ShowElementsSequence(Action onComplete)
    {
        foreach (var group in elementGroups)
        {
            group.PlayShow();
            yield return new WaitForSeconds(group.duration);
        }
        onComplete?.Invoke();
    }

    private IEnumerator HideElementsSequence(Action onComplete)
    {
        for (int i = elementGroups.Count - 1; i >= 0; i--)
        {
            var group = elementGroups[i];
            group.PlayHide();
            yield return new WaitForSeconds(group.playHide ? group.duration : 0);
        }
        onComplete?.Invoke();
    }

    private void HideAllElementsImmediate()
    {
        foreach (var group in elementGroups)
        {
            group.HideImmediate();
        }
    }
}
