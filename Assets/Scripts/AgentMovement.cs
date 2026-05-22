using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class AgentMovement : MonoBehaviour
{
    public List<Transform> targetPositions = new List<Transform>(); // Switched to List for easy removal

    private NavMeshAgent agent;
    private bool isMoving = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // Disable automatic rotation
        agent.updateUpAxis = false;   // Disable automatic up axis adjustment
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
    }

    /// <summary>
    /// Helper method to extract positions from remaining targets and trigger movement.
    /// </summary>
    private void FindAndMoveToClosest()
    {
        // Clean up any null references in the list just in case
        targetPositions.RemoveAll(t => t == null);

        if (targetPositions.Count == 0)
        {
            Debug.Log("All destinations visited!");
            return;
        }

        Vector3[] destinations = new Vector3[targetPositions.Count];
        for (int i = 0; i < targetPositions.Count; i++)
        {
            destinations[i] = targetPositions[i].position;
        }

        MoveToClosest(destinations);
    }

    /// <summary>
    /// Evaluates destinations and commands the agent to move to the closest valid one.
    /// </summary>
    public void MoveToClosest(Vector3[] destinations)
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
            targetPositions.RemoveAt(bestIndex);
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
