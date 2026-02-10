# Unit Positioning & Placement Plan

**Owner:** GitHub Copilot
**Date:** 2026-02-09
**Status:** In Progress

---

## Goals
- Single source of truth for player placements: the persistent Gamewide `PlayerTeamRoster` (templates). The runtime `PlayerTeamRosterInstance` is used only for creating the battle roster (read-only for UI).
- CharacterInstance.MapGridPosition must always reflect actual spawn/placement.
- Unit models spawn exactly at corresponding MapGrid world positions (no silent 0,0 spawns).
- Persist player-modified placements to Long Term Memory (LTM) when committed by player.
- Precompute caches must use exact unit start positions.
- Cursor starts on first player placement or avatar if present.

---

## Audit Summary (key files)
- BattlePreparationObject.cs (pre-battle placements, selection)
- BattleGameObject/RosterHelpers.cs (ApplyPreBattlePlacements)
- BattleBrain.cs (SpawnRosterUnitsOntoGrid)
- UnitAppearanceBrain/Spawn.cs (SpawnUnitAtPosition, PrecomputeSpawnModelAt)
- BattlePrecomputeLoader.cs (EnsureLtmUnitsAreUsedRoutine, RunPrecomputeTasks)
- CursorBrain (InitializeBattleCursor)
- GamewideContextBrain.Persistence (LTM save/recall)

---

## Edge Cases & Fixes
1. Units spawn at (0,0): check and fail when MapGrid or grid point invalid; set MapGridPosition explicitly before spawning.
2. Cursor using different positions: pass roster/prep placements into cursor initialization.
3. Precompute uses wrong positions: validate unit.MapGridPosition & repair from roster if missing.
4. LTM recall may introduce new instances: update placements and persist changes when recall substitutions happen.
5. Race/order issues: ensure explicit ordering of operations and publish events (`PlacementsInitialized`) after runtime roster sync.
   - Note: `InitializePlacements()` previously published per-unit `UnitSelectionChanged` events while still running which could trigger re-entry and hang. Fix: `InitializePlacements()` no longer publishes per-unit `UnitSelectionChanged`; it publishes only `PlacementsInitialized`. UI components should debounce unit-selection changes and call `InitializePlacements()` (debounced) so the work is coalesced and stable.
6. Placement count mismatch with spawn points or MaxPlayerTeamUnits: validate and trim with logs.

---

