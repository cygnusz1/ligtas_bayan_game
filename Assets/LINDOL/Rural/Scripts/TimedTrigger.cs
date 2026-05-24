using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Added for List support

public class TimedTrigger : MonoBehaviour
{
    [Header("Object Groups")]
    [Tooltip("All objects in this list will activate immediately.")]
    [SerializeField] private GameObject[] primaryObjects; 
    
    [Tooltip("All objects in this list will activate after the timer.")]
    [SerializeField] private GameObject[] secondaryObjects;

    [Header("Timing")]
    [SerializeField] private float activeDuration = 5.0f;

    private bool isRunning = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isRunning)
        {
            StartCoroutine(ActivationSequence());
        }
    }

    private IEnumerator ActivationSequence()
    {
        isRunning = true;

        // 1. Activate all Primary, Deactivate all Secondary
        ToggleGroup(primaryObjects, true);
        ToggleGroup(secondaryObjects, false);

        // 2. Wait
        yield return new WaitForSeconds(activeDuration);

        // 3. Swap: Deactivate all Primary, Activate all Secondary
        ToggleGroup(primaryObjects, false);
        ToggleGroup(secondaryObjects, true);

        isRunning = false;
    }

    // Helper function to loop through any array and set its state
    private void ToggleGroup(GameObject[] group, bool state)
    {
        if (group == null) return;

        foreach (GameObject obj in group)
        {
            if (obj != null)
            {
                obj.SetActive(state);
            }
        }
    }
}