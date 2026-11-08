using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ArcherController : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private int teamID = 0;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 70f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float minimumRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Enemy Detection")]
    [SerializeField] private float detectionRadius = 8f;

    [Header("Movement")]
    [SerializeField] private float waypointTolerance = 0.15f;
    [SerializeField] private Transform[] waypoints;

    [Header("Visuals")]
    [SerializeField] private Renderer unitRenderer;

    [Header("Projectile")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float arrowSpeed = 12f;

    private float currentHealth;
    private float attackTimer;

    private int currentWaypointIndex;

    private ArcherController currentTarget;

    private bool isMoving;
    private bool isDead;

    private Rigidbody unitRigidbody;

    public int TeamID => teamID;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private readonly Color neutralColor = Color.grey;
    private readonly Color playerColor = new Color(0.44f, 0.76f, 0.9f);
    private readonly Color aiColor = new Color(1f, 0.51f, 0.51f);

    private Platform destinationPlatform;

    private void Awake()
    {
        currentHealth = maxHealth;

        unitRigidbody = GetComponent<Rigidbody>();

        unitRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        unitRigidbody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        isMoving = waypoints != null && waypoints.Length > 0;
    }

    private void Start()
    {
        UpdateVisuals();
    }

    public void SetTeam(int newTeamID)
    {
        teamID = newTeamID;
    }

    private void Update()
    {
        if (isDead)
            return;

        if (currentTarget != null && currentTarget.IsDead)
            currentTarget = null;

        if (currentTarget == null)
            FindEnemy();
    }

    private void FixedUpdate()
    {
        if (isDead || unitRigidbody == null)
            return;

        if (currentTarget != null)
        {
            HandleCombat();
        }
        else if (isMoving)
        {
            FollowWaypoints();
        }
    }

    public void SetRoute(Transform[] newWaypoints, Platform destination)
    {
        waypoints = newWaypoints;
        currentWaypointIndex = 0;
        destinationPlatform = destination;

        isMoving = waypoints != null && waypoints.Length > 0;
        currentTarget = null;
    }

    private void FindEnemy()
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
            ArcherController enemy =
                col.GetComponentInParent<ArcherController>();

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
                currentTarget = enemy;
            }
        }
    }

    private void HandleCombat()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            currentTarget = null;
            return;
        }

        Vector3 direction =
            currentTarget.transform.position - unitRigidbody.position;

        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            unitRigidbody.MoveRotation(
                Quaternion.Slerp(
                    unitRigidbody.rotation,
                    targetRotation,
                    10f * Time.fixedDeltaTime
                )
            );
        }

        if (distance > attackRange)
        {
            unitRigidbody.MovePosition(
                unitRigidbody.position +
                direction.normalized * moveSpeed * Time.fixedDeltaTime
            );
        }
        else if (distance < minimumRange)
        {
            unitRigidbody.MovePosition(
                unitRigidbody.position -
                direction.normalized * moveSpeed * Time.fixedDeltaTime

            );
        }
        else
        {
            attackTimer -= Time.fixedDeltaTime;

            if (attackTimer <= 0f)
            {
                Attack();
                attackTimer = attackCooldown;
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

        Vector3 direction =
            waypoint.position - unitRigidbody.position;

        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= waypointTolerance)
        {
            currentWaypointIndex++;
            return;
        }

        direction.Normalize();

        float movement = moveSpeed * Time.fixedDeltaTime;

        Vector3 nextPosition = unitRigidbody.position +
                            direction * Mathf.Min(movement, distance);

        unitRigidbody.MovePosition(nextPosition);

        unitRigidbody.MoveRotation(
            Quaternion.Slerp(
                unitRigidbody.rotation,
                Quaternion.LookRotation(direction),
                10f * Time.fixedDeltaTime
            )
        );
    }

    private void FinishRoute()
    {
        isMoving = false;

        if (destinationPlatform != null)
        {
            
            destinationPlatform = null;
        }
    }

    private void Attack()
{
    Debug.Log("Archer is trying to shoot!");

    if (currentTarget == null || currentTarget.IsDead)
        return;

    if (arrowPrefab == null)
    {
        Debug.LogError("Arrow Prefab is missing!");
        return;
    }

    if (shootPoint == null)
    {
        Debug.LogError("Shoot Point is missing!");
        return;
    }

    Vector3 targetPosition =
        currentTarget.transform.position + Vector3.up * 0.8f;

    Vector3 direction =
        (targetPosition - shootPoint.position).normalized;

    GameObject arrow = Instantiate(
        arrowPrefab,
        shootPoint.position,
        Quaternion.LookRotation(direction)
    );

    Debug.Log("Arrow spawned!");

    Rigidbody arrowRb = arrow.GetComponent<Rigidbody>();

    if (arrowRb == null)
    {
        Debug.LogError("Arrow Rigidbody is missing!");
        Destroy(arrow);
        return;
    }

    arrowRb.linearVelocity = direction * arrowSpeed;

    ArrowProjectile projectile =
        arrow.GetComponent<ArrowProjectile>();

    if (projectile == null)
    {
        Debug.LogError("ArrowProjectile script is missing!");
        Destroy(arrow);
        return;
    }

    projectile.Initialize(damage, teamID, shootPoint.GetComponent<Collider>());
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minimumRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void UpdateVisuals()
    {
        Color ownerColor = neutralColor;

        switch (teamID)
        {
            case 1:
                ownerColor = playerColor;
                break;

            case 2:
                ownerColor = aiColor;
                break;
        }

        if (unitRenderer != null)
            unitRenderer.material.color = ownerColor;
        
    }
}
