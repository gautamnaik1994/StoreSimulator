using UnityEngine;

public class CheckoutHandler : MonoBehaviour
{
    [SerializeField] private SupermarketLayoutSO layoutAsset;

    private CheckoutCounter associatedCounter;

    private void OnEnable()
    {
        if (layoutAsset != null && !layoutAsset.CheckoutHandlers.Contains(this))
        {
            layoutAsset.CheckoutHandlers.Add(this);
        }
        Debug.Log($"Registered checkout handler for '{gameObject.name}' with layout asset. Total handlers: {layoutAsset.CheckoutHandlers.Count}");

    }

    private void OnDisable()
    {
        if (layoutAsset != null)
        {
            layoutAsset.CheckoutHandlers.Remove(this);
        }
        Debug.Log($"Unregistered checkout handler for '{gameObject.name}' with layout asset. Total handlers: {layoutAsset.CheckoutHandlers.Count}");
    }

    void Start()
    {
        // Find the associated checkout counter in the layout asset based on the name of this GameObject
        associatedCounter = layoutAsset.CheckoutCounters.Find(counter => counter.CounterName == gameObject.name);
        if (associatedCounter == null)
        {
            Debug.LogError($"No matching checkout counter found in layout asset for '{gameObject.name}'. Please ensure the names match.");
        }
    }
}
