using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DisableSpecificScripts : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The GameObject that holds the scripts.")]
    [SerializeField] private GameObject targetObject;

    [Tooltip("Type the exact names of the scripts you want to disable.")]
    [SerializeField] private List<string> scriptNames;

    [SerializeField] private float disabledDuration = 3.0f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && targetObject != null)
        {
            StartCoroutine(DisableSequence());
        }
    }

    private IEnumerator DisableSequence()
    {
        // 1. Find and Disable all scripts in the list
        List<MonoBehaviour> foundScripts = new List<MonoBehaviour>();

        foreach (string sName in scriptNames)
        {
            // This looks for the script component by its class name string
            MonoBehaviour script = targetObject.GetComponent(sName) as MonoBehaviour;
            if (script != null)
            {
                script.enabled = false;
                foundScripts.Add(script);
            }
        }

        // 2. Wait
        yield return new WaitForSeconds(disabledDuration);

        // 3. Re-enable them
        foreach (MonoBehaviour script in foundScripts)
        {
            if (script != null) script.enabled = true;
        }
    }
}