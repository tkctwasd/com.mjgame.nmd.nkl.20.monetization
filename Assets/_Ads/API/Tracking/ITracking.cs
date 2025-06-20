using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITracking 
{
    bool IsInitialized { get; set; }
    void Initialize();
    void SetInfo();
    void OnAdsRevenue(AdRevenueInfo adInfo);
}
