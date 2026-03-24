using UnityEngine;

public enum WeaponType
{
    Melee,
    Ranged,
    Magic
}

public struct DamageInfo
{
    public GameObject Attacker;
    public float DamageAmount;
    public Vector2 ImpactForce;
    public Vector2 HitPoint;

    public DamageInfo(GameObject attacker, float dmg, Vector2 force, Vector2 hitPoint)
    {
        Attacker = attacker;
        DamageAmount = dmg;
        ImpactForce = force;
        HitPoint = hitPoint;
    }
}
