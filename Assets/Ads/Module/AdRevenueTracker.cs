using Firebase.Analytics;
using UnityEngine;
using static MaxSdkBase;
public class AdRevenueTracker
{
        public void SendAllTracker(AdInfo adInfo)
        {
                SendRevenueMAXToFirebase(adInfo);
                SendRevenueMAXToAdjust(adInfo);
                SendRevenueMAXToAppsflyer(adInfo);
        }
        void SendRevenueMAXToFirebase(AdInfo adInfo)
        {
                Parameter[] AdParameters = {
                                        new Parameter("ad_platform", "Applovin"),
                                        new Parameter("ad_source", adInfo.NetworkName),
                                        new Parameter("ad_unit_name", adInfo.AdUnitIdentifier),
                                        new Parameter("currency","USD"),
                                        new Parameter("value", adInfo.Revenue),
                                        new Parameter("placement", adInfo.Placement),
                                        new Parameter("country_code", MaxSdk.GetSdkConfiguration().CountryCode),
                                        new Parameter("ad_format", adInfo.AdFormat)
                              };

                FirebaseAnalytics.LogEvent("ad_impression", AdParameters);
        }

        void SendRevenueMAXToAdjust(AdInfo adInfo)
        {
#if USE_ADJUST
                LogMessage.Log("AdjustRevenue " + Time.time + " " + adInfo.Revenue);
                var adRevenue = new AdjustSdk.AdjustAdRevenue("applovin_max_sdk");
                adRevenue.SetRevenue(adInfo.Revenue, "USD");
                adRevenue.AdRevenueNetwork = adInfo.NetworkName;
                adRevenue.AdRevenueUnit = adInfo.AdUnitIdentifier;
                adRevenue.AdRevenuePlacement = adInfo.Placement;
                AdjustSdk.Adjust.TrackAdRevenue(adRevenue);
#endif
        }

        void SendRevenueMAXToAppsflyer(AdInfo adInfo)
        {
#if USE_APPSFLYER
                LogMessage.Log("AppsflyerRevenue: " + Time.time + " " + adInfo.Revenue);
                Dictionary<string, string> additionalParams = new Dictionary<string, string>();
                additionalParams.Add(AdRevenueScheme.COUNTRY, MaxSdk.GetSdkConfiguration().CountryCode);
                additionalParams.Add(AdRevenueScheme.AD_UNIT, adInfo.AdUnitIdentifier);
                additionalParams.Add(AdRevenueScheme.AD_TYPE, adInfo.AdFormat);
                additionalParams.Add(AdRevenueScheme.PLACEMENT, adInfo.Placement);
                var logRevenue = new AFAdRevenueData(adInfo.NetworkName, MediationNetwork.ApplovinMax, "USD", adInfo.Revenue);
                AppsFlyer.logAdRevenue(logRevenue, additionalParams);
#endif
        }
}