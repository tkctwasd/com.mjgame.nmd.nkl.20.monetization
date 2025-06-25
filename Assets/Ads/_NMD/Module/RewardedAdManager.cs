namespace A
{
    using System;
    using UnityEngine;
    using static MaxSdkBase;

    public class RewardedAdManager
    {
        // NEW: Tham chiếu đến ApplovinMaxManager
        private ApplovinMaxManager _applovinMaxManager;
        private string _adUnitId;
        private int _retryAttempt;
        private string _rewardedLocation;

        public Action<string> OnUserRewarded;
        public Action OnAdDisplayed;
        public Action OnAdHidden;

        private Action _onRewardedFinishedCallback;

        // Constructor: Khởi tạo Manager với tham chiếu và Ad Unit ID
        public RewardedAdManager(ApplovinMaxManager applovinManager, string adUnitId)
        {
            _applovinMaxManager = applovinManager; // Gán tham chiếu
            _adUnitId = adUnitId;
        }

        public void Initialize()
        {
            LogW.Log($"Initializing Rewarded Ad with ID: {_adUnitId}", "Rewarded");

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;

            LoadRewardedAd();
        }

        public bool IsRewardedAdReady()
        {
            return MaxSdk.IsRewardedAdReady(_adUnitId);
        }

        public void LoadRewardedAd()
        {
            if (MaxSdk.IsRewardedAdReady(_adUnitId))
            {
                LogW.Log("Rewarded Ad is already loaded.", "Rewarded");
                return;
            }

            LogW.Log("Loading Rewarded Ad...", "Rewarded");
            MaxSdk.LoadRewardedAd(_adUnitId);
        }

        public void ShowRewarded(string placement, Action callBack)
        {
            _rewardedLocation = placement;
            _onRewardedFinishedCallback = callBack;

            LogW.Log($"Attempting to show Rewarded Ad at {placement}. IsReady: {IsRewardedAdReady()}", "Rewarded");
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads", placement);

            if (IsRewardedAdReady())
            {
                LogW.Log($"Showing Rewarded Ad from {placement}", "Rewarded");
                FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_success", placement);
                MaxSdk.ShowRewardedAd(_adUnitId);
            }
            else
            {
                LogW.Log($"Rewarded Ad not ready for {placement}. Invoking callback.", "Rewarded");
                FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_not_ready", placement);
                LoadRewardedAd();
                _onRewardedFinishedCallback?.Invoke();
                _onRewardedFinishedCallback = null;
            }
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Rewarded Ad loaded. Source: {adInfo.NetworkName}", "Rewarded");
            _retryAttempt = 0;
        }

        private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            LogW.Log($"Rewarded Ad load failed: {errorInfo.Message}", "Rewarded");
            FirebaseAnalyticManager.Instance.LogAd("rewarded_load_fail", _rewardedLocation, errorInfo.Message);
            _retryAttempt++;
            double retryDelay = Math.Pow(2, Math.Min(6, _retryAttempt));
            LogW.Log($"Retrying Rewarded Ad load in {retryDelay} seconds...", "Rewarded");
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Rewarded Ad displayed. Source: {adInfo.NetworkName}", "Rewarded");
            OnAdDisplayed?.Invoke(); // Kích hoạt sự kiện
        }

        private void OnRewardedAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Rewarded Ad display failed: {errorInfo.Message}", "Rewarded");
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_fail", _rewardedLocation, errorInfo.Message);

            _onRewardedFinishedCallback?.Invoke();
            _onRewardedFinishedCallback = null;

            LoadRewardedAd();
            OnAdHidden?.Invoke(); // Kích hoạt sự kiện
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Rewarded Ad clicked. Source: {adInfo.NetworkName}", "Rewarded");
            if (!string.IsNullOrEmpty(_rewardedLocation))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_click", _rewardedLocation);
            }
        }

        private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Rewarded Ad hidden", "Rewarded");
            LoadRewardedAd();
            _rewardedLocation = "";
            OnAdHidden?.Invoke(); // Kích hoạt sự kiện
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Rewarded Ad received reward. Amount: {reward.Amount}, Label: {reward.Label}", "Rewarded");
            FirebaseAnalyticManager.Instance.LogAd("show_rewarded_ads_completed", _rewardedLocation);

            _onRewardedFinishedCallback?.Invoke();
            _onRewardedFinishedCallback = null;
        }
    }
}