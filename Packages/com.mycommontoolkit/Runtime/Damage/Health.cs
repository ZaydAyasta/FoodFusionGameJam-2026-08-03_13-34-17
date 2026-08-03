using System;
using UnityEngine;

namespace MyCommonToolkit
{
    namespace DamageSystem
    {
        public class Health : MonoBehaviour,IDamageable
        {
            public float maxHealth;
            public float CurrentHealth { get; private set; }
            public event Action<float> OnHealthChanged;
            public event Action OnDeath;
            public bool isDamageable=true;
            void Awake()
            {
                CurrentHealth = maxHealth;
            }
            public void TakeDamage(float damage)
            {
                if (!isDamageable) return;
                CurrentHealth -= damage;
                OnHealthChanged?.Invoke(-damage);
                if (CurrentHealth <= 0)
                    OnDeath?.Invoke();
            }
            public void Heal(float amount)
            {
                CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
                OnHealthChanged?.Invoke(amount);
            }
        }
    }
}
