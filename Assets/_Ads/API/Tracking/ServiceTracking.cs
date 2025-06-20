using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServiceTracking : BaseModule
{
    static List<ITracking> trackings = new List<ITracking>();
    public override void Initialize()
    {
        foreach(var tracking in trackings)
        {
            tracking.Initialize();
        }
    }
    public override void SetInfo()
    {
        foreach(var tracking in trackings)
        {
            tracking.SetInfo();
        }
    }
    public override void Dispose() { }
    public static void Register(ITracking tracking)
    {
        if(!trackings.Contains(tracking))
        {
            trackings.Add(tracking);
        }
    }
    public void OnAdsRevenue(AdRevenueInfo adInfo)
    {
        Wasd.Log("OnAdsRevenue " + trackings.Count);
        foreach(var tracking in trackings)
        {
            tracking.OnAdsRevenue(adInfo);
        }
    }
}
