#if UNITY_ANDROID && USE_GOOGLE_REVIEW
using Google.Play.Review;
#endif
using System;
using System.Collections;
using UnityEngine;

public class AppReviewManager : MonoBehaviour
{
    public static AppReviewManager Ins;
#if UNITY_ANDROID && USE_GOOGLE_REVIEW
    private ReviewManager reviewManager;
    private PlayReviewInfo playReviewInfo;
#endif
    float nextTimeCanShow = 0f;
    float delayShowAgain = 180f;
    private void Awake()
    {
        Ins = this;
        DontDestroyOnLoad(gameObject);
    }
    public void ShowInAppReview(Action completedShow = null)
    {
        if(Time.time < nextTimeCanShow)
        {
            Debug.Log("Show In App Review Delay");
            return;
        }
        Debug.Log("Show In App Review");
        AppReviewiOS(completedShow);
#if UNITY_ANDROID && USE_GOOGLE_REVIEW
        AppReviewAndroid(completedShow);
#endif
    }
    private void AppReviewiOS(Action completedShow)
    {
#if UNITY_IOS
        bool popupShown = UnityEngine.iOS.Device.RequestStoreReview();
        if(popupShown)
        {
            nextTimeCanShow = Time.time + delayShowAgain;
            completedShow?.Invoke();
            Debug.Log("Show In App Review Completed");
        }
        else
        {
            Debug.LogWarning($"Show In App Review Error");
        }
#endif
    }

#if UNITY_ANDROID && USE_GOOGLE_REVIEW
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
        nextTimeCanShow = Time.time + delayShowAgain;
        completedShow?.Invoke();
        Debug.Log("Show In App Review Completed");
    }
#endif
}
