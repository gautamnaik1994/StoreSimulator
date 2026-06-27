using UnityEngine;
using System.Collections.Generic;



[System.Serializable]
public class Holding
{
    public string SectionName; // e.g., "Milk Section", "Bakery"
    public Vector2 CenterPosition; // Center position of the holding area
    float width = 5f; // Width of the holding area
    float height = 2f; // Height of the holding area


    // // Helper function for your agents to quickly grab an open spot
    // public bool TryGetEmptySlot(out Vector2 slotPosition)
    // {
    //     foreach (var slot in Slots)
    //     {
    //         if (!slot.IsOccupied)
    //         {
    //             slotPosition = slot.Position;
    //             return true;
    //         }
    //     }

    //     // Fallback if full: return the center or first slot
    //     slotPosition = Slots.Count > 0 ? Slots[0].Position : Vector2.zero;
    //     return false;
    // }
    // Helper funtion to get a random position within 3 units of the center of the holding area
    public Vector2 GetRandomPositionInHoldingArea()
    {        // Assuming the holding area is a circle with a radius of 3 units
        // Vector2 randomDirection = Random.insideUnitCircle.normalized; // Get a random direction
        // float randomDistance = Random.Range(0f, Radius); // Get a random distance from the center
        // return CenterPosition + randomDirection * randomDistance; // Return the random position within the holding area
        // use rectangle of width 5 and height 2 
        float randomX = Random.Range(-width / 2, width / 2);
        float randomY = Random.Range(-height / 2, height / 2);
        return CenterPosition + new Vector2(randomX, randomY);

    }

}
