using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] private CameraController cameraController;
    private TextElement agentName, agentMood, agentStatus, agentProfession, agentAge, agentGender, agentIncomeLevel, moneySpent, moneyAvailable, shoppingList, impulseList, costlyItems, cartContents, agentHistory, agentType, primaryTrait;

    private VisualElement rightDrawer, statusIcon, moodIcon;

    private ProgressBar moneySpentBar;

    private Button closeButton;

    private AgentMovementEnhanced selectedAgent;

    private PersonaDatabase database;

    private int agentIDCounter = 0; // To assign unique IDs to agents

    private WorldManager worldManager;

    private ListView agentHistoryListView;

    private float timer = 0f;
    private float interval = 5.0f; // Run code every 5 seconds

    private Image agentIconStatusRing, agentIcon;

    private Button spawnAgentsButton, settingsButton;

    private bool isSimulationRunning = false;

    private VisualElement worldSettingsContainer;

    public AudioSource crowdAudioSource;

    private IntegerField shopperCountInputField;
    public List<AgentMovementEnhanced> allAgentList = new List<AgentMovementEnhanced>();

    void GetTextElementByID(string id, out TextElement textElement)
    {
        textElement = uiDocument.rootVisualElement.Q<TextElement>(id);
        if (textElement == null)
        {
            Debug.LogError($"TextElement with ID '{id}' not found in the UI Document.");
        }
    }

    void Start()
    {

        cameraController = gameObject.GetComponent<CameraController>();
        worldManager = gameObject.GetComponent<WorldManager>();


        // GetTextElementByID("agentHistory", out agentHistory);
        GetTextElementByID("agentName", out agentName);
        GetTextElementByID("agentMood", out agentMood);
        GetTextElementByID("agentStatus", out agentStatus);
        GetTextElementByID("agentProfession", out agentProfession);
        GetTextElementByID("agentAge", out agentAge);
        GetTextElementByID("agentGender", out agentGender);
        GetTextElementByID("agentIncomeLevel", out agentIncomeLevel);
        GetTextElementByID("moneySpent", out moneySpent);
        GetTextElementByID("moneyAvailable", out moneyAvailable);
        GetTextElementByID("shoppingList", out shoppingList);
        GetTextElementByID("impulseList", out impulseList);
        GetTextElementByID("costlyItems", out costlyItems);
        GetTextElementByID("cartContents", out cartContents);
        GetTextElementByID("agentType", out agentType);
        GetTextElementByID("primaryTrait", out primaryTrait);

        rightDrawer = uiDocument.rootVisualElement.Q<VisualElement>("RightDrawer");
        closeButton = uiDocument.rootVisualElement.Q<Button>("Close");
        closeButton.clicked += CloseAgentDetails;
        database = Resources.Load<PersonaDatabase>("PersonaDatabase");
        moneySpentBar = uiDocument.rootVisualElement.Q<ProgressBar>("moneySpentBar");
        agentHistoryListView = uiDocument.rootVisualElement.Q<ListView>("AgentHistoryList");
        statusIcon = uiDocument.rootVisualElement.Q<VisualElement>("statusIcon");
        moodIcon = uiDocument.rootVisualElement.Q<VisualElement>("moodIcon");
        agentIconStatusRing = uiDocument.rootVisualElement.Q<Image>("ring");
        agentIcon = uiDocument.rootVisualElement.Q<Image>("triangle");
        spawnAgentsButton = uiDocument.rootVisualElement.Q<Button>("startSimulation");
        worldSettingsContainer = uiDocument.rootVisualElement.Q<VisualElement>("worldSettings");
        shopperCountInputField = uiDocument.rootVisualElement.Q<IntegerField>("shopperCount");
        settingsButton = uiDocument.rootVisualElement.Q<Button>("Settings");
        shopperCountInputField.RegisterValueChangedCallback(evt =>
        {
            agentCount = evt.newValue;
            Debug.Log($"Shopper Count changed to: {agentCount}");
        });
        agentCount = shopperCountInputField.value; // Initialize agentCount with the current value of the input field

        spawnAgentsButton.clicked += () =>
        {
            // hide the container
            SpawnAgents();
            crowdAudioSource.Play();
            worldSettingsContainer.style.display = DisplayStyle.None;
        };

        settingsButton.clicked += () =>
        {

        };
    }

    void LateUpdate()
    {
        timer += Time.deltaTime;
        if (timer >= interval && selectedAgent != null && rightDrawer.style.display == DisplayStyle.Flex)
        {
            timer = 0f;
            ForceUpdateAgentDetails();

        }

    }

    void CloseAgentDetails()
    {
        rightDrawer.style.display = DisplayStyle.None;
    }

    public void UpdateAgentDetails(AgentDetails agentData)
    {
        var historyEntries = agentData.HistoryEntries ?? new List<AgentHistoryEntryData>();
        agentName.text = agentData.AgentName;
        agentMood.text = agentData.CurrentMood;
        agentStatus.text = agentData.CurrentState;
        agentProfession.text = agentData.AgentProfession;
        agentAge.text = agentData.AgentAge.ToString();
        agentGender.text = agentData.AgentGender;
        agentIncomeLevel.text = agentData.AgentIncomeLevel;
        moneySpent.text = agentData.TotalMoneySpent.ToString();
        moneyAvailable.text = agentData.RemainingMoney.ToString();
        shoppingList.text = agentData.ShoppingList;
        impulseList.text = agentData.ImpulseFavorites;
        costlyItems.text = agentData.CostlyItems;
        cartContents.text = agentData.CartItems;
        moneySpentBar.highValue = agentData.BaselineMoney;
        moneySpentBar.value = agentData.TotalMoneySpent;
        statusIcon.style.backgroundColor = agentData.StateColor;
        moodIcon.style.backgroundColor = agentData.MoodColor;
        agentIconStatusRing.tintColor = agentData.StateColor;
        agentIcon.tintColor = agentData.MoodColor;
        agentType.text = agentData.AgentType.ToString();
        primaryTrait.text = agentData.PrimaryTrait.ToString();


        agentHistoryListView.bindItem = (element, index) =>
        {
            if (index < 0 || index >= historyEntries.Count)
            {
                return;
            }

            var entryData = historyEntries[index];
            var descriptionLabel = element.Q<Label>("description");
            var timestampLabel = element.Q<Label>("timestamp");
            var agentStateLabel = element.Q<Label>("agentState");
            var agentMoodLabel = element.Q<Label>("agentMood");

            descriptionLabel.text = entryData.ActionDescription;
            timestampLabel.text = $"Time: {entryData.Timestamp:F2}s";
            agentStateLabel.text = entryData.State;
            agentMoodLabel.text = entryData.Mood;
            SetBorderColor(agentStateLabel, entryData.StateColor);
            SetBorderColor(agentMoodLabel, entryData.MoodColor);

            // var label = element.Q<Label>("history_item");
            // label.text = $"[{entryData.Timestamp:F2}s] State: {entryData.State}, Mood: {entryData.Mood}, Action: {entryData.ActionDescription}";
        };

        agentHistoryListView.itemsSource = historyEntries;
        agentHistoryListView.Rebuild();


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
            // toggle off previous selection indicator if we had one
            if (selectedAgent != null)
            {
                selectedAgent.ToggleSelectionIndicator(false);
            }
            selectedAgent = closestAgent;

            cameraController.SetSelectedAgent(selectedAgent.gameObject);
            selectedAgent.ToggleSelectionIndicator(true); // Show the selection indicator on the selected agent
            var agentData = selectedAgent.GetAgentDetails();
            rightDrawer.style.display = DisplayStyle.Flex;
            UpdateAgentDetails(agentData);

        }
    }



    private System.Collections.IEnumerator BatchSpawnCoroutine()
    {
        float budgetModifier = worldManager.GetBudgetModifier();
        float purchaseLikelihoodModifier = worldManager.GetGlobalPurchaseLikelihoodModifier();
        float browseTimeModifier = worldManager.GetGlobalBrowseTimeModifier();
        float speedModifier = worldManager.GetGlobalSpeedModifier();

        AgentModifiers modifiers = new AgentModifiers
        {
            BudgetModifier = budgetModifier,
            PurchaseLikelihoodModifier = purchaseLikelihoodModifier,
            BrowseTimeModifier = browseTimeModifier,
            SpeedModifier = speedModifier
        };

        int batches = Mathf.CeilToInt((float)agentCount / agentBatchCount);
        for (int i = 0; i < batches; i++)
        {
            int agentsToSpawn = Mathf.Min(agentBatchCount, agentCount - (i * agentBatchCount));
            for (int j = 0; j < agentsToSpawn; j++)
            {
                AgentPersonaData profile = database.personas[agentIDCounter % database.personas.Count]; // Loop through personas if we have more agents than profiles
                // AgentPersonaData profile = database.personas[Random.Range(0, database.personas.Count)]; // Randomly select a persona for each agent
                if (profile == null)
                {
                    Debug.LogError($"Persona profile is null for agent ID {agentIDCounter}. Check the PersonaDatabase.");
                    continue; // Skip this agent if the profile is null
                }
                GameObject obj = Instantiate(agentPrefab, spawnPoint.position, Quaternion.identity);
                AgentMovementEnhanced agent = obj.GetComponent<AgentMovementEnhanced>();
                allAgentList.Add(agent); // Add the agent to the list of all agents

                agent.InitializeWithPersona(profile, agentIDCounter, modifiers);
                agentIDCounter++;
            }
            yield return new WaitForSeconds(delay);
        }


    }

    private void SetBorderColor(VisualElement element, Color color)
    {
        element.style.borderBottomColor = color;
        element.style.borderTopColor = color;
        element.style.borderLeftColor = color;
        element.style.borderRightColor = color;
    }
    void ForceUpdateAgentDetails()
    {
        if (selectedAgent is null) return;
        var agentData = selectedAgent.GetAgentDetails();
        UpdateAgentDetails(agentData);
    }
}




