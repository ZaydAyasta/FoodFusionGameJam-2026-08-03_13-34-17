using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDeathNotifier : MonoBehaviour
{
    private Health health;
    private bool notified;

    public event Action<EnemyDeathNotifier> Died;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        Arm();
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= HandleDied;
    }

    public void Arm()
    {
        if (health == null)
            health = GetComponent<Health>();

        if (health == null)
            return;

        notified = false;
        health.Died -= HandleDied;
        health.Died += HandleDied;
    }

    private void HandleDied()
    {
        if (notified)
            return;

        notified = true;
        Died?.Invoke(this);
    }
}
