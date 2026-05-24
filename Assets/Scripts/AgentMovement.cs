using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

public class AgentMovement : MonoBehaviour
{

    public enum ShopperState { Pathfinding, Browsing, Buying, Finished }
    public List<Vector2> shoppingListItemsLocations = new(); // Switched to List for easy removal

    // serialize exit positions in the inspector for easy assignment, but we won't remove them from the list like targets since we only head to them at the end

    public List<Transform> availableExitLocations = new(); // Switched to List for easy removal

    [SerializeField]
    private SupermarketLayoutSO layoutData; // Reference to the ScriptableObject containing the layout data

    private NavMeshAgent agent;
    private bool isMoving = false;

    private ProductSlot currentTargetSlot; // Keep track of the current target slot to mark it as occupied when we arrive

    private List<string> shoppingList = new() { "Milk", "Fruits" }; // This will hold the names of the items we need to buy, which should correspond to the section names in our layout data

    void Start()
    {
        Debug.Log($"Agent '{gameObject.name}' starting with shopping list: {string.Join(", ", shoppingList)}");
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // Disable automatic rotation
        agent.updateUpAxis = false;   // Disable automatic up axis adjustment
        // randomize priority to avoid agents getting stuck on each other
        agent.avoidancePriority = Random.Range(10, 90); // Add a small random value to further reduce ties
        // randomize speed slightly to add some variation between agents
        agent.speed += Random.Range(-0.5f, 0.5f);
        // randomize stopping distance slightly to add some variation between agents
        agent.stoppingDistance += Random.Range(-0.2f, 0.2f);
        // randomize acceleration slightly to add some variation between agents
        agent.acceleration += Random.Range(-0.5f, 0.5f);


        // populate our shopping list item locations based on the section names in our shopping list and the data in our layout scriptable object
        foreach (string item in shoppingList)
        {
            ProductSection section = layoutData.ProductSections.Find(s => s.SectionName == item);
            if (section != null)
            {
                if (section.TryGetEmptySlot(out Vector2 slotPosition))
                {

                    shoppingListItemsLocations.Add(slotPosition);
                    section.Slots.Find(s => s.Position == slotPosition).IsOccupied = true; // Mark this slot as occupied so other agents won't target it
                }
                else
                {
                    Debug.LogWarning($"No available slots found in section '{item}' for agent '{gameObject.name}'.");
                }
            }
            else
            {
                Debug.LogWarning($"Section '{item}' not found in layout data for agent '{gameObject.name}'.");
            }
        }

    }

    void Update()
    {
        // 1. Trigger the initial search
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            FindAndMoveToClosest();
        }

        // 2. Continuous check to see if we arrived
        if (isMoving && HasReachedDestination())
        {
            isMoving = false;
            Debug.Log("Destination reached! Finding the next closest target...");

            _ = PauseAgent(2000); // Pause for 1 second to simulate browsing time
            // clear the current target slot reference since we've arrived and are now "browsing" it
            currentTargetSlot = null;
            FindAndMoveToClosest();
        }
        // Detect if 2 or more agents are stuck on each other by checking if the agent has a path but isn't moving



        // Optional: Add a key to reset the agent for testing purposes
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("Resetting agent position and targets...");
            agent.Warp(Vector2.zero); // Reset agent to the center of the map (or you can set this to a specific spawn point)
        }
    }

    void LateUpdate()
    {

        if (agent.hasPath && agent.velocity.sqrMagnitude < 0.01f)
        {
            Debug.LogWarning("Agent seems to be stuck. Attempting to get unstuck.");
            agent.avoidancePriority = Random.Range(10, 90);
        }
    }

    /// <summary>
    /// Helper method to extract positions from remaining targets and trigger movement.
    /// </summary>
    private void FindAndMoveToClosest()
    {
        // Clean up any null references in the list just in case
        shoppingListItemsLocations.RemoveAll(t => t == null);

        if (shoppingListItemsLocations.Count == 0)
        {
            Debug.Log("All destinations visited!");
            if (availableExitLocations.Count > 0)
            {
                Debug.Log("Heading to exit...");
                MoveToClosest(availableExitLocations.ConvertAll(t => (Vector2)t.position).ToArray(), false);
            }

            return;
        }

        Vector2[] destinations = new Vector2[shoppingListItemsLocations.Count];
        for (int i = 0; i < shoppingListItemsLocations.Count; i++)
        {
            // Add a small random offset to the target position to help prevent agents from clustering on the exact same spot
            Vector2 randomOffset = new(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));
            destinations[i] = shoppingListItemsLocations[i] + randomOffset;
        }

        MoveToClosest(destinations, true);
    }

    /// <summary>
    /// Evaluates destinations and commands the agent to move to the closest valid one.
    /// </summary>
    public void MoveToClosest(Vector2[] destinations, bool removeFromTargetPositions = true)
    {
        if (destinations == null || destinations.Length == 0) return;

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

        if (pathFound)
        {
            agent.SetDestination(bestDestination);
            // 
            isMoving = true;

            // Remove the target we are heading to from our list so we don't pick it next time
            if (removeFromTargetPositions && bestIndex >= 0 && bestIndex < shoppingListItemsLocations.Count)
            {
                shoppingListItemsLocations.RemoveAt(bestIndex);
            }
        }
        else
        {
            Debug.LogWarning("No complete NavMesh path found to any of the remaining destinations.");
            isMoving = false;
        }
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

    private async Task PauseAgent(int delayMs)
    {
        agent.isStopped = true; // Stop the agent while waiting
        await Task.Delay(delayMs);
        agent.isStopped = false; // Resume the agent after the delay
    }
}
