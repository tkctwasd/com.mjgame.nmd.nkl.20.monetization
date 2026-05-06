#if USE_IAP
using System;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

public static class IAPService
{
    const string k_Environment = "production";

    public static async void Initialize(Action onSuccess, Action<string> onError)
    {
        try
        {
            var options = new InitializationOptions()
                .SetEnvironmentName(k_Environment);

            await UnityServices.InitializeAsync(options);
            Debug.Log("IAPService".IAP("Unity Services Initialized"));
            onSuccess?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("IAPService".IAP($"Init failed: {e.Message}"));
            onError?.Invoke(e.Message);
        }
    }
}
#endif