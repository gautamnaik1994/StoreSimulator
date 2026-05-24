using UnityEngine;
using UnityEngine.InputSystem;


public class AgentManager : MonoBehaviour
{

    public int agentCount = 10;
    public GameObject agentPrefab;

    public Transform spawnPoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void Update()
    {
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            SpawnAgents();
        }
    }

    void SpawnAgents()
    {

        for (int i = 0; i < agentCount; i++)
        {
            // add a delay between spawns to avoid all agents spawning on top of each other and getting stuck
            Vector2 randomOffset = new(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            Instantiate(agentPrefab, spawnPoint.position + (Vector3)randomOffset, Quaternion.identity);

        }
    }
}
