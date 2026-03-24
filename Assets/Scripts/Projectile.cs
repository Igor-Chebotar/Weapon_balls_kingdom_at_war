using UnityEngine;

public class Projectile : MonoBehaviour
{
    Rigidbody2D rb;
    float damage = 10f;
    GameObject shooter;
    float spawnTime;
    bool active = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 dir, float speed, GameObject owner)
    {
        shooter = owner;
        active = true;
        spawnTime = Time.time;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.AddForce(dir * speed, ForceMode2D.Impulse);

        // повернуть стрелу по направлению
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetDamage(float d) { damage = d; }

    void Update()
    {
        if (!active) return;

        // автовозврат в пул через 5 сек
        if (Time.time - spawnTime > 5f)
        {
            ReturnMe();
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!active) return;
        if (col.gameObject == shooter) return;

        var target = col.gameObject.GetComponent<IDamageable>();
        if (target != null)
        {
            Vector2 hitPt = col.GetContact(0).point;
            Vector2 force = rb.velocity.normalized * 5f;
            DamageInfo info = new DamageInfo(shooter, damage, force, hitPt);
            target.TakeDamage(info);
        }

        ReturnMe();
    }

    void ReturnMe()
    {
        active = false;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0;
        PoolManager.Instance.ReturnToPool(gameObject);
    }

    // не трогать — нужно для пула
    void OnEnable()
    {
        active = false;
    }
}
