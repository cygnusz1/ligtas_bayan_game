using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using HFPS.Systems;

namespace MountainsideEarthquake
{
    /// <summary>
    /// Drives the end-game Assessment Panel for MountainSide_Earthquake.
    ///
    /// Single-slide mode  — leave statsSlide/rewardSlide null. AssessmentPanel itself is the stats view.
    ///                      nextButton and all reward fields can be left unassigned.
    /// Two-slide mode     — assign statsSlide, rewardSlide, nextButton, and all reward fields.
    ///                      Slide 1 shows stats; Next cross-fades to Slide 2 (reward/stars/badge).
    ///
    /// Hook ShowAssessmentAfterDialogue() to the final Objective's CompleteEvent in the Inspector,
    /// or use MountainsideEndingTrigger which calls ShowAssessment() directly.
    /// </summary>
    public class MountainsideAssessmentUI : MonoBehaviour
    {
        public static MountainsideAssessmentUI Instance { get; private set; }

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
        [SerializeField] private float assessmentDelay = 21f;
        [SerializeField] private int magalingObjectiveID = 5;

        [Header("Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool _assessmentStarted = false;
        private MountainsideAssessmentData _lastData;

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
        /// Called by the ObjectiveEvent on the final objective.
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

        /// <summary>Stops the timer, populates the UI, and fades in the assessment panel.</summary>
        public void ShowAssessment()
        {
            if (MountainsideAssessmentTracker.Instance == null)
            {
                Debug.LogError("[MountainsideAssessmentUI] MountainsideAssessmentTracker not found in scene.");
                return;
            }

            _lastData = MountainsideAssessmentTracker.Instance.FinalizeAndGetData();
            PopulateStatsSlide(_lastData);

            if (TwoSlideMode)
            {
                rewardSlide.SetActive(false);
                statsSlide.SetActive(true);
            }

            assessmentPanel.SetActive(true);
            StartCoroutine(FadeIn());

            if (HFPS_GameManager.HasReference)
                HFPS_GameManager.Instance.isLocked = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void PopulateStatsSlide(MountainsideAssessmentData data)
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

        private void PopulateRewardSlide(MountainsideAssessmentData data)
        {
            MountainsideAssessmentData.RewardData reward = data.GetReward();

            if (rewardBadgeNameText != null)
                rewardBadgeNameText.text = reward.BadgeName;

            if (badgeImage != null)
            {
                badgeImage.sprite = reward.Tier switch
                {
                    MountainsideAssessmentData.BadgeTier.Gold   => goldBadgeSprite,
                    MountainsideAssessmentData.BadgeTier.Silver => silverBadgeSprite,
                    _                                           => bronzeBadgeSprite
                };
            }

            if (motivationalText != null)
                motivationalText.text = GetMotivationalMessage(reward.Stars);

            if (rewardStarImages == null || rewardStarImages.Length != 3)
            {
                Debug.LogWarning("[MountainsideAssessmentUI] rewardStarImages must contain exactly 3 elements.");
                return;
            }

            for (int i = 0; i < rewardStarImages.Length; i++)
            {
                if (rewardStarImages[i] == null)
                {
                    Debug.LogWarning($"[MountainsideAssessmentUI] rewardStarImages[{i}] is null.");
                    continue;
                }
                rewardStarImages[i].sprite = i < reward.Stars ? starFilledSprite : starEmptySprite;
            }
        }

        private IEnumerator CrossFade(CanvasGroup from, GameObject fromGO, CanvasGroup to, GameObject toGO)
        {
            const float duration = 0.4f;
            toGO.SetActive(true);
            to.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                from.alpha = 1f - t;
                to.alpha = t;
                yield return null;
            }

            from.alpha = 0f;
            to.alpha = 1f;
            fromGO.SetActive(false);
        }

        private static string GetMotivationalMessage(int stars)
        {
            return stars switch
            {
                3 => "Excellent work! You responded swiftly and made all the right decisions.",
                2 => "Good job! With more practice, you can reach peak performance.",
                _ => "Keep practicing! Every drill makes you better prepared for the real thing."
            };
        }

        private IEnumerator FadeIn()
        {
            panelCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
