/// <summary>
/// Persists the chosen MMM pillar name and the queued scene name across scene loads
/// within the same play session. Uses static fields — no MonoBehaviour or ScriptableObject needed.
/// </summary>
public static class MMMSession
{
    public const string Matino   = "Matino";
    public const string Mahusay  = "Mahusay";
    public const string Maasahan = "Maasahan";

    public static string ChosenPillar { get; private set; }
    public static string QueuedScene  { get; private set; }

    /// <summary>Stores the target scene and resets the pillar before showing the MMM panel.</summary>
    public static void QueueScene(string sceneName)
    {
        QueuedScene   = sceneName;
        ChosenPillar  = string.Empty;
    }

    /// <summary>Saves the player's chosen MMM pillar.</summary>
    public static void SetPillar(string pillarName)
    {
        ChosenPillar = pillarName;
    }
}
