using UnityEngine;

public class DataSave : Singleton<DataSave>
{
    public DataMonetization DataMonetization { get; private set; }
    public DataExample DataExample { get; private set; }

    // Keys
    private const string KEY_MONETIZATION = "data_monetization_re";
    private const string KEY_EXAMPLE = "data_example_re";

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        LoadGame();
    }

    #region Save/Load

    public void SaveGame()
    {
        JsonDataUtility.SaveData(KEY_MONETIZATION, DataMonetization);
        JsonDataUtility.SaveData(KEY_EXAMPLE, DataExample);
        Debug.Log("<color=orange>[SaveSystem]</color> All data saved.");
    }

    public void LoadGame()
    {
        DataMonetization = JsonDataUtility.LoadData<DataMonetization>(KEY_MONETIZATION) ?? new DataMonetization();
        DataExample = JsonDataUtility.LoadData<DataExample>(KEY_EXAMPLE) ?? new DataExample();

        Debug.Log("<color=cyan>[SaveSystem]</color> All data loaded.");
    }

    public void NewGame()
    {
        DataMonetization = new DataMonetization();
        DataExample = new DataExample();

        SaveGame();
        Debug.Log("<color=orange>[SaveSystem]</color> New game started.");
    }

    public void DeleteSave()
    {
        JsonDataUtility.DeleteData(KEY_MONETIZATION);
        JsonDataUtility.DeleteData(KEY_EXAMPLE);

        Debug.Log("<color=red>[SaveSystem]</color> All data deleted.");
    }

    #endregion

    #region Unity Events

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveGame();
    }

    #endregion
}
