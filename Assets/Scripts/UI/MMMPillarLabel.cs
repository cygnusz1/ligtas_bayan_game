using UnityEngine;
using TMPro;

/// <summary>
/// Reads MMMSession.ChosenPillar on Start and sets the TMP_Text at the top of the gameplay HUD.
/// Attach this to a TMP_Text GameObject in every gameplay scene.
/// </summary>
public class MMMPillarLabel : MonoBehaviour
{
    [SerializeField] private TMP_Text pillarLabel;

    private void Start()
    {
        if (string.IsNullOrEmpty(MMMSession.ChosenPillar))
        {
            Debug.LogWarning("[MMMPillarLabel] MMMSession.ChosenPillar is empty — was the MMM selection screen skipped?");
        }

        pillarLabel.text = MMMSession.ChosenPillar;
    }
}
