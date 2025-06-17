public interface IAdsTracker
{
    void TrackEvent(string eventName, object data = null);
    void TrackRevenue(string adUnit, string network, double revenue, string currency, string adType);
    void TrackImpression(string adUnit, string network, string adType);
    void TrackClick(string adUnit, string network, string adType);
    // Có thể mở rộng thêm các hàm khác nếu cần
}
