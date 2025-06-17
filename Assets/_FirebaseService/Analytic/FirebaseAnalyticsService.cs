using System;
using System.Collections.Generic;
using UnityEngine;
#if FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

namespace FirebaseModules.Analytics
{
    public static class FirebaseAnalyticsService
    {
        private static bool _isEnabled = true;

        public static void Initialize()
        {
            _isEnabled = true;
            Debug.Log("<color=yellow><b>[Firebase]</b></color>-<color=orange><i>[Analytics]</i></color> Initialized");
        }

        // Gửi sự kiện dạng đơn giản
        public static void LogEvent(string eventName)
        {
            if (!_isEnabled) return;
#if FIREBASE_ANALYTICS
            FirebaseAnalytics.LogEvent(eventName);
#endif
            Debug.Log($"<color=yellow><b>[Firebase]</b></color>-<color=orange><i>[Analytics]</i></color> Event: {eventName}");
        }

        // Gửi sự kiện với tham số
        public static void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!_isEnabled) return;
#if FIREBASE_ANALYTICS
            var paramList = new List<Parameter>();
            foreach (var kv in parameters)
            {
                if (kv.Value is string s) paramList.Add(new Parameter(kv.Key, s));
                else if (kv.Value is int i) paramList.Add(new Parameter(kv.Key, i));
                else if (kv.Value is long l) paramList.Add(new Parameter(kv.Key, l));
                else if (kv.Value is float f) paramList.Add(new Parameter(kv.Key, f));
                else if (kv.Value is double d) paramList.Add(new Parameter(kv.Key, d));
                else paramList.Add(new Parameter(kv.Key, kv.Value.ToString()));
            }
            FirebaseAnalytics.LogEvent(eventName, paramList.ToArray());
#endif
            Debug.Log($"<color=yellow><b>[Firebase]</b></color>-<color=orange><i>[Analytics]</i></color> Event: {eventName} → {UnityEngine.JsonUtility.ToJson(parameters)}");
        }

        // Set thông tin người dùng
        public static void SetUserId(string userId)
        {
#if FIREBASE_ANALYTICS
            FirebaseAnalytics.SetUserId(userId);
#endif
            Debug.Log($"<color=yellow><b>[Firebase]</b></color>-<color=orange><i>[Analytics]</i></color> SetUserId: {userId}");
        }

        public static void SetUserProperty(string name, string value)
        {
#if FIREBASE_ANALYTICS
            FirebaseAnalytics.SetUserProperty(name, value);
#endif
            Debug.Log($"<color=yellow><b>[Firebase]</b></color>-<color=orange><i>[Analytics]</i></color> SetUserProperty: {name} = {value}");
        }


       
    }
}
