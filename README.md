# CupkekGames Sequencer

Boot/initialization sequencer + scene management. Deterministic startup order, scene transitions, ServiceLocator integration.

## What's inside

- **`Sequencer/`** (CupkekGames.Systems.Sequencer.asmdef) — boot sequencer; runs initialization steps in deterministic order.
- **`Sequencer.SceneManagement/`** (CupkekGames.Systems.Sequencer.SceneManagement.asmdef) — bridge: sequencer steps that load/unload scenes.
- **`Sequencer.ServiceLocator/`** (CupkekGames.Systems.Sequencer.ServiceLocator.asmdef) — bridge: sequencer steps that register services.

(SceneManagement itself moved out into its own package `com.cupkekgames.scenemanagement`.)

## Dependencies

- `com.cupkekgames.scenemanagement`
- `com.cupkekgames.services`
- `com.cupkekgames.singleton`
- `com.cupkekgames.keyvaluedatabase`
