using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : WeaponBase
{
    Dictionary<GameObject, float> hitCooldowns = new Dictionary<GameObject, float>();
    float cdTime = 0.4f;

    void Start()
    {
        // триггер чтоб проходить сквозь всё
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.isKinematic = true;
    }

    void Update()
    {
        List<GameObject> toRemove = new List<GameObject>();
        var keys = new List<GameObject>(hitCooldowns.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == null || Time.time > hitCooldowns[keys[i]])
                toRemove.Add(keys[i]);
        }
        foreach (var k in toRemove)
            hitCooldowns.Remove(k);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner) return;

        var target = other.GetComponent<IDamageable>();
        if (target == null) return;

        if (hitCooldowns.ContainsKey(other.gameObject))
            return;

        // урон = прокачанное значение
        float dmg = 1f;
        if (GameManager.Instance != null)
            dmg = GameManager.Instance.GetWeaponDamage();

        Vector2 hitPt = other.ClosestPoint(transform.position);
        Vector2 force = ((Vector2)other.transform.position - (Vector2)transform.position).normalized * 5f;

        target.TakeDamage(new DamageInfo(owner, dmg, force, hitPt));
        hitCooldowns[other.gameObject] = Time.time + cdTime;
    }
}
