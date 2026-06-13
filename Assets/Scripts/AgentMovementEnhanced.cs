using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;




[RequireComponent(typeof(NavMeshAgent))]
public class AgentMovementEnhanced : MonoBehaviour
{
    [SerializeField]
    private SupermarketLayoutSO layoutData;
    private NavMeshAgent agent;
    private List<string> shoppingList = new List<string>() { "Milk", "Chips" };
    private Dictionary<AgentState, Color> AgentStateColors;
    private Renderer agentRenderer;
    private float stateTimer = 0f;
    private float shoppingElapsedTime = 0f;
    private int checkoutCurrentQueueIndex = -1; // Track the agent's position in the checkout queue
    [SerializeField] private float queueCheckInterval = 1.5f; // Check for open cash registers every 1.5 seconds
    private float nextQueueCheckTime = 0f;

    [Header("AI Performance")]
    [SerializeField] private float evaluationInterval = 0.2f; // Think 5 times a second, not 60+
    private float nextEvaluationTime = 0f;
    private bool forceReevaluation = false; // Flag to bypass cooldown if needed
    private ProductSection currentTargetSection;

    private ProductSlot currentTargetItem;


    public enum AgentState { Evaluating, NavigatingToShelf, BrowsingShelf, Wandering, WaitingToQueue, GoingToCheckout, WaitingInLine, Leaving }
    public AgentState currentState = AgentState.Evaluating;
    private float impulseProbability = 0.3f; // 30% chance to make an impulse detour
    private readonly List<string> impulseFavorites = new() { "Chocolate", "Soda", "Candy" };

    // create a agent history to track their shopping behavior and decisions and state transitions
    private List<AgentHistoryEntry> agentHistory = new List<AgentHistoryEntry>();

    // Tracks the current ranked destinations chosen by the brain for this execution cycle
    private Queue<Vector2> rankedDestinationsQueue = new Queue<Vector2>();

    // Tracks sections that were full, allowing us to penalize them temporarily during evaluation
    private Dictionary<string, float> sectionCooldowns = new Dictionary<string, float>();
    [SerializeField] private float fullShelfCooldownDuration = 15.0f;

    private struct AgentHistoryEntry
    {
        public float timestamp;
        public AgentState state;
        public string actionDescription;

        public AgentHistoryEntry(float time, AgentState agentState, string description)
        {
            timestamp = time;
            state = agentState;
            actionDescription = description;
        }
    }

    void Awake()
    {
        AgentStateColors = new Dictionary<AgentState, Color>()
        {
            { AgentState.Evaluating, ParseColor("#38BDF8") },
            { AgentState.NavigatingToShelf, ParseColor("#34D399") },
            { AgentState.BrowsingShelf, ParseColor("#4ADE80") },
            { AgentState.Wandering,  ParseColor("#FBBF24")},
            { AgentState.WaitingToQueue, ParseColor("#A855F7") },
            { AgentState.GoingToCheckout, ParseColor("#A855F7") },
            { AgentState.WaitingInLine,ParseColor("#F43F5E") },
            { AgentState.Leaving, ParseColor("#94A3B8") }
        };
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // Disable automatic rotation
        agent.updateUpAxis = false;   // Disable automatic up axis adjustment
        agent.avoidancePriority = Random.Range(10, 90); // Add a small random value to further reduce ties
        agent.speed += Random.Range(-0.5f, 0.5f);
        // agent.stoppingDistance += Random.Range(-0.2f, 0.2f);
        agent.acceleration += Random.Range(-0.5f, 0.5f);

        agentRenderer = GetComponent<Renderer>();


        shoppingList.Clear();
        foreach (var section in layoutData.ProductSections)
        {
            if (Random.value < 0.2f) // 30% chance to add each section to the shopping list
            {
                shoppingList.Add(section.SectionName);
            }
        }
        if (shoppingList.Count == 0) // Ensure at least one item is on the shopping list
        {
            shoppingList.Add(layoutData.ProductSections[Random.Range(0, layoutData.ProductSections.Count)].SectionName);

        }
        ChangeState(AgentState.Evaluating);
        // write to the agent history that they have been initialized with a shopping list
        agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Initialized with shopping list: {string.Join(", ", shoppingList)}"));
    }



