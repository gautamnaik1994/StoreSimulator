using System.Collections.Generic;
using UnityEngine;

public class LayoutBaker : MonoBehaviour
{
    [SerializeField] private SupermarketLayoutSO layoutAsset;

    [ContextMenu("Bake Sections and Slots")]
    public void Bake()
    {
        if (layoutAsset == null) return;


        layoutAsset.ProductSections.Clear();
        layoutAsset.ExitLocations.Clear();
        layoutAsset.CheckoutCounters.Clear();
        layoutAsset.HoldingAreas.Clear();


        // Loop through all top-level children of this GameObject (e.g., "Milk Section", "Cereal Isle")
        foreach (Transform sectionTransform in transform)
        {
            // check if transform is active before processing to allow for easy exclusion of certain sections from the layout by simply deactivating them in the hierarchy
            if (!sectionTransform.gameObject.activeInHierarchy)
            {
                Debug.Log($"Skipping inactive section '{sectionTransform.name}'");
                continue;
            }

            // caompare tag to see if this is an exit location or a product section
            if (sectionTransform.CompareTag("Exit"))
            {
                layoutAsset.ExitLocations.Add(sectionTransform.position);
                continue; // Skip the rest of the loop for this iteration since it's an exit, not a product section
            }

            if (sectionTransform.CompareTag("Holding"))
            {
                Holding newHolding = new Holding
                {
                    SectionName = sectionTransform.name,
                    CenterPosition = sectionTransform.position,
                };
                layoutAsset.HoldingAreas.Add(newHolding);
                continue; // Skip the rest of the loop for this iteration since it's a holding area, not a product section
            }

            if (sectionTransform.CompareTag("Checkout"))
            {

                CheckoutCounter newCounter = new CheckoutCounter
                {
                    CounterName = sectionTransform.name,
                    position = sectionTransform.position
                };
                // Get the component "CheckoutHandler" from the current sectionTransform and assign it to the newCounter's handler reference

                // Loop through the sub-children (the actual 3 individual slot positions)
                foreach (Transform slotTransform in sectionTransform)
                {
                    CheckoutSlot newSlot = new CheckoutSlot
                    {
                        Position = slotTransform.position,
                        IsOccupied = false
                    };
                    newCounter.QueueSlots.Add(newSlot);
                    Debug.Log($"Added checkout slot {slotTransform.name} at position {newSlot.Position} to counter '{sectionTransform.name}'");
                }

                layoutAsset.CheckoutCounters.Add(newCounter);
                continue; // Skip the rest of the loop for this iteration since it's a checkout counter, not a product section
            }

            ProductSection newSection = new ProductSection
            {
                SectionName = sectionTransform.name,
                Price = Random.Range(50, 500) // Assign a random price for demonstration; you can customize this as needed
            };

            // Loop through the sub-children (the actual 3 individual slot positions)
            foreach (Transform slotTransform in sectionTransform)
            {
                ProductSlot newSlot = new ProductSlot
                {
                    Position = slotTransform.position,
                    IsOccupied = false
                };
                newSection.Slots.Add(newSlot);
                Debug.Log($"Added product slot {slotTransform.name} at position {newSlot.Position} to section '{newSection.SectionName}'");
            }

            layoutAsset.ProductSections.Add(newSection);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(layoutAsset);
#endif
        Debug.Log("Nested store layout successfully baked!");
    }
}