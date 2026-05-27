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
    public string SectionName; // e.g., "Milk Section", "Bakery"
    public List<ProductSlot> Slots = new List<ProductSlot>();

    // Helper function for your agents to quickly grab an open spot
    public bool TryGetEmptySlot(out Vector2 slotPosition)
    {
        foreach (var slot in Slots)
        {
            if (!slot.IsOccupied)
            {
                slotPosition = slot.Position;
                return true;
            }
        }

        // Fallback if full: return the center or first slot
        slotPosition = Slots.Count > 0 ? Slots[0].Position : Vector2.zero;
        return false;
    }

}
