using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PlatformUnity
{
    public static string PlatformName(this string name)
    {
#if UNITY_ANDROID
        return name + "Android.json";
#elif UNITY_IOS
        return name + "iOS.json";
#else
        return name;
#endif
    }
}
