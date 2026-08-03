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
        notified = false;
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.Died -= HandleDied;
    }

    private void HandleDied()
    {
        if (notified)
            return;

        notified = true;
        Died?.Invoke(this);
    }
}
