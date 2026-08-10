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
        SetClosed(closed, closed);
    }

    public void SetClosed(bool closed, bool colliderEnabled)
    {
        if (blockingCollider != null)
            blockingCollider.enabled = colliderEnabled;

        if (visual != null)
            visual.enabled = closed;
    }
}
