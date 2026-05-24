using UnityEngine;

namespace RuralEarthquake.Navigation
{
    /// <summary>
    /// Data marker that identifies a GameObject as a navigation destination for a specific objective.
    /// Attach this to any objective trigger or world-space anchor you want the ground arrow to point toward.
    /// </summary>
    public class ObjectiveWorldMarker : MonoBehaviour
    {
        [Tooltip("The objective identifier this marker corresponds to. Must match the objectiveID in the ObjectivesScriptable asset.")]
        public int objectiveID;

        [Tooltip("Optional override transform to point toward. If null, this GameObject's transform is used.")]
        public Transform overrideTarget;

        /// <summary>
        /// Returns the effective world-space target position for this marker.
        /// </summary>
        public Vector3 TargetPosition => overrideTarget != null ? overrideTarget.position : transform.position;
    }
}
