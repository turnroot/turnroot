# Player Turn System: Consolidated Implementation Guide

## Current State Assessment

**✅ Solid Foundation:**
- State machine architecture (`PlayerTurnState` with validation)
- Command pattern integration (undo/redo ready)
- Combat math layer (`DamageCalculator` - your most complete system)
- Event-driven Brain architecture
- Persistence layer with roster management

**🚧 Partially Built:**
- `BattleInputControllerBrain` (skeleton exists, needs implementation)
- Turn rotation (missing player turn wait logic)
- UI system (infrastructure exists, not wired to battle)

**❌ Missing Critical Pieces:**
- Tile validation with actual pathfinding
- Input-to-action translation
- Combat/movement previews
- Action menu system

---

## Critical Path: What's Blocking You Now

### 1. GetUnitAtPosition() - **Start Here**
**Location:** `BattleInputControllerBrain.cs`

**Current:** Returns null, blocking everything.

**Fix:**
```csharp
private CharacterInstance GetUnitAtPosition(MapGridPoint position)
{
    // Option A: Simple linear search (fine for <50 units)
    var allUnits = BattleContext.Participants.GetAllUnits(); // Add this helper
    return allUnits.FirstOrDefault(u => u.MapGridPosition == position.CoordinatesInt);
    
    // Option B: Maintain Dictionary<Vector2Int, CharacterInstance> that updates on moves
    // Better performance, but needs cache invalidation
}
```

**Add to BattleParticipants:**
```csharp
public List<CharacterInstance> GetAllUnits()
{
    var result = new List<CharacterInstance>();
    result.AddRange(Allies);
    result.AddRange(Targets);
    result.AddRange(ThirdParty);
    return result;
}
```

---

### 2. CalculateValidTiles() - Wire Up Pathfinding
**Location:** `BattleInputControllerBrain.cs`

**Current:** Empty with TODO.

**What You Need:**
- Movement range from unit stats (add `GetMovementRange()` to CharacterInstance)
- Pathfinding via your existing `BattleContextAIHelper`
- Attack range calculation from equipped weapon

**Implementation Steps:**
1. Get unit's movement stat → convert to tile range
2. Call `_aiHelper.CalculateReachableTiles(unit, currentPos, movementRange)`
   - If this method doesn't exist, check what pathfinding methods AIHelper exposes
   - You likely have something like `GetReachableTilesForUnit()` already
3. For attack tiles: iterate reachable tiles, project weapon range from each
4. Cache results in `_validMoveTiles` and `_validAttackTiles`

**Clear cache whenever:**
- Unit changes
- BattleContext changes
- Player cancels back to NoUnitSelected

---

### 3. Previous State Tracking
**Location:** `PlayerTurnFlow.cs`

**Add:**
```csharp
private PlayerTurnStates _previousState = PlayerTurnStates.Inactive;

public PlayerTurnStates GetPreviousState() => _previousState;

// In TransitionToState(), before changing CurrentState:
_previousState = CurrentState;
```

This enables proper cancel/undo behavior.

---

### 4. Input Buffering
**Location:** `BattleInputControllerBrain.Update()`

**Current:** Polls every frame, will cause double-inputs.

**Add:**
```csharp
private float _lastInputTime;
private const float INPUT_COOLDOWN = 0.15f; // Tune this

void Update()
{
    if (Time.time - _lastInputTime < INPUT_COOLDOWN) return;
    
    if (_navigateAction?.WasPressedThisFrame() == true)
    {
        _lastInputTime = Time.time;
        // ... rest of logic
    }
}
```

---

## Phase-by-Phase Roadmap

### Phase 1: Basic Turn Flow (Week 1)

**Goal:** Select unit → move → wait → turn ends

#### Step 1.1: Complete Navigation
**File:** `BattleInputControllerBrain.HandleNavigateInput()`

**Tasks:**
- Convert Vector2 input to grid direction (snap to cardinal/diagonal)
- Calculate new cursor position
- Validate against mapGrid bounds
- Apply state-based constraints (only valid move tiles if in move selection)
- Update cursor visual position

**Test:** Cursor moves on grid, respects boundaries.

---

#### Step 1.2: Complete Confirm Input
**File:** `BattleInputControllerBrain.HandleConfirmInput()`