    // Update is called once per frame
    void Update()
    {
        shoppingElapsedTime += Time.deltaTime;
        stateTimer += Time.deltaTime; // Incremented exactly once per frame now

        switch (currentState)
        {
            case AgentState.Evaluating:
                if (Time.time >= nextEvaluationTime || forceReevaluation)
                {
                    nextEvaluationTime = Time.time + evaluationInterval;
                    forceReevaluation = false;

                    // Run the heavy selection, impulse checks, and path estimations here
                    CentralizeDecisionMaking();
                }
                break;
            case AgentState.BrowsingShelf:
                HandleBrowsingShelf();
                break;

            case AgentState.WaitingToQueue:
                HandleWaitingToQueue();
                break;

            case AgentState.GoingToCheckout:
                // Handle arrival at the actual assigned queue slot here if needed
                break;
            case AgentState.WaitingInLine:
                // Handle waiting in line logic here if needed
                break;
            case AgentState.NavigatingToShelf:
                HandleNavigatingToShelf();
                break;
            case AgentState.Leaving:
                HandleLeavingState();
                break;
            default:
                break;
        }
    }

    public void ForceBrainUpdate()
    {
        forceReevaluation = true;
    }


    private void CentralizeDecisionMaking()
    {
        // 1. Structural Check: Is the shopper done?
        if (shoppingList.Count == 0)
        {
            ChangeState(AgentState.WaitingToQueue);
            Vector2 holdingArea = GetClosestHoldingArea();
            agent.SetDestination(holdingArea);
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, "Shopping list completed. Moving to holding area."));

