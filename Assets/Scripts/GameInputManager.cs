using UnityEngine;
using UnityEngine.InputSystem;

public class GameInputManager : MonoBehaviour
{
    private InputSystem_Actions inputActions;
    private AgentManager agentManager;
    private CrowdBuster crowdBuster;

    private SimulationManager simulationManager;
    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        agentManager = gameObject.GetComponent<AgentManager>();
        crowdBuster = gameObject.GetComponent<CrowdBuster>();
        simulationManager = gameObject.GetComponent<SimulationManager>();
        var simulationMap = inputActions.Simulation;
        var cameraMap = inputActions.Camera;
        simulationMap.Spawn.performed += OnSpawnPressed;
        simulationMap.AgentDetails.performed += OnGetAgentDetailsPressed;
        simulationMap.CrowdBuster.performed += OnCrowdBusterPressed;
        simulationMap.Speed.performed += OnSpeedPressed;
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        var simulationMap = inputActions.Simulation;
        simulationMap.Spawn.performed -= OnSpawnPressed;
        simulationMap.AgentDetails.performed -= OnGetAgentDetailsPressed;
        simulationMap.CrowdBuster.performed -= OnCrowdBusterPressed;
        simulationMap.Speed.performed -= OnSpeedPressed;
        inputActions.Disable();
    }

    private void OnSpawnPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Spawn action triggered");
        agentManager.SpawnAgents();
    }

    private void OnGetAgentDetailsPressed(InputAction.CallbackContext context)
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        agentManager.GetAgentDetailsOnClick(mousePosition);
    }

    private void OnCrowdBusterPressed(InputAction.CallbackContext context)
    {
        Debug.Log("CrowdBuster action triggered");
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        crowdBuster.TriggerExplosionAtMouse(mousePosition);
    }

    private void OnSpeedPressed(InputAction.CallbackContext context)
    {
        Debug.Log("Speed action triggered");
        simulationManager.CycleSimulationSpeed();
    }
}