**State-by-State Logic:**

**NoUnitSelected:**
- Check if cursor over player unit → call `_playerTurnFlow.SelectUnit()`

**NoActionChosen:**
- Open action menu (see Phase 2)

**MoveActionChosenChoosingDestination:**
- Validate tile with `ValidateTileSelection()`
- Store destination
- Transition to `MoveActionChosenDestinationSelected`

**MoveActionChosenDestinationSelected:**
- Transition to `ConfirmAction`

**ConfirmAction:**
- Execute command via `BattleContext.MoveUnitToPoint()`
- Transition to `ExecutingAction`
- After animation (stub with delay), call `CompletePlayerTurn()`

---

#### Step 1.3: Complete Cancel Input
**File:** `BattleInputControllerBrain.HandleCancelInput()`

**Key Transitions:**
- From ActionChosen states → back to NoActionChosen
- From DestinationSelected → back to Choosing
- From ConfirmAction → RequestUndo() and restore state

**Don't forget:** Clear cached tile data on cancel.

---

#### Step 1.4: Wire Turn Completion
**File:** `TurnRotisserie.cs`

**Changes:**
```csharp
void Awake()
{
    _brain.OnPlayerTurnCompleted += HandlePlayerTurnCompleted;
}

private void HandlePlayerTurnCompleted(CharacterInstance unit)
{
    if (GetActiveUnit() == unit)
    {
        Progress(); // Continue rotation
    }
}
```

**File:** `BattleInputControllerBrain.cs`

**Add:**
```csharp
private void CompletePlayerTurn()
{
    ClearCachedData();
    _brain.PublishPlayerTurnCompleted(SelectedUnit);
    _playerTurnFlow?.TransitionToState(PlayerTurnStates.Inactive);
}
```

**Test:** Two player units, first moves, second auto-activates.

---

### Phase 2: Action Menu (Week 2)

#### Step 2.1: Create Menu Structure
**New File:** `BattleActionMenu.cs`

**Needs:**
- Enum for actions (Attack, Item, Wait, Trade, etc.)
- List of menu items with enable/disable state
- Navigation methods (up/down)
- Selection callback

**New File:** `ActionMenuItemUI.cs`

**Needs:**
- Text display
- Icon (optional)
- Highlight visual
- Disabled state visual

---

#### Step 2.2: Determine Available Actions
**In BattleActionMenu:**

**Logic:**
- Attack: Has weapon AND enemies in range
- Item: Has usable items in inventory
- Wait: Always available
- Trade/Rescue/Talk: Check for valid targets adjacent

**Helper Methods:**
- `HasEnemiesInRange(unit, weapon, context)` - check targets within weapon.UpperRange
- `HasUsableItems(unit)` - filter inventory for IsUsable items
- `HasAdjacentAllies(unit, context)` - for trade/rescue

---

#### Step 2.3: Wire to Input Controller
**File:** `BattleInputControllerBrain.OpenActionMenu()`

**Tasks:**
- Instantiate menu prefab
- Build menu with available actions
- Subscribe to OnActionSelected
- Position near unit

**File:** `HandleActionSelected(BattleAction action)`

**Switch on action:**
- Attack → transition to `AttackActionChosenChoosingTarget`, calculate attack tiles
- Item → transition to `UseItemActionChosenChoosingItem`, show item menu
- Wait → call `_playerTurnFlow.WaitAndEndTurn()`, complete turn immediately

**Test:** Menu shows, selecting Attack highlights valid targets, selecting Wait ends turn.

---

### Phase 3: Combat System (Week 3)

#### Step 3.1: Attack Target Selection
**File:** `BattleInputControllerBrain.HandleConfirmInput()`

**Add case for AttackActionChosenChoosingTarget:**
- Validate target with `ValidateTargetSelection()`
- Store target
- Transition to `AttackActionChosenTargetSelected`

**File:** `ValidateTargetSelection()`

**Checks:**
- Target is CharacterInstance (not null)
- Target is enemy (BattleContext.IsTarget())
- Target is in weapon range
- Line of sight (optional, depends on your rules)

---

#### Step 3.2: Combat Forecast
**New File:** `CombatForecast.cs` (static helper)

