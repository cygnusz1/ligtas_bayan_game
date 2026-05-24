using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the MMM overlay panel in MainMenu.unity.
/// Shown after a scene card is clicked; loads the queued scene after a pillar is chosen.
/// </summary>
public class MMMSelectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeInDuration = 0.4f;

    [Header("Pillar Buttons")]
    [SerializeField] private Button matinoButton;
    [SerializeField] private Button mahusayButton;
    [SerializeField] private Button maasahanButton;

    [Header("Back")]
    [SerializeField] private Button backButton;

    private void Awake()
    {
        matinoButton.onClick.AddListener(()   => OnPillarSelected(MMMSession.Matino));
        mahusayButton.onClick.AddListener(()  => OnPillarSelected(MMMSession.Mahusay));
        maasahanButton.onClick.AddListener(() => OnPillarSelected(MMMSession.Maasahan));
        backButton.onClick.AddListener(OnBackClicked);

        panelRoot.SetActive(false);
    }

    /// <summary>Called by each scene card button to open the MMM panel for the given scene.</summary>
    public void Show(string sceneName)
    {
        MMMSession.QueueScene(sceneName);
        panelRoot.SetActive(true);
        StartCoroutine(FadeIn(panelCanvasGroup));
    }

    private void OnPillarSelected(string pillarName)
    {
        MMMSession.SetPillar(pillarName);
        SceneManager.LoadScene(MMMSession.QueuedScene);
    }

    private void OnBackClicked()
    {
        StopAllCoroutines();
        panelCanvasGroup.alpha = 0f;
        panelRoot.SetActive(false);
    }

    private IEnumerator FadeIn(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }
}
