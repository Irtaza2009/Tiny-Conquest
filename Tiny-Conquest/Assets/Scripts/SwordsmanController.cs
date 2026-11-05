using UnityEngine;

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
    [SerializeField] private LayerMask enemyLayer;

    [Header("Movement")]
    [SerializeField] private float waypointTolerance = 0.3f;
    [SerializeField] private Transform[] waypoints;

    private float currentHealth;
    private float attackTimer;

    private int currentWaypointIndex;

    private SwordsmanController currentTarget;

    private bool isMoving;
    private bool isDead;

    public int TeamID => teamID;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;

        isMoving = waypoints != null && waypoints.Length > 0;
    }

    private void Update()
    {
        if (isDead)
            return;

        if (currentTarget != null && currentTarget.IsDead)
        {
            currentTarget = null;
        }

        if (currentTarget == null)
        {
            FindEnemy();
        }

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

        Vector3 direction = waypoint.position - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= waypointTolerance)
        {
            currentWaypointIndex++;
            return;
        }

        direction.Normalize();

        float movementThisFrame = moveSpeed * Time.deltaTime;

        if (movementThisFrame >= distance)
        {
            transform.position = new Vector3(
                waypoint.position.x,
                transform.position.y,
                waypoint.position.z
            );
            currentWaypointIndex++;
            return;
        }

        transform.position += direction * movementThisFrame;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                10f * Time.deltaTime
            );
        }
    }

    private void FindEnemy()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            enemyLayer
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
                continue;

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
        float distance = Vector3.Distance(
            transform.position,
            currentTarget.transform.position
        );

        Vector3 direction =
            currentTarget.transform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                10f * Time.deltaTime
            );
        }

        if (distance > attackRange)
        {
            transform.position +=
                direction.normalized * moveSpeed * Time.deltaTime;
        }
        else
        {
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                Attack();
                attackTimer = attackCooldown;
            }
        }
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
        {
            Die();
        }
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
