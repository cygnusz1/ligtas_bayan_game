using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HFPS.Systems;

namespace RuralEarthquake
{
    /// <summary>
    /// Manages the slide-by-slide tutorial panel shown at scene start.
    /// Locks the HFPS player, cycles through serialized slide GameObjects,
    /// then unlocks the player when the last slide is dismissed.
    /// Hook the Next button's onClick to OnNextButtonClicked().
    /// </summary>
    public class TutorialSlideController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Slides")]
        // Each entry is a child GameObject you design (one panel per mechanic).
        [SerializeField] private GameObject[] slides;

        [Header("Controls")]
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text nextButtonLabel;
        [SerializeField] private TMP_Text slideCounterText;

        [Header("Post-Tutorial Activation")]
        [Tooltip("GameObjects to activate once the tutorial finishes.")]
        [SerializeField] private GameObject[] postTutorialObjects;

        private const string NextLabel = "Next ▶";
        private const string StartLabel = "Start!";

        private int _currentIndex = 0;
        private bool _tutorialActive = false;

        private void Awake()
        {
            // Hide panel immediately to avoid a one-frame flicker.
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);

            // NOTE: Do NOT call nextButton.onClick.AddListener here.
            // The button is already wired in the Inspector. Adding a second listener
            // causes every click to fire OnNextButtonClicked twice, skipping slides.
        }

        private void Start()
        {
            if (slides == null || slides.Length == 0)
            {
                Debug.LogWarning("[TutorialSlideController] No slides assigned — skipping tutorial.");
                return;
            }

            // Wait one frame so HFPS_GameManager finishes its own Start/Awake
            // before we try to lock the cursor and player input.
            StartCoroutine(ShowTutorialNextFrame());
        }

        private void Update()
        {
            // HFPS re-locks the cursor in its own Update every frame.
            // We must continuously override it for the duration of the tutorial.
            if (_tutorialActive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private IEnumerator ShowTutorialNextFrame()
        {
            // Wait for end of frame so all other Start() methods (including HFPS) have run.
            yield return new WaitForEndOfFrame();
            ShowTutorial();
        }

        /// <summary>
        /// Activates the tutorial panel, locks the player, and shows the first slide.
        /// </summary>
        private void ShowTutorial()
        {
            _tutorialActive = true;

            // Lock HFPS player input.
            if (HFPS_GameManager.HasReference)
                HFPS_GameManager.Instance.isLocked = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Time.timeScale = 0f;

            tutorialPanel.SetActive(true);
            _currentIndex = 0;
            ShowSlide(_currentIndex);

            StartCoroutine(FadeIn());
        }

        /// <summary>
        /// Activates the slide at the given index and deactivates all others.
        /// Updates the counter text and the Next button label.
        /// </summary>
        private void ShowSlide(int index)
        {
            for (int i = 0; i < slides.Length; i++)
            {
                if (slides[i] != null)
                    slides[i].SetActive(i == index);
            }

            bool isLastSlide = index == slides.Length - 1;

            if (slideCounterText != null)
                slideCounterText.text = $"{index + 1} / {slides.Length}";

            if (nextButtonLabel != null)
                nextButtonLabel.text = isLastSlide ? StartLabel : NextLabel;
        }

        /// <summary>
        /// Called by the Next button's onClick (wired in Inspector only) —
        /// advances to the next slide, or ends the tutorial on the last slide.
        /// </summary>
        public void OnNextButtonClicked()
        {
            if (_currentIndex < slides.Length - 1)
            {
                _currentIndex++;
                ShowSlide(_currentIndex);
            }
            else
            {
                EndTutorial();
            }
        }

        /// <summary>
        /// Fades out the panel, restores time scale, and unlocks the player.
        /// </summary>
        private void EndTutorial()
        {
            StartCoroutine(FadeOutAndClose());
        }

        private IEnumerator FadeIn()
        {
            panelCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;
        }

        private IEnumerator FadeOutAndClose()
        {
            // Stop enforcing cursor override before we give control back.
            _tutorialActive = false;

            // Prevent the button from being clicked during the fade.
            if (nextButton != null)
                nextButton.interactable = false;

            float elapsed = 0f;
            float startAlpha = panelCanvasGroup.alpha;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(startAlpha * (1f - elapsed / fadeDuration));
                yield return null;
            }
            panelCanvasGroup.alpha = 0f;

            tutorialPanel.SetActive(false);

            // Restore game state.
            Time.timeScale = 1f;

            if (HFPS_GameManager.HasReference)
                HFPS_GameManager.Instance.isLocked = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Activate any gameplay objects that should only start after the tutorial ends.
            if (postTutorialObjects != null)
            {
                foreach (var obj in postTutorialObjects)
                {
                    if (obj != null)
                        obj.SetActive(true);
                }
            }
        }
    }
}
