using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    Text txt;
    float timer;
    float lifetime = 0.8f;
    Vector3 velocity;
    bool alive = false;

    void Awake()
    {
        txt = GetComponentInChildren<Text>();
    }

    public void Init(float amount, Vector2 pos)
    {
        transform.position = new Vector3(pos.x, pos.y, 0);
        if (txt != null)
            txt.text = amount.ToString("F0");

        timer = 0;
        alive = true;

        // рандомное направление вверх
        velocity = new Vector3(Random.Range(-0.5f, 0.5f), 2f, 0);

        // цвет по силе удара
        if (txt != null)
        {
            if (amount > 20)
                txt.color = Color.red;
            else
                txt.color = Color.white;
        }
    }

    void Update()
    {
        if (!alive) return;

        timer += Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        velocity *= 0.95f; // замедление

        // фейд
        if (txt != null)
        {
            float alpha = 1f - (timer / lifetime);
            txt.color = new Color(txt.color.r, txt.color.g, txt.color.b, alpha);
        }

        if (timer >= lifetime)
        {
            alive = false;
            // вернуть в пул если есть
            if (PoolManager.Instance != null)
                PoolManager.Instance.ReturnToPool(gameObject);
            else
                Destroy(gameObject);
        }
    }
}
