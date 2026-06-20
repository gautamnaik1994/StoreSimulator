using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentMovementEnhanced : MonoBehaviour
{
    [SerializeField]
    private SupermarketLayoutSO layoutData;
    private NavMeshAgent agent;
    private List<string> shoppingList = new List<string>();
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

    public GameObject selectionIndicator;

    [Header("Detour Settings")]
    [Range(0f, 1f)] public float randomBrowseProbability = 0.15f; // 15% chance to aimlessly wander instead of shopping

    public enum AgentState { Evaluating, NavigatingToShelf, BrowsingShelf, Wandering, WaitingToQueue, GoingToCheckout, CheckingOut, Leaving }
    private Dictionary<AgentState, Color> agentStateColors;
    public enum AgentType { Regular, BargainHunter, ImpulseBuyer, WindowShopper, BulkShopper, FocusedShopper }
    public enum AgentBudgetLevel { Low, Medium, High }
    public enum AgentPersonalityTrait { Impulsive, Cautious, BudgetConscious, TimeSensitive, BrandLoyalist, Indecisive }
    public enum AgentMood { Happy, Neutral, Frustrated, Impatient, Lost, Sad }
    private Dictionary<AgentMood, Color> agentMoodColors;
    public AgentState currentState = AgentState.Evaluating;
    private float impulseProbability = 0.3f; // 30% chance to make an impulse detour
    private List<string> impulseFavorites;

    // Tracks the current ranked destinations chosen by the brain for this execution cycle
    private Queue<ShoppingTarget> rankedDestinationsQueue = new Queue<ShoppingTarget>();
    // Tracks sections that were full, allowing us to penalize them temporarily during evaluation
    private Dictionary<string, float> sectionCooldowns = new Dictionary<string, float>();
    [SerializeField] private float fullShelfCooldownDuration = 15.0f;
    public GameObject agentStatusRing; // Optional: A SpriteRenderer to visually indicate that agent is thinking (evaluating) 
    private SpriteRenderer statusRingRenderer;
    public int wanderDuration = 10; // Time in seconds the agent will spend wandering before re-evaluating their shopping list

    // Add these fields to your script
    [Header("Psychological Thresholds")]
    private float patience = 1.0f;     // Drops when waiting or dealing with full shelves
    private float frustration = 0.0f;  // Rises when blocked or empty-handed
    private float fatigue = 0.0f;      // Rises over time based on total shopping duration

    // create an agent history to track their shopping behavior and decisions and state transitions
    private List<AgentHistoryEntry> agentHistory = new List<AgentHistoryEntry>();

    int maxRandomDetours = 4;
    private struct AgentHistoryEntry
    {
        public float timestamp;
        public AgentState state;
        public string actionDescription;
        public AgentMood AgentMood;

        public AgentHistoryEntry(float time, AgentState agentState, string description, AgentMood mood = AgentMood.Neutral)
        {
            timestamp = time;
            state = agentState;
            actionDescription = description;
            AgentMood = mood;
        }
    }

    private struct ShoppingTarget
    {
        public ProductSection Section;
        public ProductSlot Slot;
        public Vector2 Position => Slot.Position;
        public ShoppingTarget(ProductSection section, ProductSlot slot)
        {
            Section = section;
            Slot = slot;
        }
    }
    private int TotalMoney = 10000;
    private int BaselineTotalMoney = 10000;
    private readonly List<ProductSection> cartItems = new List<ProductSection>();
    private int TotalMoneySpent = 0;
    private AgentMood currentMood = AgentMood.Neutral;

    private string AgentPersonaName = "Default Persona";
    private int AgentAge = 30;
    private string AgentIncomeLevel = "Medium";
    private string AgentGender = "Unspecified";
    private string AgentProfession = "Unemployed";

    private List<ProductSection> CostlyItems = new List<ProductSection>();

    private float baselineSpeed;
    private float originalImpulseProbability;

    void Awake()
    {
        agentStateColors = new Dictionary<AgentState, Color>()
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


        agentMoodColors = new Dictionary<AgentMood, Color>()
        {
            { AgentMood.Happy, ParseColor("#34D399") },
            { AgentMood.Neutral, ParseColor("#FBBF24") },
            { AgentMood.Frustrated, ParseColor("#F43F5E") },
            { AgentMood.Impatient, ParseColor("#FB7185") },
            {AgentMood.Sad, ParseColor("#3B82F6") },
            { AgentMood.Lost, ParseColor("#94A3B8") }
        };

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // Disable automatic rotation
        agent.updateUpAxis = false;   // Disable automatic up axis adjustment

        agentRenderer = GetComponent<SpriteRenderer>();
        statusRingRenderer = agentStatusRing.GetComponent<SpriteRenderer>();
    }

    public void InitializeWithPersona(AgentPersonaData persona)
    {
        shoppingList = new List<string>(persona.base_shopping_list);
        impulseFavorites = new List<string>(persona.base_impulse_favorites);
        TotalMoney = persona.base_total_money;
        BaselineTotalMoney = TotalMoney;
        agent.speed = persona.baseline_physics.base_speed + Random.Range(-0.2f, 0.2f);
        agent.acceleration = persona.baseline_physics.base_acceleration + Random.Range(-0.2f, 0.2f);
        agent.avoidancePriority = persona.baseline_physics.base_avoidance_priority + Random.Range(-5, 5);
        impulseProbability = persona.psychological_profile.base_impulse_probability;
        randomBrowseProbability = persona.psychological_profile.base_random_browse_probability;
        AgentPersonaName = persona.persona_name;
        baselineSpeed = agent.speed;
        originalImpulseProbability = impulseProbability;
        AgentAge = persona.demographics.age;
        AgentGender = persona.demographics.gender;
        AgentIncomeLevel = persona.demographics.income_level;
        AgentProfession = persona.demographics.profession;

        ChangeState(AgentState.Evaluating);
        ChangeMood(AgentMood.Neutral);

        // Log the initialization in the agent history
        agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Initialized with persona: {persona.persona_name}, Shopping List: {string.Join(", ", shoppingList)}, Impulse Favorites: {string.Join(", ", impulseFavorites)}, Total Money: {TotalMoney}", currentMood));
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
        // Only rotate if the agent is actually moving
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            // Calculate the angle in degrees from the velocity vector
            float angle = Mathf.Atan2(agent.velocity.y, agent.velocity.x) * Mathf.Rad2Deg;

            // Subtract 90 degrees if your sprite's "front" faces upwards by default
            float targetAngle = angle - 90f;

            // Apply the rotation strictly around the Z-axis
            transform.rotation = Quaternion.Euler(0, 0, targetAngle);
        }
    }

    private void DynamicMoodEvaluation()
    {
        // Accumulate base fatigue over time
        fatigue += Time.deltaTime * 0.005f;

        // Determine mood based on state mixtures
        if (frustration > 0.7f && patience < 0.2f)
        {
            ChangeMood(AgentMood.Impatient);
        }
        else if (frustration > 0.4f)
        {
            ChangeMood(AgentMood.Frustrated);
        }
        else if (fatigue > 0.8f || CostlyItems.Count > 0) // If the agent is exhausted or has too many costly items
        {
            ChangeMood(AgentMood.Sad); // Simulating exhausted/low energy
        }
        else if (cartItems.Count > 3 && frustration < 0.2f)
        {
            ChangeMood(AgentMood.Happy);
        }
        else
        {
            ChangeMood(AgentMood.Neutral);
        }

        // APPLY BUSINESS/PHYSICS CONSEQUENCES OF MOOD
        ApplyMoodSideEffects();
    }

    private void ApplyMoodSideEffects()
    {
        switch (currentMood)
        {
            case AgentMood.Impatient:
                // Impatient agents walk faster but drop items from their list early to checkout faster!
                agent.speed = baselineSpeed * 1.3f;
                if (shoppingList.Count > 1 && Random.value < 0.01f)
                {
                    string abandonedItem = shoppingList[Random.Range(0, shoppingList.Count)];
                    shoppingList.Remove(abandonedItem);
                    agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"[Loss of Sale] Abandoned looking for {abandonedItem} due to impatience.", currentMood));
                }
                break;

            case AgentMood.Frustrated:
                // Frustrated agents stop impulse buying completely
                impulseProbability = 0f;
                break;

            case AgentMood.Happy:
                // Happy agents buy more impulses and walk at a leisurely pace
                agent.speed = baselineSpeed * 0.9f;
                impulseProbability = originalImpulseProbability * 1.5f;
                break;
        }
    }

    public void ForceBrainUpdate()
    {
        forceReevaluation = true;
    }

    public void ToggleSelectionIndicator(bool isActive)
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(isActive);
        }
    }

    private void CentralizeDecisionMaking()
    {
        DynamicMoodEvaluation();
        // 1. Structural Check: Is the shopper done?
        if (shoppingList.Count == 0)
        {
            ChangeState(AgentState.WaitingToQueue);
            // ChangeMood(AgentMood.Happy);
            Vector2 holdingArea = GetClosestHoldingArea();
            agent.SetDestination(holdingArea);
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, "Shopping list completed. Moving to holding area.", currentMood));

            nextQueueCheckTime = Time.time;
            return;
        }

        // --- DETOUR DETERMINATION PHASE ---
        float decisionRoll = Random.value;

        // CASE A: Random Detour / Wandering (Simulating aimless window shopping)
        if (decisionRoll < randomBrowseProbability && maxRandomDetours > 0) // Only take a random detour if we have any left in the tank
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
            // reduce the chance of taking another random detour immediately after by scaling down the probability and setting a hard limit on consecutive detours
            // randomBrowseProbability *= 0.7f;
            maxRandomDetours--; // Reduce the number of available random detours
            // ChangeMood(AgentMood.Neutral);
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Taking a random detour to wander near section: {randomSection.SectionName}", currentMood));
            return;
        }

        // CASE B: Core Shopping List & Impulse Calculations
        List<ShoppingTarget> potentialTargets = new List<ShoppingTarget>();

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
                potentialTargets.Add(new ShoppingTarget(section, section.Slots[randomSlotIndex]));
            }
        }

        // Reset cooldowns if standard targets are completely starved
        if (potentialTargets.Count == 0 && shoppingList.Count > 0)
        {
            sectionCooldowns.Clear();
            foreach (string item in shoppingList)
            {
                ProductSection section = layoutData.ProductSections.Find(s => s.SectionName == item);
                // AI added code below to prevent null reference exceptions if the section or its slots are missing
                if (section == null || section.Slots == null || section.Slots.Count == 0)
                {
                    continue;
                }

                int randomSlotIndex = Random.Range(0, section.Slots.Count);
                potentialTargets.Add(new ShoppingTarget(section, section.Slots[randomSlotIndex]));
            }
        }

        // Step 2: Query nearest options out of our valid targets
        ShoppingTarget[] sortedTargets = FindClosestDestinations(potentialTargets.ToArray(), 3);

        rankedDestinationsQueue.Clear();
        foreach (ShoppingTarget target in sortedTargets)
        {
            rankedDestinationsQueue.Enqueue(target);
        }

        // CASE C: Impulse Buying (Intercepting the closest target choice)
        // If we pass our probability roll and have favorite items left to exploit
        if (decisionRoll >= randomBrowseProbability && decisionRoll < (randomBrowseProbability + impulseProbability))
        {
            // Try to pick an impulse item that isn't already on the standard shopping list
            // <string> validImpulseChoices = impulseFavorites.FindAll(fav => !shoppingList.Contains(fav));
            List<string> validImpulseChoices = impulseFavorites;

            if (validImpulseChoices.Count > 0)
            {
                string chosenImpulseItem = validImpulseChoices[Random.Range(0, validImpulseChoices.Count)];
                ProductSection impulseSection = layoutData.ProductSections.Find(s => s.SectionName == chosenImpulseItem);

                if (impulseSection != null)
                {
                    ShoppingTarget impulseSlotPos = new ShoppingTarget(impulseSection, impulseSection.Slots[Random.Range(0, impulseSection.Slots.Count)]);

                    // CRITICAL DESIGN MOVE: Re-create the queue to put impulse at the absolute FRONT
                    Queue<ShoppingTarget> interceptQueue = new Queue<ShoppingTarget>();
                    interceptQueue.Enqueue(impulseSlotPos); // Impulse is Destination #1!

                    // Append the remaining regular targets back behind it
                    while (rankedDestinationsQueue.Count > 0)
                    {
                        interceptQueue.Enqueue(rankedDestinationsQueue.Dequeue());
                    }

                    rankedDestinationsQueue = interceptQueue;
                    agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"[Impulse Alert] Spotted {chosenImpulseItem}! Added to front of itinerary layout.", currentMood));
                }
            }
        }

        // 3. Set off toward the front item in our finalized queue layout
        if (rankedDestinationsQueue.Count > 0)
        {
            // agentStatusRing.SetActive(false);
            NavigateToNextCachedTarget();
        }
    }

    private void NavigateToNextCachedTarget()
    {
        if (rankedDestinationsQueue.Count > 0)
        {
            ShoppingTarget nextTarget = rankedDestinationsQueue.Dequeue();
            currentTargetSection = nextTarget.Section;
            // check if cuurentTargetSection is present in the shopping list or impulse favorites, if not, then skip it and move to the next one in the queue until we find one that is, or we run out of options and have to force a re-evaluation

            while (!shoppingList.Contains(currentTargetSection.SectionName) && !impulseFavorites.Contains(currentTargetSection.SectionName))
            {
                if (rankedDestinationsQueue.Count == 0)
                {
                    ChangeState(AgentState.Evaluating);
                    return;
                }

                nextTarget = rankedDestinationsQueue.Dequeue();
                currentTargetSection = nextTarget.Section;
            }

            ChangeState(AgentState.NavigatingToShelf);
            // ChangeMood(AgentMood.Neutral);
            agent.SetDestination(nextTarget.Position);
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Navigating to shelf: {currentTargetSection.SectionName} at {nextTarget.Position}", currentMood));
        }
        else
        {
            // No more cached routes available, force brain to re-evaluate whole picture
            ChangeState(AgentState.Evaluating);
            // ChangeMood(AgentMood.Neutral);
            // agentStatusRing.SetActive(true); // Optional: Turn on the status ring to indicate a thinking state
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, "No more cached targets. Forcing re-evaluation of options.", currentMood)); // Log that we're out of cached options and need
        }
    }

    void HandleBrowsingShelf()
    {
        if (!HasReachedDestination(arrivalThreshold: 0.5f)) return;
        if (currentTargetSection == null)
        {
            Debug.LogWarning("BrowsingShelf reached without a valid currentTargetSection. Returning to evaluation.");
            ChangeState(AgentState.Evaluating);
            return;
        }

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

                agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, logMessage, currentMood));
                // ChangeMood(AgentMood.Happy);
                // update the cart with the newly purchased item
                UpdateCart(currentTargetSection);
            }
            else
            {
                CostlyItems.Add(currentTargetSection);
                // ChangeMood(AgentMood.Sad);
                agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Browsed {purchasedItemName} but couldn't afford it. Needed {currentTargetSection.Price}, had {TotalMoney}.", currentMood));
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
            ChangeMood(AgentMood.Neutral);
        }
    }

    void HandleWanderingState()
    {
        // If they finish walking to their random wander slot OR get tired of looking around
        if (HasReachedDestination(arrivalThreshold: 2.0f) || stateTimer >= wanderDuration)
        {
            stateTimer = 0f;
            ChangeState(AgentState.Evaluating); // Loop back to brain to reassess target list
            // ChangeMood(AgentMood.Neutral);
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, "Finished wandering. Re-evaluating shopping list and targets.", currentMood)); // Log that we're done wandering and going back to evaluation
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
        if (handlerWithLeastAgents is null)
        {
            // Optional: Every few seconds, pick a small micro-adjustment spot 
            // in the holding area to make them look like they are shuffling impatiently
            if (Random.value < 0.2f)
            {
                agent.SetDestination(GetClosestHoldingArea() + Random.insideUnitCircle * 2f);
                agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, "All checkout lines are full. Shuffling in holding area.", currentMood));
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
            // ChangeMood(AgentMood.Neutral);
            agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Joining checkout line at position {checkoutCurrentQueueIndex} with assigned spot at {assignedPosition}", currentMood));
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
                // ChangeMood(AgentMood.Neutral);
                agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Navigating to specific slot at {slotPosition} in section {currentTargetSection.SectionName}", currentMood));
            }
            else
            {
                // REALISM: Shelf is completely full! 
                Debug.Log($"Shelf {currentTargetSection.SectionName} is full. Diverting to next option.");
                // Change mood state dynamically based on operational frustration
                if (currentMood == AgentMood.Neutral) currentMood = AgentMood.Frustrated;
                else if (currentMood == AgentMood.Frustrated) currentMood = AgentMood.Impatient;
                agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Shelf full at {currentTargetSection.SectionName}. Shifting to runner-up target.", currentMood));
                if (currentMood == AgentMood.Impatient)
                {
                    wanderDuration = Mathf.Clamp(wanderDuration - 2, 2, 10);
                    randomBrowseProbability *= 0.5f; // Stop wasting time wandering
                }
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

        // Vector2 closestExit = FindClosestDestination(layoutData.ExitLocations.ToArray());
        agent.SetDestination(layoutData.ExitLocations[0]); // For simplicity, just head to the first exit
        agentHistory.Add(new AgentHistoryEntry(Time.time, currentState, $"Heading to exit at {layoutData.ExitLocations[0]}", currentMood));
        // ChangeMood(AgentMood.Neutral);
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
        Holding targetHolding = layoutData.HoldingAreas[0];
        return targetHolding.GetRandomPositionInHoldingArea();
    }


    private ShoppingTarget? FindClosestDestination(ShoppingTarget[] destinations)
    {
        ShoppingTarget[] closestDestinations = FindClosestDestinations(destinations, 1);
        return closestDestinations.Length > 0 ? closestDestinations[0] : null;
    }

    void SetAvoidancePriorityBasedOnQueuePosition(int queuePosition)
    {
        // Lower priority for those closer to the front of the line (lower index)
        int priority = Mathf.Clamp(queuePosition, 1, 90); // Example: 0th in line = 10 priority, 1st in line = 20 priority, etc.
        agent.avoidancePriority = priority;
    }

    private ShoppingTarget[] FindClosestDestinations(ShoppingTarget[] destinations, int maxResults)
    {
        if (destinations == null || destinations.Length == 0 || maxResults <= 0)
        {
            return System.Array.Empty<ShoppingTarget>();
        }

        List<(ShoppingTarget destination, float pathLength)> rankedDestinations = new();
        NavMeshPath path = new();

        for (int i = 0; i < destinations.Length; i++)
        {
            ShoppingTarget target = destinations[i];

            if (agent.CalculatePath(target.Position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                rankedDestinations.Add((target, GetPathLength(path)));
            }
        }

        if (rankedDestinations.Count == 0)
        {
            int fallbackCount = Mathf.Min(maxResults, destinations.Length);
            ShoppingTarget[] fallbackDestinations = new ShoppingTarget[fallbackCount];
            System.Array.Copy(destinations, fallbackDestinations, fallbackCount);
            return fallbackDestinations;
        }

        rankedDestinations.Sort((left, right) => left.pathLength.CompareTo(right.pathLength));

        int resultCount = Mathf.Min(maxResults, rankedDestinations.Count);
        ShoppingTarget[] closestDestinations = new ShoppingTarget[resultCount];

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
        if (statusRingRenderer is not null)
        {
            statusRingRenderer.color = agentStateColors.TryGetValue(currentState, out var color) ? color : Color.white;
        }
    }

    public void ChangeMood(AgentMood newMood)
    {
        currentMood = newMood;
        if (agentRenderer is not null)
        {
            agentRenderer.color = agentMoodColors.TryGetValue(newMood, out var color) ? color : Color.white;
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

        var totalLength = 0f;
        for (var i = 0; i < path.corners.Length - 1; i++)
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

    public AgentDetails GetAgentDetails()
    {

        string historyDetails = "";
        var historyEntries = new List<AgentHistoryEntryData>(agentHistory.Count);
        foreach (var entry in agentHistory)
        {
            historyDetails += $"- [{entry.timestamp:F2}s] State: {entry.state}, Action: {entry.actionDescription}\n";
            historyEntries.Add(new AgentHistoryEntryData
            {
                Timestamp = entry.timestamp,
                State = entry.state.ToString(),
                ActionDescription = entry.actionDescription,
                Mood = entry.AgentMood.ToString()
            });
        }

        return new AgentDetails
        {
            AgentName = AgentPersonaName,
            Position = transform.position.ToString(),
            AgentAge = AgentAge,
            AgentGender = AgentGender,
            AgentIncomeLevel = AgentIncomeLevel,
            AgentProfession = AgentProfession,
            CurrentState = currentState.ToString(),
            CurrentMood = currentMood.ToString(),
            ShoppingList = string.Join(", ", shoppingList),
            ImpulseFavorites = string.Join(", ", impulseFavorites),
            CostlyItems = string.Join(", ", CostlyItems.ConvertAll(item => item.SectionName)),
            CartItems = string.Join(", ", cartItems.ConvertAll(item => item.SectionName)),
            TotalMoneySpent = TotalMoneySpent,
            RemainingMoney = TotalMoney,
            BaselineMoney = BaselineTotalMoney,
            History = historyDetails,
            HistoryEntries = historyEntries
        };
    }

    private void OnDisable()
    {
        // Debug.Log($"Agent '{gameObject.name}' is being disabled. Final shopping list: {string.Join(", ", shoppingList)}. Total money spent: {TotalMoneySpent}. Remaining money: {TotalMoney}.");

    }
}


