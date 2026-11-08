using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class UnitController : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private int teamID;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRadius = 4f;

    [Header("Movement")]
    [SerializeField] private float waypointTolerance = 0.15f;
    [SerializeField] private float pathHalfWidth = 1.25f;
    [SerializeField] private float pathEdgeBuffer = 0.1f;
    [SerializeField] private Transform[] waypoints;

    [Header("Visuals")]
    [SerializeField] private Renderer unitRenderer;

    [Header("Fall Protection")]
    [SerializeField] private float fallDeathHeight = -5f;

    private float currentHealth;
    private int currentWaypointIndex;
    private bool isMoving;
    private bool isDead;
    private Platform destinationPlatform;

    protected Rigidbody UnitRigidbody { get; private set; }
    protected UnitController CurrentTarget { get; private set; }
    protected float MoveSpeed => moveSpeed;
    protected float WaypointTolerance => waypointTolerance;
    protected int Team => teamID;
    public Collider UnitCollider { get; private set; }

    public int TeamID => teamID;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public Vector3 AimPosition => UnitCollider != null
        ? UnitCollider.bounds.center
        : transform.position + Vector3.up * 0.8f;
    public Vector3 Velocity => UnitRigidbody != null
        ? UnitRigidbody.linearVelocity
        : Vector3.zero;

    private readonly Color neutralColor = Color.yellow;
    private readonly Color playerColor = new Color(0.44f, 0.76f, 0.9f);
    private readonly Color aiColor = new Color(1f, 0.51f, 0.51f);

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        UnitRigidbody = GetComponent<Rigidbody>();
        UnitCollider = GetComponent<Collider>();

        UnitRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        UnitRigidbody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        isMoving = waypoints != null && waypoints.Length > 0;
    }

    protected virtual void Start()
    {
        UpdateVisuals();
    }

    protected virtual void Update()
    {
        if (isDead)
            return;

        if (CurrentTarget != null && CurrentTarget.IsDead)
            CurrentTarget = null;

        if (CurrentTarget == null)
            FindEnemy();
    }

    protected virtual void FixedUpdate()
    {
        if (isDead || UnitRigidbody == null)
            return;

        if (UnitRigidbody.position.y <= fallDeathHeight)
        {
            Die();
            return;
        }

        if (CurrentTarget != null)
            HandleCombat();
        else if (isMoving)
            FollowWaypoints();
    }

    protected abstract void HandleCombat();

    public void SetTeam(int newTeamID)
    {
        teamID = newTeamID;
        UpdateVisuals();
    }

    public void SetRoute(Transform[] newWaypoints, Platform destination)
    {
        waypoints = newWaypoints;
        currentWaypointIndex = 0;
        destinationPlatform = destination;
        isMoving = waypoints != null && waypoints.Length > 0;
        CurrentTarget = null;
    }

    protected void FindEnemy()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = Mathf.Infinity;

        foreach (Collider col in nearbyObjects)
        {
            UnitController enemy =
                col.GetComponentInParent<UnitController>();

            if (enemy == null ||
                enemy == this ||
                enemy.IsDead ||
                enemy.TeamID == teamID)
            {
                continue;
            }

            float distance = Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                CurrentTarget = enemy;
            }
        }
    }

    protected void MoveWithinPath(Vector3 desiredPosition)
    {
        desiredPosition = ConstrainToPath(desiredPosition);
        UnitRigidbody.MovePosition(desiredPosition);
    }

    private Vector3 ConstrainToPath(Vector3 desiredPosition)
        {
            if (!isMoving ||
                waypoints == null ||
                waypoints.Length == 0 ||
                pathHalfWidth <= 0f)
            {
                return desiredPosition;
            }

            Vector3 closestPoint;

            if (!TryGetClosestPointOnPath(
                desiredPosition,
                out closestPoint))
            {
                return desiredPosition;
            }

            Vector3 offset = desiredPosition - closestPoint;
            offset.y = 0f;

            float allowedOffset = Mathf.Max(
                0f,
                pathHalfWidth - pathEdgeBuffer
            );

            if (offset.magnitude <= allowedOffset)
                return desiredPosition;

            Vector3 constrainedPosition = closestPoint;

            if (offset.sqrMagnitude > 0.001f)
            {
                constrainedPosition +=
                    offset.normalized * allowedOffset;
            }

            constrainedPosition.y = desiredPosition.y;
            return constrainedPosition;
        }

    private bool TryGetClosestPointOnPath(
        Vector3 position,
        out Vector3 closestPoint)
        {
            closestPoint = Vector3.zero;
            bool foundPoint = false;
            float closestDistanceSqr = Mathf.Infinity;
            Vector3 previousPoint = Vector3.zero;
            bool hasPreviousPoint = false;

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint == null)
                    continue;

                Vector3 currentPoint = waypoint.position;
                currentPoint.y = position.y;

                CheckClosestPoint(
                    position,
                    currentPoint,
                    ref closestPoint,
                    ref closestDistanceSqr,
                    ref foundPoint
                );

                if (hasPreviousPoint)
                {
                    Vector3 segment = currentPoint - previousPoint;
                    float segmentLengthSqr = segment.sqrMagnitude;

                    if (segmentLengthSqr > 0.001f)
                    {
                        float projection = Vector3.Dot(
                            position - previousPoint,
                            segment
                        ) / segmentLengthSqr;

                        Vector3 pointOnSegment = previousPoint +
                            segment * Mathf.Clamp01(projection);

                        CheckClosestPoint(
                            position,
                            pointOnSegment,
                            ref closestPoint,
                            ref closestDistanceSqr,
                            ref foundPoint
                        );
                    }
                }

                previousPoint = currentPoint;
                hasPreviousPoint = true;
            }

            return foundPoint;
        }

    private static void CheckClosestPoint(
        Vector3 position,
        Vector3 candidate,
        ref Vector3 closestPoint,
        ref float closestDistanceSqr,
        ref bool foundPoint)
        {
            float distanceSqr = (position - candidate).sqrMagnitude;

            if (distanceSqr >= closestDistanceSqr)
                return;

            closestDistanceSqr = distanceSqr;
            closestPoint = candidate;
            foundPoint = true;
        }

    private void FollowWaypoints()
    {
        if (waypoints == null ||
            currentWaypointIndex >= waypoints.Length)
        {
            FinishRoute();
            return;
        }

        Transform waypoint = waypoints[currentWaypointIndex];

        if (waypoint == null)
        {
            currentWaypointIndex++;
            return;
        }

        Vector3 direction = waypoint.position - UnitRigidbody.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= WaypointTolerance)
        {
            currentWaypointIndex++;
            return;
        }

        direction.Normalize();

        float movement = MoveSpeed * Time.fixedDeltaTime;
        Vector3 nextPosition = UnitRigidbody.position +
            direction * Mathf.Min(movement, distance);

        MoveWithinPath(nextPosition);
        UnitRigidbody.MoveRotation(Quaternion.Slerp(
            UnitRigidbody.rotation,
            Quaternion.LookRotation(direction),
            10f * Time.fixedDeltaTime
        ));
    }

    private void FinishRoute()
    {
        isMoving = false;

        if (destinationPlatform != null)
        {
            destinationPlatform.ReceiveTroop(this);
            destinationPlatform = null;
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
            Die();
    }

    protected void Die()
    {
        isDead = true;
        Destroy(gameObject);
    }

    private void UpdateVisuals()
    {
        if (unitRenderer == null)
            return;

        Color ownerColor = neutralColor;

        if (teamID == 1)
            ownerColor = playerColor;
        else if (teamID == 2)
            ownerColor = aiColor;

        unitRenderer.material.color = ownerColor;
    }

    protected void DrawDetectionGizmo(Color color, float radius)
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