**Method:** `Calculate(attacker, defender, context) → CombatForecastData`

**Data Structure:**
```csharp
public class CombatForecastData
{
    public int AttackerDamage, DefenderDamage;
    public float AttackerHit, DefenderHit;
    public float AttackerCrit, DefenderCrit;
    public int AttackerAttacks, DefenderAttacks; // 1 or 2
    public bool DefenderCanCounter;
    public bool AttackerDies, DefenderDies;
}
```

**Use your existing DamageCalculator methods:**
- CalculatePotentialDamage()
- CalculateHitChance()
- CalculateCriticalChance()
- CalculateAttackCount()
- CanCounterAttack()
- WouldKill()

---

#### Step 3.3: Forecast UI
**New Prefab:** CombatForecastPanel

**Components:**
- Two columns (attacker/defender)
- Each shows: Portrait, Name, HP, Dmg, Hit%, Crit%
- Color HP red if would die
- Show "x2" if double attack
- Show "--" if can't counter

**File:** `CombatForecastPanel.cs`

**Methods:**
- `Display(attacker, defender, forecast)`
- `Show()` / `Hide()`

**Wire in:** `BattleInputControllerBrain.ShowCombatForecast()`
- Call when cursor over valid target
- Update on cursor move
- Hide on cancel

---

#### Step 3.4: Execute Attack
**File:** `BattleInputControllerBrain.ExecuteConfirmedAction()`

**Logic:**
```csharp
if (previous state was AttackTargetSelected)
{
    var result = BattleContext.AttackTarget(SelectedUnit, _selectedTarget);
    if (result.Success)
    {
        // Play animation (stub for now with delay)
        StartCoroutine(WaitForAnimation(2f, CompletePlayerTurn));
    }
}
```

**Test:** Attack → see forecast → confirm → damage applies → turn ends.

---

### Phase 4: Preview Systems (Week 4)

#### Step 4.1: Movement Path Visualization
**New File:** `MovementPathVisualizer.cs`

**Two Approaches:**
1. **Line Renderer:** Simple, smooth, less FE-authentic
2. **Arrow Sprites:** More work, looks like Fire Emblem

**Method:** `ShowPath(List<MapGridPoint> path)`

**Needs:**
- Pathfinding from current pos to cursor (A*)
- Clear path when cursor moves or state changes

**Integration:** Call from `UpdatePreviewsForCursorPosition()` when in move selection state.

---

#### Step 4.2: Tile Highlighting
**New Component:** `BattleTileHighlighter.cs`

**Method:** `HighlightTiles(Dictionary<MapGridPoint, TileHighlightType>)`

**Types:**
- Blue: Valid move
- Red: Valid attack (with enemy)
- Yellow: Attack range (no enemy)
- Green: Ally/heal target

