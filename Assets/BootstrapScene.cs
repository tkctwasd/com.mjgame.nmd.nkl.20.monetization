using UnityEngine;

public static class BootstrapScene
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        Debug.Log("<color=yellow>[BootstrapScene]</color> <color=white> Load Instance Dont Destroy ...</color>");
        TryLoad("Ads");
        TryLoad("Canvas");
        TryLoad("DataSave");
        TryLoad("GameManager");
        TryLoad("GameController");
        TryLoad("PoolManager");
        TryLoad("MainCamController");
        TryLoad("AppReviewManager");
        TryLoad("IAPManager");
        // TryLoad("Hacking");
        Debug.Log("<color=yellow>[BootstrapScene]</color> <color=white> Load Instance </color><color=green>Complete</color>");
    }

    static void TryLoad(string prefabName)
    {
        if (GameObject.Find(prefabName) == null)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            if (prefab != null)
            {
                GameObject instance = Object.Instantiate(prefab);
                instance.name = prefabName;
                Object.DontDestroyOnLoad(instance);
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy prefab {prefabName} trong Resources!");
            }
        }
    }
}
