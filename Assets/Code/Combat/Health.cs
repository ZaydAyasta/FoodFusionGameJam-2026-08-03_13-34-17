using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float invulnerabilityDuration;
    [SerializeField] private bool destroyOnDeath;
    [SerializeField, Range(0f, 0.5f)] private float damageReduction;

    private DamageFlash damageFlash;
    private PlayerDash playerDash;
    private float invulnerableUntil;
    private bool dead;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public bool IsDead => dead;
    public float DamageReduction => damageReduction;

    public event Action<float, float> HealthChanged;
    public event Action Died;

    private void Awake()
    {
        damageFlash = GetComponent<DamageFlash>();
        playerDash = GetComponent<PlayerDash>();
        if (damageFlash == null)
            damageFlash = gameObject.AddComponent<DamageFlash>();

        CurrentHealth = maxHealth;
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (dead || amount <= 0f || Time.time < invulnerableUntil ||
            (playerDash != null && playerDash.IsDashing))
            return;

        float receivedDamage = amount * (1f - damageReduction);
        CurrentHealth = Mathf.Max(0f, CurrentHealth - receivedDamage);
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

    public void IncreaseMaxHealthUpTo(float amount, float maximum, bool healByIncrease)
    {
        if (dead || amount <= 0f || maximum <= maxHealth)
            return;

        float previousMaxHealth = maxHealth;
        maxHealth = Mathf.Min(maximum, maxHealth + amount);
        float appliedIncrease = maxHealth - previousMaxHealth;

        if (healByIncrease)
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + appliedIncrease);
        else
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, maxHealth);

        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    public void AddDamageReduction(float amount, float maximum = 0.5f)
    {
        if (amount <= 0f)
            return;

        damageReduction = Mathf.Clamp(damageReduction + amount, 0f, Mathf.Clamp01(maximum));
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
