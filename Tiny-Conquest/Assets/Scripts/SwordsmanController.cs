using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SwordsmanController : MonoBehaviour
{
    [Header("Team")]
    [SerializeField] private int teamID = 0;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Enemy Detection")]
    [SerializeField] private float detectionRadius = 4f;

    [Header("Movement")]
    [SerializeField] private float waypointTolerance = 0.15f;
    [SerializeField] private Transform[] waypoints;

    private float currentHealth;
    private float attackTimer;

    private int currentWaypointIndex;

    private SwordsmanController currentTarget;

    private bool isMoving;
    private bool isDead;

    private Rigidbody unitRigidbody;

    public int TeamID => teamID;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

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

    public void SetRoute(Transform[] newWaypoints)
    {
        waypoints = newWaypoints;
        currentWaypointIndex = 0;

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
            SwordsmanController enemy =
                col.GetComponentInParent<SwordsmanController>();

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
            Vector3 movement =
                direction.normalized * moveSpeed * Time.fixedDeltaTime;

            unitRigidbody.MovePosition(
                unitRigidbody.position + movement
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
            isMoving = false;
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

    private void Attack()
    {
        if (currentTarget == null || currentTarget.IsDead)
            return;

        currentTarget.TakeDamage(damage);
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