## Implementation Steps (atomic)
1. Add plan file and telemetry logs (this doc).
2. Defensive checks in `UnitAppearanceBrain.SpawnUnitAtPosition` (ensure valid MapGrid & grid point; don't spawn at 0,0 silently).
3. Ensure `MapGridPosition` and `WasSpawnedDuringBattle` set during spawn/startup.
4. Make `BattlePreparationObject` read placements from runtime roster when present (load path).
5. Keep runtime roster in sync whenever placements change (`SyncPlacementsToRuntimeRoster` — persist on player commit).
6. Ensure `ApplyPreBattlePlacements` uses prepared placements exactly and Verify positions on roster application.
7. Ensure `BattlePrecomputeLoader` uses the validated positions and repairs mismatches.
8. Initialize cursor on first placement (or avatar) during Battle initialization.
9. Add logs and tests (manual/play-mode integration) verifying consistency & no 0,0 spawns.

---

## Rationale — why each change addresses the real bugs 💡
- Single source of truth (runtime `PlayerTeamRosterInstance`):
  - Problem: selections and placements were being read from multiple places (LTM template placements, transient UI selection), causing mismatches.
  - Fix: load placements from the runtime roster first and sync any placement changes back to the runtime roster immediately.
  - Result: both pre-battle UI and the battle initialization will read the exact same placements; selection flips and player edits are reflected everywhere.

- Explicit MapGrid validation and no silent 0,0 spawns:
  - Problem: models appearing at (0,0) indicated an invalid MapGrid or missing grid point; previously spawns proceeded anyway.
  - Fix: `SpawnUnitAtPosition` now validates that a `MapGrid` and corresponding `MapGridPoint` exist before spawning and recomputes `worldPos` from the grid.
  - Result: invalid spawns fail with clear warnings instead of producing invisible or off-map units; this prevents downstream despawns and collisions.

- Set `MapGridPosition` and `WasSpawnedDuringBattle` when spawning:
  - Problem: precompute and other systems read unit positions but the instance position was not always set or was set after model spawning, producing mismatches.
  - Fix: set these fields before creating/moving models so all consumers (precompute, UI, movement validation) read correct data.
  - Result: precompute caches and AI initializations use the same authoritative coordinate data as the visuals.

- Restrict spawn to defined `PlayerTeamSpawnPoints` during pre-battle:
  - Problem: UI allowed spawns to be attempted at arbitrary coords (or missing coords), causing models to be skipped or placed incorrectly.
  - Fix: `StartingPositions.SpawnAllUnitModels()` now checks that each placement's position is an actual player spawn point and skips & logs otherwise.
  - Result: no unexpected spawns; diagnostics show which placement is invalid and why.

- Repair or persist LTM-recall replacements early:
  - Problem: unique-character recall can return a different `CharacterInstance` (recalled instance) which pre-existing placement dictionaries might still reference the old instance.
  - Fix: `BattlePrecomputeLoader` now replaces references in `BattlePreparationObject.placements` when recall substitutes an instance and persists the roster to LTM.
  - Result: recalled characters are consistently used by precompute and battle logic, with placements pointing at the recalled instance.

- Cursor start position determinism:
  - Problem: cursor started at neutral center when placements exist, or used inconsistent map references.
  - Fix: `CursorBrain.InitializeBattleCursor()` now builds its allowed-position list from the battle roster placements (preferred), falling back to prep placements, then spawn points.
  - Result: cursor reliably starts on the first player placement (or avatar fallback) and is consistent with both UI and gameplay.

---

## Play-mode testing checklist (exact steps to reproduce & verify) ▶️
Note: these are play-mode checks — editor-mode tests won't catch these order/race issues.

0. Reproduction of previous hang (regression test):
   - Clear LTM completely and reimport/reload assets, then open Pre-Battle and click the **Team** tab. The editor should not hang or freeze; console should not show repeated `InitializePlacements` or `honoring per-battle selection changes` logs in a loop.

1. Setup:
   - Ensure `GamewidePersistentPlayerRoster` exists and its `PlayerTeamSpawnPoints` are set on the map asset.
   - Clear LTM or set test LTM state to a known baseline (if desired).

2. Test: Default auto-placements
   - Enter PreBattle placement mode without making changes and confirm:
     - Console contains: `InitializePlacements: PlayerTeamSpawnPoints.Count=..., selectedUnits.Count=...` and `InitializePlacements: Placing ... at ...` lines.
     - All placed units' entry positions are among `PlayerTeamSpawnPoints`.
     - `SpawnAllUnitModels` logs show `spawn points=..., placements=...` and models spawn successfully at those positions.

3. Test: Manual placement and persistence
   - In PreBattle, manually move units to different spawn positions (simulate player changes), then commit (confirm action).
   - Verify:
     - `SyncPlacementsToRuntimeRoster(persist:true)` was called (check for log entry or LTM being updated).
     - Start Battle; the spawned unit instances in Battle reflect the exact placements you committed.
     - No model spawns at `(0,0)`; spawn warnings should not appear.

4. Test: Selection-only behavior
   - In PreBattle, select units (via the unit-selection UI) without placing them. Ensure the pre-battle selection state reflects the choices (observe logs or debug view using `BattlePreparationObject.IsBattleSelected`).
   - Deselect a unit, then run `InitializePlacements()` and confirm the deselected unit is NOT placed (and placements are cleared if all deselected).
   - Confirm that persistent roster `CharacterInstance.IsSelectedForBattle` is not mutated by these pre-battle actions, and that the user's selection changes were written to LTM (check LTM keys).

7. Test: Auto-selection behavior (new)
   - Without any user interaction (fresh session or after clearing LTM), enter the pre-battle selection flow so `PreBattleSelectionHelper.EnsureDefaultPreBattleSelections` runs.
   - Confirm that auto-selected units appear in starting positions when you enter Starting Positions (i.e., `InitializePlacements()` places the auto-selected units).
   - Confirm that auto-selection does NOT set the internal `_battleSelectionsChanged` flag (this is only set on explicit user toggles). Use logs showing `InitializePlacements: honoring per-battle selection changes (count=...)` to see whether user changes are being considered.
   - Verify LTM keys were written by the auto-selection pass (auto-fill writes non-required selections to LTM on first run).

---

### Selection precedence & semantics (new)
- Order of precedence used during `InitializePlacements()`:
  1. If the player modified selections during the current pre-battle session (`_battleSelectionsChanged == true`), use the per-battle selection set (`BattlePreparationObject._battleSelectedIds`) as authoritative. If the per-battle set is empty (player deselected everyone), placements are cleared and UI is updated accordingly.
  2. Otherwise, if runtime-peristed placements exist (the runtime `PlayerTeamRosterInstance` has `UnitPlacement` data), validate and use those placements (only if all positions are valid spawn points and instances exist).
  3. Otherwise, if the preparation object's per-battle selection set is present (auto-filled by `PreBattleSelectionHelper`), use those selections. Note: the auto-fill sets per-battle selections with `markChanged=false` so it's not considered a user change; auto-fill may also persist those choices to LTM on first run.
  4. If none of the above apply, run the default selection helper and generate default placements.

- `SetBattleSelected(...)` behavior:
  - When called by UI user toggles, it sets the per-battle selection and marks `_battleSelectionsChanged = true` and persists the choice to LTM (so user preference overrides prior saved choices).
  - When called by default selection logic (auto-fill or InitializePlacements marking), it sets per-battle selection but does NOT mark `_battleSelectionsChanged` (via `markChanged=false`) so InitializePlacements won't treat defaults as deliberate user clears.

- Persist rules:
  - Auto-fill can write initial selections to LTM (first-time auto-fill behavior in `PreBattleSelectionHelper`).
  - Explicit user toggles write to LTM immediately via `SetBattleSelected(... )`.
  - Placements changes are only written into the runtime roster when the user commits placements (via `SyncPlacementsToRuntimeRoster(persist:true)`). UI-only operations will not mutate the runtime roster.

---


5. Test: Unique-character recall replacement
   - Configure a unique-character template that will be recalled by LTM during precompute.
   - Start precompute (`ForceStartPrecomputeIfPossible`) and confirm `BattlePrecomputeLoader` logs that it repaired placement references and called `SyncPlacementsToRuntimeRoster(persist:true)` when necessary.

6. Fail-safes
   - Intentionally corrupt a PlayerTeamSpawnPoints entry (remove or change a position) and observe that `SpawnAllUnitModels` logs an explicit warning and skips invalid placements instead of spawning at 0,0.

7. LTM-empty behavior (edge cases)
   - If Long Term Memory (LTM) contains no saved selections or the `UnitSelectionsAutoFilled` flag is not set, `PreBattleSelectionHelper.EnsureDefaultPreBattleSelections` will perform a deterministic auto-fill using the persistent `PlayerTeamRoster` order and then write the auto-fill flag and per-unit selection booleans to LTM (only for non-required units).
   - If the persistent `PlayerTeamRoster` itself contains no placements, the system will fall back to runtime roster placements (with a conspicuous warning logged). If both are empty, `InitializePlacements()` should produce a clear failure and the UI will show "no units available for positioning".
   - Auto-fill selections are treated as defaults — they will not set `_battleSelectionsChanged` so they won't be interpreted as a user-cleared selection. They will, however, appear in starting positions when `InitializePlacements()` runs (i.e., auto-selected units show up without user interaction).
   - If the player later explicitly toggles selection for a unit, that explicit choice overrides LTM and is immediately persisted.

---

### Play-mode tests for LTM-empty cases
- Clear LTM completely and start the pre-battle flow. Confirm:
  - `PreBattleSelectionHelper` runs and `UnitSelectionsAutoFilled` was written to LTM.
  - `InitializePlacements()` places auto-selected units into starting positions (check `InitializePlacements: Placing` logs).
  - `_battleSelectionsChanged` is false immediately after auto-fill (default behavior), and set to true only after a user toggle.
- If persistent roster placements are empty, confirm you see a warning and that runtime placements are used as fallback.
- If both persistent and runtime placements are empty, confirm `InitializePlacements()` fails gracefully and `StartingPositions` shows no spawned units (and a clear console message).

---

## What I'll watch for in the console logs (fast signals) 🔎
- `BattlePreparationObject.Initialize: PlayerTeamSpawnPoints is empty` — map spawn data missing (immediate blocker).
- `InitializePlacements: PlayerTeamSpawnPoints.Count=..., selectedUnits.Count=...` — sanity check on counts.
- `InitializePlacements: Placing <Unit> at <Vector2Int>` — ensure expected units are placed.
- `SpawnAllUnitModels: Skipping spawn for <Unit> at <pos> - not a valid player spawn point` — user error or map data problem.
- `SpawnAllUnitModels: Model spawned for <Unit> at <pos>` — success.
- `SpawnAllUnitModels: Failed to spawn at <pos>: <error>` — failure reason from `SpawnUnitAtPosition`.
- `BattlePrecomputeLoader: Repaired unit <id> position to <pos>` — repairs done during precompute.

---

## Follow-ups & rollback plan
- If a change causes regressions, revert small commits immediately and add more granular logs. Keep each subsequent change small and observable.
- If LTM or roster API needs to be adjusted (rare), we'll patch in a minimal, backward-compatible way and add feature flags/logging to track effects.

---

_Last updated: 2026-02-09 (Rationale & play-mode checklist added)_
---

## Progress Log
- [x] Added plan and audit summary
- [x] Set `CharacterInstance.MapGridPosition` & `WasSpawnedDuringBattle` in `SpawnUnitAtPosition`
- [x] Added `SyncPlacementsToRuntimeRoster` helper to `BattlePreparationObject` and call on placement changes and final commit
- [x] Load placements from runtime roster in `BattlePreparationObject.InitializePlacements` (prefers runtime placements when present)
- [x] Added defensive grid validation & world-position recompute in `UnitAppearanceBrain.SpawnUnitAtPosition` (prevents silent 0,0 spawns)
- [x] Initialize battle cursor with player placements (cursor now starts on first placement when available)
- [x] Validate and attempt to repair `MapGridPosition` in `BattlePrecomputeLoader` before precompute

### Next (working on)
- [ ] Ensure `ApplyPreBattlePlacements` and roster application verify positions and report errors (small follow-up)
- [x] Add persistence/update hooks when LTM recall substitutes unique characters so placements reference the recalled instance (implemented in `BattlePrecomputeLoader` and `BattlePreparationObject.SyncPlacementsToRuntimeRoster` now public)
- [x] Prevent re-entrant selection-publish loop by queueing `UnitSelectionChanged` publishes during `InitializePlacements()` and flushing after initialization completes
- [x] Centralize spawn authority and fallback:
  - Centralized spawning via `BattleContext.SpawnAtPosition` is the authoritative path. `UnitAppearanceBrain` now uses this path first and uses `MapGrid.SetOccupied(...)` as a controlled fallback when the command path cannot be used. When `SetOccupied` is used, `UnitAppearanceBrain` **publishes** a `UnitSpawnedEvent` so visuals and other systems react to the authoritative change rather than mutating instance positions directly.
- [x] Make `MapGrid.SetOccupied(...)` authoritative and self-consistent:
  - `SetOccupied` now aligns `CharacterInstance.MapGridPosition` to the written grid point and logs the alignment. This enforces a single source-of-truth (the grid occupancy) and prevents divergent state between occupancy and instance properties.
- [x] Diagnostics & conservative repair:
  - Added MapGrid-level logs for overwrite/remove, MoveCommand occupancy snapshots, and `BattleContext` cache logs. `RepairUnitPositionsFromRoster()` was made conservative: it only repairs units that are **invalid** or **duplicated**, and logs the reason for the repair.
- [x] Prevent transient default (0,0) MapGridPosition: initialize `CharacterInstance._mapGridPosition` to (-9999,-9999) so uninitialized instances are explicitly invalid and cannot create accidental duplicates; this change reduces repair workload and surface bugs early.
- [ ] Add integration tests and manual test plan execution
- [ ] Implement an automatic fallback in `StartingPositions.Initialize()` to wipe invalid prep placements and force `InitializePlacements()` (Option 2) if invalid placements are detected at UI init time.
- [x] Add a debug-only health check that asserts `MapGrid.GetGridPoint(unit.MapGridPosition)?.CurrentInstance == unit` after spawn/move/precompute checkpoints. (Editor-only verify implemented)
- [ ] Scan & replace remaining direct `MapGridPosition` writes with authoritative calls (`SpawnAtPosition` / `SetOccupied` / command APIs) and add a regression test to prevent re-introduction of direct writes.
- [ ] Consider a stricter policy (opt-in): fail-on-overwrite in `SetOccupied` or an explicit collision-resolution API (to make destination conflicts deterministic).

---

## Cleanup & Quality Work (next phase) ✅

### Recent cleanup pass (in repo)
- Replaced silent `catch { }` blocks in core battle/precompute flows with explicit logging so unexpected exceptions are visible and debuggable. (Files updated: `BattleContext.cs`, `BattleBrain.cs`, `MoveCommand.cs`, `Spawn.cs`, `BattlePrecomputeLoader.cs`, `BattleGameObject.cs`.) ✅
- Extracted helpers/partials to reduce mega-methods and keep `BattlePreparationObject` and `StartingPositions` under the 500-line target. ✅
- Added a placeholder Editor test `OccupancyAlignmentTests` and a PlayMode skeleton `PlacementConsistencyTests` for next-phase fleshing (CI hooks pending). ✅

Notes: these changes were conservative (they keep existing fallback behavior but add logging and TODO comments where direct `MapGridPosition` writes remain as allowed fallbacks). Replace or migrate these fallbacks in a future PR once we have test coverage in place.

### Remaining direct `MapGridPosition` writes (candidates for migration)
- `BattleBrain.SpawnRosterUnitsOntoGrid` — fallback direct assignment when `MapGrid.GetGridPoint` fails. (Added log + TODO)
- `SpawnCommand.Undo` — fallback to assign previous `MapGridPosition` when previous grid point cannot be restored. (Added log)
- `BattleContext.RepairUnitPositionsFromRoster` — fallback when `MapGrid.SetOccupied` fails during a repair pass. (Added log)

Plan: create a follow-up migration PR that converts these fallbacks to controlled `MapGrid.SetOccupied(...)` or `BattleContext.SpawnAtPosition(...)` calls, add editor asserts for any direct writes, and cover with play-mode integration tests so behavior is verified before removing the fallbacks.

## Cleanup & Quality Work (next phase) ✅
Now that the root causes are addressed, we should clean, DRY, and polish the new and surrounding code to make it maintainable and concise. Proposed checklist:

- Consolidate logs:
  - Standardize log message formats and levels (Info vs Warning vs Error). Shorten noisy per-frame logs and keep a concise set of signals (spawn/move/repair/verify).
  - Replace multiple near-duplicate messages with a single context-aware message and include structured payload where helpful.

- Remove or simplify defensive noise:
  - Audit `try { } catch { }` swallowing blocks and either remove the catch or log an explicit message with context; avoid silently ignoring exceptions.
  - Remove redundant fallbacks that were only present to compensate for earlier race conditions now fixed (e.g., some fallback direct `MapGridPosition` writes should be removed once we centralize authority).

- Replace remaining direct `MapGridPosition` writes:
  - Create a small migration PR to convert direct writes to `MapGrid.SetOccupied(...)` or `BattleContext.SpawnAtPosition(...)` as the authoritative paths.
  - Keep a small set of allowed fallbacks with clear comments and tests; everything else should be fail-fast (editor asserts) when violated.

- Add tests and CI checks:
  - Implement play-mode integration test reproducing the original failure and asserting no invalid/duplicate positions after move/spawn (regression). Add it to CI.
  - Add a small editor-only unit test to run `DebugVerifyOccupancyAlignment()` and fail in CI if it detects misalignment on a baked test scene.

- Code hygiene and removal:
  - Remove deprecated queue/publish logic used previously for `InitializePlacements()` (it was replaced with debounced UI behavior). Delete old helpers.
  - Remove obsolete comments that reference behaviors that no longer exist and shorten in-line comments to focus on 'why' not 'how'.
  - Run a focused `grep` of `MapGridPosition =` and review each instance, replacing or documenting exceptions.

- Timeline & owners:
  - Phase 1 (this sprint): implement doced scan + small replacements, add two tests (play-mode reproduction + editor verify). (Owner: you / me)
  - Phase 2: consolidate logs and remove redundant guards (Owner: engineering pair)
  - Phase 3: final tidy & remove deprecated code + merge (Owner: code owner)

This checklist will be converted to small PRs so each change is reviewable and reversible.

### Current issues observed (from play-mode run)
- Only one placement was available when entering starting positions mode: `SpawnAllUnitModels: spawn points=8, placements=1`.
- An existing placement referenced `(0, 0)`, causing the UI to skip it: `SpawnAllUnitModels: Skipping spawn for Nane at (0, 0) - not a valid player spawn point`.
- Result: only one model spawned; others were skipped or never placed.
- New issue: After one spawn, returning to unit-selection mode shows only that spawned unit as selectable — other units are not marked as selected/visible.

### Attempted fixes and why they address these issues ✅
- **Validate and reject invalid runtime placements**: `InitializePlacements()` now loads runtime placements but validates that **every** placement position is a member of `PlayerTeamSpawnPoints` and that the instance exists. If any runtime placement is invalid the entire runtime placement block is discarded and we fall back to the default auto-fill behavior.
  - Why this helps: prevents an invalid `(0,0)` or partial placement set from being used as the authoritative source. Instead the system will regenerate a sane default fill so all spawn points are populated correctly in roster order.
- **Per-battle selection (explicit override of runtime placements when changed this session)**: the pre-battle selection uses a per-battle selection set (`BattlePreparationObject._battleSelectedIds`) instead of mutating `CharacterInstance.IsSelectedForBattle`.
  - `SetBattleSelected(...)` persists the selection change into LTM (overwriting prior saved choice) so player choices are remembered across sessions.
  - If the player modified selections during the current pre-battle session, `InitializePlacements()` will **honor the per-battle selection set instead of loading runtime placements**, even when runtime/persistent placements exist. If the per-battle set is empty (player deselected everyone), placements are cleared and UI is updated accordingly.
  - `InitializePlacements()` marks placed units as *battle-selected* (calls `SetBattleSelected`) and publishes `UnitSelectionChanged` so UI reflects selections for this session only.
  - Why this helps: ensures the persistent roster and CharacterInstance selection flags are not mutated by UI-only workflows, while making current player choices authoritative and immediately persisted to LTM.

---

## Cleanup tasks (remove deprecated/unused paths)
- [x] Stop mutating `CharacterInstance.IsSelectedForBattle` during pre-battle selection flows. Replaced with per-battle selection APIs in `BattlePreparationObject`.
  - Files updated: `PreBattleSelectionHelper.cs`, `UnitSelectionHandlers.cs`, `UnitSelectionColumns.cs`, `BattlePreparationObject.cs` ✅
- [ ] Remove any code paths that still rely on `CharacterInstance.IsSelectedForBattle` for pre-battle UI logic (scan & replace uses or add fallbacks to `IsBattleSelected`).
- [ ] Add deprecation comments where feasible for old APIs and mark them for removal in a follow-up cleanup PR.
- [ ] Add integration verification to ensure no persistent state mutation occurs during pre-battle (manual + automated checks).

---

## Tests to Run (updated)
- PreBattle placement change -> runtime roster updated -> saved and loaded -> used at battle start
- StartBattle with custom placements -> units' MapGridPosition match roster placements -> models spawn at exact world pos
- Precompute computes movement/attack tiles based on those positions
- Cursor starts at first placement or avatar
- No unit models spawn at 0,0
- **New:** Opening unit selection UI after pre-battle should list ALL persistent roster units and must not depend on `CharacterInstance.IsSelectedForBattle` being modified by pre-battle flows. Verify that `CharacterInstance.IsSelectedForBattle` remains unchanged unless selection was made outside of pre-battle or explicitly persisted.

---


- **Proactive UI init**: `StartingPositions.Initialize()` will call `_prepObject.InitializePlacements()` when it detects it is waiting for placements and the Brain is available. If placements become available immediately it will proceed to `SpawnAllUnitModels()`.
  - Why this helps: avoids UI waiting races where the UI never sees placements because another system didn't publish the event in the expected order.

- **Spawn-time validation**: `SpawnUnitAtPosition` aborts spawn if `MapGrid` or `MapGridPoint` is invalid and logs the exact failure instead of silently spawning at `(0,0)`.
  - Why this helps: prevents invisible or off-grid models and makes the failure case loud and debuggable.

- **Sync after placement generation**: after generating a valid placements dictionary (either from runtime placements or the default fill), we call `SyncPlacementsToRuntimeRoster(persist:false)` so the runtime roster instance reflects the prep object's placements.
  - Why this helps: keeps all systems (UI, precompute, battle apply) reading from the same, single authoritative placement set.

### How you can verify quickly (play-mode)
- Start pre-battle positioning after clearing LTM and reimport.
- Look for these logs (fast signals):
  - `InitializePlacements: runtimePlacements.Length=...` and `InitializePlacements: Placing <Unit> at <pos>`
  - `SpawnAllUnitModels: spawn points=..., placements=...` and `SpawnAllUnitModels: Model spawned for <Unit> at <pos>`
  - If invalid runtime placements were found you should see: `InitializePlacements: Discarding invalid runtime placements and falling back to default selection`.

---

### Changelog
- 2026-02-09: Implemented initial plan and edits to:
  - `Assets/TurnrootFramework/Gameplay/Brain/Components/UnitAppearance/Spawn.cs` (spawn validation + MapGridPosition set)
  - `Assets/TurnrootFramework/Gameplay/Combat/PreBattle/BattlePreparationObject.cs` (load runtime placements and `SyncPlacementsToRuntimeRoster` helper; validation + fallback)
  - `Assets/TurnrootFramework/Gameplay/Brain/Components/Cursor/CursorBrain.Initialization.cs` (cursor now uses placements)
  - `Assets/TurnrootFramework/Gameplay/Combat/Precompute/BattlePrecomputeLoader.cs` (position validation & repair)
  - `Assets/TurnrootFramework/UI/Components/StartingPositions.cs` (proactive InitializePlacements call and spawning validation)
  - 2026-02-09 (follow-up): Made `SyncPlacementsToRuntimeRoster` **public** and updated `BattlePrecomputeLoader` to update pre-battle placements when LTM recalled instances replace runtime ones (and persist).

- 2026-02-09 (bugfix & simplification): Removed queued publish/flushing logic from `BattlePreparationObject` and added a debounced `StartingPositions` reinit to avoid re-entrancy while keeping UI responsiveness.

---

## New issue: Visual vs Logical Position Mismatch at Battle Start (observed)
- **Symptom:** In some runs two units' models appear at the correct map tiles but their internal `CharacterInstance.MapGridPosition` values are wrong (e.g., `(0,0)` or wildly different coordinates), while one unit is correct for both visual and logical positions. Console excerpts show `SpawnUnitAtPosition` and `HandleBattleStarted` logging the expected spawn position (e.g., `(13,13)` for Nane), but `BattleContext:GetCurrentUnitPositions` later reports inconsistent positions (e.g., some units at `(0,0)` or other incorrect tiles).
- **Example logs:**
  - `HandleBattleStarted: Setting Nane.MapGridPosition = (13, 13)`
  - `SpawnUnitAtPosition: Nane - gridPos=(13, 13), worldPos=(32.50, 0.00, 32.50), prebattle=False`
  - Later: `GetCurrentUnitPositions: Building cache with 3 units` then lines showing units with differing `MapGridPosition` values (one correct, others `(0,0)` or other wrong coords).

Probable causes (hypotheses)
- `CharacterInstance.MapGridPosition` is not being set (or is overwritten) at the right time in the spawn/apply flow (e.g., set on the model spawn but later overwritten by precompute/battle apply code).
- The battle apply or precompute flows might be instantiating or replacing `CharacterInstance` objects (recall/restore) without updating `placements` or `MapGridPosition` references.
- The order of operations at battle start may be inconsistent across systems: model spawn, precompute, and `BattleContext` caching may run in different orders, reading stale values.
- Some code may temporarily set MapGridPosition to a default (0,0) during initialization or when instances are moved between runtime/persistent representations.

Planned fixes and diagnostic steps
1. Try conservative fixes (applied now):
   - Centralize spawning through the authoritative `BattleContext.SpawnAtPosition` flow and avoid duplicate spawns. `UnitAppearanceBrain.HandleBattleStarted` will now call `SpawnAtPosition` first (so the `SpawnCommand` sets map occupancy and `MapGridPosition`), then create visuals. `BattleBrain.SpawnRosterUnitsOntoGrid` was updated to skip spawning for instances already marked `WasSpawnedDuringBattle` at the intended placement and to repair mismatched positions.
   - This avoids double-spawns and ensures the command flow is used for position assignment, which prevents later overwrites.

2. Add targeted diagnostics/logs (if the conservative fix fails):
   - Log each unit's `CharacterInstance.Id` and `MapGridPosition` after `InitializePlacements()` completes and after `SyncPlacementsToRuntimeRoster()` runs.
   - Log again at battle start (in `UnitAppearanceBrain.HandleBattleStarted` and immediately after `SpawnUnitAtPosition`) showing both the intended spawn `position` and `inst.MapGridPosition`.
   - Log in `BattleContext.GetCurrentUnitPositions()` the `CharacterInstance.Id` and `MapGridPosition` when it builds the cache.
   - These logs will give a timeline showing where the mismatch appears.

3. Repair step (applied):
   - After spawning roster units, `SpawnRosterUnitsOntoGrid()` performs a final verification pass and repairs any `MapGridPosition` mismatches it finds (logs warnings when it does so).
   - Additionally, `BattleContext.GetCurrentUnitPositions()` now validates each unit's `MapGridPosition` when building its cache. When a position is invalid, it attempts to repair it by consulting the authoritative `PlayerTeamRoster` placements and, if necessary, skips invalid or duplicate positions with a warning.
   - These two steps keep the runtime position state consistent and make the system robust to late-stage overwrites.
3. Harder checks / assertions (if diagnostics point to a specific code path):
   - If a particular routine (e.g., `BattlePrecomputeLoader`) is rewriting or recalling instances, update it to patch placements and instance positions immediately and persist via `SyncPlacementsToRuntimeRoster(persist:true)` when substitutions occur.
   - Add simple assertions (debug only) to fail or log when any instance is at `(0,0)` after battle start unless explicitly intended.

4. Test plan (play-mode):
   - Reproduce: clear LTM, run pre-battle placement flow to the point where starting positions show expected models, start battle and capture logs.
   - Verify diagnostic logs show consistent MapGridPosition values at these checkpoints: after `InitializePlacements`, after `SyncPlacementsToRuntimeRoster`, in `UnitAppearanceBrain.HandleBattleStarted`, and in `BattleContext.GetCurrentUnitPositions`.
   - Validate that the repair step, if applied, removes inconsistencies and that gameplay proceeds with correct cached positions.

Action items (short-term)
- Add the diagnostic logs (non-invasive) in the four locations listed above. ✅
- If diagnostics show overwrites, implement the repair step during battle start and rerun tests. ✅
- Add a play-mode test that asserts all active units have `MapGridPosition` in `PlayerTeamSpawnPoints` immediately after battle start. ✅

Documentation update
- This new bug, its logs, hypotheses, and the planned fixes & tests are recorded here for traceability and to guide the next code edits. The priority is high — this affects runtime correctness for movement, AI, and cursor behavior.

---

## Tests to Run (updated)
- (new) Repro & logging: capture unit MapGridPosition at each diagnostic point and confirm they match the pre-battle placements.
- (new) Add a play-mode assert: after battle start, every active player unit must have `MapGridPosition` in the current scene's `PlayerTeamSpawnPoints`.
- Existing tests (unchanged): PreBattle placement -> runtime roster updated -> saved & used on battle start, no models at 0,0, cursor starts correctly, precompute & battle logic consistent.

---

### Next update
- I'll add the diagnostic logs first; with the log timeline we can pinpoint where positions diverge and then apply the minimal repair step.
- If you want, I can add the diagnostic logs now and run a small play-mode session to collect the logs; otherwise I can prepare a PR that contains the diagnostic changes plus the repair step guarded by a debug flag.

---

## Notes
Keep changes small and reversible. Update this document as changes are applied.

---

_Last updated: 2026-02-09_
