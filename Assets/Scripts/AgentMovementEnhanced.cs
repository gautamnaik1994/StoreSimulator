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
    private SpriteRenderer agentRenderer;
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
    [Header("Detour Settings")]
    [Range(0f, 1f)] public float randomBrowseProbability = 0.15f; // 15% chance to aimlessly wander instead of shopping

    public enum AgentState { Evaluating, NavigatingToShelf, BrowsingShelf, Wandering, WaitingToQueue, GoingToCheckout, CheckingOut, Leaving }
    public AgentState currentState = AgentState.Evaluating;
    private float impulseProbability = 0.3f; // 30% chance to make an impulse detour
    private List<string> impulseFavorites;

    // create a agent history to track their shopping behavior and decisions and state transitions
    private List<AgentHistoryEntry> agentHistory = new List<AgentHistoryEntry>();

    // Tracks the current ranked destinations chosen by the brain for this execution cycle
    private Queue<Vector2> rankedDestinationsQueue = new Queue<Vector2>();

    // Tracks sections that were full, allowing us to penalize them temporarily during evaluation
    private Dictionary<string, float> sectionCooldowns = new Dictionary<string, float>();
    [SerializeField] private float fullShelfCooldownDuration = 15.0f;
    public GameObject agentStatusRing; // Optional: A SpriteRenderer to visually indicate that agent is thinking (evaluating) 
    public int wanderDuration = 10; // Time in seconds the agent will spend wandering before re-evaluating their shopping list

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
    public int TotalMoney = 10000;
    private readonly List<ProductSection> cartItems = new List<ProductSection>();
    private int TotalMoneySpent = 0;

    private List<ProductSection> CostlyItems;

    void Awake()
    {
        AgentStateColors = new Dictionary<AgentState, Color>()
        {
            { AgentState.Evaluating, ParseColor("#38BDF8") },
            { AgentState.NavigatingToShelf, ParseColor("#34D399") },
            { AgentState.BrowsingShelf, ParseColor("#4ADE80") },
            { AgentState.Wandering,  ParseColor("#fb5bfb")},
            { AgentState.WaitingToQueue, ParseColor("#A855F7") },
            { AgentState.GoingToCheckout, ParseColor("#FBBF24") },
            { AgentState.CheckingOut,ParseColor("#F43F5E") },
            { AgentState.Leaving, ParseColor("#94A3B8") }
        };
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // Disable automatic rotation
        agent.updateUpAxis = false;   // Disable automatic up axis adjustment
        agent.avoidancePriority = Random.Range(10, 90); // Add a small random value to further reduce ties
        agent.speed += Random.Range(-0.5f, 0.5f);
        // agent.stoppingDistance += Random.Range(-0.2f, 0.2f);
        agent.acceleration += Random.Range(-0.5f, 0.5f);
        // agentStatusRing.SetActive(false);

        agentRenderer = GetComponent<SpriteRenderer>();


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

        // build a list of impulse favorites from the layout data but make sure it doesn't overlap with the shopping list
        impulseFavorites = new List<string>();
        foreach (var section in layoutData.ProductSections)
        {
            if (!shoppingList.Contains(section.SectionName))
            {
                if (Random.value < 0.5f)
                {
                    impulseFavorites.Add(section.SectionName);
                }
            }
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

            case AgentState.Wandering:
                HandleWanderingState();
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
            case AgentState.CheckingOut:
                // Handle checkout completion and transition to leaving state here if needed
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

            nextQueueCheckTime = Time.time;
            agentStatusRing.SetActive(false);
            return;
        }

        // --- DETOUR DETERMINATION PHASE ---
        float decisionRoll = Random.value;

        // CASE A: Random Detour / Wandering (Simulating aimless window shopping)
        if (decisionRoll < randomBrowseProbability)
        {
            rankedDestinationsQueue.Clear(); // Drop the queue plans
            currentTargetSection = null;
            currentTargetItem = null;

            // Pick a completely random zone position from any section in the layout
            int randomSectionIdx = Random.Range(0, layoutData.ProductSections.Count);
            var randomSection = layoutData.ProductSections[randomSectionIdx];
            Vector2 randomWanderPoint = randomSection.Slots[Random.Range(0, randomSection.Slots.Count)].Position;

            agent.SetDestination(randomWanderPoint);
            ChangeState(AgentState.Wandering); // Walks over and idles/browses aimlessly 
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Taking a random detour to wander near section: {randomSection.SectionName}"));
            return;
        }

        // CASE B: Core Shopping List & Impulse Calculations
        List<Vector2> potentialTargets = new List<Vector2>();

        // Step 1: Add regular shopping list items (Your existing logic)
        foreach (string item in shoppingList)
        {
            if (sectionCooldowns.TryGetValue(item, out float cooldownExpiry) && Time.time < cooldownExpiry)
            {
                continue;
            }

            ProductSection section = layoutData.ProductSections.Find(s => s.SectionName == item);
            if (section != null)
            {
                int randomSlotIndex = Random.Range(0, section.Slots.Count);
                potentialTargets.Add(section.Slots[randomSlotIndex].Position);
            }
        }

        // Reset cooldowns if standard targets are completely starved
        if (potentialTargets.Count == 0 && shoppingList.Count > 0)
        {
            sectionCooldowns.Clear();
            foreach (string item in shoppingList)
            {
                ProductSection section = layoutData.ProductSections.Find(s => s.SectionName == item);
                if (section != null) potentialTargets.Add(section.Slots[0].Position);
            }
        }

        // Step 2: Query nearest options out of our valid targets
        Vector2[] sortedTargets = FindClosestDestinations(potentialTargets.ToArray(), 3);

        rankedDestinationsQueue.Clear();
        foreach (Vector2 target in sortedTargets)
        {
            rankedDestinationsQueue.Enqueue(target);
        }

        // CASE C: Impulse Buying (Intercepting the closest target choice)
        // If we pass our probability roll and have favorite items left to exploit
        if (decisionRoll >= randomBrowseProbability && decisionRoll < (randomBrowseProbability + impulseProbability))
        {
            // Try to pick an impulse item that isn't already on the standard shopping list
            // List<string> validImpulseChoices = impulseFavorites.FindAll(fav => !shoppingList.Contains(fav));
            List<string> validImpulseChoices = impulseFavorites;

            if (validImpulseChoices.Count > 0)
            {
                string chosenImpulseItem = validImpulseChoices[Random.Range(0, validImpulseChoices.Count)];
                ProductSection impulseSection = layoutData.ProductSections.Find(s => s.SectionName == chosenImpulseItem);

                if (impulseSection != null)
                {
                    Vector2 impulseSlotPos = impulseSection.Slots[Random.Range(0, impulseSection.Slots.Count)].Position;

                    // CRITICAL DESIGN MOVE: Re-create the queue to put impulse at the absolute FRONT
                    Queue<Vector2> interceptQueue = new Queue<Vector2>();
                    interceptQueue.Enqueue(impulseSlotPos); // Impulse is Destination #1!

                    // Append the remaining regular targets back behind it
                    while (rankedDestinationsQueue.Count > 0)
                    {
                        interceptQueue.Enqueue(rankedDestinationsQueue.Dequeue());
                    }

                    rankedDestinationsQueue = interceptQueue;
                    agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"[Impulse Alert] Spotted {chosenImpulseItem}! Added to front of itinerary layout."));
                }
            }
        }

        // 3. Set off toward the front item in our finalized queue layout
        if (rankedDestinationsQueue.Count > 0)
        {
            agentStatusRing.SetActive(false);
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
            agentStatusRing.SetActive(true); // Optional: Turn on the status ring to indicate a thinking state
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, "No more cached targets. Forcing re-evaluation of options.")); // Log that we're out of cached options and need
        }
    }

    void HandleBrowsingShelf()
    {
        if (HasReachedDestination(arrivalThreshold: 0.5f))
        {
            // Simulate browsing time
            if (stateTimer >= Random.Range(2f, 5f))
            {
                bool itemPurchased = false;
                string purchasedItemName = currentTargetSection.SectionName;
                bool wasPlanned = shoppingList.Contains(purchasedItemName);
                bool wasImpulse = impulseFavorites.Contains(purchasedItemName);

                // compare price to check if the agent can afford it
                if (currentTargetSection.Price <= TotalMoney)
                {
                    itemPurchased = true;
                }

                // 1. Clear it from the lists it belongs to
                if (wasPlanned)
                {
                    shoppingList.Remove(purchasedItemName);
                }

                if (wasImpulse)
                {
                    // Removing it from favorites ensures they don't repeatedly impulse-buy 
                    // the exact same item over and over during a single shopping trip.
                    impulseFavorites.Remove(purchasedItemName);
                }

                if (itemPurchased)
                {

                    // 2. Log exact historical behavior for debugging data
                    string logMessage = $"Finished browsing {purchasedItemName}. ";
                    if (wasPlanned && wasImpulse) logMessage += "(Cleared from both Shopping and Impulse lists)";
                    else if (wasPlanned) logMessage += "(Planned item complete)";
                    else if (wasImpulse) logMessage += "(Spontaneous impulse buy complete)";

                    agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, logMessage));
                    // update the cart with the newly purchased item
                    UpdateCart(currentTargetSection);
                }
                else
                {
                    agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Browsed {purchasedItemName} but couldn't afford it. Needed {currentTargetSection.Price}, had {TotalMoney}."));
                }


                // 3. Clean up slot references and state transition
                if (currentTargetItem != null)
                {
                    currentTargetItem.IsOccupied = false; // Free up the physical layout space for other agents
                }

                currentTargetSection = null;
                currentTargetItem = null;

                // Shift right back to the central decision hub
                ChangeState(AgentState.Evaluating);
                agentStatusRing.SetActive(true);
            }
        }
    }

    void HandleWanderingState()
    {
        // If they finish walking to their random wander slot OR get tired of looking around
        if (HasReachedDestination(arrivalThreshold: 2.0f) || stateTimer >= wanderDuration)
        {
            stateTimer = 0f;
            ChangeState(AgentState.Evaluating); // Loop back to brain to reassess target list
            agentStatusRing.SetActive(true); // Optional: Turn on the status ring to indicate a thinking state
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, "Finished wandering. Re-evaluating shopping list and targets.")); // Log that we're done wandering and going back to evaluation
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
                agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, "All checkout lines are full. Shuffling in holding area."));
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
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Joining checkout line at position {checkoutCurrentQueueIndex} with assigned spot at {assignedPosition}"));
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
        agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Heading to exit at {closestExit}"));
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
    private void UpdateCart(ProductSection section)
    {
        cartItems.Add(section);
        TotalMoneySpent += section.Price;
        TotalMoney -= section.Price;
    }
    public void ChangeState(AgentState newState)
    {
        currentState = newState;
        stateTimer = 0f;
        if (agentRenderer != null)
        {
            agentRenderer.color = AgentStateColors.ContainsKey(currentState) ? AgentStateColors[currentState] : Color.white;
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
    void OnDisable()
    {
        Debug.Log($"Agent '{gameObject.name}' is being disabled. Final shopping list: {string.Join(", ", shoppingList)}. Total money spent: {TotalMoneySpent}. Remaining money: {TotalMoney}.");

    }
}


