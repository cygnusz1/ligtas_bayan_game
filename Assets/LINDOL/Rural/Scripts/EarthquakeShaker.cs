using UnityEngine;
using System.Collections;

public class EarthquakeShaker : MonoBehaviour
{
    // In HFPS, drag the "Main Camera" or the "Camera Pivot" here
    public Transform cameraTransform;
    
    [Header("Shake Settings")]
    public float shakeDuration = 2.0f;
    public float shakeAmount = 0.1f;
    public float decreaseFactor = 1.0f;

    [Header("NPC Evacuation")]
    public NPCEarthquakeRunner[] npcRunners;

    private Vector3 originalPos;
    private bool isShaking = false;

    void OnEnable()
    {
        if (cameraTransform == null)
            cameraTransform = GetComponent<Transform>();
    }

    [ContextMenu("Test Shake")] // Allows you to right-click the script in Inspector to test
    public void StartQuake()
    {
        if (!isShaking)
        {
            StartCoroutine(Shake());
        }
    }

    IEnumerator Shake()
    {
        isShaking = true;

        foreach (var runner in npcRunners)
        {
            if (runner != null)
                runner.StartEvacuation();
        }
        // Store the position relative to the player's head at the MOMENT the shake starts
        originalPos = cameraTransform.localPosition;
        
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            // Calculate random offset
            Vector3 randomOffset = Random.insideUnitSphere * shakeAmount;
            
            // Apply shake while maintaining the original Y height to prevent "floor clipping"
            cameraTransform.localPosition = originalPos + randomOffset;

            elapsed += Time.deltaTime;
            
            // Gradually reduce shake strength (Damping)
            shakeAmount = Mathf.Lerp(shakeAmount, 0, elapsed / shakeDuration);

            yield return null;
        }

        cameraTransform.localPosition = originalPos;
        isShaking = false;
        // Reset shake amount for next time (adjust this to your default value)
        shakeAmount = 0.1f; 
    }
}