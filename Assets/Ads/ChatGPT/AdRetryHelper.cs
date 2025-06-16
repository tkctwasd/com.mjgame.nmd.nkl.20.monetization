using UnityEngine;
using System;

public class AdRetryHelper
{
    readonly AdsRemoteConfig rc;
    int attempt;
    readonly Action retryMethod;

    public AdRetryHelper(AdsRemoteConfig cfg, Action retry)
    {
        rc = cfg; retryMethod = retry;
    }

    public void Reset() => attempt = 0;

    public void ScheduleRetry(MonoBehaviour host)
    {
        if (attempt >= rc.maxRetry) { Debug.LogWarning("[Ads] Max retry reached"); return; }
        float delay = rc.baseDelay * Mathf.Pow(2, attempt) * UnityEngine.Random.Range(0.8f, 1.2f);
        attempt++;
        host.Invoke(nameof(InvokeRetry), delay);
        void InvokeRetry() => retryMethod.Invoke();
    }
}
