using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TrapDamageZone : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float interval = 0.5f;
    [SerializeField] private CombatFaction targetFaction = CombatFaction.Player;

    private readonly Dictionary<Health, float> nextDamageAtByTarget = new();

    private void Awake()
    {
        Collider2D zone = GetComponent<Collider2D>();
        zone.isTrigger = true;
    }

    public void Configure(float damageAmount, float damageInterval, CombatFaction faction)
    {
        damage = Mathf.Max(0f, damageAmount);
        interval = Mathf.Max(0.05f, damageInterval);
        targetFaction = faction;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        FactionMember faction = other.GetComponentInParent<FactionMember>();
        if (faction == null || faction.Faction != targetFaction)
            return;

        Health health = other.GetComponentInParent<Health>();
        if (health == null || health.IsDead)
            return;

        if (nextDamageAtByTarget.TryGetValue(health, out float nextDamageAt) && Time.time < nextDamageAt)
            return;

        health.TakeDamage(damage);
        nextDamageAtByTarget[health] = Time.time + interval;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Health health = other.GetComponentInParent<Health>();
        if (health != null)
            nextDamageAtByTarget.Remove(health);
    }
}
