using UnityEngine;

public class ArcherController : UnitController
{
    [Header("Archer Stats")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float minimumRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Projectile")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float arrowSpeed = 12f;

    private float attackTimer;

    protected override void HandleCombat()
    {
        if (CurrentTarget == null || CurrentTarget.IsDead)
            return;

        Vector3 direction =
            CurrentTarget.transform.position - UnitRigidbody.position;

        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance > 0.01f)
        {
            UnitRigidbody.MoveRotation(Quaternion.Slerp(
                UnitRigidbody.rotation,
                Quaternion.LookRotation(direction),
                10f * Time.fixedDeltaTime
            ));
        }

        if (distance > attackRange)
        {
            UnitRigidbody.MovePosition(
                UnitRigidbody.position +
                direction.normalized * MoveSpeed * Time.fixedDeltaTime
            );
        }
        else if (distance < minimumRange)
        {
            UnitRigidbody.MovePosition(
                UnitRigidbody.position -
                direction.normalized * MoveSpeed * Time.fixedDeltaTime
            );
        }
        else
        {
            attackTimer -= Time.fixedDeltaTime;

            if (attackTimer <= 0f)
            {
                FireArrow();
                attackTimer = attackCooldown;
            }
        }
    }

    private void FireArrow()
    {
        if (arrowPrefab == null || shootPoint == null)
        {
            Debug.LogError(
                name + ": Archer requires an arrow prefab and shoot point.",
                this
            );
            return;
        }

        Vector3 targetPosition =
            CurrentTarget.transform.position + Vector3.up * 0.8f;
        Vector3 direction =
            (targetPosition - shootPoint.position).normalized;

        GameObject arrow = Instantiate(
            arrowPrefab,
            shootPoint.position,
            Quaternion.LookRotation(direction)
        );

        Rigidbody arrowRigidbody = arrow.GetComponent<Rigidbody>();

        if (arrowRigidbody == null)
        {
            Debug.LogError(name + ": Arrow prefab requires a Rigidbody.", this);
            Destroy(arrow);
            return;
        }

        arrowRigidbody.linearVelocity = direction * arrowSpeed;

        ArrowProjectile projectile =
            arrow.GetComponent<ArrowProjectile>();

        if (projectile == null)
        {
            Debug.LogError(
                name + ": Arrow prefab requires ArrowProjectile.",
                this
            );
            Destroy(arrow);
            return;
        }

        projectile.Initialize(damage, TeamID, UnitCollider);
    }

    private void OnDrawGizmosSelected()
    {
        DrawDetectionGizmo(Color.blue, minimumRange);
        DrawDetectionGizmo(Color.red, attackRange);
    }
}