            // Reset the timer so they check lines immediately upon changing state
            nextQueueCheckTime = Time.time;
            return;
        }


        List<Vector2> potentialTargets = new List<Vector2>();
        foreach (string item in shoppingList)
        {
            // Skip shelves that we verified were completely full very recently
            if (sectionCooldowns.TryGetValue(item, out float cooldownExpiry) && Time.time < cooldownExpiry)
            {
                continue;
            }

            ProductSection section = layoutData.ProductSections.Find(s => s.SectionName == item);
            if (section != null)
            {
                potentialTargets.Add(section.Slots[0].Position);
            }
        }

        // If ALL remaining options are on cooldown, clear cooldowns to prevent freezing
        if (potentialTargets.Count == 0 && shoppingList.Count > 0)
        {
            sectionCooldowns.Clear();
            foreach (string item in shoppingList)
            {
                ProductSection section = layoutData.ProductSections.Find(s => s.SectionName == item);
                if (section != null) potentialTargets.Add(section.Slots[0].Position);
            }
        }

        // 3. Populate the destination queue in order of proximity
        if (potentialTargets.Count > 0)
        {
            // Fetch up to 3 closest valid locations in sorted order
            Vector2[] sortedTargets = FindClosestDestinations(potentialTargets.ToArray(), 3);

            rankedDestinationsQueue.Clear();
            foreach (Vector2 target in sortedTargets)
            {
                rankedDestinationsQueue.Enqueue(target);
            }

            // Set off toward the first (closest) option
            NavigateToNextCachedTarget();
        }
    }

    private void NavigateToNextCachedTarget()
    {
        if (rankedDestinationsQueue.Count > 0)
        {
            Vector2 nextLocation = rankedDestinationsQueue.Dequeue();
            currentTargetSection = layoutData.sectionLookup[nextLocation].section;

            ChangeState(AgentState.NavigatingToShelf);
            agent.SetDestination(nextLocation);
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Navigating to shelf: {currentTargetSection.SectionName} at {nextLocation}"));
        }
        else
        {
            // No more cached routes available, force brain to re-evaluate whole picture
            ChangeState(AgentState.Evaluating);
        }
    }

    void HandleBrowsingShelf()
    {
        if (HasReachedDestination(arrivalThreshold: 0.5f))
        {
            // Simulate browsing time
            if (stateTimer >= Random.Range(2f, 5f))
            {
                // Remove the item from the shopping list and mark the slot as occupied
                shoppingList.Remove(currentTargetSection.SectionName);
                agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Finished browsing {currentTargetSection.SectionName}. Remaining list: {string.Join(", ", shoppingList)}"));
                currentTargetItem.IsOccupied = false; // Mark the slot as unoccupied for other agents
                currentTargetSection = null;
                currentTargetItem = null;

                ChangeState(AgentState.Evaluating);

            }
        }
    }

    void HandleWaitingToQueue()
    {
        // Performance Guard 1: Don't check lanes while physically walking to the holding zone
        if (!HasReachedDestination())
        {
            return;
        }

        // Performance Guard 2: Throttles line checking so it doesn't execute every frame
        if (Time.time < nextQueueCheckTime)
        {
            return;
        }

        // Set the timestamp for the next allowed check
        nextQueueCheckTime = Time.time + queueCheckInterval;

        CheckoutHandler handlerWithLeastAgents = GetCheckoutHandlerWithLeastAgents();

        // If lines are full, the agent safely stands still and tries again when the interval passes
        if (handlerWithLeastAgents == null)
        {
            // Optional: Every few seconds, pick a small micro-adjustment spot 
            // in the holding area to make them look like they are shuffling impatiently
            if (Random.value < 0.2f)
            {
                agent.SetDestination(GetClosestHoldingArea() + Random.insideUnitCircle * 2f);
            }
            return;
        }

        // A spot is open! Try to claim it
        if (handlerWithLeastAgents.TryJoinLine(this, out Vector2 assignedPosition, out int positionIndex))
        {
            checkoutCurrentQueueIndex = positionIndex;
            SetAvoidancePriorityBasedOnQueuePosition(checkoutCurrentQueueIndex);
            agent.SetDestination(assignedPosition);
            ChangeState(AgentState.GoingToCheckout);
        }
    }

    void HandleNavigatingToShelf()
    {
        if (!HasReachedDestination(arrivalThreshold: 5.0f))
        {
            return; // Still navigating, no further action needed
        }

        if (currentTargetSection != null && currentTargetItem == null)
        {
            if (currentTargetSection.TryGetEmptySlot(out Vector2 slotPosition))
            {
                layoutData.sectionLookup[slotPosition].slot.IsOccupied = true;
                currentTargetItem = layoutData.sectionLookup[slotPosition].slot;
                agent.SetDestination(slotPosition);

                ChangeState(AgentState.BrowsingShelf);
                agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Navigating to specific slot at {slotPosition} in section {currentTargetSection.SectionName}"));
            }
            else
            {
                // REALISM: Shelf is completely full! 
                Debug.Log($"Shelf {currentTargetSection.SectionName} is full. Diverting to next option.");
                agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Shelf full at {currentTargetSection.SectionName}. Shifting to runner-up target."));

                // 1. Remember that this section is full so the brain ignores it for a bit
                sectionCooldowns[currentTargetSection.SectionName] = Time.time + fullShelfCooldownDuration;

                // 2. Clear current broken targets
                currentTargetSection = null;
                currentTargetItem = null;

                // 3. Seamlessly pivot to the next closest location in our pre-calculated queue
                NavigateToNextCachedTarget();
            }
        }
    }

    private void HandleLeavingState()
    {
        if (!HasReachedDestination())
        {
            return; // Keep heading to exit until we arrive
        }

        Vector2 closestExit = FindClosestDestination(layoutData.ExitLocations.ToArray());
        agent.SetDestination(closestExit);
    }

    CheckoutHandler GetCheckoutHandlerWithLeastAgents()
    {
        CheckoutCounter CounterWithLeastAgents = null;
        CheckoutHandler HandlerWithLeastAgents = null;
        int leastAgentsInLine = int.MaxValue;

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
            return null; // No available checkout counters
        }
        return HandlerWithLeastAgents;
    }

    Vector2 GetClosestHoldingArea()
    {
        // Vector2 closestHolding = FindClosestDestination(layoutData.HoldingAreas.ConvertAll(holding => holding.CenterPosition).ToArray());
        Holding targetHolding = layoutData.HoldingAreas[0];
        return targetHolding.GetRandomPositionInHoldingArea();
    }


    public Vector2 FindClosestDestination(Vector2[] destinations)
    {
        Vector2[] closestDestinations = FindClosestDestinations(destinations, 1);
        return closestDestinations.Length > 0 ? closestDestinations[0] : Vector2.zero;

    }

    void SetAvoidancePriorityBasedOnQueuePosition(int queuePosition)
    {
        // Lower priority for those closer to the front of the line (lower index)
        int priority = Mathf.Clamp(queuePosition, 1, 90); // Example: 0th in line = 10 priority, 1st in line = 20 priority, etc.
        agent.avoidancePriority = priority;
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
    public void ChangeState(AgentState newState)
    {
        currentState = newState;
        stateTimer = 0f;
        if (agentRenderer != null)
        {
            agentRenderer.material.color = AgentStateColors.ContainsKey(currentState) ? AgentStateColors[currentState] : Color.white;
        }
    }


    private Color ParseColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        return Color.white; // Fallback
    }

    public void MoveToLocation(Vector2 location)
    {
        agent.SetDestination(location);
    }

    /// <summary>
    /// The bulletproof check for NavMeshAgent arrival.
    /// </summary>
    private bool HasReachedDestination(float arrivalThreshold = 0.1f)
    {
        // Check if the agent is still calculating the path
        if (agent.pathPending) return false;

        // Check if the agent has reached its stopping threshold
        if (agent.remainingDistance <= agent.stoppingDistance + arrivalThreshold)
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

    private float EstimatePathLength(Vector2 target)
    {
        NavMeshPath path = new();
        if (agent.CalculatePath(target, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            return GetPathLength(path);
        }

        return Vector2.Distance(transform.position, target) * 1.5f;
    }


}


