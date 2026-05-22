using UnityEngine;
using UnityEngine.AI;


public class POI : MonoBehaviour
{

    public float pauseDuration = 5f; // Duration to pause the agent in seconds
    // This script will detect if an navmesh agent has entered the collider. It should pause the Navmesh Agent for the specified duration and then resume it. This will be used to create a point of interest for the agent to stop at and look around before continuing on its path.

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Something entered POI");
        if (other.CompareTag("Agent")) // Check if the collider belongs to an agent
        {
            Debug.Log("Agent entered POI");
            NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                StartCoroutine(PauseAgent(agent)); // Start the coroutine to pause the agent
            }
        }
    }

    private System.Collections.IEnumerator PauseAgent(NavMeshAgent agent)
    {
        agent.isStopped = true; // Pause the agent
        yield return new WaitForSeconds(pauseDuration); // Wait for the specified duration
        agent.isStopped = false; // Resume the agent
    }
}
