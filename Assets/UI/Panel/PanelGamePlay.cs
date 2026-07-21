using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelGamePlay : UIPanel
{
  public Button btnSetting;
  public Button btShowBanner;
  public Button btnShop;
  void OnEnable()
  {
    AdsManager.Instance.BannerModule.ShowBanner();
  }


  protected override void Awake()
  {
    base.Awake();
    btnSetting.onClick.AddListener(() =>
    {
      UIManager.Instance.ShowPanel<PanelSetting>();
    });
    btShowBanner.onClick.AddListener(() =>
    {
      AdsManager.Instance.RewardedModule.ShowRewarded("reward", () =>
      {
      });
    });
  }
}