using UnityEngine;
using System.Collections;

public class HeatmapManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private Vector2 storeSize = new Vector2(50f, 30f); // Example: Rectangular store
    [SerializeField] private Vector2 storeOffset = Vector2.zero;
    [SerializeField] private float cellSize = 0.5f; // Size of each square cell in Unity world units

    // Replaces the old single gridResolution
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

    // Controls whether the texture actively updates and renders
    [SerializeField] private bool isMapVisible = false;

    private AgentManager agentManager; // Reference to the AgentManager to access all agents



    private Coroutine renderLoopCoroutine;

    private void Start()
    {
        agentManager = GetComponent<AgentManager>();

        InitializeHeatmap();

        // Data Collection Loop: Runs continuously in the background
        StartCoroutine(SampleAgentsLoop());

        // Conditional Render Loop: Only runs if visible at start
        EvaluateRenderState();
    }

    private void InitializeHeatmap()
    {
        // 1. Calculate proportional grid resolutions to guarantee perfect square cells
        gridResolutionX = Mathf.Max(1, Mathf.RoundToInt(storeSize.x / cellSize));
        gridResolutionY = Mathf.Max(1, Mathf.RoundToInt(storeSize.y / cellSize));

        gridData = new float[gridResolutionX, gridResolutionY];

        // 2. Create the texture with matching rectangular resolution aspect ratio
        heatmapTexture = new Texture2D(gridResolutionX, gridResolutionY);
        heatmapTexture.filterMode = FilterMode.Bilinear;
        heatmapTexture.wrapMode = TextureWrapMode.Clamp;

        // 3. Create the sprite mapping world units accurately
        // We use gridResolutionX / storeSize.x to define the Pixels Per Unit (PPU)
        float ppu = gridResolutionX / storeSize.x;
        heatmapSprite = Sprite.Create(heatmapTexture, new Rect(0, 0, gridResolutionX, gridResolutionY), new Vector2(0.5f, 0.5f), ppu);
        heatmapDisplay.sprite = heatmapSprite;

        // 4. Center and size the overlay to match the store rectangle boundaries
        heatmapDisplay.transform.position = new Vector3(storeOffset.x + (storeSize.x / 2f), storeOffset.y + (storeSize.y / 2f), -1f);
        heatmapDisplay.transform.localScale = Vector3.one; // 1:1 Scale because PPU perfectly matches world dimensions now!
    }

    /// <summary>
    /// Public method to toggle heatmap visibility from a UI Button or Keyboard hotkey
    /// </summary>
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
            // If turned on, force an immediate texture update and start the render loop
            UpdateHeatmapTexture();
            if (renderLoopCoroutine == null)
            {
                renderLoopCoroutine = StartCoroutine(RenderLoop());
            }
        }
        else
        {
            // If turned off, stop the render loop to save massive CPU/GPU overhead
            if (renderLoopCoroutine != null)
            {
                StopCoroutine(renderLoopCoroutine);
                renderLoopCoroutine = null;
            }
        }
    }

    // LOOP 1: Data Collection (Always active, lightweight matrix additions)
    private IEnumerator SampleAgentsLoop()
    {
        var wait = new WaitForSeconds(sampleInterval);
        while (true)
        {
            AgentMovementEnhanced[] agents = agentManager.allAgentList.ToArray(); // Access the list of all agents from AgentManager

            foreach (var agent in agents)
            {
                if (agent.isActiveAndEnabled == false) continue; // Skip inactive agents
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

    // LOOP 2: Texture Blitting (Only active when visible, saves performance when hidden)
    private IEnumerator RenderLoop()
    {
        var wait = new WaitForSeconds(sampleInterval);
        while (true)
        {
            UpdateHeatmapTexture();
            yield return wait;
        }
    }

    private void UpdateHeatmapTexture()
    {
        Color[] colorMap = new Color[gridResolutionX * gridResolutionY];
        for (int y = 0; y < gridResolutionY; y++)
        {
            for (int x = 0; x < gridResolutionX; x++)
            {
                float value = gridData[x, y];
                int index = y * gridResolutionX + x;

                if (value > 0)
                {
                    float normalizedValue = value / maxIntensity;
                    Color gradientColor = heatmapGradient.Evaluate(normalizedValue);
                    gradientColor.a = Mathf.Clamp(normalizedValue * 1.5f, 0.2f, 0.8f);
                    colorMap[index] = gradientColor;
                }
                else
                {
                    colorMap[index] = Color.clear;
                }
            }
        }

        heatmapTexture.SetPixels(colorMap);
        heatmapTexture.Apply();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = new Vector3(storeOffset.x + (storeSize.x / 2f), storeOffset.y + (storeSize.y / 2f), 0);
        Gizmos.DrawWireCube(center, new Vector3(storeSize.x, storeSize.y, 0.1f));
    }
}