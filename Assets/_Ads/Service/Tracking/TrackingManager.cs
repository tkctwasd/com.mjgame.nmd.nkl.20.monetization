using System.Collections.Generic;
using UnityEngine;

namespace game.tracking
{
    public class TrackingManager
    {
        static List<ITracking> Trackings { get; } = new List<ITracking>();
        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {

        }

        public static void AddTracking(ITracking tracking)
        {
            if (!Trackings.Contains(tracking))
                Trackings.Add(tracking);
        }

    }

    public class AdInfo
    {
        public string Source { get; set; }
        public double Revenue { get; set; }
        public string Currency { get; set; }
        public int AdImpressionsCount { get; set; }
        public string AdRevenueNetwork { get; set; }
        public string AdRevenueUnit { get; set; }
        public string AdRevenuePlacement { get; set; }
    }
}
