using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentMovement_v2 : MonoBehaviour
{
    [SerializeField]
    private SupermarketLayoutSO layoutData; // Reference to the ScriptableObject containing the layout data

    private NavMeshAgent agent;

    public enum AgentState { Evaluating, MovingToTarget, Buying, BuyingComplete, Wandering, Exiting }

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


        // build shopping list item locations based on the section names in our shopping list and the data in our layout scriptable object
        foreach (string item in shoppingList)
        {
            ProductSection section = layoutData.ProductSections.Find(s => s.SectionName == item);
            if (section != null)
            {
                // get section center as target since we won't be booking specific slots in this version, just trying to get to the general area
                shoppingListItemsLocations.Add(section.Slots[0].Position); // Assuming the first slot

            }

        }
        ChangeState(AgentState.Evaluating);
        EvaluateNextTarget();

    }

    public void ChangeState(AgentState newState)
    {
        currentState = newState;
        stateTimer = 0f;

        // Visual Feedback (Wow Factor for Hackathon Judges)
        // if (stateDebugText != null)
        // {
        //     switch (newState)
        //     {
        //         case AgentState.Evaluating:   stateDebugText.text = "🤔 Thinking..."; break;
        //         case AgentState.MovingToTarget: stateDebugText.text = "🏃 Heading to " + currentTargetItem?.name; break;
        //         case AgentState.Buying:         stateDebugText.text = "🛒 Buying..."; break;
        //         case AgentState.Wandering:      stateDebugText.text = "❓ Aisle Full! Wandering..."; break;
        //         case AgentState.Exiting:        stateDebugText.text = "👋 Leaving!"; break;
        //     }
        // }
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
            ChangeState(AgentState.Exiting);
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


    /// <summary>
    /// Calculates the closest destination from an array of potential targets using NavMesh pathfinding
    /// </summary>
    public Vector2 FindClosestDestination(Vector2[] destinations)
    {
        if (destinations == null || destinations.Length == 0) return Vector2.zero;

        Vector2 bestDestination = Vector2.zero;
        float shortestPathLength = float.MaxValue;
        bool pathFound = false;
        int bestIndex = -1;

        NavMeshPath path = new();

        for (int i = 0; i < destinations.Length; i++)
        {
            Vector2 target = destinations[i];

            if (agent.CalculatePath(target, path))
            {
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    float pathLength = GetPathLength(path);

                    if (pathLength < shortestPathLength)
                    {
                        shortestPathLength = pathLength;
                        bestDestination = target;
                        bestIndex = i;
                        pathFound = true;
                    }
                }
            }
        }
        return pathFound ? bestDestination : destinations[0]; // Fallback to first destination if no valid path found

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
                // Debug.Log("Section is full, switching to wandering state.");
                // agent.SetDestination(transform.position); // Stop moving while we decide where to wander
                // // add a coroutine delay here to simulate wandering around for a bit before re-evaluating targets
                // StartCoroutine(WanderAndReevaluate());

            }
        }
    }

    private IEnumerator WanderAndReevaluate()
    {
        yield return new WaitForSeconds(2f); // Simulate wandering for 2 seconds
        ChangeState(AgentState.Evaluating);
        EvaluateNextTarget();
    }

    void HandleWanderingState()
    {
        // In this simple implementation, we'll just wait for a short duration and then re-evaluate our targets
        stateTimer += Time.deltaTime;
        if (stateTimer >= wanderDuration)
        {
            EvaluateNextTarget();
            ChangeState(AgentState.Evaluating);
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



}
