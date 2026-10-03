using System;


#region Monetization
[Serializable]
public class DataMonetization
{
    /// <summary>
    /// Instance dang dung. DataSave cua game gan lai sau khi load/new game:
    /// <code>DataMonetization.Current = DataMonetization;</code>
    /// </summary>
    public static DataMonetization Current { get; set; } = new DataMonetization();

    public bool IsRemoveAdsPurchased;

    public DataMonetization()
    {
        IsRemoveAdsPurchased = false;
    }
    public void RemoveAdsPurchased()
    {
        IsRemoveAdsPurchased = true;
    }

}

#endregion
