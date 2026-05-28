using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentMovement : MonoBehaviour
{
    [SerializeField]
    private SupermarketLayoutSO layoutData; // Reference to the ScriptableObject containing the layout data

    private NavMeshAgent agent;

    public enum AgentState { Evaluating, MovingToTarget, Buying, BuyingComplete, GoingToCheckout, WaitingInCheckoutLine, Wandering, Exiting }

    [Header("State Machine")]
    public AgentState currentState = AgentState.Evaluating;

    [Header("Agent Attributes")]
    [Range(0f, 1f)] public float impulseProbability = 0.4f;
    public List<string> shoppingList = new List<string>() { "Milk", "Chips" };
    public List<string> impulseFavorites = new List<string>() { "Chips", "Soda", "New Energy Drink" };

    [Header("Navigation & Timing")]
    public float buyDuration = 4.0f;
    public float wanderDuration = 6.0f;

    private ProductSlot currentTargetItem;
    private ProductSection currentTargetSection;
    private float stateTimer = 0f;
    private bool isPurchaseInProgress = false;
    private Coroutine buyingCoroutine;

    private int currentQueueIndex = -1; // Track the agent's position in the checkout queue

    [SerializeField]
    private List<Vector2> shoppingListItemsLocations = new(); // This will hold the positions of the items we need to buy, which should correspond to the section names in our layout data


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false; // Disable automatic rotation
        agent.updateUpAxis = false;   // Disable automatic up axis adjustment
        agent.avoidancePriority = Random.Range(10, 90); // Add a small random value to further reduce ties
        agent.speed += Random.Range(-0.5f, 0.5f);
        agent.stoppingDistance += Random.Range(-0.2f, 0.2f);
        agent.acceleration += Random.Range(-0.5f, 0.5f);

        // generate a random shopping list from ProductSections in our layout data for this agent
        shoppingList.Clear();
        foreach (var section in layoutData.ProductSections)
        {
            if (Random.value < 0.3f) // 30% chance to add each section to the shopping list
            {
                shoppingList.Add(section.SectionName);
            }
        }
        if (shoppingList.Count == 0) // Ensure at least one item is on the shopping list
        {
            shoppingList.Add(layoutData.ProductSections[Random.Range(0, layoutData.ProductSections.Count)].SectionName);

        }
        Debug.Log($"Agent '{gameObject.name}' starting with shopping list: {string.Join(", ", shoppingList)}");

        ChangeState(AgentState.Evaluating);
        EvaluateNextTarget();

    }

    public void SetAvoidancePriorityBasedOnQueuePosition(int queuePosition)
    {
        // Lower priority for those closer to the front of the line (lower index)
        int priority = Mathf.Clamp(queuePosition * 10, 10, 90); // Example: 0th in line = 10 priority, 1st in line = 20 priority, etc.
        agent.avoidancePriority = priority;
    }

    public void ChangeState(AgentState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }

    void Update()
    {
        // Execute logic based on active state
        switch (currentState)
        {
            case AgentState.MovingToTarget:
                HandleMovingState();
                break;
            case AgentState.Buying:
                HandleBuyingState();
                break;
            case AgentState.Wandering:
                HandleWanderingState();
                break;

            case AgentState.WaitingInCheckoutLine:
                // Waiting logic can be handled by the CheckoutCounter, so we might not need to do anything here for now
                break;

            case AgentState.GoingToCheckout:
                HandleGoingToCheckoutState();
                break;

            case AgentState.Evaluating:
                break;
            case AgentState.Exiting:
                HandleExitingState();
                break;
        }
    }

    private void HandleExitingState()
    {
        Vector2 closestExit = FindClosestDestination(layoutData.ExitLocations.ToArray());
        agent.SetDestination(closestExit);
    }

    public void EvaluateNextTarget()
    {
        Debug.Log("Evaluating next target..., Current shopping list: " + string.Join(", ", shoppingList));

        if (shoppingList.Count == 0)
        {
            Debug.Log("Shopping list complete! Heading to exit...");
            ChangeState(AgentState.GoingToCheckout);
            return;
        }

        // // Hackathon Feature: Roll for Impulse Buying diversion
        // if (Random.value < impulseProbability)
        // {
        //     GameObject launchItem = FindClosestLaunchOrFavorite();
        //     if (launchItem != null)
        //     {
        //         currentTargetItem = launchItem;
        //         agent.SetDestination(currentTargetItem.transform.position);
        //         ChangeState(AgentState.MovingToTarget);
        //         if(stateDebugText != null) stateDebugText.text = "🤑 Impulse Buy Distraction!";
        //         return;
        //     }
        // }

        // // Standard Logic: Find closest item from core list
        // build a list of potential targets based on the remaining items in the shopping list and the layout data
        List<Vector2> potentialTargets = new();
        foreach (string item in shoppingList)
        {
            ProductSection section = layoutData.ProductSections.Find(s => s.SectionName == item);
            if (section != null)
            {
                potentialTargets.Add(section.Slots[0].Position); // Again, using the first slot as a general target for the section
            }
        }
        // currentTargetSection = layoutData.sectionLookup[potentialTargets[0]]; // Set the current target section based on the first potential target (this is a simplification and could be improved to find the actual closest section)
        if (potentialTargets.Count > 0)
        {
            Vector2 closestLocation = FindClosestDestination(potentialTargets.ToArray());
            currentTargetSection = layoutData.sectionLookup[closestLocation].section; // Set the current target section based on the closest location
            ChangeState(AgentState.MovingToTarget);
            // agent.SetDestination(closestLocation);
            Debug.Log($"Heading to next item: {currentTargetSection.SectionName} at {closestLocation}");
            agent.SetDestination(closestLocation);

        }

    }

    void HandleGoingToCheckoutState()
    {
        CheckoutCounter CounterWithLeastAgents = null;
        CheckoutHandler HandlerWithLeastAgents = null;
        int leastAgentsInLine = int.MaxValue;
        // foreach (var counter in layoutData.CheckoutCounters)
        // {
        //     if (!counter.IsFull && counter.AgentCount < leastAgentsInLine)
        //     {
        //         CounterWithLeastAgents = counter;
        //         leastAgentsInLine = counter.AgentCount;
        //     }
        // }

        // if (CounterWithLeastAgents == null)
        // {
        //     Debug.LogWarning($"Agent '{gameObject.name}' found no available checkout counters. Wandering.");
        //     ChangeState(AgentState.Wandering);
        //     return;
        // }

        // CounterWithLeastAgents.TryJoinLine(this, out Vector2 assignedPosition, out int positionIndex);
        // currentQueueIndex = positionIndex;
        // agent.SetDestination(assignedPosition);
        // ChangeState(AgentState.WaitingInCheckoutLine);

        // Updated logic to check with CheckoutHandlers instead of directly with CheckoutCounters
        foreach (var handler in layoutData.CheckoutHandlers)
        {
            if (!handler.IsFull && handler.AgentCount < leastAgentsInLine)
            {
                CounterWithLeastAgents = handler.associatedCounter;
                HandlerWithLeastAgents = handler;
                leastAgentsInLine = handler.AgentCount;
            }

        }

        if (CounterWithLeastAgents == null)
        {
            Debug.LogWarning($"Agent '{gameObject.name}' found no available checkout counters. Wandering.");
            ChangeState(AgentState.Wandering);
            return;
        }

        HandlerWithLeastAgents.TryJoinLine(this, out Vector2 assignedPosition, out int positionIndex);
        currentQueueIndex = positionIndex;
        agent.SetDestination(assignedPosition);
        ChangeState(AgentState.WaitingInCheckoutLine);

    }


    /// <summary>
    /// Calculates the closest destination from an array of potential targets using NavMesh pathfinding
    /// </summary>
    public Vector2 FindClosestDestination(Vector2[] destinations)
    {
        Vector2[] closestDestinations = FindClosestDestinations(destinations, 1);
        return closestDestinations.Length > 0 ? closestDestinations[0] : Vector2.zero;

    }

    /// <summary>
    /// Calculates the top 3 closest destinations from an array of potential targets using NavMesh pathfinding.
    /// </summary>
    public Vector2[] FindTop3ClosestDestinations(Vector2[] destinations)
    {
        return FindClosestDestinations(destinations, 3);

    }

    private Vector2[] FindClosestDestinations(Vector2[] destinations, int maxResults)
    {
        if (destinations == null || destinations.Length == 0 || maxResults <= 0)
        {
            return System.Array.Empty<Vector2>();
        }

        List<(Vector2 destination, float pathLength)> rankedDestinations = new();
        NavMeshPath path = new();

        for (int i = 0; i < destinations.Length; i++)
        {
            Vector2 target = destinations[i];

            if (agent.CalculatePath(target, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                rankedDestinations.Add((target, GetPathLength(path)));
            }
        }

        if (rankedDestinations.Count == 0)
        {
            int fallbackCount = Mathf.Min(maxResults, destinations.Length);
            Vector2[] fallbackDestinations = new Vector2[fallbackCount];
            System.Array.Copy(destinations, fallbackDestinations, fallbackCount);
            return fallbackDestinations;
        }

        rankedDestinations.Sort((left, right) => left.pathLength.CompareTo(right.pathLength));

        int resultCount = Mathf.Min(maxResults, rankedDestinations.Count);
        Vector2[] closestDestinations = new Vector2[resultCount];

        for (int i = 0; i < resultCount; i++)
        {
            closestDestinations[i] = rankedDestinations[i].destination;
        }

        return closestDestinations;

    }

    /// <summary>
    /// The bulletproof check for NavMeshAgent arrival.
    /// </summary>
    private bool HasReachedDestination()
    {
        // Check if the agent is still calculating the path
        if (agent.pathPending) return false;

        // Check if the agent has reached its stopping threshold
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // Confirm the agent has no path left, or has completely stopped moving
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                return true;
            }
        }

        return false;
    }

    private float GetPathLength(NavMeshPath path)
    {
        if (path.corners.Length < 2) return 0f;

        float totalLength = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            totalLength += Vector2.Distance(path.corners[i], path.corners[i + 1]);
        }
        return totalLength;
    }

    void HandleMovingState()
    {
        agent.avoidancePriority = Random.Range(10, 90); // Add a small random value to reduce ties in avoidance with other agents
        // Debug.Log($"Current Target Section: {currentTargetSection.SectionName}, Remaining Distance: {agent.remainingDistance}");
        if (currentTargetSection == null) { ChangeState(AgentState.Evaluating); return; }

        // Check if close to the target item shelf
        if (agent.remainingDistance <= 5.0f && currentTargetItem == null)
        {
            // Check if the target section is occupied
            if (currentTargetSection.TryGetEmptySlot(out Vector2 slotPosition))
            {
                // Move to the specific slot position in the section
                Debug.Log($"Moving to section {currentTargetSection.SectionName} at slot position {slotPosition}");
                layoutData.sectionLookup[slotPosition].slot.IsOccupied = true; // Mark the slot as occupied
                currentTargetItem = layoutData.sectionLookup[slotPosition].slot;
                agent.SetDestination(slotPosition);
                ChangeState(AgentState.Buying);

            }
            else
            {
                // If the section is full, switch to wandering state
                ChangeState(AgentState.Wandering);

            }
        }
    }

    void HandleWanderingState()
    {
        agent.avoidancePriority = 1;
        // In this simple implementation, we'll just wait for a short duration and then re-evaluate our targets
        stateTimer += Time.deltaTime;
        if (stateTimer >= wanderDuration)
        {

            if (shoppingList.Count == 0)
            {
                ChangeState(AgentState.GoingToCheckout);
                return;
            }
            // // Standard Logic: Find closest item from core list
            // build a list of potential targets based on the remaining items in the shopping list and the layout data
            List<Vector2> potentialTargets = new();
            foreach (string item in shoppingList)
            {
                ProductSection section = layoutData.ProductSections.Find(s => s.SectionName == item);
                if (section != null)
                {
                    potentialTargets.Add(section.Slots[0].Position); // Again, using the first slot as a general target for the section
                }
            }
            // currentTargetSection = layoutData.sectionLookup[potentialTargets[0]]; // Set the current target section based on the first potential target (this is a simplification and could be improved to find the actual closest section)
            if (potentialTargets.Count > 0)
            {
                Vector2 closestLocation = FindTop3ClosestDestinations(potentialTargets.ToArray())[1]; // Get the 2nd closest of the top 3 destinations to add some variation
                currentTargetSection = layoutData.sectionLookup[closestLocation].section; // Set the current target section based on the closest location
                ChangeState(AgentState.MovingToTarget);
                // agent.SetDestination(closestLocation);
                Debug.Log($"Heading to next item: {currentTargetSection.SectionName} at {closestLocation}");
                agent.SetDestination(closestLocation);

            }
        }
    }

    void HandleBuyingState()
    {
        if (isPurchaseInProgress)
        {
            return;
        }

        if (HasReachedDestination() && currentTargetSection != null && currentTargetItem != null)
        {
            Debug.Log($"Arrived at target item in section and starting purchase process.");
            buyingCoroutine = StartCoroutine(CompletePurchaseAfterDelay(currentTargetSection, currentTargetItem));
        }
    }

    private IEnumerator CompletePurchaseAfterDelay(ProductSection targetSection, ProductSlot targetItem)
    {
        isPurchaseInProgress = true;

        yield return new WaitForSeconds(buyDuration);

        if (targetSection != null && targetItem != null)
        {
            Debug.Log($"Finished buying item from section {targetSection.SectionName}");
            Debug.Log($"Marking slot at position {targetItem.Position} as unoccupied.");
            targetItem.IsOccupied = false;
            shoppingList.Remove(targetSection.SectionName);

            if (currentTargetItem == targetItem)
            {
                currentTargetItem = null;
            }

            if (currentTargetSection == targetSection)
            {
                currentTargetSection = null;
            }
        }

        isPurchaseInProgress = false;
        buyingCoroutine = null;
        EvaluateNextTarget();
    }

    private void OnDisable()
    {
        if (buyingCoroutine != null)
        {
            StopCoroutine(buyingCoroutine);
            buyingCoroutine = null;
        }

        isPurchaseInProgress = false;

        // 
    }

    public void MoveToLocation(Vector2 location)
    {
        agent.SetDestination(location);
    }

}
