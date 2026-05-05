// ─────────────────────────────────────────────────────────────────────────────
//  IAPConstants.cs
//  ScriptableObject chứa tất cả Product ID — tương tự AdConstants.
//  Tạo asset: Assets > Create > IAP > IAP Constants
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IAPConstants", menuName = "IAP/IAP Constants")]
public class IAPConstants : ScriptableObject
{
    [Header("Non-Consumable")]
    [ColorLabel(1f, 0.65f, 0f)] public string RemoveAdsId = "com.yourcompany.game.removeads";

    [Header("Consumable")]
    [ColorLabel(0f, 0.65f, 1f)]
    public List<string> ConsumableIds = new()
    {
        "com.yourcompany.game.coins_small",
    };

    [Header("Subscription")]
    public List<string> SubscriptionIds = new()
    {
        "com.yourcompany.game.weekly",
    };
}