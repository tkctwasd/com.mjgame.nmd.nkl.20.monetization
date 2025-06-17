using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.RemoteConfig;
using Newtonsoft.Json;
using System.Linq;

namespace FirebaseModules.RemoteConfig
{
    public static class RemoteConfigService
    {
        private static bool _isReady;
        public static bool IsReady => _isReady;
        public static event Action OnReady;

        private static readonly Dictionary<string, object> _modelCache = new();

        // ——— INIT ———
        public static async Task InitializeAsync(string resourceDefaults, uint cacheSeconds = 0)
        {
            await ApplyResourceDefaults(resourceDefaults);
            await FetchRemoteConfigSafeAsync(cacheSeconds);

            _isReady = true;
            OnReady?.Invoke();
        }

        // ——— STEP 1: LOAD RESOURCES DEFAULT ———
        private static async Task ApplyResourceDefaults(string file)
        {
            var ta = Resources.Load<TextAsset>(file);
            if (!ta) { Debug.LogWarning($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Missing Resources/{file}.json"); return; }

            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(ta.text);
                await FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(dict);
                Debug.Log($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Loaded {dict.Count} defaults from Resources/{file}.json");
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Failed to parse Resources/{file}.json: {e.Message}");
            }
        }

        // ——— STEP 2: FETCH FROM FIREBASE ———
        private static async Task FetchRemoteConfigSafeAsync(uint cacheSeconds)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Offline → using cache or local.");
                return;
            }

            try
            {
                var t = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.FromSeconds(cacheSeconds));
                await t;

                if (!t.IsCompletedSuccessfully)
                {
                    Debug.LogWarning("<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Fetch failed → fallback to cached/default values");
                    return;
                }

                await FirebaseRemoteConfig.DefaultInstance.ActivateAsync();
                Debug.Log("<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> RemoteConfig fetch & activate successful");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Fetch exception: {e.Message}");
            }
        }

        // ——— VALUE ACCESSORS ———
        public static T Value<T>(string key, T fallback = default)
        {
            if (!_isReady)
            {
                Debug.LogWarning($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Value<{typeof(T).Name}> used before ready. Returning fallback.");
                return fallback;
            }

            var configValue = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

            // Check if value exists
            if (configValue.Source == ValueSource.StaticValue)
            {
                Debug.LogWarning($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Key '{key}' not found, returning fallback");
                return fallback;
            }

            try
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)configValue.StringValue;

                if (typeof(T) == typeof(bool))
                    return (T)(object)configValue.BooleanValue;

                if (typeof(T) == typeof(int))
                    return (T)(object)(int)configValue.LongValue;

                if (typeof(T) == typeof(long))
                    return (T)(object)configValue.LongValue;

                if (typeof(T) == typeof(float))
                    return (T)(object)(float)configValue.DoubleValue;

                if (typeof(T) == typeof(double))
                    return (T)(object)configValue.DoubleValue;

                string stringValue = configValue.StringValue;
                if (typeof(T).IsEnum)
                {
                    return (T)Enum.Parse(typeof(T), stringValue);
                }

                Debug.LogError($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Unsupported type {typeof(T).Name} for key '{key}'");
                return fallback;
            }
            catch (Exception e)
            {
                Debug.LogError($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Error converting value for key '{key}': {e.Message}");
                return fallback;
            }
        }

        // ——— VALUE ACCESSORS ———
        public static T Model<T>(string key, T fallback = default) where T : class, new()
        {
            if (_modelCache.TryGetValue(key, out var c)) return (T)c;

            if (!_isReady)
            {
                Debug.LogWarning($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Model<{typeof(T).Name}> called before ready. Returning fallback.");
                return fallback != null ? fallback : new T();
            }

            string json = FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            if (string.IsNullOrEmpty(json)) return fallback != null ? fallback : new T();

            try
            {
                var obj = JsonConvert.DeserializeObject<T>(json);
                _modelCache[key] = obj;
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Failed to parse model '{key}': {e.Message}");
                return fallback != null ? fallback : new T();
            }
        }

        // ——— ADDITIONAL HELPER METHODS ———
        public static void ClearModelCache(string key) => _modelCache.Remove(key);
        public static void ClearAllCache() => _modelCache.Clear();

        // ——— ADDITIONAL DEBUG METHODS ———
        public static void LogAllValues()
        {
            if (!_isReady)
            {
                Debug.Log("<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> Not ready yet");
                return;
            }

            var allKeys = FirebaseRemoteConfig.DefaultInstance.GetKeysByPrefix("");
            Debug.Log($"<color=yellow><b>[Firebase]</b></color>-<i><color=green>[RemoteConfig]</color></i> All Remote Config values ({allKeys.Count()}):");

            foreach (var key in allKeys)
            {
                var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
                Debug.Log($"  {key}: {value.StringValue} (Value: {value.Source})");
            }
        }

        public static bool HasKey(string key)
        {
            if (!_isReady) return false;
            var value = FirebaseRemoteConfig.DefaultInstance.GetValue(key);
            return value.Source != Firebase.RemoteConfig.ValueSource.StaticValue;
        }
    }
}