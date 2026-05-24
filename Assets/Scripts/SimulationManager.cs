using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [SerializeField] private SupermarketLayoutSO layoutData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        layoutData.ResetLayout(); // Reset the layout at the start of the simulation to clear any occupied slots from previous runs
    }
}
