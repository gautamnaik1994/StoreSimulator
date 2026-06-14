using UnityEngine;
using UnityEngine.InputSystem;


public class AgentManager : MonoBehaviour
{

    public int agentCount = 10;
    public GameObject agentPrefab;

    public Transform spawnPoint;

    public int delay = 1; // Delay in seconds between each batch of agents spawned

    public int agentBatchCount = 3; // Number of agents to spawn in each batch when 'S' is pressed  
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void LateUpdate()
    {
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            StartCoroutine(BatchSpawnCoroutine(delay));
        }
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(worldPoint);

            if (hit != null)
            {
                AgentMovementEnhanced agent = hit.GetComponent<AgentMovementEnhanced>();
                if (agent != null)
                {
                    Debug.Log("Agent Details: " + agent.GetAgentDetails());
                }
            }
        }
    }


    private System.Collections.IEnumerator BatchSpawnCoroutine(float delay)
    {
        int batches = Mathf.CeilToInt((float)agentCount / agentBatchCount);
        for (int i = 0; i < batches; i++)
        {
            int agentsToSpawn = Mathf.Min(agentBatchCount, agentCount - (i * agentBatchCount));
            for (int j = 0; j < agentsToSpawn; j++)
            {
                Instantiate(agentPrefab, spawnPoint.position, Quaternion.identity);
            }
            yield return new WaitForSeconds(delay);
        }
    }
    // write a function which will get the details of the agent when mouse is clicked over it

}




