using UnityEngine;

public class MonetizationBridge : MonoBehaviour
{
    private void Start()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnResumeRequested += OnResumeRequested;
            AdsManager.Instance.OnResumeCompleted += OnResumeCompleted;
        }
#if USE_IAP
        if (IAPManager.Instance != null)
            IAPManager.Instance.OnPurchaseFlowChanged += OnPurchaseFlowChanged;
#endif
    }

    private void OnDestroy()
    {
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.OnResumeRequested -= OnResumeRequested;
            AdsManager.Instance.OnResumeCompleted -= OnResumeCompleted;
        }
#if USE_IAP
        if (IAPManager.Instance != null)
            IAPManager.Instance.OnPurchaseFlowChanged -= OnPurchaseFlowChanged;
#endif
    }

    private void OnResumeRequested()
    {
        UIManager.Instance.ShowPanel<PanelResume>();
        AdsManager.Instance.AppOpenModule.ShowAppOpen(false);
    }

    private void OnResumeCompleted()
    {
        var panel = UIManager.Instance.GetPanel<PanelResume>();
        if (panel != null) panel.Complete();
    }

#if USE_IAP
    private void OnPurchaseFlowChanged(bool isPurchasing)
    {
        // Game tu them PanelOverlay (chan input khi dang mua) roi bo comment:
        // if (isPurchasing) UIManager.Instance.ShowPanel<PanelOverlay>();
        // else UIManager.Instance.HidePanel<PanelOverlay>();
    }
#endif
}
