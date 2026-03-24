using UnityEngine;

public class GoldCoin : MonoBehaviour
{
    int value = 1;
    float lifetime = 15f;
    float spawnTime;

    void Start()
    {
        spawnTime = Time.time;
        gameObject.layer = 2; // IgnoreRaycast — ни с чем не сталкивается

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
        if (other.GetComponent<PlayerController>() == null) return;
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("coin");
        if (GameManager.Instance != null)
            GameManager.Instance.AddGold(value);
        Destroy(gameObject);
    }

    public void CollectSilently()
    {
        // тихий подбор (конец уровня)
        if (GameManager.Instance != null)
            GameManager.Instance.AddGold(value);
    }
}
