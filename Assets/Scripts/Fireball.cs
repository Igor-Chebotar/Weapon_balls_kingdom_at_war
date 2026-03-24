using UnityEngine;

public class Fireball : MonoBehaviour
{
    public float damage = 3f;
    public GameObject owner;
    float lifetime = 5f;
    float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (Time.time - spawnTime > lifetime)
            Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // дмг игроку
        var player = col.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            var target = col.gameObject.GetComponent<IDamageable>();
            if (target != null)
            {
                Vector2 hitPt = col.contacts.Length > 0 ? col.GetContact(0).point : (Vector2)transform.position;
                Vector2 force = ((Vector2)col.gameObject.transform.position - (Vector2)transform.position).normalized * 3f;
                target.TakeDamage(new DamageInfo(owner, damage, force, hitPt));
            }
        }

        // бум
        Destroy(gameObject);
    }
}
