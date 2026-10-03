using UnityEngine;

public class DataSave : SaveBase<DataSave>
{
    private const string KEY_MONETIZATION = "data_monetization_fls";

    public DataMonetization DataMonetization { get; private set; }

    protected override void OnLoadGame()
    {
        DataMonetization = JsonDataUtility.LoadData<DataMonetization>(KEY_MONETIZATION) ?? new DataMonetization();
        DataMonetization.Current = DataMonetization;
    }

    protected override void OnSaveGame()
    {
        JsonDataUtility.SaveData(KEY_MONETIZATION, DataMonetization);
    }

    protected override void OnNewGame()
    {
        DataMonetization = new DataMonetization();
        DataMonetization.Current = DataMonetization;
    }

    protected override void OnDeleteSave()
    {
        JsonDataUtility.DeleteData(KEY_MONETIZATION);

    }
}
