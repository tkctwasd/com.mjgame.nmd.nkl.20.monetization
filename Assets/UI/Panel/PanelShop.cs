using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelShop : UIPanel
{
    public Button btnClose;
    protected override void Awake()
    {
        base.Awake();
        btnClose.onClick.AddListener(OnClickBtnClose);
    }

    void OnEnable()
    {
        AdsManager.Instance.MRECModule.ShowMREC();
    }

    public void OnClickBtnClose()
    {
        Hide();
    }
    public override void Hide()
    {
        base.Hide();
        AdsManager.Instance.MRECModule.HideMREC();
    }
}