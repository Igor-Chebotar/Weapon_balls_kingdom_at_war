using UnityEngine;

public class EnemySword : MonoBehaviour
{
    public float damage = 2f;
    public GameObject owner;
    float cdTimer;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (cdTimer > 0) return;
        if (other.gameObject == owner) return;

        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        var target = other.GetComponent<IDamageable>();
        if (target == null) return;

        Vector2 hitPt = other.ClosestPoint(transform.position);
        Vector2 force = ((Vector2)other.transform.position - (Vector2)transform.position).normalized * 4f;
        target.TakeDamage(new DamageInfo(owner, damage, force, hitPt));
        cdTimer = 0.5f;
    }

    void Update()
    {
        if (cdTimer > 0) cdTimer -= Time.deltaTime;
    }
}
