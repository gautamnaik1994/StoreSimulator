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
    // public float CheckoutSpeedSeconds = 3.0f;

    // Queue holding the agents currently waiting
    private Queue<AgentMovement> waitingAgents = new Queue<AgentMovement>();
    // private bool isProcessingCheckout = false;

    public int AgentCount => waitingAgents.Count;
    public bool IsFull => waitingAgents.Count >= QueueSlots.Count;

    public string CounterName; // Optional: for debugging or display purposes

    /// <summary>
    /// Attempts to join the back of this checkout line.
    /// </summary>
    public bool TryJoinLine(AgentMovement agent, out Vector2 assignedPosition, out int positionIndex)
    {
        if (IsFull)
        {
            assignedPosition = Vector2.zero;
            positionIndex = -1;
            return false;
        }

        waitingAgents.Enqueue(agent);
        // Position index is based on their current place in the queue
        assignedPosition = QueueSlots[waitingAgents.Count - 1].Position;
        positionIndex = waitingAgents.Count - 1;

        // Start processing if this is the only agent and we aren't active
        // if (!isProcessingCheckout)
        // {
        //     // StartCoroutine(ProcessCheckoutQueue());
        // }

        return true;
    }

    // private IEnumerator ProcessCheckoutQueue()
    // {
    //     isProcessingCheckout = true;

    //     while (waitingAgents.Count > 0)
    //     {
    //         AgentMovement_v2 frontAgent = waitingAgents.Peek();

    //         // Wait until the agent actually arrives at the front of the line (index 0)
    //         // You might want to pass a flag or check if they are close enough to QueuePositions[0]
    //         yield return new WaitUntil(() => HasAgentArrivedAtFront(frontAgent));

    //         // Simulate scanning items
    //         yield return new WaitForSeconds(CheckoutSpeedSeconds);

    //         // Pop the agent out of the queue and release them to exit
    //         waitingAgents.Dequeue();
    //         // frontAgent.CompleteCheckoutAndExit(); // Custom method we will add to Agent

    //         // Shift everybody forward in line physically
    //         UpdateLinePositions();
    //     }

    //     isProcessingCheckout = false;
    // }

    // private bool HasAgentArrivedAtFront(AgentMovement_v2 agent)
    // {
    //     // Simple distance check to the front spot
    //     return Vector2.Distance(agent.transform.position, QueuePositions[0]) < 0.5f;
    // }

    // private void UpdateLinePositions()
    // {
    //     int index = 0;
    //     foreach (var agent in waitingAgents)
    //     {
    //         Vector2 nextSpot = QueuePositions[index];
    //         // agent.MoveToQueueSpot(nextSpot); // Signal agent to advance forward
    //         index++;
    //     }
    // }
}