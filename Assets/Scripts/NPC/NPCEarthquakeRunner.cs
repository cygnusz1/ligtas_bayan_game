using System.Collections;
using UnityEngine;

/// <summary>
/// Moves an NPC through a sequential waypoint path on scene start.
/// Uses a CharacterController for horizontal wall collision and a downward
/// raycast ground-snap to reliably descend stairs and slopes.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class NPCEarthquakeRunner : MonoBehaviour
{
    [Header("Path")]
    public Transform[] waypoints;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float waypointTolerance = 0.4f;
    public float rotationSpeed = 10f;

    /// <summary>Seconds to wait before the NPC starts running.</summary>
    public float startDelay = 12f;

    [Header("Physics")]
    public float gravity = -9.81f;

    /// <summary>
    /// How far below the NPC's feet the ground snap raycast will reach.
    /// Increase this if the stair steps are tall and the NPC still floats.
    /// </summary>
    public float groundSnapDistance = 1.2f;

    [Header("References")]
    public Animator animator;

    private const string IsRunningParam = "IsRunning";

    /// <summary>
    /// When true, the Animator is disabled after the NPC reaches the final waypoint
    /// (freezes the last frame). Use this only if the Animator controller does not
    /// have an Idle state wired to IsRunning = false.
    /// </summary>
    public bool disableAnimatorOnFinish = false;

    // Ray is cast from capsule center height so it doesn't self-intersect with the CC capsule.
    private const float RayOriginHeight = 0.9f;

    private CharacterController _controller;
    private float _verticalVelocity;
    private bool _isEvacuating;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[NPCEarthquakeRunner] '{name}': Waypoints array is empty or null. Component disabled.", this);
            enabled = false;
            return;
        }

        if (animator == null)
            Debug.LogWarning($"[NPCEarthquakeRunner] '{name}': Animator reference is null.", this);

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        StartEvacuation();
    }

    /// <summary>Begins the evacuation run along the waypoint path.</summary>
    public void StartEvacuation()
    {
        if (_isEvacuating)
            return;

        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning($"[NPCEarthquakeRunner] '{name}': Cannot evacuate — waypoints are not assigned.", this);
            return;
        }

        _isEvacuating = true;

        if (animator != null)
            animator.SetBool(IsRunningParam, true);

        StartCoroutine(MoveAlongPath());
    }

    private IEnumerator MoveAlongPath()
    {
        foreach (Transform target in waypoints)
        {
            if (target == null)
                continue;

            Vector3 flatTarget = new Vector3(target.position.x, 0f, target.position.z);

            while (true)
            {
                Vector3 flatSelf = new Vector3(transform.position.x, 0f, transform.position.z);

                if (Vector3.Distance(flatSelf, flatTarget) <= waypointTolerance)
                    break;

                // Rotate smoothly toward the waypoint on the Y axis only
                Vector3 direction = (flatTarget - flatSelf).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }

                // Apply gravity
                if (_controller.isGrounded)
                    _verticalVelocity = -2f;  // Small constant keeps the controller pressed against the ground
                else
                    _verticalVelocity += gravity * Time.deltaTime;

                // Move directly toward the flat target direction so the NPC always
                // closes the distance to the waypoint, regardless of rotation lag.
                Vector3 motion = direction * moveSpeed + Vector3.up * _verticalVelocity;
                _controller.Move(motion * Time.deltaTime);

                // Ground snap: pull the NPC down onto each stair step the CC can't handle alone
                SnapToGround();

                yield return null;
            }
        }

        // Reached the final waypoint — stop running
        _isEvacuating = false;

        if (animator != null)
        {
            animator.SetBool(IsRunningParam, false);

            // Fallback: if the controller has no Idle state wired up,
            // disabling the Animator freezes it on the last frame.
            if (disableAnimatorOnFinish)
                animator.enabled = false;
        }
    }

    /// <summary>
    /// Casts a ray downward from inside the capsule to detect stair geometry
    /// below the NPC's feet and snaps the position down to match it.
    /// </summary>
    private void SnapToGround()
    {
        // Origin is at capsule center — starting inside the capsule prevents
        // the ray from registering a hit on the CharacterController's own surface.
        Vector3 origin = transform.position + Vector3.up * RayOriginHeight;
        float maxDistance = RayOriginHeight + groundSnapDistance;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance))
            return;

        float deltaY = hit.point.y - transform.position.y;

        // Only snap downward — never push the NPC upward through this path
        if (deltaY < -0.01f)
            _controller.Move(new Vector3(0f, deltaY, 0f));
    }
}
