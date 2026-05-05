using System;
using Unity.Services.Core;
using Unity.Services.Core.Environments;

namespace CasualGame.IAP
{
    public static class IAPInitializer
    {
        const string k_Environment = "production";

        public static async void Initialize(Action onSuccess, Action<string> onError)
        {
            try
            {
                var options = new InitializationOptions()
                    .SetEnvironmentName(k_Environment);

                await UnityServices.InitializeAsync(options);
                onSuccess?.Invoke();
            }
            catch (Exception exception)
            {
                onError?.Invoke(exception.Message);
            }
        }
    }
}
