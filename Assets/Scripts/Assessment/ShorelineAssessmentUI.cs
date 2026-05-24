using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using HFPS.Systems;

namespace ShorelineEarthquake
{
    /// <summary>
    /// Drives the end-game Assessment Panel for Shoreline_Earthquake.
    ///
    /// Single-slide mode  — leave statsSlide/rewardSlide null. AssessmentPanel itself is the stats view.
    ///                      nextButton and all reward fields can be left unassigned.
    /// Two-slide mode     — assign statsSlide, rewardSlide, nextButton, and all reward fields.
    ///                      Slide 1 shows stats; Next cross-fades to Slide 2 (reward/stars/badge).
    ///
    /// Hook ShowAssessmentAfterDialogue() to the final Objective's CompleteEvent in the Inspector,
    /// or use ShorelineEndingTrigger which calls ShowAssessment() directly.
    /// </summary>
    public class ShorelineAssessmentUI : MonoBehaviour
    {
        public static ShorelineAssessmentUI Instance { get; private set; }

        [Header("Panel")]
        [SerializeField] private GameObject assessmentPanel;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private float fadeInDuration = 1f;

        [Header("Stats Slide (optional — leave null for single-slide mode)")]
        [SerializeField] private GameObject statsSlide;
        [SerializeField] private CanvasGroup statsSlideCanvasGroup;

        [Header("Stats Text")]
        [SerializeField] private TMP_Text timeTakenText;
        [SerializeField] private TMP_Text survivalRatingText;
        [SerializeField] private TMP_Text ratingDescriptionText;
        [SerializeField] private TMP_Text learnedText;

        [Header("Reward Slide (optional — leave null for single-slide mode)")]
        [SerializeField] private GameObject rewardSlide;
        [SerializeField] private CanvasGroup rewardSlideCanvasGroup;
        [SerializeField] private Image badgeImage;
        [SerializeField] private Sprite goldBadgeSprite;
        [SerializeField] private Sprite silverBadgeSprite;
        [SerializeField] private Sprite bronzeBadgeSprite;
        [SerializeField] private TMP_Text rewardBadgeNameText;
        [SerializeField] private Image[] rewardStarImages;
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;
        [SerializeField] private TMP_Text motivationalText;

        [Header("Dialogue Delay")]
        [SerializeField] private float assessmentDelay = 40f;
        [SerializeField] private int magalingObjectiveID = 5;

        [Header("Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool _assessmentStarted = false;
        private ShorelineAssessmentData _lastData;

        private bool TwoSlideMode => statsSlide != null && rewardSlide != null;

        private const string LearnedContent =
            "• During an earthquake, DROP to the ground, take COVER under a sturdy table, and HOLD ON.\n\n" +
            "• Stay away from windows, heavy furniture, and unsecured objects that may fall.\n\n" +
            "• After shaking stops, check for injuries. Aftershocks may follow.\n\n" +
            "• Move to open ground away from buildings and power lines.\n\n" +
            "• Know your evacuation route and assembly point before a disaster happens.";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            assessmentPanel.SetActive(false);

            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        /// <summary>
        /// Called by the ObjectiveEvent on the final objective when the player reaches the endpoint.
        /// Waits for the dialogue to finish, completes the final objective, then shows the assessment.
        /// </summary>
        public void ShowAssessmentAfterDialogue()
        {
            if (_assessmentStarted) return;
            _assessmentStarted = true;
            StartCoroutine(DelayedAssessment());
        }

        private IEnumerator DelayedAssessment()
        {
            yield return new WaitForSeconds(assessmentDelay);

            if (ObjectiveManager.HasReference)
                ObjectiveManager.Instance.CompleteObjective(magalingObjectiveID);

            ShowAssessment();
        }

        /// <summary>
        /// Stops the timer, populates the stats slide, and fades in the assessment panel.
        /// </summary>
        public void ShowAssessment()
        {
            if (ShorelineAssessmentTracker.Instance == null)
            {
                Debug.LogError("[ShorelineAssessmentUI] ShorelineAssessmentTracker not found in scene.");
                return;
            }

            _lastData = ShorelineAssessmentTracker.Instance.FinalizeAndGetData();
            PopulateStatsSlide(_lastData);

            if (TwoSlideMode)
            {
                rewardSlide.SetActive(false);
                statsSlide.SetActive(true);
            }

            assessmentPanel.SetActive(true);
            StartCoroutine(FadeIn(panelCanvasGroup));

            if (HFPS_GameManager.HasReference)
                HFPS_GameManager.Instance.isLocked = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void PopulateStatsSlide(ShorelineAssessmentData data)
        {
            if (timeTakenText != null)
                timeTakenText.text = $"Time Taken:  {data.FormattedTime()}";

            if (survivalRatingText != null)
                survivalRatingText.text = data.SurvivalRating();

            if (ratingDescriptionText != null)
                ratingDescriptionText.text = data.RatingDescription();

            if (learnedText != null)
                learnedText.text = LearnedContent;
        }

        /// <summary>Called by the Next button — populates and cross-fades to the reward slide.</summary>
        private void OnNextClicked()
        {
            if (_lastData == null || !TwoSlideMode) return;
            PopulateRewardSlide(_lastData);
            StartCoroutine(CrossFade(statsSlideCanvasGroup, statsSlide, rewardSlideCanvasGroup, rewardSlide));
        }

        private void PopulateRewardSlide(ShorelineAssessmentData data)
        {
            ShorelineAssessmentData.RewardData reward = data.GetReward();

            if (rewardBadgeNameText != null)
                rewardBadgeNameText.text = reward.BadgeName;

            if (badgeImage != null)
            {
                badgeImage.sprite = reward.Tier switch
                {
                    ShorelineAssessmentData.BadgeTier.Gold   => goldBadgeSprite,
                    ShorelineAssessmentData.BadgeTier.Silver => silverBadgeSprite,
                    _                                        => bronzeBadgeSprite
                };
            }

            if (motivationalText != null)
                motivationalText.text = GetMotivationalMessage(data.TotalSeconds);

            if (rewardStarImages == null || rewardStarImages.Length != 3)
            {
                Debug.LogWarning("[ShorelineAssessmentUI] rewardStarImages must contain exactly 3 elements.");
                return;
            }

            for (int i = 0; i < rewardStarImages.Length; i++)
            {
                if (rewardStarImages[i] == null)
                {
                    Debug.LogWarning($"[ShorelineAssessmentUI] rewardStarImages[{i}] is null.");
                    continue;
                }
                rewardStarImages[i].sprite = i < reward.Stars ? starFilledSprite : starEmptySprite;
            }
        }

        private static string GetMotivationalMessage(float totalSeconds)
        {
            if (totalSeconds <= 180f) return "Exceptional! You kept calm and acted fast — a true lifesaver.";
            if (totalSeconds <= 300f) return "Great job! Your swift response made all the difference.";
            if (totalSeconds <= 480f) return "Well done! Keep practicing to sharpen your response time.";
            return "Good effort! Review the procedures and try again — you'll get there.";
        }

        private IEnumerator CrossFade(
            CanvasGroup fromCG, GameObject fromGO,
            CanvasGroup toCG,   GameObject toGO)
        {
            float elapsed = 0f;
            float startAlpha = fromCG.alpha;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fromCG.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeInDuration);
                yield return null;
            }
            fromCG.alpha = 0f;
            fromGO.SetActive(false);

            toCG.alpha = 0f;
            toGO.SetActive(true);
            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                toCG.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            toCG.alpha = 1f;
        }

        private IEnumerator FadeIn(CanvasGroup cg)
        {
            cg.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            cg.alpha = 1f;
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
