using System.Collections;
using Firebase.RemoteConfig;
using UnityEngine;
using UnityEngine.Events;

namespace WASD
{
    [HelpURL("https://band-quark-9c4.notion.site/Class-Splash-1f4e87cbea3f809aa4d8f62a2936ce72?pvs=4")]
    public class Splash : MonoBehaviour
    {
        [SerializeField]
        bool isLoadingOnAwake = true;
        [Min(0.1f)]
        public float DurationLoading = 8;
        public float TimeLoadAppOpen = 5;
        public UnityEvent<float> OnProgressPercent;
        public UnityEvent OnComplete;
        public float Percent = 0;
        bool isCompleted { get; set; } = false;
        bool isCompletedAppOpen { get; set; } = false;
        float timeStart { get; set; }

        private void Awake()
        {
            if (isLoadingOnAwake)
                StartLoading();
        }
        public void StartLoading()
        {
            if (isCompleted) return;
            StartCoroutine(IE_Loading());
        }

        IEnumerator IE_Loading()
        {
            timeStart = Time.time;
            Time.timeScale = 1;
            isCompleted = false;
            OnProgressPercent?.Invoke(Percent);
            DurationLoading = Mathf.Max(0.1f, DurationLoading);
            yield return new WaitForEndOfFrame();
            // Run To Random 25%->30%        
            yield return IE_Progress(0, Random.Range(Percent, Random.Range(0.25f, 0.3f)));
            // Wait Remote Config
            yield return IE_WaitRemoteConfig();
            // Run To Random 50%->80%
            yield return IE_Progress(Percent, Random.Range(Percent, Random.Range(0.5f, 0.8f)));
            yield return new WaitForEndOfFrame();
            yield return IE_Progress(Percent, Percent + 0.1f, 3);
            // Wait Show App Open
            yield return IE_WaitShowAppOpen(TimeLoadAppOpen);
            // Run To 100%

            yield return IE_Progress(Percent, 1);
            Debug.Log("<color=green><b>Loading Completed</b></color>");
            Complete();
        }
        IEnumerator IE_Progress(float start, float end)
        {
            Percent = start;
            while (true)
            {
                yield return new WaitForEndOfFrame();
                float speed = AdsManager.Instance.IsCloseFirstAOA ? 10 : 1;

                Percent = Mathf.MoveTowards(Percent, end, speed * Time.deltaTime / DurationLoading);
                OnProgressPercent?.Invoke(Percent);
                if (Percent >= end)
                {
                    break;
                }
            }
        }
        IEnumerator IE_Progress(float start, float end, float duration)
        {
            Percent = start;
            float timeWait = Time.time + duration;

            while (Time.time <= timeWait)
            {
                yield return new WaitForEndOfFrame();

                Percent = Mathf.MoveTowards(Percent, end, Time.deltaTime / duration);
                OnProgressPercent?.Invoke(Percent);
                if (Percent >= end)
                {
                    break;
                }
            }
        }

        IEnumerator IE_WaitRemoteConfig(float duration = 3)
        {
            float timeWait = Time.time + duration;
            while (!RemoteConfigManager.IsRemoteConfigComplete && Time.time < timeWait)
            {
                yield return new WaitForEndOfFrame();
            }
            var ads = RemoteConfigManager.GetConfigValue<AdsConfig>(RemoteKey.AdsConfig);
            Debug.Log(ads.DurationLoading);
            DurationLoading = ads.DurationLoading;
            TimeLoadAppOpen = ads.TimeLoadAppOpen;
        }


        IEnumerator IE_WaitShowAppOpen(float duration = 3)
        {
            yield return new WaitForEndOfFrame();
            float timeWait = Time.time + duration;

            float percentTarget = (Percent + 0.95f) / 2;
            percentTarget = Mathf.Clamp(percentTarget, Percent, 0.95f);

            Debug.Log("Wait Show App Open " + Percent + " to " + percentTarget + " in " + duration);

            var coroutine = StartCoroutine(IE_Progress(Percent, percentTarget, duration));

            var ads = AdsManager.Instance;

            while (!isCompletedAppOpen && Time.time < timeWait)
            {
                yield return new WaitForEndOfFrame();

                if (!ads.IsShowFirstAOA && ads.AppOpenModule.CanShowAOA(true))
                {
                    Debug.Log("Show App Open");
                    ads.AppOpenModule.ShowAppOpen(true);
                    isCompletedAppOpen = true;
                    StopCoroutine(coroutine);
                    break;
                }
            }

            Debug.Log("Wait Show App Open Completed " + isCompletedAppOpen);
        }

        public void Complete()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                LogEventParameter.Create("internet_not_reachable")
                     .AddParam("status", "offline").Send();
            }
            isCompleted = true;
            OnComplete?.Invoke();
        }
    }
}