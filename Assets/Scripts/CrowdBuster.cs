using UnityEngine;
// 1. Crucial: Include the new Input System namespace
using UnityEngine.InputSystem;

public class CrowdBuster : MonoBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float explosionForce = 500f;
    [SerializeField] private ForceMode2D forceMode = ForceMode2D.Impulse;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 2. Safely check if a mouse is connected
        if (Mouse.current == null) return;

        // 3. New Input System syntax for "Was clicked this frame"
        // use middle mouse button for triggering the explosion
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            TriggerExplosionAtMouse();
        }
    }

    private void TriggerExplosionAtMouse()
    {
        // 4. Read the mouse position via the new API
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // Convert screen space to 2D world space
        Vector2 worldPoint = mainCamera.ScreenToWorldPoint(mousePosition);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(worldPoint, explosionRadius);

        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Agent"))
            {
                Rigidbody2D rb = col.GetComponent<Rigidbody2D>();

                if (rb != null)
                {
                    Vector2 direction = (Vector2)col.transform.position - worldPoint;
                    float distance = direction.magnitude;

                    if (distance == 0) direction = Random.insideUnitCircle.normalized;
                    else direction.Normalize();

                    float wearoff = 1 - (distance / explosionRadius);
                    Vector2 finalForce = direction * (explosionForce * wearoff);

                    rb.AddForce(finalForce, forceMode);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Debug gizmo logic remains unchanged
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}