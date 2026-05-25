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


[CreateAssetMenu(fileName = "SupermarketLayoutSO", menuName = "Scriptable Objects/SupermarketLayoutSO")]
public class SupermarketLayoutSO : ScriptableObject
{
    public List<ProductSection> ProductSections = new List<ProductSection>();
    public List<Vector2> ExitLocations = new List<Vector2>(); // List of exit positions in the store

    // create a dictionary for quick lookup of sections name by location, which can be used by agents to determine which section they are in based on their position
    public Dictionary<Vector2, (ProductSection section, ProductSlot slot)> sectionLookup = new Dictionary<Vector2, (ProductSection section, ProductSlot slot)>();

    private void OnEnable()
    {
        // Build the lookup dictionary when the ScriptableObject is loaded
        sectionLookup.Clear();
        foreach (var section in ProductSections)
        {
            foreach (var slot in section.Slots)
            {
                sectionLookup[slot.Position] = (section, slot);
            }
        }
    }

    public void ResetLayout()
    {
        foreach (var section in ProductSections)
        {
            foreach (var slot in section.Slots)
            {
                slot.IsOccupied = false; // Reset the flag
            }
        }
    }
}
