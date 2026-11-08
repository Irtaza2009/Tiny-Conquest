using UnityEngine;

public class SwordsmanController : UnitController
{
    [Header("Swordsman Stats")]
    [SerializeField] private float damage = 20f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1f;

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
            return;
        }

        attackTimer -= Time.fixedDeltaTime;

        if (attackTimer <= 0f)
        {
            CurrentTarget.TakeDamage(damage);
            attackTimer = attackCooldown;
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawDetectionGizmo(Color.yellow, attackRange);
    }
}
