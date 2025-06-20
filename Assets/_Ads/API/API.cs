using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class API
{
    static List<IService> services = new List<IService>();
    static JObject info = null;
    static bool isInitialized = false;
    public static JObject Info
    {
        get
        {
            if(info == null)
            {
                var txtInfo = Resources.Load<TextAsset>("Info");
                if(txtInfo != null)
                {
                    info = JObject.Parse(txtInfo.text);
                }
            }
            if(info == null)
            {
                Debug.LogError("Info not found");
            }
            return info;
        }
        private set
        {
            if(value != null)
            {
                info = value;
            }
        }
    }

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        if(isInitialized)
        {
            return;
        }
        isInitialized = true;
        services = new List<IService>();
        AddService(new ServiceTracking());
        AddService(new ServiceRemoteConfig());
        AddService(new ServiceLogEvent());
        AddService(new ServiceAds());
        System.Threading.Thread.Sleep(250);
        foreach(var item in services)
        {
            item.Initialize();
        }
        Wasd.LogStateInitialize("API");
    }
    public static void StartCoroutine(IEnumerator routine)
    {
        TickUpdateManager.Instance.StartCoroutine(routine);
    }
    static void AddService<T>(T service) where T : IService
    {
        if(services.Contains(service) == false)
            services.Add(service);
    }
    public static T Get<T>() where T : IService
    {
        for(int i = 0; i < services.Count; i++)
        {
            if(services[i] is T)
            {
                return (T)services[i];
            }
        }
        return default;
    }
}
