using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CheckoutSlot
{
    public Vector2 Position;
    public bool IsOccupied; // Tracks if an agent is currently standing here/heading here
}


[System.Serializable]
public class CheckoutCounter
{
    public List<CheckoutSlot> QueueSlots = new List<CheckoutSlot>(); // Define these in the Inspector (0 = front of line)
    public string CounterName; // Optional: for debugging or display purposes

    public Vector2 position;

    // public bool IsFull => waitingAgents.Count >= QueueSlots.Count;
}