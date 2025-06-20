using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IAdsService 
{
    void Initialize();
    bool CanShowAds();
    void ShowAppOpen(string location, UnityAction callback);
    bool IsAppOpenReady();
    void ShowBanner();
    void HideBanner();
    bool CanShowInterstitial();
    void ShowInterstitial(string location, UnityAction callback);
    bool CanShowRewarded();
    void ShowRewarded(string location, UnityAction<bool> callback);
}
