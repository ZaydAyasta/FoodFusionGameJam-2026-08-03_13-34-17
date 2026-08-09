using System.Collections;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = new(1f, 0.28f, 0.28f, 1f);
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField, Range(0f, 1f)] private float flashStrength = 0.45f;

    private Coroutine flashRoutine;
    private SpriteRenderer[] renderers;
    private Color[] baseColors;

    private void Awake()
    {
        CacheBaseColors();
    }

    public void Flash()
    {
        if (!isActiveAndEnabled)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        // A new hit may arrive while the previous flash is still active.
        // Always restore the real base color before applying the next flash,
        // otherwise the red tint accumulates on every overlapping hit.
        RestoreBaseColors();

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (renderers == null || baseColors == null)
            CacheBaseColors();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            renderers[i].color = Color.Lerp(baseColors[i], flashColor, flashStrength);
        }

        yield return new WaitForSeconds(flashDuration);
        RestoreBaseColors();
        flashRoutine = null;
    }

    private void CacheBaseColors()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                baseColors[i] = renderers[i].color;
        }
    }

    private void RestoreBaseColors()
    {
        if (renderers == null || baseColors == null)
            return;

        int count = Mathf.Min(renderers.Length, baseColors.Length);
        for (int i = 0; i < count; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = baseColors[i];
        }
    }

    private void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        RestoreBaseColors();
    }
}
