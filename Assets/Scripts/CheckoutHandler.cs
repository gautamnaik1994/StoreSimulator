using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class CheckoutHandler : MonoBehaviour
{
    [SerializeField] private SupermarketLayoutSO layoutAsset;

    public CheckoutCounter associatedCounter;

    // Queue holding the agents currently waiting
    private Queue<AgentMovementEnhanced> waitingAgents = new Queue<AgentMovementEnhanced>();
    // private bool isProcessingCheckout = false;

    public float CheckoutSpeedSeconds = 3.0f;
    public int AgentCount => waitingAgents.Count;
    public float EstimatedWaitTimeSeconds => waitingAgents.Count * CheckoutSpeedSeconds;

    // public bool IsFull => waitingAgents.Count >= QueueSlots.Count;
    public bool IsFull => associatedCounter != null && waitingAgents.Count >= associatedCounter.QueueSlots.Count;
    [SerializeField]
    private bool isProcessingCheckout = false;

    private void OnEnable()
    {
        if (layoutAsset != null && !layoutAsset.CheckoutHandlers.Contains(this))
        {
            layoutAsset.CheckoutHandlers.Add(this);
        }
        Debug.Log($"Registered checkout handler for '{gameObject.name}' with layout asset. Total handlers: {layoutAsset.CheckoutHandlers.Count}");

    }

    private void OnDisable()
    {
        if (layoutAsset != null)
        {
            layoutAsset.CheckoutHandlers.Remove(this);
        }
        Debug.Log($"Unregistered checkout handler for '{gameObject.name}' with layout asset. Total handlers: {layoutAsset.CheckoutHandlers.Count}");
    }

    void Start()
    {
        // Find the associated checkout counter in the layout asset based on the name of this GameObject
        associatedCounter = layoutAsset.CheckoutCounters.Find(counter => counter.CounterName == gameObject.name);
        if (associatedCounter == null)
        {
            Debug.LogError($"No matching checkout counter found in layout asset for '{gameObject.name}'. Please ensure the names match.");
        }
    }
    /// <summary>
    /// Attempts to join the back of this checkout line.
    /// </summary>
    public bool TryJoinLine(AgentMovementEnhanced agent, out Vector2 assignedPosition, out int positionIndex)
    {
        if (IsFull)
        {
            assignedPosition = Vector2.zero;
            positionIndex = -1;
            return false;
        }

        waitingAgents.Enqueue(agent);
        // Position index is based on their current place in the queue
        assignedPosition = associatedCounter.QueueSlots[waitingAgents.Count - 1].Position;
        positionIndex = waitingAgents.Count - 1;
        // agent.SetAvoidancePriorityBasedOnQueuePosition(positionIndex); // Higher priority for those closer to the front of the line (lower index)
        // assign highest agent avoidance priority to the front of the line, so we want to assign the new agent a priority based on how many are already in line

        // Start processing if this is the only agent and we aren't active
        if (!isProcessingCheckout)
        {
            StartCoroutine(ProcessCheckoutQueue());
            associatedCounter.QueueSlots[0].IsOccupied = true; // Mark the front spot as occupied when the first agent joins
            StartCoroutine(SlowUpdate()); // Start the slow update coroutine to manage line positions
        }

        return true;
    }

    private IEnumerator ProcessCheckoutQueue()
    {
        Debug.Log($"Starting checkout processing for '{gameObject.name}' with {waitingAgents.Count} agents in line.");
        isProcessingCheckout = true;

        while (waitingAgents.Count > 0)
        {

            AgentMovementEnhanced frontAgent = waitingAgents.Peek();
            Debug.Log($"Processing agent '{frontAgent.gameObject.name}' at the front of the line for '{gameObject.name}'.");

            // Wait until the agent actually arrives at the front of the line (index 0)
            // You might want to pass a flag or check if they are close enough to QueuePositions[0]
            frontAgent.MoveToLocation(associatedCounter.QueueSlots[0].Position); // Ensure they are moving to the front spot
            yield return new WaitUntil(() => HasAgentArrivedAtFront(frontAgent));

            // Simulate scanning items
            yield return new WaitForSeconds(CheckoutSpeedSeconds);

            // Pop the agent out of the queue and release them to exit
            waitingAgents.Dequeue();
            associatedCounter.QueueSlots[0].IsOccupied = false; // Mark the front spot as unoccupied for the next agent
            Debug.Log($"Processing checkout for agent: {frontAgent.gameObject.name}");
            // frontAgent.CompleteCheckoutAndExit(); // Custom method we will add to Agent
            frontAgent.ChangeState(AgentMovementEnhanced.AgentState.Leaving); // Change state to exiting, which should trigger their exit behavior

            // Shift everybody forward in line physically
            UpdateLinePositions();
        }

        isProcessingCheckout = false;
    }

    private bool HasAgentArrivedAtFront(AgentMovementEnhanced agent)
    {
        // Simple distance check to the front spot
        return Vector2.Distance(agent.transform.position, associatedCounter.QueueSlots[0].Position) < 0.5f;
    }

    private void UpdateLinePositions()
    {
        int index = 0;
        foreach (var agent in waitingAgents)
        {
            Vector2 nextSpot = associatedCounter.QueueSlots[index].Position;
            // agent.MoveToQueueSpot(nextSpot); // Signal agent to advance forward
            agent.MoveToLocation(nextSpot); // Signal agent to advance forward
            index++;
        }
    }

    private IEnumerator SlowUpdate()
    {
        if (isProcessingCheckout && waitingAgents.Count > 0 && associatedCounter.QueueSlots[0].IsOccupied == false)
        {
            // Perform any periodic checks or updates related to checkout processing here
            Debug.Log($"Slow update check for '{gameObject.name}' with {waitingAgents.Count} agents in line.");
            UpdateLinePositions(); // Ensure agents are in the correct positions, especially if something went wrong


        }

        yield return new WaitForSeconds(1f);
    }
}
