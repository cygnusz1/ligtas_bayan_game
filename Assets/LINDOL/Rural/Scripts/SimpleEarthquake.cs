using UnityEngine;

public class SimpleEarthquake : MonoBehaviour
{
    [Header("Earthquake Settings")]
    public float magnitude = 0.1f;    // Gaano kalayo ang galaw (0.05 - 0.2 is good)
    public float roughness = 20f;     // Gaano kabilis ang yanig
    public float duration = 2.0f;     // Gaano katagal ang lindol

    private Vector3 originalPos;
    private float elapsed = 0.0f;
    private bool isShaking = false;

    void Start()
    {
        // I-save ang pwesto ng object bago gumalaw
        originalPos = transform.localPosition;
    }

    // Tawagin mo ito para magsimula ang lindol (e.g. mula sa Trigger)
    [ContextMenu("Test Earthquake")]
    public void StartQuake()
    {
        elapsed = duration;
        isShaking = true;
    }

    void Update()
    {
        if (isShaking)
        {
            if (elapsed > 0)
            {
                // Gumagamit ng PerlinNoise para mas "natural" ang yanig kaysa sa Random.Range
                float x = (Mathf.PerlinNoise(Time.time * roughness, 0f) - 0.5f) * magnitude;
                float z = (Mathf.PerlinNoise(0f, Time.time * roughness) - 0.5f) * magnitude;

                // X at Z lang ang gagalaw, mananatili ang Original Y (hindi lulubog sa sahig)
                transform.localPosition = new Vector3(originalPos.x + x, originalPos.y, originalPos.z + z);

                elapsed -= Time.deltaTime;
            }
            else
            {
                // I-reset sa original position pagtapos ng lindol
                transform.localPosition = originalPos;
                isShaking = false;
            }
        }
    }
}