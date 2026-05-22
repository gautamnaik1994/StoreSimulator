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

        // Loop through all top-level children of this GameObject (e.g., "Milk Section", "Cereal Isle")
        foreach (Transform sectionTransform in transform)
        {
            ProductSection newSection = new ProductSection();
            newSection.SectionName = sectionTransform.name;

            // Loop through the sub-children (the actual 3 individual slot positions)
            foreach (Transform slotTransform in sectionTransform)
            {
                ProductSlot newSlot = new ProductSlot
                {
                    Position = slotTransform.position,
                    IsOccupied = false
                };
                newSection.Slots.Add(newSlot);
            }

            layoutAsset.ProductSections.Add(newSection);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(layoutAsset);
#endif
        Debug.Log("Nested store layout successfully baked!");
    }
}