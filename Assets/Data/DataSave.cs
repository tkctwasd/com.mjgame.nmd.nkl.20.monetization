using UnityEngine;

public class DataSave : Singleton<DataSave>
{
    public DataMonetization DataMonetization { get; private set; }

    // Keys
    private const string KEY_MONETIZATION = "data_monetization_re";

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
        Debug.Log("<color=orange>[SaveSystem]</color> All data saved.");
    }

    public void LoadGame()
    {
        DataMonetization = JsonDataUtility.LoadData<DataMonetization>(KEY_MONETIZATION) ?? new DataMonetization();

        Debug.Log("<color=cyan>[SaveSystem]</color> All data loaded.");
    }

    public void NewGame()
    {
        DataMonetization = new DataMonetization();

        SaveGame();
        Debug.Log("<color=orange>[SaveSystem]</color> New game started.");
    }

    public void DeleteSave()
    {
        JsonDataUtility.DeleteData(KEY_MONETIZATION);

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
