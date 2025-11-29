# BattleGameObject (runtime component)

Small, focused notes for the `BattleGameObject` component used by the runtime combat systems.

Summary
- `BattleGameObject` is a lightweight MonoBehaviour that holds battle-scoped references (BattleContext, MapGrid, battle conditions) and exposes a `Brain` property so it can subscribe to high-level Brain events.
- It enforces required children (e.g. `MapGrid`) and warns when key data is missing.

Key responsibilities
- Maintain per-battle state: turn counter, conditions, and a `BattleContext` instance.
- Integrate with the global `Brain`: call `ConnectToBrainEvents()` to subscribe to battle lifecycle and turn events (start/end, pre-battle, turns, exit).
- Provide a single place for scene-level battle wiring — UI, AI systems and camera sequencers should listen to the Brain events instead of polling each battle object.

API notes
- `Brain` property — assign or find the `Brain` instance for the scene and call `ConnectToBrainEvents()`.
- Turn helpers: `IncrementTurnCount()`, `ResetTurnCount()`, `Turns()` (current turn index)

Practical wiring
- Add `BattleGameObject` to a scene root that has `EnvironmentalConditions` and a `MapGrid` child.
- Hook the Brain's events for UI updates, AI decision ticks, and end-of-battle logic (for example `OnStartBattle`, `OnExitBattle(BattleExitType)`).

See also
- Gameplay / Brain — `Assets/TurnrootFramework/Gameplay/Brain/Brain.cs` (Brain event hub)
- Source: `Assets/TurnrootFramework/Gameplay/Combat/FundamentalComponents/Battles/BattleGameObject.cs`
