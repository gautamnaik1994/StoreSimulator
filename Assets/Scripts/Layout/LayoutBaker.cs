using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class LayoutBaker : MonoBehaviour
{
    [SerializeField] private SupermarketLayoutSO layoutAsset;

    private List<string> all_product_section = new List<string>();
    private ProductDatabase productDB;

    private ProductSectionInfo sectionInfo;

    [ContextMenu("Bake Sections and Slots")]
    public void Bake()
    {
        if (layoutAsset == null) return;


        layoutAsset.ProductSections.Clear();
        layoutAsset.ExitLocations.Clear();
        layoutAsset.CheckoutCounters.Clear();
        layoutAsset.HoldingAreas.Clear();

        // productDB = Resources.Load<ProductDatabase>("ProductDatabase");
        // print out all product names and prices in the database to verify it's loading correctly
        // Debug.Log("Loaded Product Database:");
        // string productList = string.Join(", ", productDB.products.Select(p => $"\"{p.productName}\" (Price: {p.price})"));
        // Debug.Log(productList);



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
                    // Debug.Log($"Added checkout slot {slotTransform.name} at position {newSlot.Position} to counter '{sectionTransform.name}'");
                }

                layoutAsset.CheckoutCounters.Add(newCounter);
                continue; // Skip the rest of the loop for this iteration since it's a checkout counter, not a product section
            }

            sectionInfo = sectionTransform.gameObject.GetComponent<ProductSectionInfo>();
            // sectionInfo.SectionDepartment = (ProductSectionInfo.Department)System.Enum.Parse(typeof(ProductSectionInfo.Department), sectionTransform.tag);
            // sectionInfo.Price = productDB[sectionTransform.name].price;
            // Debug.Log($"Section '{sectionTransform.name}' has price '{sectionInfo.Price}'");
            ProductSection newSection = new ProductSection
            {
                SectionName = sectionTransform.name,
                Price = sectionInfo.Price,
                ProductCategory = sectionInfo.SectionDepartment
            };
            // Debug.Log($"Product section '{sectionTransform.name}' price'{productDB[sectionTransform.name].price}'");

            all_product_section.Add(sectionTransform.name);

            // Loop through the sub-children (the actual 3 individual slot positions)
            foreach (Transform slotTransform in sectionTransform)
            {
                ProductSlot newSlot = new ProductSlot
                {
                    Position = slotTransform.position,
                    IsOccupied = false
                };
                newSection.Slots.Add(newSlot);
                // Debug.Log($"Added product slot {slotTransform.name} at position {newSlot.Position} to section '{newSection.SectionName}'");
            }

            layoutAsset.ProductSections.Add(newSection);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(layoutAsset);
#endif
        Debug.Log("Nested store layout successfully baked!");
        Debug.Log($"All product sections: {string.Join(", ", all_product_section.Select(section => $"\"{section}\""))}");
    }
}