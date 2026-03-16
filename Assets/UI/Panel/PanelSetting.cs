using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelSetting : UIPanel
{
    public Button btnClose;
    protected override void Awake()
    {
        base.Awake();
        btnClose.onClick.AddListener(Hide);
    }
    void OnEnable()
    {
        AdsManager.Instance.MRECModule.ShowMREC();
    }


    public override void Hide()
    {
        base.Hide();
        AdsManager.Instance.InterstitialModule.ShowInterstitial("si", () =>
        {
            AdsManager.Instance.MRECModule.HideMREC();
        });
    }
}