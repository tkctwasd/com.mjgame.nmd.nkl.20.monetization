using UnityEngine;
using UnityEngine.Events;
public class TickUpdateManager : MonoBehaviour
{
    public float SlowUpdateTime = 0.5f;
    private int regularUpdateArrayCount = 0;
    private int fixedUpdateArrayCount = 0;
    private int lateUpdateArrayCount = 0;
    private int slowUpdateArrayCount = 0;
    private TickBehaviour[] regularArray = new TickBehaviour[0];
    private TickBehaviour[] fixedArray = new TickBehaviour[0];
    private TickBehaviour[] lateArray = new TickBehaviour[0];
    private TickBehaviour[] slowArray = new TickBehaviour[0];
    private bool Initializate = false;
    private float lastSlowCall = 0;

    public static UnityAction<bool> OnPause { get; set; }
    public static UnityAction OnQuit { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        GameObject tickManager = new GameObject(nameof(TickUpdateManager));
        DontDestroyOnLoad(tickManager);
        _instance = tickManager.AddComponent<TickUpdateManager>();
    }
    /// <summary>
    /// 
    /// </summary>
    public static void AddItem(TickBehaviour behaviour)
    {
        if(Instance == null) return;
        Instance.AddItemToArray(behaviour);
    }

    /// <summary>
    /// 
    /// </summary>
    private void Start()
    {
        Initializate = true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="behaviour"></param>
    public static void RemoveSpecificItem(TickBehaviour behaviour)
    {
        if(Instance != null)
        {
            Instance.RemoveSpecificItemFromArray(behaviour);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="behaviour"></param>
    public static void RemoveSpecificItemAndDestroyIt(TickBehaviour behaviour)
    {
        Instance.RemoveSpecificItemFromArray(behaviour);

        Destroy(behaviour.gameObject);
    }

    /// <summary>
    /// 
    /// </summary>
    private void OnDestroy()
    {
        regularArray = new TickBehaviour[0];
        fixedArray = new TickBehaviour[0];
        lateArray = new TickBehaviour[0];
        slowArray = new TickBehaviour[0];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="behaviour"></param>
    private void AddItemToArray(TickBehaviour behaviour)
    {
        if(behaviour.GetType().GetMethod("OnUpdate").DeclaringType != typeof(TickBehaviour))
        {
            regularArray = ExtendAndAddItemToArray(regularArray, behaviour);
            regularUpdateArrayCount++;
        }

        if(behaviour.GetType().GetMethod("OnFixedUpdate").DeclaringType != typeof(TickBehaviour))
        {
            fixedArray = ExtendAndAddItemToArray(fixedArray, behaviour);
            fixedUpdateArrayCount++;
        }

        if(behaviour.GetType().GetMethod("OnSlowUpdate").DeclaringType != typeof(TickBehaviour))
        {
            slowArray = ExtendAndAddItemToArray(slowArray, behaviour);
            slowUpdateArrayCount++;
        }

        if(behaviour.GetType().GetMethod("OnLateUpdate").DeclaringType == typeof(TickBehaviour))
            return;

        lateArray = ExtendAndAddItemToArray(lateArray, behaviour);
        lateUpdateArrayCount++;
    }

    private int size = 0;
    public TickBehaviour[] ExtendAndAddItemToArray(TickBehaviour[] original, TickBehaviour itemToAdd)
    {
        size = original.Length;
        TickBehaviour[] finalArray = new TickBehaviour[size + 1];
        for(int i = 0; i < size; i++)
        {
            finalArray[i] = original[i];
        }
        finalArray[finalArray.Length - 1] = itemToAdd;
        return finalArray;
    }

    private void RemoveSpecificItemFromArray(TickBehaviour behaviour)
    {
        if(CheckIfArrayContainsItem(regularArray, behaviour))
        {
            regularArray = ShrinkAndRemoveItemToArray(regularArray, behaviour);
            regularUpdateArrayCount--;
        }

        if(CheckIfArrayContainsItem(fixedArray, behaviour))
        {
            fixedArray = ShrinkAndRemoveItemToArray(fixedArray, behaviour);
            fixedUpdateArrayCount--;
        }

        if(CheckIfArrayContainsItem(slowArray, behaviour))
        {
            slowArray = ShrinkAndRemoveItemToArray(slowArray, behaviour);
            slowUpdateArrayCount--;
        }

        if(!CheckIfArrayContainsItem(lateArray, behaviour)) return;

        lateArray = ShrinkAndRemoveItemToArray(lateArray, behaviour);
        lateUpdateArrayCount--;
    }

    public bool CheckIfArrayContainsItem(TickBehaviour[] arrayToCheck, TickBehaviour objectToCheckFor)
    {
        int size = arrayToCheck.Length;

        for(int i = 0; i < size; i++)
        {
            if(objectToCheckFor == arrayToCheck[i]) return true;
        }

        return false;
    }

    public TickBehaviour[] ShrinkAndRemoveItemToArray(TickBehaviour[] original, TickBehaviour itemToRemove)
    {
        int size = original.Length;
        int fix = 0;
        TickBehaviour[] finalArray = new TickBehaviour[size - 1];
        for(int i = 0; i < size; i++)
        {
            if(original[i] != itemToRemove)
            {
                finalArray[i - fix] = original[i];
            }
            else
            {
                fix++;
            }
        }
        return finalArray;
    }

    private void Update()
    {
        if(!Initializate)
            return;

        SlowUpdate();
        if(regularUpdateArrayCount == 0) return;

        for(int i = 0; i < regularUpdateArrayCount; i++)
        {
            if(regularArray[i] != null && regularArray[i].enabled)
            {
                regularArray[i].OnUpdate();
            }
        }
    }

    void SlowUpdate()
    {
        if(slowUpdateArrayCount == 0) return;
        if((Time.time - lastSlowCall) < SlowUpdateTime) return;

        lastSlowCall = Time.time;
        for(int i = 0; i < slowUpdateArrayCount; i++)
        {
            if(slowArray[i] != null && slowArray[i].enabled)
            {
                slowArray[i].OnSlowUpdate();
            }
        }
    }

    private void FixedUpdate()
    {
        if(!Initializate)
            return;

        if(fixedUpdateArrayCount == 0) return;

        for(int i = 0; i < fixedUpdateArrayCount; i++)
        {
            if(fixedArray[i] == null || !fixedArray[i].enabled) continue;

            fixedArray[i].OnFixedUpdate();
        }
    }

    private void LateUpdate()
    {
        if(!Initializate)
            return;

        if(lateUpdateArrayCount == 0) return;

        for(int i = 0; i < lateUpdateArrayCount; i++)
        {
            if(lateArray[i] == null || !lateArray[i].enabled) continue;

            lateArray[i].OnLateUpdate();
        }
    }

    private void OnApplicationPause(bool pause)
    {
        OnPause?.Invoke(pause);
    }
    private void OnApplicationQuit()
    {
        OnQuit?.Invoke();
    }

    private static TickUpdateManager _instance;
    public static TickUpdateManager Instance
    {
        get
        {
            if(_instance == null) { _instance = FindObjectOfType<TickUpdateManager>(); }
            return _instance;
        }
    }
}