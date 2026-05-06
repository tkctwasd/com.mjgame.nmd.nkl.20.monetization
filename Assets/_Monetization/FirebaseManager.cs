using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Extensions;
using System;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;
    public static event Action InitDone;
    internal bool IsInitialized = false;

    void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {

        var task = Firebase.FirebaseApp.CheckAndFixDependenciesAsync();

        yield return new WaitUntil(() => task.IsCompleted);

        try
        {
            if (task.Result == Firebase.DependencyStatus.Available)
            {
                Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;

                IsInitialized = true;

                Debug.Log("FirebaseManager".Firebase("Firebase is ready!"));

                InitDone?.Invoke();
            }
            else
            {
                Debug.LogError("FirebaseManager".Firebase($"Could not resolve all Firebase dependencies: {task.Result}"));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("FirebaseManager".Firebase($"Init exception: {e.Message}"));
        }
    }
}
