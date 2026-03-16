using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WASD;

public class PanelLoading : UIPanel
{
    [SerializeField] Splash w_Splash;
    [SerializeField] TMP_Text txtPercentLoading;
    [SerializeField] Slider slider;
    bool isCompleted;
    private void OnEnable()
    {
        var uiCanvas = UIManager.Instance.GetComponent<Canvas>();
        uiCanvas.sortingOrder = 100;
        uiCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
#if UNITY_EDITOR
        // Invoke(nameof(Completed), 2f);
#endif
        w_Splash.StartLoading();
    }
    public void OnProgressPercent(float percent)
    {
        txtPercentLoading.text = (percent * 100).ToString("0") + "%...";
        slider.value = percent;
        if (RemoteConfigManager.IsRemoteConfigComplete) ConfigEvent();
    }
    public void OnComplete()
    {
        Completed();
    }

    private void Completed()
    {
        if (isCompleted) return;
        isCompleted = true;
        ConfigEvent();
        Debug.Log("Complete");
        Hide();
        UIManager.Instance.ShowPanel<PanelGamePlay>();
    }
    #region Event
    bool isCompleEvent = false;
    void ConfigEvent()
    {
        if (isCompleEvent) return;
        isCompleEvent = true;
    }

    #endregion
}