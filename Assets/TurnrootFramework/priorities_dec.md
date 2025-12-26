18. Reusable Tile Dictionaries Not Cleared on Turn End
Issue: Stale data if unit defeated before acting
Fix: Clear _reusableMoveTiles and _reusableAttackTiles in TurnRotisserie.Progress()
19. GetAllAdjacent() Allocates List
Issue: yield return creates enumerator allocation
Fix: Add GetAllAdjacentNonAlloc(List<CharacterInstance> result) variant
20. Battle Conditions Cache Never Invalidates
Issue: Cached enemy lists stale when units spawn/die
Fix: Call condition.InvalidateCache() in OnUnitDefeated and OnUnitSpawned
1. BattleContext God Object
Issue: 30+ public properties, knows too much
Fix: Split into UnitContext, SkillContext, CombatFlags, BattleParticipants sub-objects
3. Snapshot Memory Safety
Issue: Restoring units that were removed from battle mid-fight causes null refs
Fix: Validate unit still exists in battle context before restoring state
4. AI Tile Cache Never Invalidates on Map Changes
Issue: Cache only invalidates on unit movement, not terrain/door/bridge changes
Fix: Add StateVersion counter to MapGrid, invalidate cache when version changes
6. Formation Bonus on Wrong Actions
Issue: Formation bonus applies even when moving away from allies
Fix: Check if GainPosition goal increases distance from allies before applying bonus
7. Pathfinding Recalculated Per Goal
Issue: Multiple goals trigger redundant A* computations same turn
Fix: Cache at turn level (keyed by TurnNumber), not evaluation level
8. String Interpolation in Hot Paths
Issue: Debug.Log allocations even when stripped in builds
Fix: Wrap all AI logging in [Conditional("UNITY_EDITOR")] or #if UNITY_EDITOR
9. LINQ in Gameplay Code
Issue: .All(), .Any(), .Where() cause GC allocations
Fix: Replace with manual for-loops in battle condition checks
10. No Condition Dependencies
Issue: Can't express "Condition B requires Condition A first"
Fix: Add BattleCondition[] RequiredConditions with AreRequirementsMet() check
11. No AND/OR Logic
Issue: Can't express "Defeat Boss OR Survive 10 Turns"
Fix: Create ConditionalGroup with AllMustPass/AnyCanPass enum
12. No Runtime Condition Creation
Issue: Can't add conditions mid-battle (reinforcement waves)
Fix: Add AddConditionAtRuntime(BattleCondition) to BattleGameObject
13. Ledger Keys Predictable
Issue: Attacker can brute-force common character IDs
Fix: Add device-specific salt to ledger key generation
14. Base64 Not Encryption
Issue: Save files trivially decoded
Fix: Add simple XOR cipher with device key. Don't store device key- use a deterministic method to generate it at runtime from device and hardware info. 
15. Snapshot Doesn't Handle Mid-Battle Unit Removal
Issue: Summoned/reinforcement units crash snapshot restore
Fix: Add "WasSpawned" flag to UnitState, skip restore for units not in original snapshot