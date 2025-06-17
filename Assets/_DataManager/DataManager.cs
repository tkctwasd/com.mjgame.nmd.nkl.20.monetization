using UnityEngine;

public partial class DataManager
{
    public static GamePlayUtility GamePlayUtility { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void OnInit()
    {
        GamePlayUtility = new GamePlayUtility("gameplay_data");
    }
}

