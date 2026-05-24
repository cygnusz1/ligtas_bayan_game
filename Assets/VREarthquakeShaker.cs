using UnityEngine;
using System.Collections;

public class VREarthquakeShaker : MonoBehaviour
{
    // I-assign dito ang parent object ng iyong VR Camera (e.g., Camera Offset o XR Origin)
    public Transform cameraContainer;
    
    [Header("Shake Settings")]
    public float shakeDuration = 5.0f;
    public float shakeMagnitude = 0.05f; // Panatilihing mababa para sa VR (0.01 - 0.05)
    public float dampingSpeed = 1.0f;

    private Vector3 initialPosition;
    private bool isShaking = false;

    void Start()
    {
        if (cameraContainer != null)
        {
            initialPosition = cameraContainer.localPosition;
        }
    }

    // Pwede mong i-call ito mula sa iyong Phase System
    public void StartQuake()
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }

    IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            // Gumagamit ng Random.insideUnitSphere para sa "positional" shake lang
            Vector3 randomPoint = initialPosition + Random.insideUnitSphere * shakeMagnitude;
            
            // Sa VR, mas okay kung x at z lang ang medyo gumagalaw, o limitahan ang y
            cameraContainer.localPosition = new Vector3(randomPoint.x, initialPosition.y, randomPoint.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // I-reset ang position pagkatapos ng lindol
        cameraContainer.localPosition = initialPosition;
        isShaking = false;
    }
}