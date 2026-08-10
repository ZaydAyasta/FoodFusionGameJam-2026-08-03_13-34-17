using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CharacterInput))]
public class PlayerDash : MonoBehaviour
{
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.75f;
    [SerializeField, Min(1)] private int maxDashCharges = 1;

    [Header("Afterimage Trail")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField, Min(0.01f)] private float afterimageInterval = 0.035f;
    [SerializeField, Min(0.01f)] private float afterimageLifetime = 0.22f;
    [SerializeField, Range(0f, 1f)] private float afterimageAlpha = 0.42f;
    [SerializeField] private int afterimageSortingOffset = -1;

    private CharacterInput input;
    private Rigidbody2D rb;
    private Vector2 lastMoveDirection = Vector2.right;
    private int currentDashCharges;

    public bool IsDashing { get; private set; }
    public int CurrentDashCharges => currentDashCharges;
    public int MaxDashCharges => maxDashCharges;

    private void Awake()
    {
        input = GetComponent<CharacterInput>();
        rb = GetComponent<Rigidbody2D>();
        maxDashCharges = Mathf.Max(1, maxDashCharges);
        currentDashCharges = maxDashCharges;
        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<SpriteRenderer>(true);
    }

    private void Update()
    {
        Vector2 move = input.MoveInput;
        if (move.sqrMagnitude > 0.001f)
            lastMoveDirection = move.normalized;

        if (input.DashPressed && currentDashCharges > 0 && !IsDashing)
            StartCoroutine(DashRoutine());
    }

    public void AddDashCharge(int amount = 1)
    {
        if (amount <= 0)
            return;

        maxDashCharges += amount;
        currentDashCharges += amount;
    }

    private IEnumerator DashRoutine()
    {
        IsDashing = true;
        currentDashCharges--;
        StartCoroutine(RechargeDashCharge());
        rb.linearVelocity = lastMoveDirection * dashSpeed;
        GameAudio.PlayDash();
        StartCoroutine(AfterimageTrailRoutine());

        yield return new WaitForSeconds(dashDuration);

        IsDashing = false;
    }

    private IEnumerator RechargeDashCharge()
    {
        yield return new WaitForSeconds(dashCooldown);
        currentDashCharges = Mathf.Min(maxDashCharges, currentDashCharges + 1);
    }

    private IEnumerator AfterimageTrailRoutine()
    {
        while (IsDashing)
        {
            CreateAfterimage();
            yield return new WaitForSeconds(Mathf.Max(0.01f, afterimageInterval));
        }
    }

    private void CreateAfterimage()
    {
        if (visualRenderer == null || visualRenderer.sprite == null)
            return;

        GameObject afterimageObject = new("Dash Afterimage");
        Transform afterimageTransform = afterimageObject.transform;
        afterimageTransform.position = visualRenderer.transform.position;
        afterimageTransform.rotation = visualRenderer.transform.rotation;
        afterimageTransform.localScale = visualRenderer.transform.lossyScale;

        SpriteRenderer ghost = afterimageObject.AddComponent<SpriteRenderer>();
        ghost.sprite = visualRenderer.sprite;
        ghost.flipX = visualRenderer.flipX;
        ghost.flipY = visualRenderer.flipY;
        ghost.sharedMaterial = visualRenderer.sharedMaterial;
        ghost.sortingLayerID = visualRenderer.sortingLayerID;
        ghost.sortingOrder = visualRenderer.sortingOrder + afterimageSortingOffset;

        Color color = visualRenderer.color;
        color.a *= afterimageAlpha;
        ghost.color = color;

        float lifetime = Mathf.Max(0.01f, afterimageLifetime);
        Destroy(afterimageObject, lifetime + 0.05f);
        StartCoroutine(FadeAfterimage(ghost, color, lifetime));
    }

    private static IEnumerator FadeAfterimage(SpriteRenderer ghost, Color initialColor, float lifetime)
    {
        float elapsed = 0f;
        while (ghost != null && elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            Color color = initialColor;
            color.a = initialColor.a * (1f - Mathf.Clamp01(elapsed / lifetime));
            ghost.color = color;
            yield return null;
        }

        if (ghost != null)
            Destroy(ghost.gameObject);
    }
}
