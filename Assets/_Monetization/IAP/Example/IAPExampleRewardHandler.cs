using UnityEngine;

/// <summary>
/// Điểm nối Example vào core. Lắng nghe <see cref="MonetizationEntitlements.OnGrantReward"/>
/// để cấp Money/Gem/Shield vào <see cref="DataExample"/>.
///
/// Toàn bộ hiểu biết về kinh tế game (Money/Gem/Shield/Brainrot) nằm ở đây, KHÔNG ở core.
/// Xóa cả thư mục Example → core vẫn build & chạy (reward game chỉ đơn giản là không có ai xử lý).
/// </summary>
public static class IAPExampleRewardHandler
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        // Static event → chỉ cần đăng ký một lần cho vòng đời app.
        MonetizationEntitlements.OnGrantReward -= OnGrantReward;
        MonetizationEntitlements.OnGrantReward += OnGrantReward;
    }

    static void OnGrantReward(IAPReward reward, System.DateTime? expiry)
    {
        var wallet = Wallet;
        if (wallet == null) return;

        switch (reward.RewardType)
        {
            case IAPRewardType.Money:
                wallet.AddMoney(reward.Money);
                break;

            case IAPRewardType.Gem:
                wallet.AddGem(reward.Gem);
                break;

            case IAPRewardType.Shield:
                wallet.AddShield(reward.ShieldCount);
                break;

            default:
                // Brainrot... hiển thị do UI xử lý riêng; ví dụ này không cộng vào ví.
                Debug.LogWarning("ExampleReward".IAP($"Chưa xử lý reward '{reward.RewardType}'."));
                return;
        }

        Save();
    }

    #region Wallet access

    static DataExample Wallet => DataSave.Instance != null ? DataSave.Instance.DataExample : null;

    public static double Money => Wallet != null ? Wallet.Money : 0;
    public static int Gem => Wallet != null ? Wallet.Gem : 0;
    public static int Shield => Wallet != null ? Wallet.Shield : 0;

    static void Save()
    {
        if (DataSave.Instance != null) DataSave.Instance.SaveGame();
    }

    #endregion
}
