using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // Required for scene handling
using UnityEngine.UIElements;


public class SimulationManager : MonoBehaviour
{
    [SerializeField] private SupermarketLayoutSO layoutData;
    public UIDocument uiDocument;

    private VisualElement controlSettingsContainer;
    private Button settingsButton, settingsCloseButton;

    private Button resetAllButton;

    private enum SimulationSpeed
    {
        Normal = 1,      // 1x
        Paused = 0,      // Pause
        SlowMotion = -1, // 0.5x
        FastForward = 2  // 2x
    }

    private SimulationSpeed currentSpeed = SimulationSpeed.Normal;

    void Awake()
    {
        layoutData.ResetLayout(); // Reset the layout at the start of the simulation to clear any occupied slots from previous runs
        Time.timeScale = 1f;
    }

    void Start()
    {
        controlSettingsContainer = uiDocument.rootVisualElement.Q<VisualElement>("settingsContainer");
        settingsButton = uiDocument.rootVisualElement.Q<Button>("Settings");
        resetAllButton = uiDocument.rootVisualElement.Q<Button>("resetAll");
        settingsCloseButton = uiDocument.rootVisualElement.Q<Button>("ControlClose");
        controlSettingsContainer.style.display = DisplayStyle.None; // Initially hide the world settings container
        settingsButton.clicked += () =>
        {
            Debug.Log("Settings button clicked");
            if (controlSettingsContainer.style.display == DisplayStyle.Flex)
            {
                Debug.Log("Inside the if condition, showing world settings");
                controlSettingsContainer.style.display = DisplayStyle.None;
            }
            else
            {
                Debug.Log("Inside the if condition, hiding world settings");
                controlSettingsContainer.style.display = DisplayStyle.Flex;
            }
        };
        settingsCloseButton.clicked += () =>
        {
            controlSettingsContainer.style.display = DisplayStyle.None;
        };
        resetAllButton.clicked += RestartScene;
    }


    public void CycleSimulationSpeed()
    {
        // Cycle: Normal (1x) → Paused (0x) → SlowMotion (0.5x) → FastForward (2x) → Normal
        currentSpeed = currentSpeed switch
        {
            SimulationSpeed.Normal => SimulationSpeed.Paused,
            SimulationSpeed.Paused => SimulationSpeed.SlowMotion,
            SimulationSpeed.SlowMotion => SimulationSpeed.FastForward,
            SimulationSpeed.FastForward => SimulationSpeed.Normal,
            _ => SimulationSpeed.Normal
        };

        Time.timeScale = currentSpeed switch
        {
            SimulationSpeed.Normal => 1f,
            SimulationSpeed.Paused => 0f,
            SimulationSpeed.SlowMotion => 0.5f,
            SimulationSpeed.FastForward => 2f,
            _ => 1f
        };
    }

    public void RestartScene()
    {
        // Reloads the currently active level
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
