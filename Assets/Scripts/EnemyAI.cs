using UnityEngine;

public enum EnemyState { Alive, Stunned, Dead }

public class EnemyAI : MonoBehaviour
{
    Rigidbody2D rb;
    HealthComponent hp;

    float atkDamage;
    float stunResist;
    float stunTimer;
    float moveSpeed;
    EnemyState state = EnemyState.Alive;
    EnemyType enemyType;

    // фаерболы короля
    float fireballTimer;
    float fireballInterval = 3f;
    int fireballCount = 8;
    float fireballSpeed = 6f;

    // мечи
    GameObject[] swords;
    float swordAngle;
    float swordSpinSpeed = 200f;
    float swordOrbit = 2f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hp = GetComponent<HealthComponent>();

        var col = GetComponent<Collider2D>();
        if (col != null && col.sharedMaterial == null)
        {
            var mat = new PhysicsMaterial2D("Bounce");
            mat.bounciness = 1f;
            mat.friction = 0f;
            col.sharedMaterial = mat;
        }
    }

    public void ConfigureFromData(EnemyData data)
    {
        enemyType = data.type;
        atkDamage = data.attackDamage;
        stunResist = data.stunResistance;
        moveSpeed = data.moveSpeed > 0 ? data.moveSpeed : 4f;
        rb.mass = data.mass;
        rb.gravityScale = 0;
        rb.drag = 0;
        rb.angularDrag = 0;

        hp.Initialize(data.maxHealth);

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = data.color;

        if (GetComponent<EnemyHPDisplay>() == null)
            gameObject.AddComponent<EnemyHPDisplay>();

        hp.OnDeath += OnDied;
        hp.OnHit += OnEnemyHit;

        // толчок
        Vector2 dir = Random.insideUnitCircle.normalized;
        rb.velocity = dir * moveSpeed;

        // тип
        switch (enemyType)
        {
            case EnemyType.Knight:
                transform.localScale = new Vector3(2.5f, 2.5f, 1f);
                rb.velocity = Random.insideUnitCircle.normalized * moveSpeed;
                // контактный урон
                break;

            case EnemyType.HeavyKnight:
                transform.localScale = new Vector3(3.5f, 3.5f, 1f);
                swordSpinSpeed = 50f;
                swordOrbit = 2.5f;
                CreateSingleSword(1.5f, 1.5f, atkDamage);
                break;

            case EnemyType.King:
                transform.localScale = new Vector3(2.5f, 2.5f, 1f);
                fireballTimer = 2f;
                break;
        }
    }

    // направление
    public void SetInitialDirection(Vector2 dir)
    {
        rb.velocity = dir.normalized * moveSpeed;
    }

    public void ApplyStun(float duration)
    {
        state = EnemyState.Stunned;
        stunTimer = duration;
        rb.velocity *= 0.3f;
    }

    void FixedUpdate()
    {
        if (state == EnemyState.Dead) return;

        if (state == EnemyState.Stunned)
        {
            stunTimer -= Time.fixedDeltaTime;
            if (stunTimer <= 0)
            {
                state = EnemyState.Alive;
                rb.velocity = Random.insideUnitCircle.normalized * moveSpeed;
            }
            return;
        }

        if (rb.velocity.magnitude > 0.01f)
            rb.velocity = rb.velocity.normalized * moveSpeed;
    }

    void Update()
    {
        if (state != EnemyState.Alive) return;

        // крутить мечи
        if (swords != null)
        {
            swordAngle += swordSpinSpeed * Time.deltaTime;
            for (int i = 0; i < swords.Length; i++)
            {
                if (swords[i] == null) continue;
                float offset = (360f / swords.Length) * i;
                float rad = (swordAngle + offset) * Mathf.Deg2Rad;
                Vector2 pos = (Vector2)transform.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * swordOrbit;
                swords[i].transform.position = pos;
                swords[i].transform.rotation = Quaternion.Euler(0, 0, swordAngle + offset - 90f);
            }
        }

        // фаерболы короля
        if (enemyType == EnemyType.King)
        {
            fireballTimer -= Time.deltaTime;
            if (fireballTimer <= 0)
            {
                ShootFireballs();
                fireballTimer = fireballInterval;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (state != EnemyState.Alive) return;

        // обычный рыцарь и король — урон при касании
        if (enemyType == EnemyType.Knight || enemyType == EnemyType.King)
        {
            var player = col.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                var target = col.gameObject.GetComponent<IDamageable>();
                if (target != null)
                {
                    Vector2 hitPt = col.contacts.Length > 0 ? col.GetContact(0).point : (Vector2)transform.position;
                    Vector2 force = ((Vector2)col.gameObject.transform.position - (Vector2)transform.position).normalized * 3f;
                    target.TakeDamage(new DamageInfo(gameObject, atkDamage, force, hitPt));
                }
            }
        }

        if (col.relativeVelocity.magnitude > stunResist)
            ApplyStun(1.5f);
    }

    void CreateSingleSword(float scaleX, float scaleY, float dmg)
    {
        swords = new GameObject[1];
        swords[0] = MakeSwordObj(scaleX, scaleY, dmg);
    }

    GameObject MakeSwordObj(float scaleX, float scaleY, float dmg)
    {
        var obj = new GameObject("EnemySword");
        obj.layer = 2;
        obj.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        var sr = obj.AddComponent<SpriteRenderer>();
        var heavySwordSprite = Resources.Load<Sprite>("sword_heavy");
        if (heavySwordSprite != null)
        {
            sr.sprite = heavySwordSprite;
            sr.color = Color.white;
        }
        else
        {
            // фоллбэк — белый квадрат
            Texture2D tex = new Texture2D(4, 4);
            Color[] px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px); tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            sr.color = new Color(0.4f, 0.4f, 0.4f);
        }
        sr.sortingOrder = 3;

        obj.AddComponent<BoxCollider2D>().isTrigger = true;
        var sword = obj.AddComponent<EnemySword>();
        sword.damage = dmg;
        sword.owner = gameObject;

        return obj;
    }

    
    void ShootFireballs()
    {
        // пиу
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySound("fireball");

        for (int i = 0; i < fireballCount; i++)
        {
            float angle = (360f / fireballCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            var fb = new GameObject("Fireball");
            fb.layer = 9;
            fb.transform.position = (Vector2)transform.position + dir * 2.5f;
            fb.transform.localScale = new Vector3(1.8f, 1.8f, 1f);

            var sr = fb.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 0.6f, 0.1f);
            sr.sortingOrder = 4;
            sr.sprite = GetCircleSprite();

            fb.AddComponent<CircleCollider2D>();
            var rbFb = fb.AddComponent<Rigidbody2D>();
            rbFb.gravityScale = 0;
            rbFb.velocity = dir * fireballSpeed;

            var fireball = fb.AddComponent<Fireball>();
            fireball.damage = 3f;
            fireball.owner = gameObject;
        }
    }

    // sfx по типу
    void OnEnemyHit()
    {
        if (AudioManager.Instance == null) return;
        switch (enemyType)
        {
            case EnemyType.Knight:
                AudioManager.Instance.PlaySound("knight_hit"); break;
            case EnemyType.HeavyKnight:
                AudioManager.Instance.PlaySound("heavy_knight_hit"); break;
            case EnemyType.King:
                AudioManager.Instance.PlaySound("king_hit"); break;
        }
    }

    // смерть
    void OnDied()
    {
        hp.OnDeath -= OnDied;
        hp.OnHit -= OnEnemyHit;
        state = EnemyState.Dead;

        if (swords != null)
            foreach (var s in swords)
                if (s != null) Destroy(s);

        TryDropLoot();

        if (WaveManager.Instance != null)
            WaveManager.Instance.RegisterEnemyDeath();

        Destroy(gameObject, 0.1f);
    }

    
    static Sprite _circleSprite;
    static Sprite GetCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;
        Texture2D tex = new Texture2D(32, 32);
        Color[] px = new Color[32 * 32];
        Vector2 c = new Vector2(16, 16);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                px[y * 32 + x] = Vector2.Distance(new Vector2(x, y), c) <= 14 ? Color.white : Color.clear;
        tex.SetPixels(px); tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        return _circleSprite;
    }

    // дроп
    void TryDropLoot()
    {
        float roll = Random.value;
        if (roll < 0.15f) SpawnHeal();
        else if (roll < 0.65f) SpawnCoin();
    }

    void SpawnCoin()
    {
        var obj = new GameObject("GoldCoin");
        obj.transform.position = transform.position;
        obj.layer = 2;
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = new Color(1f, 0.85f, 0.1f);
        sr.sortingOrder = 5;
        obj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        obj.AddComponent<CircleCollider2D>().isTrigger = true;
        obj.AddComponent<GoldCoin>();
    }

    void SpawnHeal()
    {
        var obj = new GameObject("HealPickup");
        obj.transform.position = transform.position;
        obj.layer = 2;
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = GetCircleSprite();
        sr.color = new Color(0.2f, 0.9f, 0.3f);
        sr.sortingOrder = 5;
        obj.transform.localScale = new Vector3(1.4f, 1.4f, 1f);
        obj.AddComponent<CircleCollider2D>().isTrigger = true;
        obj.AddComponent<HealPickup>();
    }
}
