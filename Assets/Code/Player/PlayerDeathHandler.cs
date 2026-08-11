using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDeathHandler : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float deathFreezeDuration = 1.55f;

    private Health health;
    private bool deathSequenceStarted;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.Died -= HandleDied;
    }

    private void HandleDied()
    {
        if (!deathSequenceStarted)
            StartCoroutine(PlayDeathSequence());
    }

    private IEnumerator PlayDeathSequence()
    {
        deathSequenceStarted = true;
        GameObject finalAttacker = ResolveFinalAttacker(health.LastDamageSource);

        Time.timeScale = 0f;
        GameAudio.StopAllAudio();
        GameAudio.PlayDeathHit();
        HideEverythingExceptDeathSubjects(finalAttacker);
        TintRenderers(gameObject, new Color(0.62f, 0.055f, 0.045f, 1f));
        if (finalAttacker != null)
        {
            TintRenderers(finalAttacker, Color.white);
            SetSpritePriority(finalAttacker, 12);
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, deathFreezeDuration));
        GameMenuHud.ShowGameOver();
    }

    private static GameObject ResolveFinalAttacker(GameObject damageSource)
    {
        if (damageSource == null)
            return null;

        FactionMember faction = damageSource.GetComponentInParent<FactionMember>();
        if (faction != null)
            return faction.gameObject;

        Rigidbody2D body = damageSource.GetComponentInParent<Rigidbody2D>();
        return body != null ? body.gameObject : damageSource;
    }

    private void HideEverythingExceptDeathSubjects(GameObject finalAttacker)
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Transform renderedTransform = renderer.transform;
            bool belongsToPlayer = renderedTransform == transform || renderedTransform.IsChildOf(transform);
            bool belongsToAttacker = finalAttacker != null &&
                (renderedTransform == finalAttacker.transform || renderedTransform.IsChildOf(finalAttacker.transform));
            if (belongsToPlayer || belongsToAttacker)
                continue;

            renderer.enabled = false;
        }
    }

    private static void TintRenderers(GameObject target, Color color)
    {
        foreach (SpriteRenderer renderer in target.GetComponentsInChildren<SpriteRenderer>(true))
        {
            Shader solidShader = Resources.Load<Shader>("Shaders/SolidSprite") ??
                Shader.Find("FoodFusion/SolidSprite");
            if (solidShader != null)
                renderer.material = new Material(solidShader);
            renderer.color = color;
            renderer.enabled = true;
        }
    }

    private static void SetSpritePriority(GameObject target, int sortingOrder)
    {
        foreach (SpriteRenderer renderer in target.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.enabled = true;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
