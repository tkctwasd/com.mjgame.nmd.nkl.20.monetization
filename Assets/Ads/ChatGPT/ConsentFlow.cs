using System;
using UnityEngine;
#if UNITY_IOS
using Unity.Advertisement.IosSupport;   // ATT
#endif

public enum ConsentState { Unknown, Accepted, Declined }

public class ConsentFlow : MonoBehaviour
{
    public static ConsentFlow Instance { get; private set; }
    public ConsentState State { get; private set; } = ConsentState.Unknown;
    public event Action<ConsentState> OnConsentResolved;

    void Awake()
    {
        if (Instance) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    }

    public void StartFlow()
    {
#if UNITY_IOS
        if (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
            ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
        {
            ATTrackingStatusBinding.RequestAuthorizationTracking(OnATTCompleted);
            return;
        }
#endif
        ShowGdprDialogIfNeeded();
    }

#if UNITY_IOS
    void OnATTCompleted(ATTrackingStatusBinding.AuthorizationTrackingStatus status)
    {
        ShowGdprDialogIfNeeded();
    }
#endif

    void ShowGdprDialogIfNeeded()
    {
        // Tuỳ ý: popup tự code, CMP hoặc AppLovin CMP
        // Giả lập: chấp nhận toàn bộ
        ResolveConsent(true);
    }

    public void ResolveConsent(bool accepted)
    {
        State = accepted ? ConsentState.Accepted : ConsentState.Declined;
        MaxSdk.SetHasUserConsent(accepted);
        //        MaxSdk.SetIsAgeRestrictedUser(false);            // hoặc có UI riêng
        MaxSdk.SetDoNotSell(!accepted);                  // CCPA
        OnConsentResolved?.Invoke(State);
    }
}
