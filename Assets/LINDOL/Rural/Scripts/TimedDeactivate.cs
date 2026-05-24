using UnityEngine;
using System.Collections;

public class TimedDeactivate : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How many seconds to wait before deactivating this object.")]
    [SerializeField] private float duration = 3.0f;

    [Tooltip("If true, it starts the countdown as soon as the object is enabled.")]
    [SerializeField] private bool deactivateOnStart = true;

    private void OnEnable()
    {
        if (deactivateOnStart)
        {
            StartCountdown();
        }
    }

    // You can call this function from another script if needed
    public void StartCountdown()
    {
        StopAllCoroutines(); // Prevents multiple timers running at once
        StartCoroutine(DeactivateAfterTime());
    }

    private IEnumerator DeactivateAfterTime()
    {
        // Wait for the specified duration
        yield return new WaitForSeconds(duration);

        // Turn the object off
        gameObject.SetActive(false);
    }
}