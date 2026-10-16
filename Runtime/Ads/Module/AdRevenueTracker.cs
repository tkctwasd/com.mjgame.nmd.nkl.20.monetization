using System.Collections.Generic;

public struct AdRevenueData
{
    public string NetworkName, AdUnitId, Placement, AdFormat, CountryCode;
    public double Revenue;
}

public interface IAdRevenueSink
{
    void Send(AdRevenueData data);
}

public class AdRevenueTracker
{
    static readonly List<IAdRevenueSink> sinks = new List<IAdRevenueSink>();

    // Tat Domain Reload thi static list khong bi reset, bo qua sink cung type de khong ban trung
    public static void Register(IAdRevenueSink sink)
    {
        for (var i = 0; i < sinks.Count; i++)
            if (sinks[i].GetType() == sink.GetType()) return;
        sinks.Add(sink);
    }

    public void SendAllTracker(AdRevenueData data)
    {
        for (var i = 0; i < sinks.Count; i++) sinks[i].Send(data);
    }
}
