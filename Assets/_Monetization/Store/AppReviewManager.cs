#if UNITY_ANDROID
using Google.Play.Review;
#endif
using System;
using System.Collections;
using UnityEngine;

public class AppReviewManager : Singleton<AppReviewManager>
{
#if UNITY_ANDROID
    private ReviewManager reviewManager;
    private PlayReviewInfo playReviewInfo;
#endif

    public void ShowInAppReview(Action completedShow = null)
    {
        if (ShouldShowRatePopup())
        {
#if UNITY_IOS
            AppReviewiOS(completedShow);
#elif UNITY_ANDROID
        AppReviewAndroid();
#endif
        }
    }
    private void AppReviewiOS(Action completedShow)
    {
#if UNITY_IOS
        bool popupShown = UnityEngine.iOS.Device.RequestStoreReview();
        if (popupShown)
        {
            completedShow?.Invoke();
            Debug.Log("Show In App Review Completed");
        }
        else
        {
            Debug.LogWarning($"Show In App Review Error");
        }
        Debug.Log("ShowRate");
#endif
    }

#if UNITY_ANDROID
    private void AppReviewAndroid(Action completedShow)
    {
        StartCoroutine(IEReviewAndroid(completedShow));
    }
    IEnumerator IEReviewAndroid(Action completedShow)
    {
        reviewManager = new ReviewManager();
        var requestFlowOperation = reviewManager.RequestReviewFlow();

        yield return requestFlowOperation;

        if(requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"Show In App Review Error: {requestFlowOperation.Error}");
            yield break;
        }

        playReviewInfo = requestFlowOperation.GetResult();
        var lauchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);

        yield return lauchFlowOperation;

        playReviewInfo = null;
        if(lauchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debug.LogWarning($"Show In App Review Error: {requestFlowOperation.Error}");
            yield break;
        }
        completedShow?.Invoke();
        Debug.Log("Show In App Review Completed");
    }
#endif

    private bool ShouldShowRatePopup()
    {
        // var remote = RemoteConfigManager.GetConfigValue<PopupRate>(RemoteKey.PopupRate);
        // if (!remote.CanShowRate) return false;
        return true;
    }

}
