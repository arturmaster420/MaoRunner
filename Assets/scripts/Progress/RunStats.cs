// MaoRunner / Progress / RunStats.cs
using UnityEngine;

namespace MaoRunner.Progress
{
    public class RunStats : MonoBehaviour
    {
        public static RunStats Instance { get; private set; }

        public string keyTotalRuns = "stats_total_runs";
        public string keyTotalTime = "stats_total_time";
        public string keyMaxDistance = "stats_max_distance";
        public string keyMaxKills = "stats_max_kills";

        float runTime;
        public float currentDistance { get; private set; }
        public int currentKills { get; private set; }

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartRun()
        {
            runTime = 0f;
            currentDistance = 0f;
            currentKills = 0;
        }

        public void EndRun()
        {
            PlayerPrefs.SetInt(keyTotalRuns, PlayerPrefs.GetInt(keyTotalRuns, 0) + 1);
            PlayerPrefs.SetFloat(keyTotalTime, PlayerPrefs.GetFloat(keyTotalTime, 0f) + runTime);
            var maxDist = PlayerPrefs.GetFloat(keyMaxDistance, 0f);
            if (currentDistance > maxDist) PlayerPrefs.SetFloat(keyMaxDistance, currentDistance);
            var maxKills = PlayerPrefs.GetInt(keyMaxKills, 0);
            if (currentKills > maxKills) PlayerPrefs.SetInt(keyMaxKills, currentKills);
            PlayerPrefs.Save();
        }

        void Update()
        {
            runTime += Time.deltaTime;
        }

        public void AddDistance(float d) => currentDistance += d;
        public void AddKill(int k=1) => currentKills += k;

        public int TotalRuns => PlayerPrefs.GetInt(keyTotalRuns, 0);
        public float TotalTime => PlayerPrefs.GetFloat(keyTotalTime, 0f);
        public float MaxDistance => PlayerPrefs.GetFloat(keyMaxDistance, 0f);
        public int MaxKills => PlayerPrefs.GetInt(keyMaxKills, 0);
    }
}
