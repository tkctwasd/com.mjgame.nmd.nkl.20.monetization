namespace A
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Firebase.Extensions;
    using System;

    public class FirebaseManager : MonoBehaviour
    {
        public static FirebaseManager Instance;
        public static event Action InitDone;

        private float timeInit;
        internal bool IsInitialized = false;

        void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            while (!IsInitialized && (timeInit < 60f))
            {
                timeInit += 1f;
                try
                {
                    Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                    {
                        Firebase.DependencyStatus dependencyStatus = task.Result;
                        if (dependencyStatus == Firebase.DependencyStatus.Available)
                        {
                            Debug.Log("Firebase is ready!");
                            IsInitialized = true;
                            Firebase.FirebaseApp app = Firebase.FirebaseApp.DefaultInstance;

                            InitDone?.Invoke();
                        }
                        else
                        {
                            Debug.Log("Could not resolve all Firebase dependencies: " + dependencyStatus);
                        }
                    });

                }
                catch (System.Exception) { }
                yield return new WaitForSeconds(1f);
            }
        }
    }

}