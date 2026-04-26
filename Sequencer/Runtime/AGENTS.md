# Sequencer Runtime Guidance

## Purpose

This package provides a simple SO-based startup/runtime sequencing system.

Core concept:

- `SequenceGroupSO` is the list-of-sequences object (also usable as a nested node).
- `SequenceRunner` is the MonoBehaviour runner for sequences (starts on `Awake` by default).
- `SequencerSessionMarkComplete` pre-marks **`OncePerPlaySession`** nodes only. **`SequenceGroupSO` list entries are traversed recursively, but the group asset itself is not marked** — mixed groups still run; only session-gated steps skip. Session skips apply **only** to `OncePerPlaySession` (the set is ignored for `Always` even if something called `MarkCompletedThisSession` on it).

## Contracts

- Sequencer nodes are **SO only**.
- Implement nodes by inheriting `SequencerNodeSO`.
- Each node asset has **Execution Policy** (inspector on the SO):
  - **Always** — runs whenever the sequence runs (default).
  - **OncePerPlaySession** — runs at most once per Enter Play Mode session; state resets on the next play. Use when the same SO is assigned to `SequenceRunner`s in **multiple scenes** so global bootstrap (registry, services, etc.) does not repeat on every load. To treat nodes as done **without executing** them (e.g. bootstrap ran in another scene), use `SequencerSession.MarkCompletedThisSession` or add `SequencerSessionMarkComplete` in the scene.
  - **Session reset** — completion is stored in a static set cleared on **`BeforeSceneLoad`** each time you enter Play Mode (before the first scene’s `Awake`), so it stays correct even with **Enter Play Mode Options → Disable Domain Reload**. (Older `SubsystemRegistration`-only resets could miss a play cycle when the domain is not reloaded.)
- `ISequencerNode` has:
  - `Execute(SequencerRuntime runtime)`
  - `Dispose(SequencerRuntime runtime)`

`Dispose` is **deferred** until the owning **`SequenceRunner`** is **destroyed** (`OnDestroy`), or until **`StartSequence`** runs again (previous run is flushed first). Nodes dispose in **reverse completion order** (nested children before parents). If `Execute` throws, that node is still queued for dispose the same way.

### Execution tracing

- Filter the Console by **`SequencerExec`** to see root steps, nested **Group child** lines, **`BEGIN` / `END`** (actually ran), and **`SKIP`** (session pre-mark / OncePerPlaySession).
- Disable logs on **`SequenceRunner`** via **Log Execution** (inspector).

## Composition Rules

- `SequenceGroupSO` may contain any `SequencerNodeSO`.
- Nested `SequenceGroupSO` is supported.
- Runtime has cycle protection; cyclic references are logged as errors and skipped.

### Pre-marking groups

- Listing a **`SequenceGroupSO`** walks its nested list and marks **only** descendants whose policy is **`OncePerPlaySession`**. The **group** is not added to the session set, so `SequenceGroupSO.Execute` still runs and drives children; **`Always`** children execute normally.
- To pre-skip **all** session-gated work under a runner root, assign the **same** `SequenceGroupSO` on `SequencerSessionMarkComplete` as on `SequenceRunner` (or list individual `OncePerPlaySession` SOs).
- **Two `InstantiatePrefabNodeSO`s (deps vs scene UI):** if the first is **`OncePerPlaySession`** and you **pre-mark** it with `SequencerSessionMarkComplete` on a **cold** scene (no earlier bootstrap), it **will not run** and `Instantiate()` on the second prefab can throw in `Awake` (e.g. missing `INamedDatabaseProvider` “Items”). Only pre-mark dependency instantiation when those services already exist from an **earlier scene in the same play session**, or keep the dependency step **`Always`** on first-load paths, or remove it from the mark list for that scene.

## Built-in Nodes

