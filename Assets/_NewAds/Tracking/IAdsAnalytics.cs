public interface IAdsAnalytics
{
    void LogEvent(string eventName, object data = null);
    // Có thể mở rộng thêm các hàm khác nếu cần
}
