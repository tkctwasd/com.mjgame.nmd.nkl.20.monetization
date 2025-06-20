using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
#if FIREBASE_ANALYTICS
using Firebase.Analytics;
#endif

public class FirebaseAnalytics : IAdsAnalytics
{
      public void LogEvent(string eventName, object data = null)
      {
            if (data == null)
            {
                  Debug.Log($"[FirebaseAnalytics] LogEvent: {eventName}");
#if FIREBASE_ANALYTICS
                  Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName);
#endif
            }
            else if (data is Dictionary<string, object> dict)
            {
                  Debug.Log($"[FirebaseAnalytics] LogEvent: {eventName} | Data: {JsonConvert.SerializeObject(dict)}");
#if FIREBASE_ANALYTICS
                  var paramList = new List<Parameter>();
                  foreach (var kv in dict)
                  {
                        if (kv.Value is int i)
                              paramList.Add(new Parameter(kv.Key, i));
                        else if (kv.Value is float f)
                              paramList.Add(new Parameter(kv.Key, f));
                        else if (kv.Value is double d)
                              paramList.Add(new Parameter(kv.Key, d));
                        else
                              paramList.Add(new Parameter(kv.Key, kv.Value.ToString()));
                  }
                  Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, paramList.ToArray());
#endif
            }
            else
            {
                  // Nếu truyền object ẩn danh, chuyển sang Dictionary bằng Newtonsoft.Json
                  var json = JsonConvert.SerializeObject(data);
                  var dictFromJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                  Debug.Log($"[FirebaseAnalytics] LogEvent: {eventName} | Data: {json}");
#if FIREBASE_ANALYTICS
                  var paramList = new List<Parameter>();
                  foreach (var kv in dictFromJson)
                  {
                        if (kv.Value is int i)
                              paramList.Add(new Parameter(kv.Key, i));
                        else if (kv.Value is float f)
                              paramList.Add(new Parameter(kv.Key, f));
                        else if (kv.Value is double d)
                              paramList.Add(new Parameter(kv.Key, d));
                        else
                              paramList.Add(new Parameter(kv.Key, kv.Value.ToString()));
                  }
                  Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, paramList.ToArray());
#endif
            }
      }
      public void Example()
      {
            // analytics.LogEvent("player_died", new { checkpoint = 2, reason = "saw" });
      }
}