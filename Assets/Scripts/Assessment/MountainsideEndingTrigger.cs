using System.Collections;
using UnityEngine;

namespace MountainsideEarthquake
{
    /// <summary>
    /// Attach to the ending GameObject that starts disabled.
    /// When it is enabled (by the previous trigger chain), waits for the
    /// dialogue audio to finish, then calls ShowAssessment on MountainsideAssessmentUI.
    /// </summary>
    public class MountainsideEndingTrigger : MonoBehaviour
    {
        [Tooltip("Duration in seconds to wait before showing the assessment. Set this to match your dialogue audio length.")]
        [SerializeField] private float dialogueDuration = 10f;

        private void OnEnable()
        {
            StartCoroutine(TriggerAssessmentAfterDialogue());
        }

        private IEnumerator TriggerAssessmentAfterDialogue()
        {
            yield return new WaitForSeconds(dialogueDuration);

            if (MountainsideAssessmentUI.Instance != null)
                MountainsideAssessmentUI.Instance.ShowAssessment();
            else
                Debug.LogError("[MountainsideEndingTrigger] MountainsideAssessmentUI instance not found.");
        }
    }
}
