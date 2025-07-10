using System;
using UnityEngine;
using Firebase.Analytics;
using System.Collections.Generic;

public class FirebaseAnalyticManager : MonoBehaviour
{
    public static FirebaseAnalyticManager Instance;

    private bool isReady = false;

    private void Awake()
    {
        Instance = this;
    }
    private void OnEnable()
    {
        FirebaseManager.InitDone += OnFirebaseReady;
    }

    private void OnDestroy()
    {
        FirebaseManager.InitDone -= OnFirebaseReady;
    }

    private void OnFirebaseReady()
    {
        isReady = true;
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        Debug.Log("FirebaseAnalytics".Ads(" is ready."));
    }

    public void LogAd(string nameEvent, string placement, string error = "")
    {
        if (!isReady)
        {
            Debug.Log("FirebaseAnalytics".Ads("Firebase not ready, skip LogAd."));
            return;
        }

        placement ??= "";
        error ??= "";

        try
        {
            if (string.IsNullOrEmpty(placement))
            {
                if (string.IsNullOrEmpty(error))
                {
                    FirebaseAnalytics.LogEvent(nameEvent, new Parameter("placement", "empty"));
                    LogMessage.Log("placement empty", nameEvent);
                }
                else
                {
                    FirebaseAnalytics.LogEvent(nameEvent, new Parameter("error", error));
                    LogMessage.Log($"error only: {error}", nameEvent);
                }
            }
            else
            {
                FirebaseAnalytics.LogEvent(nameEvent,
                    new Parameter("placement", placement),
                    new Parameter("error", error));
                LogMessage.Log($"{placement} {error}", nameEvent);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase LogAd ERROR] {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
        }
    }
    public void LogEvent(string eventName, Parameter[] parameters)
    {
        if (!isReady)
        {
            Debug.LogWarning($"[FirebaseAnalyticManager] Firebase not ready, skip LogEvent: {eventName}");
            return;
        }

        try
        {
            FirebaseAnalytics.LogEvent(eventName, parameters);
            Debug.Log("FirebaseAnalytics".Ads($"LogEvent: {eventName} with {parameters.Length} params"));
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase LogEvent ERROR] {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
        }
    }

}

public class LogEventParameter
{
    public string EventName;
    private List<Parameter> parameters = new List<Parameter>();

    private LogEventParameter(string eventName)
    {
        EventName = eventName;
    }

    public static LogEventParameter Create(string eventName)
    {
        return new LogEventParameter(eventName);
    }

    public LogEventParameter AddParam(string key, object value)
    {
        parameters.Add(new Parameter(key, value.ToString()));
        return this;
    }

    public void Send()
    {
        if (FirebaseAnalyticManager.Instance == null)
        {
            LogMessage.Log("FirebaseAnalyticManager.Instance is null, skip send.", "LogEventParameter");
            return;
        }
        FirebaseAnalyticManager.Instance?.LogEvent(EventName, parameters.ToArray());
    }
}

