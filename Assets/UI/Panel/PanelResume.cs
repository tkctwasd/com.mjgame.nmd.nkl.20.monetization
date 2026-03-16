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
    bool isCompleted;

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
        float timer = 0f;

        while (timer < Duration)
        {
            timer += Time.deltaTime;
            float percent = Mathf.Clamp01(timer / Duration);
            UpdateUI(percent);
            yield return null;
        }
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