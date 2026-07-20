using UnityEngine;
using Newtonsoft.Json;

public static class JsonDataUtility
{
    public static void SaveData<T>(string key, T data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        // string json = JsonConvert.SerializeObject(data, Formatting.None);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public static T LoadData<T>(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return default;
        string json = PlayerPrefs.GetString(key);
        var s = JsonConvert.DeserializeObject<T>(json);
        return s;
    }

    public static void DeleteData(string key)
    {
        if (PlayerPrefs.HasKey(key))
            PlayerPrefs.DeleteKey(key);
    }
}