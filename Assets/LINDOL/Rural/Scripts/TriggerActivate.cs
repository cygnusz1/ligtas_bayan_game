using UnityEngine;

public class TriggerActivate : MonoBehaviour
{
    [Header("Target Object")]
    public GameObject targetObject; // Dapat mai-drag mo dito ang object mula sa Hierarchy

    [Header("Options")]
    public bool triggerOnce = true;
    private bool hasTriggered = false;

    void Start()
    {
        // Siguraduhin nating may Collider at naka-Trigger
        if (GetComponent<Collider>() != null)
        {
            GetComponent<Collider>().isTrigger = true;
        }
        else
        {
            Debug.LogError("Hoy! Walang Box Collider itong Trigger object mo. Mag-add ka muna!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Chine-check kung Player ang pumasok
        if (other.CompareTag("Player") && !hasTriggered)
        {
            if (targetObject != null)
            {
                targetObject.SetActive(true); // Eto yung mag-a-activate
                
                if (triggerOnce) hasTriggered = true;
                
                Debug.Log("Success! Na-activate na si: " + targetObject.name);
            }
        }
    }
}