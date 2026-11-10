# CupkekGames Sequencer — AI Agent Instructions

## Package Overview

**CupkekGames Sequencer** (`com.cupkekgames.sequencer`) is the boot/initialization system. Deterministic startup order, with bridges to scene loading (`com.cupkekgames.scenemanagement`, its own package) and ServiceLocator registration.

## Critical: Do not hand-edit Unity serialized assets or `.meta` files

Apply scene/SO changes in Unity Editor; preserve `.meta` GUIDs.

## Package Structure

```
com.cupkekgames.sequencer/
  package.json
  README.md
  AGENTS.md
  Sequencer/                       ← CupkekGames.Sequencer.asmdef (+ Editor)
    Runtime/                         (boot sequencer; deterministic init order)
    Editor/
  Sequencer.SceneManagement/       ← CupkekGames.Sequencer.SceneManagement.asmdef (bridge: sequencer + scene loading)
    Runtime/
  Sequencer.ServiceLocator/        ← CupkekGames.Sequencer.ServiceLocator.asmdef (bridge: sequencer + service registration)
    Runtime/
```

## Dependencies

- `com.cupkekgames.scenemanagement`
- `com.cupkekgames.services`
- `com.cupkekgames.singletons`
- `com.cupkekgames.keyvaluedatabases`
- `com.unity.addressables` (optional, via `versionDefines`)

## Coding Conventions

- **Namespaces**: `CupkekGames.Sequencer` (all runtime asmdefs), `CupkekGames.Sequencer.Editor`
- **Asmdefs**: GUID references; `versionDefines` for `com.unity.addressables`
- **Strict typing**
