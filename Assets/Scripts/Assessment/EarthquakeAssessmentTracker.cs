using UnityEngine;

namespace RuralEarthquake
{
    /// <summary>
    /// Tracks elapsed time since the scene started.
    /// Call FinalizeAndGetData() when the game ends to stop the timer and retrieve session data.
    /// </summary>
    public class EarthquakeAssessmentTracker : MonoBehaviour
    {
        public static EarthquakeAssessmentTracker Instance { get; private set; }

        private float _elapsedSeconds;
        private bool _isTracking = true;

        public float ElapsedSeconds => _elapsedSeconds;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (_isTracking)
                _elapsedSeconds += Time.deltaTime;
        }

        /// <summary>Stops the timer and returns a snapshot of the session data.</summary>
        public AssessmentData FinalizeAndGetData()
        {
            _isTracking = false;
            return new AssessmentData(_elapsedSeconds);
        }
    }

    public sealed class AssessmentData
    {
        private const float OutstandingThreshold = 180f;  // under 3 min
        private const float GoodThreshold = 300f;  // under 5 min
        private const float AdequateThreshold = 480f;  // under 8 min

        public readonly float TotalSeconds;

        public AssessmentData(float totalSeconds)
        {
            TotalSeconds = totalSeconds;
        }

        /// <summary>Returns formatted time, e.g. "4m 32s" or "45s".</summary>
        public string FormattedTime()
        {
            int minutes = Mathf.FloorToInt(TotalSeconds / 60f);
            int seconds = Mathf.FloorToInt(TotalSeconds % 60f);
            return minutes > 0 ? $"{minutes}m {seconds}s" : $"{seconds}s";
        }

        /// <summary>Returns a survival rating label based on completion time.</summary>
        public string SurvivalRating()
        {
            if (TotalSeconds <= OutstandingThreshold) return "Outstanding";
            if (TotalSeconds <= GoodThreshold) return "Good";
            if (TotalSeconds <= AdequateThreshold) return "Adequate";
            return "Needs Improvement";
        }

        /// <summary>Short description shown below the rating label.</summary>
        public string RatingDescription()
        {
            if (TotalSeconds <= OutstandingThreshold)
                return "You responded with exceptional speed and calm under pressure.";
            if (TotalSeconds <= GoodThreshold)
                return "You handled the situation efficiently and with good judgment.";
            if (TotalSeconds <= AdequateThreshold)
                return "You completed the scenario, but there is room for faster response.";
            return "Your response time was slow. Practice the procedures to improve.";
        }

        /// <summary>Returns a 1–3 star reward with a badge name and tier based on completion time.</summary>
        public RewardData GetReward()
        {
            if (TotalSeconds <= OutstandingThreshold) return new RewardData(3, "Calm Under Pressure", BadgeTier.Gold);
            if (TotalSeconds <= GoodThreshold)        return new RewardData(3, "Swift Responder",     BadgeTier.Gold);
            if (TotalSeconds <= AdequateThreshold)    return new RewardData(2, "Prepared Survivor",   BadgeTier.Silver);
            return new RewardData(1, "Needs More Practice", BadgeTier.Bronze);
        }

        public enum BadgeTier { Gold, Silver, Bronze }

        /// <summary>Encapsulates star count, badge name, and badge tier for the end-screen reward display.</summary>
        public readonly struct RewardData
        {
            public readonly int Stars;
            public readonly string BadgeName;
            public readonly BadgeTier Tier;

            public RewardData(int stars, string badgeName, BadgeTier tier)
            {
                Stars = stars;
                BadgeName = badgeName;
                Tier = tier;
            }
        }
    }
}
