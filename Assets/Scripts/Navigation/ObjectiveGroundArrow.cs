using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using HFPS.Systems;

namespace RuralEarthquake.Navigation
{
    /// <summary>
    /// Spawns a row of scrolling rectangular dash segments on the ground beneath the player
    /// that point toward the currently active objective destination.
    /// Hides automatically when no active, non-completed objective with a registered target exists.
    /// </summary>
    public class ObjectiveGroundArrow : MonoBehaviour
    {
        [Header("Objective Targets")]
        [Tooltip("Maps each objective ID to a world-space destination. The Transform should be placed AT the destination, NOT on the trigger.")]
        [SerializeField] private ObjectiveTarget[] objectiveTargets;

        [Header("Segment Appearance")]
        [Tooltip("Material applied to every dash segment.")]
        [SerializeField] private Material segmentMaterial;

        [Tooltip("Maximum number of dash tiles in the trail (pool size).")]
        [SerializeField] private int segmentCount = 20;

        [Tooltip("Distance between the start of consecutive segments (metres).")]
        [SerializeField] private float segmentSpacing = 0.55f;

        [Tooltip("Distance of the first segment in front of the arrow pivot (metres).")]
        [SerializeField] private float startOffset = 0.5f;

        [Tooltip("How close to the destination the trail stops (metres). Prevents segments from overlapping the target.")]
        [SerializeField] private float endOffset = 0.6f;

        [Tooltip("Width (X) and length (Z) of each dash tile in local units.")]
        [SerializeField] private Vector2 segmentSize = new Vector2(0.10f, 0.38f);

        [Header("Animation")]
        [Tooltip("Speed at which dashes scroll forward toward the objective (m/s).")]
        [SerializeField] private float scrollSpeed = 1.3f;

        [Header("Positioning")]
        [Tooltip("How far above the floor the segment pivot hovers (world units).")]
        [SerializeField] private float hoverHeight = 0.05f;

        [Header("Rotation")]
        [Tooltip("Slerp speed for rotating the trail toward the active objective.")]
        [SerializeField] private float rotationSpeed = 6f;

        // ── Internals ──────────────────────────────────────────────────────────
        private readonly Dictionary<int, Transform> _targetMap = new Dictionary<int, Transform>();
        private readonly List<Transform> _segmentTransforms  = new List<Transform>();
        private readonly List<Renderer>  _segmentRenderers   = new List<Renderer>();

        // Cache CharacterController reference so we can compute exact feet position
        private CharacterController _characterController;

        // ──────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            BuildTargetMap();
            CreateSegments();

            // Grab the CharacterController from the parent player — used to find exact feet Y
            if (transform.parent != null)
                _characterController = transform.parent.GetComponent<CharacterController>();
        }

        private void BuildTargetMap()
        {
            _targetMap.Clear();

            if (objectiveTargets == null)
                return;

            foreach (ObjectiveTarget entry in objectiveTargets)
            {
                if (entry.target == null)
                {
                    Debug.LogWarning($"[ObjectiveGroundArrow] Target for objectiveID {entry.objectiveID} is null — skipping.");
                    continue;
                }

                if (_targetMap.ContainsKey(entry.objectiveID))
                {
                    Debug.LogWarning($"[ObjectiveGroundArrow] Duplicate objectiveID {entry.objectiveID} — only the first entry is used.");
                    continue;
                }

                _targetMap[entry.objectiveID] = entry.target;
            }
        }

