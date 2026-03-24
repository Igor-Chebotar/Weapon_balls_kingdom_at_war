using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    protected WeaponData weaponData;
    protected GameObject owner;
    protected Rigidbody2D rb;

    public virtual void Initialize(WeaponData data, GameObject owner)
    {
        this.weaponData = data;
        this.owner = owner;
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.mass = data.Mass;
        }
    }

    public virtual void Attack() { }
}
