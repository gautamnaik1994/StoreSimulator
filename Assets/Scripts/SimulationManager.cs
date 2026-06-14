using UnityEngine;
using UnityEngine.InputSystem;

public class SimulationManager : MonoBehaviour
{
    [SerializeField] private SupermarketLayoutSO layoutData;

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

    void LateUpdate()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            CycleSimulationSpeed();
        }
    }

    private void CycleSimulationSpeed()
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
}