        private void CreateSegments()
        {
            for (int i = 0; i < segmentCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = $"DashSegment_{i}";
                go.transform.SetParent(transform, false);

                // Rotate 90° on X so the Quad lies flat on the XZ plane
                go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                go.transform.localScale    = new Vector3(segmentSize.x, segmentSize.y, 1f);
                go.transform.localPosition = new Vector3(0f, 0f, startOffset + i * segmentSpacing);

                // Purely visual — remove the auto-added collider
                Destroy(go.GetComponent<Collider>());

                MeshRenderer mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial    = segmentMaterial;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows    = false;
                mr.enabled           = false;

                _segmentTransforms.Add(go.transform);
                _segmentRenderers.Add(mr);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Cached reference to the active target so AnimateSegments can use the distance
        private Transform _activeTarget;

        private void Update()
        {
            if (ObjectiveManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            _activeTarget = FindActiveTarget(ObjectiveManager.Instance.activeObjectives);

            if (_activeTarget == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            SnapToFeet();
            RotateToward(_activeTarget.position);
            AnimateSegments();
        }

        /// <summary>
        /// Iterates activeObjectives and returns the Transform for the first non-completed objective
        /// that has a registered destination target.
        /// </summary>
        private Transform FindActiveTarget(List<ObjectiveModel> activeObjectives)
        {
            if (activeObjectives == null || activeObjectives.Count == 0)
                return null;

            foreach (ObjectiveModel model in activeObjectives)
            {
                // Skip completed objectives — they stay in the list but isCompleted = true
                if (model.isCompleted)
                    continue;

                if (_targetMap.TryGetValue(model.identifier, out Transform t))
                    return t;
            }

            return null;
        }

        /// <summary>
        /// Positions the arrow pivot exactly at the player's feet level using the
        /// CharacterController's dimensions — no raycast, no layer issues.
        /// </summary>
        private void SnapToFeet()
        {
            if (_characterController == null || transform.parent == null)
                return;

            // Compute world-space half-height: (CC height / 2) × parent lossy scale
            float worldHalfHeight = (_characterController.height * 0.5f) * transform.parent.lossyScale.y;

            // Feet Y = player pivot Y − half-height + center offset (center is always 0 here)
            float feetY = transform.parent.position.y - worldHalfHeight + hoverHeight;

            Vector3 pos = transform.position;
            pos.y = feetY;
            transform.position = pos;
        }

        private void RotateToward(Vector3 targetWorld)
        {
            Vector3 dir = targetWorld - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.001f)
                return;

            Quaternion desired = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desired, Time.deltaTime * rotationSpeed);
        }

        /// <summary>
        /// Advances each segment's local Z by the scroll offset to create a flowing trail effect.
        /// Segments beyond the distance to the target (minus endOffset) are hidden so the trail
        /// appears to reach exactly up to the destination.
        /// </summary>
        private void AnimateSegments()
        {
            float scroll = (Time.time * scrollSpeed) % segmentSpacing;

            // Flat distance from arrow pivot to target (ignore Y)
            float targetDistance = float.MaxValue;
            if (_activeTarget != null)
            {
                Vector3 toTarget = _activeTarget.position - transform.position;
                toTarget.y = 0f;
                targetDistance = toTarget.magnitude;
            }

            float maxLocalZ = targetDistance - endOffset;

            for (int i = 0; i < _segmentTransforms.Count; i++)
            {
                float localZ = startOffset + i * segmentSpacing + scroll;
                _segmentTransforms[i].localPosition = new Vector3(0f, 0f, localZ);

                // Hide segment if it would overshoot the destination
                bool withinRange = localZ < maxLocalZ;
                if (_segmentRenderers[i].enabled != withinRange)
                    _segmentRenderers[i].enabled = withinRange;
            }
        }

        /// <summary>
        /// Forces all segments hidden when <paramref name="visible"/> is false.
        /// When true, per-segment visibility is managed by AnimateSegments instead.
        /// </summary>
        private void SetVisible(bool visible)
        {
            if (visible)
                return; // AnimateSegments handles individual segment visibility

            foreach (Renderer r in _segmentRenderers)
            {
                if (r != null)
                    r.enabled = false;
            }
        }
    }

    /// <summary>Associates an objective ID with a world-space navigation destination.</summary>
    [System.Serializable]
    public struct ObjectiveTarget
    {
        [Tooltip("Objective identifier from ObjectivesScriptable.")]
        public int objectiveID;

        [Tooltip("The world-space Transform at the DESTINATION (not the trigger). Place an empty GameObject at the target location.")]
        public Transform target;
    }
}
