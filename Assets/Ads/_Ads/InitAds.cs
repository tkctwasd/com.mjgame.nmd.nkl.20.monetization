using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InitAds
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init() => Object.DontDestroyOnLoad(Object.Instantiate(Resources.Load("Ads")));
}
