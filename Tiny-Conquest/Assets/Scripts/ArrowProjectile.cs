using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    private float damage;
    private int teamID;

    private Collider shooterCollider;

    private void Awake()
    {
        Collider myCollider =  GetComponent<Collider>();

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

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Arrow collided with: " + collision.gameObject.name);

        SwordsmanController swordsman =
            collision.collider.GetComponentInParent<SwordsmanController>();

        ArcherController archer =
            collision.collider.GetComponentInParent<ArcherController>();

        if (swordsman != null && swordsman.TeamID != teamID)
            swordsman.TakeDamage(damage);

        if (archer != null && archer.TeamID != teamID)
            archer.TakeDamage(damage);

        Destroy(gameObject);
    }

}