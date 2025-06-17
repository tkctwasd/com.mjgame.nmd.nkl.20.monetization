using UnityEngine;

public class DataUtility<T> where T : class
{
    public T Profile;
    IData<T> typeData;
    string key;
    public DataUtility(string key)
    {
        this.key = key;
        Init();
    }

    public virtual void Init()
    {
        typeData = new PlayerPrefsData<T>(key);
        LoadData();
        Application.focusChanged += Application_focusChanged;
        TickUpdateManager.OnPause += Application_pauseChanged;
        Application.quitting += Application_quitting;
    }

    void LoadData()
    {
        Profile = typeData.LoadData();
    }

    public void Save()
    {
        string data = JsonUtility.ToJson(Profile);
        typeData.Save(data);
    }

    private void Application_focusChanged(bool isFocus)
    {
        if (!isFocus)
        {
            Save();
        }
    }

    private void Application_pauseChanged(bool isPause)
    {
        if(isPause) Save();
    }
    
    private void Application_quitting()
    {
        Debug.Log("Save");
        Save();
    }
}
