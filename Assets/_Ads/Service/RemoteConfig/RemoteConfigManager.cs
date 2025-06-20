using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game.remoteconfig
{
    public class RemoteConfigManager
    {
        public static IRemoteConfig RemoteConfig { get; private set; }
        public static void AddConfig(IRemoteConfig remoteConfig)
        {
            RemoteConfig = remoteConfig;
        }
        [RuntimeInitializeOnLoadMethod]
        static void OnInit()
        {

        }

        public static T GetValue<T>(string key)
        {
            if (RemoteConfig == null)
            {
                Debug.LogError("W_RemoteConfig is not set.");
                return default;
            }
            return RemoteConfig.GetValue<T>(key);
        }
    }
}