using System;

namespace game.tracking
{
    public interface ITracking
    {
        void Init(Action callback);
        void AdsRevenue(AdInfo info);
    }
}
