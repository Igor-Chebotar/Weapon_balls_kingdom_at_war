using UnityEngine;

public class HealPickup : MonoBehaviour
{
    float lifetime = 30f;
    float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
        gameObject.layer = 2; // IgnoreRaycast

        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.simulated = true;
        }
        else
        {
            // если нет — добавим, иначе триггер может не сработать
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }
    }

    void Update()
    {
        if (Time.time - spawnTime > lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        var hp = other.GetComponent<HealthComponent>();
        if (hp == null) return;

        // зачем лечить если и так фулл хп
        if (hp.GetHP() >= hp.GetMaxHP()) return;

        // хилка на 60% макс хп
        float healAmount = hp.GetMaxHP() * 0.6f;
        hp.Heal(healAmount);
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("heal");
        Destroy(gameObject);
    }
}
