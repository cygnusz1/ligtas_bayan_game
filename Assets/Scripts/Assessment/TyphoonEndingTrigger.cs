using System.Collections;
using UnityEngine;

namespace TyphoonScenario
{
    /// <summary>
    /// Attach to the Pasasalamat GameObject that starts disabled.
    /// When it is enabled by HFPSToggle (after the medkit is picked up),
    /// waits for the dialogue audio to finish, then shows the assessment.
    /// </summary>
    public class TyphoonEndingTrigger : MonoBehaviour
    {
        [Tooltip("Duration in seconds to wait before showing the assessment. Set this to match your dialogue audio length.")]
        [SerializeField] private float dialogueDuration = 6f;

        private void OnEnable()
        {
            StartCoroutine(TriggerAssessmentAfterDialogue());
        }

        private IEnumerator TriggerAssessmentAfterDialogue()
        {
            yield return new WaitForSeconds(dialogueDuration);

            if (TyphoonAssessmentUI.Instance != null)
                TyphoonAssessmentUI.Instance.ShowAssessment();
            else
                Debug.LogError("[TyphoonEndingTrigger] TyphoonAssessmentUI instance not found.");
        }
    }
}
