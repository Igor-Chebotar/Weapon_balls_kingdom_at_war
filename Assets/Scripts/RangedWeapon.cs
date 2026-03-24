using UnityEngine;

public class RangedWeapon : WeaponBase
{
    float nextShot;

    void Update()
    {
        if (weaponData == null) return;

        if (Time.time >= nextShot)
        {
            Attack();
            nextShot = Time.time + weaponData.AttackRate;
        }
    }

    public override void Attack()
    {
        if (weaponData.ProjectilePrefab == null)
        {
            return;
        }

        // направление к мышке
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;
        Vector2 dir = (mouseWorld - transform.position).normalized;

        var obj = PoolManager.Instance.SpawnObject(weaponData.ProjectilePrefab.name, transform.position);
        var proj = obj.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Launch(dir, 15f, owner);
        }
    }
}
