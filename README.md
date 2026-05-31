Supermarket Store Simulator
===========================

A Unity-based simulator to model a supermarket / retail store layout and validate new product placements for layout optimization, shopper flow, and checkout performance.

Key features
-

- Simulate shoppers (agents) moving through a store layout with aisles, product sections, points-of-interest (POIs), checkouts, and exits.
- Evaluate new product placements by tracking dwell time, pickups, path overlap, and conversion metrics.
- Configurable store layouts via the `SupermarketLayoutSO` asset and prefabs for aisles, shelves, POIs, checkouts, and agents.
- Headless-friendly data export for automated experiments and analysis.

Quick Start
-

1. Install Unity (project tested with Unity 2021.3 LTS or later; open `StoreSimulator.slnx` in the Unity Editor).
2. Open the project folder in Unity and load the scene: `SampleScene` (Assets/Scenes/SampleScene.unity).
3. In the Hierarchy, select `SimulationManager` to configure simulation parameters (agent count, seed, experiment duration).
4. Configure your layout via the `SupermarketLayoutSO` ScriptableObject (Assets/Scripts/SupermarketLayoutSO.asset) or use the provided layouts in `Assets/Prefabs`.
5. Press Play to run the simulation in the Editor. For batch experiments, configure exports in `SimulationManager` and run headless builds.

How it works (overview)
-

- Layout representation: store geometry, aisles, POIs, and checkouts are defined with prefabs and a `SupermarketLayoutSO` asset.
- Agents: shoppers are spawned with behavior defined in `AgentMovement.cs` and managed by `AgentManager.cs`.
- Events & metrics: interactions (pickups, dwell, queueing at checkout) are recorded by `CheckoutHandler.cs`, `POI.cs`, and `SimulationManager.cs`.
- Output: metrics and event logs are written to the project's output folder (configurable) for post-processing.

Experimenting with new product placement
-

1. Add or edit a POI prefab (`Assets/Prefabs/POI.prefab`) to represent the new product or endcap.
2. Place the POI in your layout (scene) or add it in the `SupermarketLayoutSO` asset.
3. Configure attractiveness, stock levels, and visibility on the POI component to influence agent choice.
4. Run multiple randomized trials (vary seeds and agent counts) and collect exported metrics.
5. Analyze results for uplift in pickups, conversion rate, average time-to-pickup, and impact on traffic patterns.

Configuration & tuning
-

- `SimulationManager.cs` — core experiment parameters: `agentCount`, `experimentDuration`, `randomSeed`, export path.
- `AgentManager.cs` / `AgentMovement.cs` — tweak movement speed, pathfinding, and decision heuristics.
- `POI.cs` / `ProductSection.cs` — configure attractiveness, inventory, and product categories.
- `CheckoutHandler.cs` / `CheckoutCounter.cs` — model queueing, service times, and register configuration.

Assets and important files
-

- `Assets/Prefabs/agent.prefab` — shopper agent prefab.
- `Assets/Prefabs/POI.prefab` — point-of-interest prefab for products, displays, and promotional endcaps.
- `Assets/Prefabs/Checkout.prefab` — checkout counter prefab.
- `Assets/Scripts/SimulationManager.cs` — orchestrates runs and exports.
- `Assets/Scripts/SupermarketLayoutSO.cs` and `SupermarketLayoutSO.asset` — layout definition asset.
- `Assets/Scenes/SampleScene.unity` — working example layout and default experiment settings.
