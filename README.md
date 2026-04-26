# CupkekGames Sequencer

Boot/initialization sequencer + scene management. Deterministic startup order, scene transitions, ServiceLocator integration.

## What's inside

- **`Sequencer/`** (CupkekGames.Systems.Sequencer.asmdef) — boot sequencer; runs initialization steps in deterministic order.
- **`Sequencer.SceneManagement/`** (CupkekGames.Systems.Sequencer.SceneManagement.asmdef) — bridge: sequencer steps that load/unload scenes.
- **`Sequencer.ServiceLocator/`** (CupkekGames.Systems.Sequencer.ServiceLocator.asmdef) — bridge: sequencer steps that register services.
- **`SceneManagement/`** (CupkekGames.Systems.SceneManagement.asmdef) — scene transition system.

## Dependencies

- `com.cupkekgames.core`
- `com.cupkekgames.luna` (transition UI)
- `com.cupkekgames.data` (ServiceLocator integration)
- `com.cupkekgames.addressables` (Addressables-based scene loading)