**Implementation:**
- Sprite overlay on each tile
- Pool sprite instances (don't create 100 every time)
- Clear when changing states

**Wire to:** `CalculateValidTiles()` completion

---

#### Step 4.3: Damage Numbers
**New Prefab:** DamageNumber (TextMeshPro with animation)

**Trigger:** When `DamageCommand` executes

**Component:** `DamageNumberSpawner.cs`

**Method:** `Spawn(position, damage, isCrit, isMiss)`

**Animation:** Float up, fade out, destroy after 1.5s

**Use:** Object pooling for performance

---

### Phase 5: Polish & Edge Cases

#### Step 5.1: Undo System
**File:** `PlayerTurnFlow.HandlePlayerUndoAction()`

**Track undo count per turn:**
```csharp
private int _undoCountThisTurn = 0;

private void HandlePlayerUndoAction()
{
    _undoCountThisTurn++;
    _battleBrain.Brain.RestoreSnapshot();
    
    // Check limit (get from settings)
    if (_undoCountThisTurn > MaxUndos)
    {
        // Apply penalty or disable
    }
}
```

**Reset on turn complete.**

---

#### Step 5.2: Canto Movement
**File:** `BattleInputControllerBrain.ExecuteConfirmedAction()`

**After attack animation:**
```csharp
if (SelectedUnit.HasCanto())
{
    int remainingMoves = CalculateRemainingMovement(SelectedUnit);
    if (remainingMoves > 0)
    {
        _playerTurnFlow.SpecialTurnReset(); // Back to NoActionChosen
        CalculateValidTiles(SelectedUnit); // With reduced range
        return; // Don't complete turn
    }
}
CompletePlayerTurn();
```

---

#### Step 5.3: Item Usage
**Similar to attack flow:**

**States:**
- UseItemActionChosenChoosingItem (show inventory menu)
- UseItemActionChosenItemSelected (if item needs target)
- If no target needed, use immediately

**Menu:** List inventory, filter IsUsable, show effect description

**Execute:** `BattleContext.UseItem(user, item, target)`

---

## Critical Architecture Notes

### Performance

**Tile Calculation:** Don't recalculate on every cursor move. Calculate once per unit activation, cache until state changes.

**Unit Lookups:** If you have 40+ units, consider `Dictionary<Vector2Int, CharacterInstance>` instead of linear search. Update on moves.

**Object Pooling:** Use for UI elements (damage numbers, tile highlights, arrows). You're already using pooled collections in DamageCalculator - extend this pattern.

### Memory Management

**Event Subscriptions:** You're correctly storing delegates and unsubscribing. Maintain this pattern everywhere.

**Active Roster Tracking:** In `GamewideContextBrain._activeRosterInstances`, you add but never remove. Add cleanup when battles end:
```csharp
public void ClearBattleRosters()
{
    _activeRosterInstances.Clear();
    // ... rest of cleanup
}
```

### State Machine Integrity

**Always validate transitions.** Your `PlayerTurnState.TransitionToState()` switch is correct - don't bypass it.

**Track previous state** for cancel operations.

**Clear cached data** when returning to NoUnitSelected or NoActionChosen.

---

## Testing Strategy

### Unit Tests (Priority)

**Add Unity Test Framework to project.**

**Critical Tests:**
1. State transitions (can't skip states)
2. Damage formulas (weapon triangle, crits)
3. Command execution/undo
4. Tile validity (out of bounds, occupied, etc.)

**Example:**
```csharp
[Test]
public void WeaponTriangleAdvantage()
{
    // Setup: Sword vs Axe
    var damage = DamageCalculator.CalculatePotentialDamage(...);
    Assert.Greater(damage, baseDamage);
}
```

### Integration Tests

**Manually test each flow:**
- [ ] Select unit → move → wait → turn ends → next unit activates
- [ ] Select unit → attack → confirm → damage applies
- [ ] Cancel at each state → returns to correct previous state
- [ ] Undo after confirmation → state restored
- [ ] Multiple player units in sequence
- [ ] Enemy turn still works (don't break existing AI)

---

## Implementation Priority Order

**Week 1 - Make It Work:**
1. Fix GetUnitAtPosition() ✅
2. Wire CalculateValidTiles() to pathfinding ✅
3. Add previous state tracking ✅
4. Complete HandleNavigateInput() ✅
5. Complete HandleConfirmInput() for basic movement ✅
6. Wire turn completion to TurnRotisserie ✅

**Week 2 - Add Interactions:**
7. Build action menu system ✅
8. Implement attack target selection ✅
9. Calculate combat forecast ✅
10. Execute attack commands ✅

**Week 3 - Visual Feedback:**
11. Combat forecast UI ✅
12. Movement path preview ✅
13. Tile highlighting ✅
14. Damage numbers ✅

**Week 4 - Refinement:**
15. Undo/redo system ✅
16. Item usage flow ✅
17. Canto movement ✅
18. Edge case handling ✅

---

## Your Immediate Next Steps

**Today:** Pick ONE task from Week 1 and complete it end-to-end. I recommend starting with #1 (GetUnitAtPosition) since everything depends on it.

**This Week:** Get the basic turn flow working. One unit, move and wait only. No combat yet.

**Test Scene Setup:**
- 10x10 grid
- 2 player units
- Basic terrain
- BattleContext initialized
- Debug logging everywhere

**Success Criteria for Week 1:**
- Click unit → cursor appears
- Arrow keys move cursor
- Press confirm on valid tile → unit moves
- Press wait → turn ends
- Next unit activates automatically

Once that works, everything else is just adding more cases to the switch statements you've already structured.
