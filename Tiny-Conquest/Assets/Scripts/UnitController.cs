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
    [SerializeField] private Transform[] waypoints;

    [Header("Visuals")]
    [SerializeField] private Renderer unitRenderer;

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
    protected Collider UnitCollider { get; private set; }

    public int TeamID => teamID;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private readonly Color neutralColor = Color.grey;
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

        UnitRigidbody.MovePosition(nextPosition);
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

    private void Die()
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
