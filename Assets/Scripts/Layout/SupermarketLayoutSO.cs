using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SupermarketLayoutSO", menuName = "Scriptable Objects/SupermarketLayoutSO")]
public class SupermarketLayoutSO : ScriptableObject
{
    public List<ProductSection> ProductSections = new List<ProductSection>();
    public List<Vector2> ExitLocations = new List<Vector2>(); // List of exit positions in the store
    public Dictionary<Vector2, (ProductSection section, ProductSlot slot)> sectionLookup = new Dictionary<Vector2, (ProductSection section, ProductSlot slot)>();

    public List<CheckoutCounter> CheckoutCounters = new List<CheckoutCounter>();
    [System.NonSerialized]
    public List<CheckoutHandler> CheckoutHandlers = new List<CheckoutHandler>();
    public List<Holding> HoldingAreas = new List<Holding>();
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
