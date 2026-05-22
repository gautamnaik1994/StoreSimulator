using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class AgentMovement : MonoBehaviour
{

    public enum ShopperState { Pathfinding, Browsing, Finished }
    public List<Transform> shoppingListItemsLocations = new List<Transform>(); // Switched to List for easy removal

    // serialize exit positions in the inspector for easy assignment, but we won't remove them from the list like targets since we only head to them at the end

    public List<Transform> availableExitLocations = new List<Transform>(); // Switched to List for easy removal

    [SerializeField]
    private SupermarketLayoutSO layoutData; // Reference to the ScriptableObject containing the layout data

    private NavMeshAgent agent;
    private bool isMoving = false;

    private List<string> shoppingList = new List<string>() { "Milk", "Fruits" }; // This will hold the names of the items we need to buy, which should correspond to the section names in our layout data

    void Start()
    {
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
                if (section.TryGetEmptySlot(out Vector3 slotPosition))
                {                    // Create a temporary transform to hold the slot position and add it to our list of targets
                    GameObject tempTarget = new GameObject(item + "_Target");
                    tempTarget.transform.position = slotPosition;
                    shoppingListItemsLocations.Add(tempTarget.transform);
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

            FindAndMoveToClosest();
        }
        // Detect if 2 or more agents are stuck on each other by checking if the agent has a path but isn't moving



        // Optional: Add a key to reset the agent for testing purposes
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            Debug.Log("Resetting agent position and targets...");
            agent.Warp(Vector3.zero); // Reset agent to the center of the map (or you can set this to a specific spawn point)
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
                MoveToClosest(availableExitLocations.ConvertAll(e => e.position).ToArray(), false);
            }

            return;
        }

        Vector3[] destinations = new Vector3[shoppingListItemsLocations.Count];
        for (int i = 0; i < shoppingListItemsLocations.Count; i++)
        {
            // Add a small random offset to the target position to help prevent agents from clustering on the exact same spot
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            destinations[i] = shoppingListItemsLocations[i].position + randomOffset;
        }

        MoveToClosest(destinations, true);
    }

    /// <summary>
    /// Evaluates destinations and commands the agent to move to the closest valid one.
    /// </summary>
    public void MoveToClosest(Vector3[] destinations, bool removeFromTargetPositions = true)
    {
        if (destinations == null || destinations.Length == 0) return;

        Vector3 bestDestination = Vector3.zero;
        float shortestPathLength = float.MaxValue;
        bool pathFound = false;
        int bestIndex = -1;

        NavMeshPath path = new NavMeshPath();

        for (int i = 0; i < destinations.Length; i++)
        {
            Vector3 target = destinations[i];

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
            totalLength += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return totalLength;
    }
}
