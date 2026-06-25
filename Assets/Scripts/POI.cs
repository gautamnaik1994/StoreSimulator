using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(CircleCollider2D))]
public class POI : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform visualSpriteChild;

    [Header("Settings")]
    public int maxCapacity = 3;
    public float pauseDuration = 5f;
    public float radiusControlInfluence = 0.15f;

    private string poiTag;

    [Header("Smoothness")]
    [Tooltip("Higher numbers mean faster transitions. Try values between 2 and 10.")]
    public float shrinkGrowSpeed = 5f;

    private CircleCollider2D poiCollider;
    private float originalVisualScaleX;
    private float targetScale; // Tracks the ideal scale we WANT to reach

    private Dictionary<AgentMovementEnhanced, Coroutine> activePauses = new Dictionary<AgentMovementEnhanced, Coroutine>();
    private List<AgentMovementEnhanced> agentsInTriggerZone = new List<AgentMovementEnhanced>();

    public string POIName; // Name of the POI, can be set in the Inspector
    public int price = 10; // Price of the product at this POI, can be set in the Inspector

    void Start()
    {
        poiCollider = GetComponent<CircleCollider2D>();
        POIName = string.IsNullOrEmpty(POIName) ? gameObject.name : POIName; // Default to GameObject name if not set

        if (visualSpriteChild == null)
        {
            Debug.LogError("Please assign the Visual Sprite Child transform in the inspector!");
            return;
        }

        originalVisualScaleX = visualSpriteChild.localScale.x;
        targetScale = originalVisualScaleX; // Start at full size
        poiTag = gameObject.tag; // Store the tag for later use
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Agent"))
        {

            AgentMovementEnhanced agent = other.GetComponent<AgentMovementEnhanced>();
            if (agent != null && !agentsInTriggerZone.Contains(agent) && agent.poiProductsInteractedWith.Contains(POIName) == false)
            {

                agentsInTriggerZone.Add(agent);
            }
        }
    }

    private void Update()
    {
        // 1. Smoothly interpolate the sprite scale towards the target scale
        SmoothScaleSprite();

        // Clean up any destroyed or null agents safely
        agentsInTriggerZone.RemoveAll(a => a == null);

        // Evaluate every agent currently inside the physics boundary
        for (int i = agentsInTriggerZone.Count - 1; i >= 0; i--)
        {
            AgentMovementEnhanced agent = agentsInTriggerZone[i];

            if (activePauses.ContainsKey(agent)) continue;

            int currentOccupants = activePauses.Count;

            // Hard Capacity Check
            if (currentOccupants >= maxCapacity) continue;

            // Soft Radius Check (Use the CURRENT TARGET SCALE to dictate the actual physical threshold logic)
            float softRadiusFactor = Mathf.Max(0.1f, 1f - (currentOccupants * radiusControlInfluence));
            float physicalTriggerRadius = poiCollider.radius * transform.lossyScale.x;
            float currentSoftRadius = physicalTriggerRadius * softRadiusFactor;

            float distanceToPOI = Vector2.Distance(transform.position, agent.transform.position);

            if (distanceToPOI <= currentSoftRadius)
            {
                Coroutine pauseCoroutine = StartCoroutine(PauseAgent(agent));
                activePauses.Add(agent, pauseCoroutine);

                // Recalculate target scale because someone entered
                CalculateTargetScale(activePauses.Count);

            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Agent"))
        {
            AgentMovementEnhanced agent = other.GetComponent<AgentMovementEnhanced>();
            if (agent == null) return;

            // Remove from the general physical zone tracking list
            if (agentsInTriggerZone.Contains(agent))
            {
                agentsInTriggerZone.Remove(agent);
            }

            if (activePauses.ContainsKey(agent))
            {
                StopCoroutine(activePauses[agent]);
                activePauses.Remove(agent);

                // FIX: Only resume the agent if it is actively placed on a NavMesh
                if (agent.IsOnNavMesh())
                {
                    // agent.isStopped = false;
                    agent.UnPauseAgent(); // Use the new method to safely resume movement
                }

                // Recalculate target scale because someone left
                CalculateTargetScale(activePauses.Count);
            }
        }
    }

    private System.Collections.IEnumerator PauseAgent(AgentMovementEnhanced agent)
    {
        agent.PauseAgent(); // Use the new method to safely pause movement
        agent.BuyRandomPOIProduct(POIName, poiTag, price); // Trigger the purchase action
        yield return new WaitForSeconds(pauseDuration);
        agent.UnPauseAgent(); // Use the new method to safely resume movement
    }

    // Instead of forcing the scale directly, this sets the mathematical destination
    private void CalculateTargetScale(int occupantCount)
    {
        float newScaleFactor = Mathf.Max(0.05f, 1f - (occupantCount * radiusControlInfluence));
        targetScale = originalVisualScaleX * newScaleFactor;
    }

    // Constantly eases the sprite size frame-by-frame toward the current destination
    private void SmoothScaleSprite()
    {
        if (visualSpriteChild == null) return;

        // Linearly interpolate the current scale to the target scale based on Time.deltaTime
        float currentScaleX = visualSpriteChild.localScale.x;
        float blendedScale = Mathf.Lerp(currentScaleX, targetScale, Time.deltaTime * shrinkGrowSpeed);

        visualSpriteChild.localScale = new Vector3(blendedScale, blendedScale, 1f);
    }
}