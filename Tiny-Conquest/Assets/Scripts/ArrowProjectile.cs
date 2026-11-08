using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    private float damage;
    private int teamID;

    private Collider shooterCollider;

    private void Awake()
    {
        Collider myCollider = GetComponent<Collider>();

        ArrowProjectile[] arrows =
            FindObjectsByType<ArrowProjectile>(FindObjectsSortMode.None);
        
        foreach (ArrowProjectile arrow in arrows)
        {
            if (arrow == this)
                continue;

            Collider otherCollider = arrow.GetComponent<Collider>();

            if (otherCollider != null)
                Physics.IgnoreCollision(myCollider, otherCollider);
        }
    }

    public void Initialize(float arrowDamage, int shooterTeam, Collider shooter)
{
    damage = arrowDamage;
    teamID = shooterTeam;
    shooterCollider = shooter;

    Collider arrowCollider = GetComponent<Collider>();

    if (arrowCollider != null && shooterCollider != null)
    {
        Physics.IgnoreCollision(arrowCollider, shooterCollider);
    }

    Destroy(gameObject, 5f);
}

    private void OnTriggerEnter(Collider other)
    {
        UnitController unit =
            other.GetComponentInParent<UnitController>();

        if (unit != null && unit.TeamID != teamID)
        {
            unit.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (unit == null)
            Destroy(gameObject);
    }

}