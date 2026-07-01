using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeatmapManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private Vector2 storeSize = new Vector2(50f, 30f);
    [SerializeField] private Vector2 storeOffset = Vector2.zero;
    [SerializeField] private float cellSize = 0.5f;

    private int gridResolutionX;
    private int gridResolutionY;

    private float[,] gridData;
    private Texture2D heatmapTexture;
    private Sprite heatmapSprite;
    private float maxIntensity = 1f;

    [Header("Sampling Settings")]
    [SerializeField] private float sampleInterval = 0.5f;
    [SerializeField] private float intensityWeight = 1f;

    [Header("Visualization")]
    [SerializeField] private SpriteRenderer heatmapDisplay;
    [SerializeField] private Gradient heatmapGradient;
    [SerializeField] private bool isMapVisible = false;

    private AgentManager agentManager;
    private Coroutine renderLoopCoroutine;

    // OPTIMIZATION 1: Cache the color array to prevent allocating a new one every frame
    private Color32[] colorMap;
    private void Start()
    {
        agentManager = GetComponent<AgentManager>();
        InitializeHeatmap();
        StartCoroutine(SampleAgentsLoop());
        EvaluateRenderState();
    }

    private void InitializeHeatmap()
    {
        gridResolutionX = Mathf.Max(1, Mathf.RoundToInt(storeSize.x / cellSize));
        gridResolutionY = Mathf.Max(1, Mathf.RoundToInt(storeSize.y / cellSize));

        gridData = new float[gridResolutionX, gridResolutionY];

        // Allocate Color32 array instead
        colorMap = new Color32[gridResolutionX * gridResolutionY];

        heatmapTexture = new Texture2D(gridResolutionX, gridResolutionY);
        heatmapTexture.filterMode = FilterMode.Bilinear;
        heatmapTexture.wrapMode = TextureWrapMode.Clamp;

        // Pre-fill with completely transparent Color32 (all zeros)
        Color32 clearColor = new Color32(0, 0, 0, 0);
        for (int i = 0; i < colorMap.Length; i++)
        {
            colorMap[i] = clearColor;
        }

        // Use SetPixels32 here as well
        heatmapTexture.SetPixels32(colorMap);
        heatmapTexture.Apply(false);

        float ppu = gridResolutionX / storeSize.x;
        heatmapSprite = Sprite.Create(heatmapTexture, new Rect(0, 0, gridResolutionX, gridResolutionY), new Vector2(0.5f, 0.5f), ppu);
        heatmapDisplay.sprite = heatmapSprite;

        heatmapDisplay.transform.position = new Vector3(storeOffset.x + (storeSize.x / 2f), storeOffset.y + (storeSize.y / 2f), -1f);
        heatmapDisplay.transform.localScale = Vector3.one;
    }

    public void ToggleHeatmapVisibility()
    {
        isMapVisible = !isMapVisible;
        heatmapDisplay.enabled = isMapVisible;
        EvaluateRenderState();
    }

    private void EvaluateRenderState()
    {
        if (isMapVisible)
        {
            // FIX: Start the texture update as a coroutine instead of a direct method call
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(UpdateHeatmapTextureSpread());
            }

            if (renderLoopCoroutine == null)
            {
                renderLoopCoroutine = StartCoroutine(RenderLoop());
            }
        }
        else
        {
            heatmapDisplay.enabled = isMapVisible;
            if (renderLoopCoroutine != null)
            {
                StopCoroutine(renderLoopCoroutine);
                renderLoopCoroutine = null;
            }
        }
    }

    // LOOP 1: Zero-Allocation Data Collection
    private IEnumerator SampleAgentsLoop()
    {
        var wait = new WaitForSeconds(sampleInterval);
        while (true)
        {
            // OPTIMIZATION 2: Iterate directly over the internal list. 
            // Removed .ToArray() to eliminate GC allocations per tick.
            List<AgentMovementEnhanced> agents = agentManager.allAgentList;
            int count = agents.Count;

            for (int i = 0; i < count; i++)
            {
                AgentMovementEnhanced agent = agents[i];
                if (agent == null || agent.isActiveAndEnabled == false) continue;

                Vector2 pos = agent.transform.position;

                float normalizedX = (pos.x - storeOffset.x) / storeSize.x;
                float normalizedY = (pos.y - storeOffset.y) / storeSize.y;

                int x = Mathf.FloorToInt(normalizedX * gridResolutionX);
                int y = Mathf.FloorToInt(normalizedY * gridResolutionY);

                if (x >= 0 && x < gridResolutionX && y >= 0 && y < gridResolutionY)
                {
                    float weight = intensityWeight;
                    if (agent.currentState == AgentMovementEnhanced.AgentState.BrowsingShelf)
                    {
                        weight *= 2f;
                    }

                    gridData[x, y] += weight;

                    if (gridData[x, y] > maxIntensity)
                    {
                        maxIntensity = gridData[x, y];
                    }
                }
            }
            yield return wait;
        }
    }

    // LOOP 2: Time-Slipped Intermittent Texture Blitting
    private IEnumerator RenderLoop()
    {
        var wait = new WaitForSeconds(sampleInterval);
        while (true)
        {
            // OPTIMIZATION 3: Spread the heavier texture color generation across multiple frames 
            // if your grid happens to scale up in size.
            yield return StartCoroutine(UpdateHeatmapTextureSpread());
        }
    }

    // OPTIMIZATION 3 & 4: Time-sliced iteration + SetPixelsData for maximum speed
    private IEnumerator UpdateHeatmapTextureSpread()
    {
        int rowsPerFrame = 15;
        Color32 clearColor = new Color32(0, 0, 0, 0);

        for (int y = 0; y < gridResolutionY; y++)
        {
            int rowOffset = y * gridResolutionX;

            for (int x = 0; x < gridResolutionX; x++)
            {
                float value = gridData[x, y];
                int index = rowOffset + x;

                if (value > 0)
                {
                    float normalizedValue = value / maxIntensity;
                    Color gradientColor = heatmapGradient.Evaluate(normalizedValue);

                    // Calculate alpha mapping (0.0 to 1.0) and clamp it
                    float alpha = Mathf.Clamp(normalizedValue * 1.5f, 0.2f, 0.8f);

                    // Convert the evaluated Color to Color32 smoothly
                    colorMap[index] = new Color32(
                        (byte)(gradientColor.r * 255),
                        (byte)(gradientColor.g * 255),
                        (byte)(gradientColor.b * 255),
                        (byte)(alpha * 255)
                    );
                }
                else
                {
                    colorMap[index] = clearColor;
                }
            }

            if (y > 0 && y % rowsPerFrame == 0)
            {
                yield return null;
            }
        }

        // Call the faster version
        heatmapTexture.SetPixels32(colorMap);
        heatmapTexture.Apply(false);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(storeOffset.x + (storeSize.x / 2f), storeOffset.y + (storeSize.y / 2f), 0);
        Gizmos.DrawWireCube(center, new Vector3(storeSize.x, storeSize.y, 0.1f));
    }
}