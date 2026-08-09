using System.Collections;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = new(1f, 0.28f, 0.28f, 1f);
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField, Range(0f, 1f)] private float flashStrength = 0.45f;

    private Coroutine flashRoutine;

    public void Flash()
    {
        if (!isActiveAndEnabled)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        Color[] originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            originalColors[i] = renderers[i].color;
            renderers[i].color = Color.Lerp(originalColors[i], flashColor, flashStrength);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].color = originalColors[i];
        }

        flashRoutine = null;
    }
}
