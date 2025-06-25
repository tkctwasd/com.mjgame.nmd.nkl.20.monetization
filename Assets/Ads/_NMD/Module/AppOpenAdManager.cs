namespace A
{
    using System;
    using UnityEngine;
    using static MaxSdkBase;

    public class AppOpenAdManager
    {
        private ApplovinMaxManager _applovinMaxManager;
        [Header("AppOpen")]
        private float timeCloseAOA;
        private string appOpenLocation;
        internal bool IsCloseFirstAOA;
        private bool isCanShowAOABackGame;
        internal bool IsShowFirstAOA;
        private bool isLogEventLoading;
        public DateTime TimeCloseAOA { get; set; }

        public AppOpenAdManager(ApplovinMaxManager applovinManager)
        {
            _applovinMaxManager = applovinManager;
            applovinManager. = DateTime.MinValue;
            IsCloseFirstAOA = false;
            IsShowFirstAOA = false;
            _isLogEventLoading = false;
        }

        public void Initialize()
        {
            LogW.Log($"Initializing App Open Ad with ID: {_adUnitId}", "AppOpen");

            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClickEvent;

            MaxSdk.LoadAppOpenAd(_adUnitId);
        }

        public void HandleApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                ShowAppOpen(false);
            }
        }

        private bool IsShowAOA()
        {
            return RemoteConfigManager.GetConfigValue<bool>("Ad_Use_AppOpen") && _canShowAOABackGameByCoordinator;
        }

        public void ShowAppOpen(bool isFromLoading)
        {
            int interval = RemoteConfigManager.GetConfigValue<int>("Ad_Interval_Show_AppOpen");
            bool passedCappingTime = (DateTime.UtcNow - _timeCloseAOA).TotalSeconds >= interval;

            _appOpenLocation = isFromLoading ? "loading" : "resume";

            if ((isFromLoading || passedCappingTime) && IsShowAOA())
            {
                if ((!_isLogEventLoading && isFromLoading) || !isFromLoading)
                {
                    FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads", _appOpenLocation);
                    _isLogEventLoading = true;
                }

                if (MaxSdk.IsAppOpenAdReady(_adUnitId))
                {
                    LogW.Log($"Showing App Open Ad from {_appOpenLocation}", "AppOpen");
                    FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads_success", _appOpenLocation);
                    MaxSdk.ShowAppOpenAd(_adUnitId);
                }
                else
                {
                    LogW.Log($"App Open Ad not ready, loading new ad from {_appOpenLocation}", "AppOpen");
                    MaxSdk.LoadAppOpenAd(_adUnitId);
                }
            }
            else
            {
                LogW.Log($"App Open Ad not shown. FromLoading: {isFromLoading}, PassedCapping: {passedCappingTime}, IsShowAOA: {IsShowAOA()}", "AppOpenDebug");
            }
        }

        // Phương thức mới để điều chỉnh khả năng hiển thị AOA từ bên ngoài
        public void SetCanShowAOABackGame(bool canShow)
        {
            _canShowAOABackGameByCoordinator = canShow;
            LogW.Log($"AppOpenAdManager: _canShowAOABackGameByCoordinator set to {canShow}", "AppOpen");
        }


        // --- Callbacks từ AppLovin MAX SDK --- (Không thay đổi logic chính)
        private void OnAppOpenAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Loaded. Source: {adInfo.NetworkName}", "AppOpen");
        }

        private void OnAppOpenAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            LogW.Log($"Ad Load Failed: {errorInfo.Message}", "AppOpen");
        }

        private void OnAppOpenAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Displayed. Source: {adInfo.NetworkName}", "AppOpen");
            if (!IsShowFirstAOA) IsShowFirstAOA = true;
        }

        private void OnAppOpenAdDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Display Failed: {errorInfo.Message}", "AppOpen");
            MaxSdk.LoadAppOpenAd(_adUnitId);
        }

        private void OnAppOpenAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Hidden", "AppOpen");
            MaxSdk.LoadAppOpenAd(_adUnitId);
            _appOpenLocation = "";
            if (!IsCloseFirstAOA) IsCloseFirstAOA = true;
            _timeCloseAOA = DateTime.UtcNow;
            _isLogEventLoading = false;
        }

        private void OnAppOpenAdClickEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            LogW.Log($"Ad Clicked. Source: {adInfo.NetworkName}", "AppOpen");
            if (!string.IsNullOrEmpty(_appOpenLocation))
            {
                FirebaseAnalyticManager.Instance.LogAd("show_app_open_ads_click", _appOpenLocation);
            }
        }
    }
}