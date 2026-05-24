using UnityEngine;
using HFPS.Player;

namespace RuralEarthquake
{
    /// <summary>
    /// Drives a sustained camera shake for the duration of the Drop, Hold, and Cover phase.
    /// Place this GameObject in the TutorialSlideController's postTutorialObjects list so it
    /// activates (and begins shaking) the moment the tutorial panel is dismissed.
    /// Wire StopShake() to the CompleteEvent on the objective GameObjects to stop it gracefully.
    /// </summary>
    public class EarthquakeShakeController : MonoBehaviour
    {
        private const float DefaultMagnitude  = 1.8f;  // 6.5 Richter feel
        private const float DefaultRoughness  = 4.5f;
        private const float DefaultFadeInTime = 2.0f;
        private const float DefaultFadeOutTime = 3.0f;

        [Header("Shake Parameters")]
        [SerializeField] private float magnitude   = DefaultMagnitude;
        [SerializeField] private float roughness   = DefaultRoughness;
        [SerializeField] private float fadeInTime  = DefaultFadeInTime;
        [SerializeField] private float fadeOutTime = DefaultFadeOutTime;

        private CameraShakeInstance _activeShake;

        private void OnEnable()
        {
            StartShake();
        }

        /// <summary>
        /// Begins the sustained earthquake shake. Called automatically via OnEnable when this
        /// GameObject is activated by the TutorialSlideController's postTutorialObjects list.
        /// </summary>
        public void StartShake()
        {
            if (CameraShaker.Instance == null)
            {
                Debug.LogWarning("[EarthquakeShakeController] CameraShaker instance not found.");
                return;
            }

            _activeShake = CameraShaker.Instance.StartShake(magnitude, roughness, fadeInTime);
        }

        /// <summary>
        /// Fades out and stops the sustained shake. Wire this to the CompleteEvent UnityEvent
        /// on both the "Drop, Hold, and Cover" and "Manatili" objective GameObjects.
        /// </summary>
        public void StopShake()
        {
            if (_activeShake == null)
                return;

            _activeShake.StartFadeOut(fadeOutTime);
            _activeShake = null;
        }
    }
}
