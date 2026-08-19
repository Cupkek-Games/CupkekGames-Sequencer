# CupkekGames Sequencer

Boot/initialization sequencer + scene management. Deterministic startup order, scene transitions, ServiceLocator integration.

## What's inside

- **`Sequencer/`** (CupkekGames.Systems.Sequencer.asmdef) — boot sequencer; runs initialization steps in deterministic order.
- **`Sequencer/Editor/`** (CupkekGames.Sequencer.Editor.asmdef) — Shader Variant Log Importer window (Tools › CupkekGames › Sequencer): imports a development build's `Player.log` shader-compilation lines into a `ShaderVariantCollection` for `WarmUpShadersNodeSO`, and prunes editor-only shaders.
- **`Sequencer.SceneManagement/`** (CupkekGames.Systems.Sequencer.SceneManagement.asmdef) — bridge: sequencer steps that load/unload scenes.
- **`Sequencer.ServiceLocator/`** (CupkekGames.Systems.Sequencer.ServiceLocator.asmdef) — bridge: sequencer steps that register services.

(SceneManagement itself moved out into its own package `com.cupkekgames.scenemanagement`.)

## Dependencies

- `com.cupkekgames.scenemanagement`
- `com.cupkekgames.services`
- `com.cupkekgames.singletons`
- `com.cupkekgames.keyvaluedatabases`
