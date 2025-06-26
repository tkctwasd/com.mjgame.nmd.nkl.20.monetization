namespace A
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Firebase.Analytics;
    using UnityEngine;

    public class FirebaseAnalyticManager : MonoBehaviour
    {
        public static FirebaseAnalyticManager Instance;

        void Awake()
        {
            Instance = this;
        }

        public void LogAd(string nameEvent, string placement, string error = "")
        {
            try
            {
                if (placement.Equals(string.Empty))
                {
                    if (error.Equals(string.Empty))
                    {
                        Firebase.Analytics.FirebaseAnalytics.LogEvent(nameEvent,
                        new Firebase.Analytics.Parameter("placement", placement));
                        LogMessage.Log($"{placement}", nameEvent);
                    }
                    else
                    {
                        Firebase.Analytics.FirebaseAnalytics.LogEvent(nameEvent);
                        LogMessage.Log($"", nameEvent);
                    }
                }
                else
                {
                    Firebase.Analytics.FirebaseAnalytics.LogEvent(nameEvent,
                    new Firebase.Analytics.Parameter("placement", placement),
                    new Firebase.Analytics.Parameter("error", error));
                    LogMessage.Log($"{placement} {error}", nameEvent);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public void LogEvent(string eventName)
        {
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

}