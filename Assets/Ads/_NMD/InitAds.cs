namespace A
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public static class InitAds
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            GameObject prefab = Resources.Load<GameObject>("Ads");
            if (prefab == null)
            {
                Debug.LogError("Ads prefab not found in Resources!");
                return;
            }
            GameObject instance = Object.Instantiate(prefab);
            Object.DontDestroyOnLoad(instance);
        }
    }

}