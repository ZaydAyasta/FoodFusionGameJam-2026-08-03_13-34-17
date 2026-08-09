using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class EnemyDeathNotifier : MonoBehaviour
{
    [Header("Room Population")]
    [Tooltip("0 = normal room population. Positive values add enemies because this type is easier. Negative values remove enemies because this type is harder.")]
    [SerializeField, Range(-8, 8)] private int populationWeight;

    private Health health;
    private bool notified;

    public event Action<EnemyDeathNotifier> Died;
    public int PopulationWeight => populationWeight;

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
