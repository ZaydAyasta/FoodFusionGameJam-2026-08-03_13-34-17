using System;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private CombatFaction faction;
    [SerializeField] private float damage = 1f;
    [SerializeField] private bool destroyOnDamage;

    public event Action<GameObject> DamageApplied;

    public void Configure(CombatFaction ownerFaction, float damageAmount, bool destroyAfterHit)
    {
        faction = ownerFaction;
        damage = damageAmount;
        destroyOnDamage = destroyAfterHit;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    private void TryDamage(GameObject target)
    {
        FactionMember targetFaction = target.GetComponentInParent<FactionMember>();
        if (targetFaction != null && targetFaction.Faction == faction)
            return;

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        damageable.TakeDamage(damage);
        DamageApplied?.Invoke(target);

        if (destroyOnDamage)
            Destroy(gameObject);
    }
}
