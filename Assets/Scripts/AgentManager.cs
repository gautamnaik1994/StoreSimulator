using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class AgentManager : MonoBehaviour
{
    public int agentCount = 10;
    public GameObject agentPrefab;
    public Transform spawnPoint;
    public int delay = 1; // Delay in seconds between each batch of agents spawned
    public int agentBatchCount = 3; // Number of agents to spawn in each batch when 'S' is pressed  

    public UIDocument uiDocument;
    private TextElement agentDetailsText;

    private VisualElement rightDrawer;

    private Button closeButton;

    private Button updateDetailsButton;

    private AgentMovementEnhanced selectedAgent;

    void Start()
    {
        agentDetailsText = uiDocument.rootVisualElement.Q<TextElement>("AgentDetails");
        agentDetailsText.text = "Click on an agent to see details here.";

        rightDrawer = uiDocument.rootVisualElement.Q<VisualElement>("RightDrawer");
        closeButton = uiDocument.rootVisualElement.Q<Button>("Close");
        updateDetailsButton = uiDocument.rootVisualElement.Q<Button>("Update");

        closeButton.clicked += CloseAgentDetails;
        updateDetailsButton.clicked += ForceUpdateAgentDetails;
    }

    void CloseAgentDetails()
    {
        rightDrawer.style.display = DisplayStyle.None;
    }

    public void UpdateAgentDetails(string details)
    {
        // check if AgentDetailsPanel display is none, if so set it to flex
        // if (agentDetailsPanel.style.display == DisplayStyle.None)
        // {
        // }
        rightDrawer.style.display = DisplayStyle.Flex;
        agentDetailsText.text = details;
    }

    public void SpawnAgents()
    {
        StartCoroutine(BatchSpawnCoroutine());
    }

    public void GetAgentDetailsOnClick(Vector2 mousePosition)
    {
        Vector2 worldPoint = Camera.main.ScreenToWorldPoint(mousePosition);

        // 1. Define a generous click radius (adjust this value in play mode to taste)
        float clickRadius = 0.5f;

        // 2. Grab ALL colliders inside that radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(worldPoint, clickRadius);

        AgentMovementEnhanced closestAgent = null;
        float shortestDistance = float.MaxValue;

        // 3. Loop through everything we hit to find the absolute closest valid agent
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<AgentMovementEnhanced>(out var agent))
            {
                // Calculate how far this object is from the exact click point
                float distance = Vector2.Distance(worldPoint, hit.transform.position);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestAgent = agent;
                }
            }
        }

        // 4. If we found a winner, select it
        if (closestAgent != null)
        {
            selectedAgent = closestAgent;
            UpdateAgentDetails("Agent Details: " + selectedAgent.GetAgentDetails());
        }
    }

    private System.Collections.IEnumerator BatchSpawnCoroutine()
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

    void ForceUpdateAgentDetails()
    {
        if (selectedAgent != null)
        {
            UpdateAgentDetails("Agent Details: " + selectedAgent.GetAgentDetails());
        }
    }
}




