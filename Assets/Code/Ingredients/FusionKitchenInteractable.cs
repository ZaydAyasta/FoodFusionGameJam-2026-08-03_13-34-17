using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class FusionKitchenInteractable : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 1.6f;
    [SerializeField] private Collider2D interactionCollider;

    private Transform player;

    private void Awake()
    {
        if (interactionCollider == null)
            interactionCollider = GetComponent<Collider2D>();

        if (interactionCollider != null)
            interactionCollider.isTrigger = true;
    }

    private void Update()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (!IsVisible() || !IsPlayerCloseEnough() || !WasClicked())
            return;

        IngredientInventory inventory = player != null
            ? player.GetComponentInParent<IngredientInventory>()
            : null;
        if (inventory == null)
            inventory = IngredientInventory.ActivePlayerInventory;

        FusionKitchenHud.Open(inventory);
    }

    private bool IsVisible()
    {
        Renderer renderer = GetComponent<Renderer>();
        return renderer == null || renderer.enabled;
    }

    private bool IsPlayerCloseEnough()
    {
        if (player == null)
        {
            CharacterInput playerInput = FindFirstObjectByType<CharacterInput>();
            if (playerInput != null)
                player = playerInput.transform;
        }

        if (player == null)
            return false;

        return Vector2.Distance(player.position, transform.position) <= interactionDistance;
    }

    private bool WasClicked()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, 100f);
        if (hit.collider != null)
            return hit.collider == interactionCollider || hit.collider.transform.IsChildOf(transform);

        Vector3 worldPoint = camera.ScreenToWorldPoint(new Vector3(
            Mouse.current.position.ReadValue().x,
            Mouse.current.position.ReadValue().y,
            Mathf.Abs(camera.transform.position.z - transform.position.z)));

        return interactionCollider != null && interactionCollider.OverlapPoint(worldPoint);
    }
}
