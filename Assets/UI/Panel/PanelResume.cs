using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelResume : UIPanel
{
    [SerializeField] TMP_Text txtPercentLoading;
    [SerializeField] Image imgLoadingProgress;
    [SerializeField] Slider slider;
    public float Duration = 2f;
    public float ShowAOATime = 1.5f;
    static bool isCompleted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void BindAdsResume()
    {
        AdsManager.OnResumeShow -= OnAdsResumeShow;
        AdsManager.OnResumeShow += OnAdsResumeShow;
        AdsManager.OnResumeComplete -= OnAdsResumeComplete;
        AdsManager.OnResumeComplete += OnAdsResumeComplete;
    }

    static void OnAdsResumeShow() => UIManager.Instance.ShowPanel<PanelResume>();

    static void OnAdsResumeComplete()
    {
        if (isCompleted) return;
        isCompleted = true;
        Debug.Log("<color=green>PanelResume Complete</color>");
        UIManager.Instance.HidePanel<PanelResume>();
    }

    private void OnEnable()
    {
        ResetState();
        StartCoroutine(ResumeIE());
    }

    void ResetState()
    {
        isCompleted = false;
        slider.value = 0;
        imgLoadingProgress.fillAmount = 0;
        txtPercentLoading.text = $"0%...";
    }

    IEnumerator ResumeIE()
    {
        float timer = 0;
        while (timer < ShowAOATime)
        {
            timer += Time.deltaTime;
            UpdateUI(Mathf.Clamp01(timer / Duration));
            yield return null;
        }
        UpdateUI(Mathf.Clamp01(ShowAOATime / Duration));
        AdsManager.Instance.AppOpenModule.ShowAppOpen(false);
    }

    void UpdateUI(float percent)
    {
        txtPercentLoading.text = $"{percent * 100f:0}%...";
        imgLoadingProgress.fillAmount = percent;
        slider.value = percent;
    }

    public void Complete()
    {
        if (isCompleted) return;

        isCompleted = true;

        Debug.Log("<color=green>PanelResume Complete</color>");

        Hide();
    }
}
