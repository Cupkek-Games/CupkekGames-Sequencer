# CupkekGames Sequencer — AI Agent Instructions

## Package Overview

**CupkekGames Sequencer** (`com.cupkekgames.sequencer`) is the boot/initialization + scene management system. Deterministic startup order, scene transitions, ServiceLocator integration.

## Critical: Do not hand-edit Unity serialized assets or `.meta` files

Apply scene/SO changes in Unity Editor; preserve `.meta` GUIDs.

## Package Structure

```
com.cupkekgames.sequencer/
  package.json
  README.md
  AGENTS.md
  Sequencer/                       ← CupkekGames.Systems.Sequencer.asmdef
    Runtime/                         (boot sequencer; deterministic init order)
    Editor/
  Sequencer.SceneManagement/       ← bridge: sequencer + scene loading
    Runtime/
  Sequencer.ServiceLocator/        ← bridge: sequencer + service registration
    Runtime/
  SceneManagement/                 ← CupkekGames.Systems.SceneManagement.asmdef
    Runtime/                         (scene transitions)
    Editor/
```

## Dependencies

- `com.cupkekgames.scenemanagement`
- `com.cupkekgames.services`
- `com.cupkekgames.singletons`
- `com.cupkekgames.keyvaluedatabases`
- `com.cupkekgames.luna` (transition UI overlays — for samples)
- `com.cupkekgames.addressableassets` (optional, Addressables-based scene loading)

## Coding Conventions

- **Namespaces**: `CupkekGames.Systems.Sequencer`, `CupkekGames.Systems.SceneManagement`, etc. (match asmdef name)
- **Asmdefs**: GUID references; `versionDefines` for `com.unity.addressables`
- **Strict typing**
