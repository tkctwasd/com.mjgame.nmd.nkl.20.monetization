using System;


#region Monetization
[Serializable]
public class DataMonetization
{
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