- `WaitForServiceSO`: waits for a service type to appear in `ServiceLocator`.
- `RegisterServiceRegistryNodeSO`: `ServiceRegistrySO` list, `ServiceProviderSO` assets, plain `ScriptableObject` services (same registration order as `ServiceRegistry` minus MonoBehaviour components). Optional unregister when the sequence run is disposed.
- `SceneLoaderStartupNodeSO`: build-index load via `SceneLoader` + `SceneTransitionDatabase` transition key — sequencer version of `SceneLoaderStartup` (fire-and-forget; pair with `CompleteDeferredLoadingTransitionNodeSO` when using deferred fade-out).
- `SceneLoaderAddressableStartupNodeSO` (**UNITY_ADDRESSABLES**): load `SceneSO` list via `SceneLoaderAddressable` + transition key; optional `SetActiveScene` on first scene (like `InitializationLoader`). Pair with `CompleteDeferredLoadingTransitionNodeSO` target **SceneLoaderAddressable** when deferring fade-out. MonoBehaviour equivalent: **`SceneLoaderAddressableStartup`** in SceneManagement.
- `InstantiatePrefabNodeSO`: instantiates prefabs (optional `DontDestroyOnLoad`, optional **destroy on dispose**). With deferred disposal, **destroy on dispose** removes instances when the **`SequenceRunner`** is destroyed (or when a **new** `StartSequence` flushes the previous run), not when the step’s coroutine returns.
- `SetGameObjectActiveNodeSO`: sets `GameObject.SetActive` by **name** in the **active scene** (matches root objects first, then optional deep search so **inactive** roots still resolve). Use as the **last** step to enable a disabled **content root** after services/registry steps.
- `CompleteDeferredLoadingTransitionNodeSO`: tries `TryCompleteDeferredLoadingTransition()` on the chosen loader; optional **cold start** fallback `FadeOutTransitionByKey` when no deferred load ran (e.g. **Play** on MainMenu only — no init `SceneLoader` load). Optional **delay** (unscaled seconds).

### Deferred loading transition

When loading with a `SceneTransition` (fade / loading UI), you can keep the **loading presentation visible** after the scene has finished loading until bootstrap/sequencer work completes:

- **Addressables** (`SceneLoaderAddressable`): pass `deferFadeOutUntilManualComplete: true` on `LoadScene`, `LoadSceneRequest`, `LoadSceneAndUnLoadCurrent`, `UnLoadScene`, or `UnloadAllCurrent`. When the load queue finishes, the transition is **not** faded out until `SceneLoaderAddressable.CompleteDeferredLoadingTransition()` runs — use the sequencer node **after** init (e.g. after `SetGameObjectActiveNodeSO`).
- **Build index** (`SceneLoader`): pass `deferFadeOutUntilManualComplete: true` on `LoadScene(string|int, …)`. Then call `SceneLoader.CompleteDeferredLoadingTransition()` (same sequencer node, target **SceneLoaderBuildIndex**). `OnSceneLoad` fires when the deferred fade-out runs.

### Inactive content root pattern

- Put gameplay/UI under a root that is **disabled in the scene** so `Awake`/`Start` on those objects do not run until sequencing finishes.
- Add a `SetGameObjectActiveNodeSO` asset as the **final** node with that root’s **name** in `Target Names` and `Active` checked.
- Keep `SequenceRunner` on an **always-active** object (or early in the hierarchy) so the coroutine can run.

## Authoring Guidelines

- Keep node behavior focused and single-purpose.
- Prefer creating new nodes over adding many toggles to one node.
- Avoid direct scene assumptions inside nodes unless explicit in node name and description.
- Use `Dispose` only for cleanup that should be paired with node execution.

## Runner Usage

- Put `SequenceRunner` in startup scenes where sequencing should begin.
- Assign one or more `SequencerNodeSO` assets in order.
- Keep game-specific startup logic in nodes, not in the runner.

## Dependency Direction

- Runner depends on nodes.
- Nodes may depend on `ServiceLocator`/registries.
- This assembly references `CupkekGames.Systems.SceneManagement` for `CompleteDeferredLoadingTransitionNodeSO` only.
- Nodes should not depend on specific scene-only component references unless designed for that.

## Scope Notes

- This system is intentionally lightweight and not a full DI container.
- Use it to gate/sequence initialization, not to replace all runtime architecture.

## Initialization patterns (any first scene)

Older ad-hoc “Option B” init-order notes are superseded by this sequencer for **ordering** (services, registry, prefabs, enabling roots). The following ideas still apply at the **scene** level:

- **Do not assume readiness in `Awake` / `OnEnable` / `Start` for global services** — use `WaitForServiceSO`, register nodes first, or subscribe to readiness where your game defines it.
- **Inactive content root** — keep gameplay/UI under a disabled root until a late sequencer step enables it (see *Inactive content root pattern* above). That replaces a purely code-driven “wait for `GameSequencer.OnReady`” base class for many setups.
- **`RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`** can still run one-off static bootstrap if you need something before the first scene’s `Awake`; prefer sequencer nodes when order must be visible and editable in assets.

Follow-ups are tracked directly in code/tasks; do not rely on separate planning markdown files in `Samples~/`.
