using UnityEngine;
using System.Collections.Generic;

// 1. THE DATA HOUSES
[System.Serializable]
public class ProductSlot
{
    public Vector2 Position;
    public bool IsOccupied; // Tracks if an agent is currently standing here/heading here

}

[System.Serializable]
public class ProductSection
{
    public int Price; // Optional: Price of the product at this slot
    public string SectionName; // e.g., "Milk Section", "Bakery"

    public Department ProductCategory;
    public List<ProductSlot> Slots = new List<ProductSlot>();

    // Helper function for your agents to quickly grab an open spot
    public bool TryGetEmptySlot(out ProductSlot emptySlot)
    {
        foreach (var slot in Slots)
        {
            if (!slot.IsOccupied)
            {
                emptySlot = slot;
                return true;
            }
        }

        // Fallback if full: return the center or first slot
        emptySlot = Slots.Count > 0 ? Slots[0] : null;
        return false;
    }

}
