using UnityEngine;

public class DoorController : MonoBehaviour
{
    [SerializeField] private Collider2D blockingCollider;
    [SerializeField] private Renderer visual;

    private void Awake()
    {
        if (blockingCollider == null)
            blockingCollider = GetComponent<Collider2D>();

        if (blockingCollider == null)
            blockingCollider = GetComponentInChildren<Collider2D>();

        if (visual == null)
            visual = GetComponent<Renderer>();
    }

    public void SetClosed(bool closed)
    {
        if (blockingCollider != null)
            blockingCollider.enabled = closed;

        if (visual != null)
            visual.enabled = closed;
    }
}
