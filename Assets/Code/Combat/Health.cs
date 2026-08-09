using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float invulnerabilityDuration;
    [SerializeField] private bool destroyOnDeath;

    private DamageFlash damageFlash;
    private float invulnerableUntil;
    private bool dead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead => dead;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        damageFlash = GetComponent<DamageFlash>();
        if (damageFlash == null)
            damageFlash = gameObject.AddComponent<DamageFlash>();

        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (dead || amount <= 0f || Time.time < invulnerableUntil)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        damageFlash?.Flash();

        if (invulnerabilityDuration > 0f)
            invulnerableUntil = Time.time + invulnerabilityDuration;

        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (dead || amount <= 0f)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void IncreaseMaxHealth(float amount, bool healByIncrease)
    {
        if (dead || amount <= 0f)
            return;

        maxHealth += amount;
        if (healByIncrease)
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        else
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void ResetHealth()
    {
        dead = false;
        invulnerableUntil = 0f;
        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void Configure(float newMaxHealth, bool resetToFull, bool destroyWhenDead)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        destroyOnDeath = destroyWhenDead;

        if (resetToFull)
        {
            ResetHealth();
            return;
        }

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void MakeInvulnerable(float duration)
    {
        if (duration > 0f)
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + duration);
    }

    private void Die()
    {
        if (dead)
            return;

        dead = true;
        Died?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}
