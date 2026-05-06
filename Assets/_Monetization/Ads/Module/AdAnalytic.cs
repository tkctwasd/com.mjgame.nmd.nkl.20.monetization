using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Analytics;
using UnityEngine;

public class AdAnalytic
{
    public static void LogAd(string nameEvent, string placement, string error = "")
    {
        if (FirebaseAnalyticManager.Instance == null)
        {
            Debug.Log("FirebaseAnalytics".Ads("Firebase not ready, skip LogAd."));
            return;
        }

        placement ??= "";
        error ??= "";

        try
        {
            Parameter[] parameters;

            if (string.IsNullOrEmpty(placement))
            {
                if (string.IsNullOrEmpty(error))
                {
                    parameters = new[]
                    {
                        new Parameter("placement", "empty")
                    };

                    LogMessage.Log("placement empty", nameEvent);
                }
                else
                {
                    parameters = new[]
                    {
                        new Parameter("error", error)
                    };

                    LogMessage.Log($"error only: {error}", nameEvent);
                }
            }
            else
            {
                parameters = new[]
                {
                    new Parameter("placement", placement),
                    new Parameter("error", error)
                };

                LogMessage.Log($"{placement} {error}", nameEvent);
            }

            FirebaseAnalyticManager.Instance.LogEvent(nameEvent, parameters);
            Debug.Log("LogAnalyticsAd".Ads($"LogAd: {nameEvent} | {placement} | {error}"));
        }
        catch (Exception e)
        {
            Debug.LogError($"[LogAnalyticsAd ERROR] {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
        }
    }

}
