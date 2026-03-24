using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour, IDamageable
{
    float _maxHp;
    float hp;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnHit;

    bool dead = false;
    public bool invincible = false;

    public void Initialize(float maxHealth)
    {
        _maxHp = maxHealth;
        hp = maxHealth;
        dead = false;
    }

    public void TakeDamage(DamageInfo info)
    {
        if (dead || invincible) return;

        hp -= info.DamageAmount;
        if (hp < 0) hp = 0;

        OnHit?.Invoke();
        OnHealthChanged?.Invoke(hp, _maxHp);

        if (hp <= 0)
            Kill();
    }

    public void Heal(float amount)
    {
        if (dead) return;
        hp += amount;
        if (hp > _maxHp) hp = _maxHp;
        OnHealthChanged?.Invoke(hp, _maxHp);
    }

    public void Kill()
    {
        hp = 0;
        dead = true;
        OnDeath?.Invoke();
    }

    public float GetHP() => hp;
    public float GetMaxHP() => _maxHp;
}
