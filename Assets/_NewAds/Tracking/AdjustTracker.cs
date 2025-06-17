
using AdjustSdk;

public class AdjustTracker : IAdsTracker
{
    public void TrackEvent(string eventName, object data = null)
    {
        // Sử dụng event token cho từng loại event, ở đây giả lập dùng eventName làm token
        var adjustEvent = new AdjustEvent(eventName);
        Adjust.TrackEvent(adjustEvent);
    }
    public void TrackRevenue(string adUnit, string network, double revenue, string currency, string adType)
    {
        // Cần event token cho revenue event, ví dụ "revenue_token"
        var adjustEvent = new AdjustEvent("revenue_token");
        adjustEvent.SetRevenue(revenue, currency);
        adjustEvent.AddCallbackParameter("ad_unit", adUnit);
        adjustEvent.AddCallbackParameter("network", network);
        adjustEvent.AddCallbackParameter("ad_type", adType);
        Adjust.TrackEvent(adjustEvent);
    }
    public void TrackImpression(string adUnit, string network, string adType)
    {
        // Cần event token cho impression event, ví dụ "impression_token"
        var adjustEvent = new AdjustEvent("impression_token");
        adjustEvent.AddCallbackParameter("ad_unit", adUnit);
        adjustEvent.AddCallbackParameter("network", network);
        adjustEvent.AddCallbackParameter("ad_type", adType);
        Adjust.TrackEvent(adjustEvent);
    }
    public void TrackClick(string adUnit, string network, string adType)
    {
        // Cần event token cho click event, ví dụ "click_token"
        var adjustEvent = new AdjustEvent("click_token");
        adjustEvent.AddCallbackParameter("ad_unit", adUnit);
        adjustEvent.AddCallbackParameter("network", network);
        adjustEvent.AddCallbackParameter("ad_type", adType);
        Adjust.TrackEvent(adjustEvent);
    }
}