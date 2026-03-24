using UnityEngine;

public enum StatType { Speed, Armor, DamageBonus }

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveForce = 12f;
    [SerializeField] float maxSpeed = 8f;
    public Rigidbody2D rb;

    bool inputActive = true;
    GameObject currentWeaponObj;

    // бусты
    float bonusSpeed = 0;
    float bonusDmg = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (!inputActive) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector2 dir = new Vector2(h, v).normalized;
        float totalForce = moveForce + bonusSpeed;

        if (dir.magnitude > 0.01f)
            rb.AddForce(dir * totalForce);

        if (rb.velocity.magnitude > maxSpeed)
            rb.velocity = rb.velocity.normalized * maxSpeed;

        // вращаем оружие вокруг игрока
        if (currentWeaponObj != null)
        {
            weaponAngle += weaponSpinSpeed * Time.fixedDeltaTime;
            float rad = weaponAngle * Mathf.Deg2Rad;
            Vector2 orbitPos = (Vector2)transform.position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * weaponOrbitRadius;

            var weaponRb = currentWeaponObj.GetComponent<Rigidbody2D>();
            if (weaponRb != null)
            {
                weaponRb.MovePosition(orbitPos);
                weaponRb.MoveRotation(weaponAngle - 90f);
            }
        }
    }

    public void SetInputActive(bool active)
    {
        inputActive = active;
    }

    [SerializeField] float weaponSpinSpeed = 300f;
    float weaponAngle = 0f;
    float weaponOrbitRadius = 1.8f;

    public void EquipWeapon(WeaponData data)
    {
        if (currentWeaponObj != null)
            Destroy(currentWeaponObj);

        currentWeaponObj = new GameObject("Weapon_" + data.Name);
        currentWeaponObj.layer = 10; // Weapon
        currentWeaponObj.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        var sr = currentWeaponObj.AddComponent<SpriteRenderer>();
        var swordSprite = Resources.Load<Sprite>("sword_player");
        if (swordSprite != null)
        {
            sr.sprite = swordSprite;
            sr.color = Color.white;
        }
        else
        {
            sr.sprite = MakeRuntimeSprite();
            sr.color = new Color(0.6f, 0.6f, 0.6f);
        }
        sr.sortingOrder = 2;

        var weaponRb = currentWeaponObj.AddComponent<Rigidbody2D>();
        weaponRb.mass = data.Mass;
        weaponRb.gravityScale = 0f;
        weaponRb.isKinematic = true;

        var col = currentWeaponObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        WeaponBase weapon = null;
        if (data.Type == WeaponType.Melee)
            weapon = currentWeaponObj.AddComponent<MeleeWeapon>();
        else if (data.Type == WeaponType.Ranged)
            weapon = currentWeaponObj.AddComponent<RangedWeapon>();

        if (weapon != null)
            weapon.Initialize(data, gameObject);

        weaponAngle = 0f;
        BalanceCharacter(data.Mass);
    }

    // белый квадрат чтобы оружие было видно
    static Sprite _runtimeSprite;
    Sprite MakeRuntimeSprite()
    {
        if (_runtimeSprite != null) return _runtimeSprite;
        Texture2D tex = new Texture2D(32, 32);
        Color[] px = new Color[32 * 32];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        _runtimeSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        return _runtimeSprite;
    }

    void BalanceCharacter(float weaponMass)
    {
        // компенсация веса чтоб шар не заваливался сильно
        rb.mass = 2f + weaponMass * 0.5f;
        rb.angularDrag = 3f;
    }

    public void AddBonusStats(StatType type, float val)
    {
        switch (type)
        {
            case StatType.Speed: bonusSpeed += val; break;
            case StatType.DamageBonus: bonusDmg += val; break;
        }
    }

    public float GetBonusDamage() => bonusDmg;
}
