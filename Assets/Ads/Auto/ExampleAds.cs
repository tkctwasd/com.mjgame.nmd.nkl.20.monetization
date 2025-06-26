using UnityEngine;

namespace A
{

    public class ExampleAds : MonoBehaviour
    {

        #region  REMOTE CONFIG
        [ContextMenu("RemoteConfig")]
        public void LoadRemoteConfig()
        {
            var allConfigs = RemoteConfigManager.GetAllRemoteConfigValues();

            foreach (var kvp in allConfigs)
            {
                Debug.Log($"<color=cyan>[REMOTE CONFIG]</color> {kvp.Key} = {kvp.Value}");
            }
        }

        [ContextMenu("LoadDictionaryRemoteConfig")]
        public void LoadDictionaryRemoteConfig()
        {
            foreach (var item in RemoteConfigManager.DataRemoteConfig)
            {
                Debug.Log("[KEY]:" + item.Key + "|" + "[VALUE]:" + item.Value);
            }
        }

        [ContextMenu("LoadTest")]
        public void LoadTest()
        {
            AdsConfig ads = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig);
            Debug.Log(ads.AppOpenCappingTime);
            Debug.Log(ads.ShowAppOpen);
            Debug.Log(ads.ShowBanner);
            Debug.Log(ads.ShowInterstitial);
        }

        [ContextMenu("LoadTest2")]
        public void LoadTest2()
        {
            int time = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig).InterstitialCappingTime;
        }
        #endregion


        #region AOA
        [ContextMenu("AOA")]
        public void ShowAOA()
        {
            ApplovinMaxManager.Instance.AppOpenAdManager.ShowAppOpen(false);
        }
        #endregion



        #region  Banner
        [ContextMenu("Banner")]
        public void ShowBanner()
        {
            ApplovinMaxManager.Instance.BannerAdManager.ShowBanner();
        }

        #endregion



        #region Reward
        [ContextMenu("Reward")]
        public void ShowReward()
        {
            Debug.Log("<color=cyan>ShowShowRewardStart......</color>");
            ApplovinMaxManager.Instance.RewardedAdManager.ShowRewarded("shop", () =>
            {
                Debug.Log("<color=orange>Nhan thuong thanh cong</color>");
            });
        }
        #endregion


        #region Inter
        [ContextMenu("Inter")]
        public void ShowInter()
        {
            Debug.Log("<color=cyan>ShowInterStart......</color>");
            ApplovinMaxManager.Instance.InterstitialAdManager.ShowInterstitial("example", () =>
            {
                Debug.Log("<color=orange>Show Inter thanh cong</color>");
            });
        }

        #endregion
    }
}