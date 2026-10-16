#if USE_ADJUST
using UnityEngine;

public class AdjustRevenueSink : IAdRevenueSink
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Register() => AdRevenueTracker.Register(new AdjustRevenueSink());

    public void Send(AdRevenueData data)
    {
        Debug.Log("Tracker".Ads("AdjustRevenue " + Time.time + " " + data.Revenue));
        var adRevenue = new AdjustSdk.AdjustAdRevenue("applovin_max_sdk");
        adRevenue.SetRevenue(data.Revenue, "USD");
        adRevenue.AdRevenueNetwork = data.NetworkName;
        adRevenue.AdRevenueUnit = data.AdUnitId;
        adRevenue.AdRevenuePlacement = data.Placement;
        AdjustSdk.Adjust.TrackAdRevenue(adRevenue);
    }
}
#endif
