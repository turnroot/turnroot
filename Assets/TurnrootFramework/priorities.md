## Phase 1: Core Player Turn Infrastructure

### Step 1.1: Create PlayerTurnFlow Component
**Location:** `Assets/TurnrootFramework/Gameplay/Brain/Components/Battle/PlayerTurnFlow.cs`

**Purpose:** State machine for player turn progression (parallel to how `TurnRotisserie` manages turn rotation).

```csharp
public enum PlayerTurnState
{
    Inactive,                    // Not player's turn
    AwaitingUnitSelection,      // Player can select a unit
    UnitSelected,               // Unit selected, showing action menu
    SelectingMoveDestination,   // Player picking where to move
    SelectingAttackTarget,      // Player picking who to attack
    SelectingItemTarget,        // Player picking item target
    ConfirmingAction,           // Preview shown, awaiting confirmation
    ExecutingAction,            // Animation/command execution
    TurnComplete                // Player done, ready for next unit
}

[RequireComponent(typeof(BattleBrain))]
public class PlayerTurnFlow : MonoBehaviour
{
    private PlayerTurnState _currentState;
    private CharacterInstance _activePlayerUnit;
    private MapGridPoint _selectedDestination;
    private CharacterInstance _selectedTarget;
    
    // TODO: State transition methods
    // TODO: Subscribe to relevant Brain events (unit activated, turn ended)
    // TODO: Publish player-specific events (state changed, action confirmed)
}
```

**Task:** Sketch out the state transition diagram on paper. Which states can transition to which? What triggers each transition?

---

### Step 1.2: Extend PlayerInputBrain
**Location:** `Assets/TurnrootFramework/Gameplay/Brain/Segments/PlayerInputBrain.cs`

**Purpose:** Manages player turn logic, coordinates with `PlayerTurnFlow`, requests tile data from AI helper.

```csharp
public class PlayerInputBrain : BrainComponent
{
    private PlayerTurnFlow _playerTurnFlow;
    private BattleContextAIHelper _aiHelper; // Reuse for pathfinding
    
    // Cached data for current player unit
    private Dictionary<MapGridPoint, float> _validMoveTiles = new();
    private Dictionary<MapGridPoint, float> _validAttackTiles = new();
    
    protected override void Awake()
    {
        base.Awake();
        _playerTurnFlow = GetComponent<PlayerTurnFlow>();
        // TODO: Get reference to AI helper from BattleContext
    }
    
    protected override void SubscribeToBrainEvents()
    {
        // TODO: Subscribe to PlayerControlledUnitActivated
        // TODO: Subscribe to PlayerTurnFlow state changes
    }
    
    // TODO: Method to calculate valid tiles for current player unit
    // TODO: Method to validate if a tile/target selection is legal
    // TODO: Methods to execute player actions via BattleContext commands
}
```

**Task:** List out all the Brain events `PlayerInputBrain` should subscribe to. Think about what it needs to know (turn start, unit activation, turn end, etc.).

---

### Step 1.3: Add Player Turn Events to Brain
**Location:** `Assets/TurnrootFramework/Gameplay/Brain/Brain.cs`

**Purpose:** Central event hub for player turn signals.

```csharp
// In Brain.cs, add to existing event regions:

#region Player Turn Events

public event Action<CharacterInstance> OnPlayerUnitActivated;
public event Action<PlayerTurnState> OnPlayerTurnStateChanged;
public event Action<CharacterInstance> OnPlayerTurnCompleted;
public event Action OnPlayerUndoAction; // For timewheel tracking

public void PublishPlayerUnitActivated(CharacterInstance unit) 
    => OnPlayerUnitActivated?.Invoke(unit);

public void PublishPlayerTurnStateChanged(PlayerTurnState newState) 
    => OnPlayerTurnStateChanged?.Invoke(newState);

public void PublishPlayerTurnCompleted(CharacterInstance unit) 
    => OnPlayerTurnCompleted?.Invoke(unit);

public void PublishPlayerUndoAction() 
    => OnPlayerUndoAction?.Invoke();

#endregion
```

**Task:** Think about where these events will be published *from*. `PlayerTurnFlow`? `PlayerInputBrain`? Both?

---

## Phase 2: Input Controller Layer

### Step 2.1: Create BattleInputController
**Location:** `Assets/YourGame/Battle/Input/BattleInputController.cs` (your game layer, not framework)

**Purpose:** Translates raw Unity Input System events into player turn requests.

```csharp
public class BattleInputController : MonoBehaviour
{
    private Brain _brain;
    private PlayerInputBrain _playerInputBrain;
    
    // Current cursor/selection state
    private MapGridPoint _cursorPosition;
    private CharacterInstance _hoveredUnit;
    
    void Update()
    {
        // TODO: Read input (arrow keys, gamepad stick)
        // TODO: Move cursor on grid
        // TODO: Handle confirm button (A/Enter)
        // TODO: Handle cancel button (B/Escape)
        // TODO: Handle menu button (X/Spacebar)
    }
    
    // TODO: Method to handle tile confirmation
    // TODO: Method to handle unit selection
    // TODO: Method to handle action menu selection
    // TODO: Method to request undo (calls Brain.UndoCommand + publishes undo event)
}
```

**Task:** Map out your control scheme. What button does what in each `PlayerTurnState`? (e.g., "B" cancels in most states, but what about in `UnitSelected` vs `SelectingMoveDestination`?)

---

### Step 2.2: Create Battle UI Controller
**Location:** `Assets/YourGame/Battle/UI/BattleUIController.cs`

**Purpose:** Visual representation of battle state - tile highlights, cursor, action menus, damage previews.

```csharp
public class BattleUIController : MonoBehaviour
{
    private Brain _brain;
    private PlayerInputBrain _playerInputBrain;
    
    // UI References
    // TODO: Cursor sprite/mesh
    // TODO: Tile highlighter system (blue for move, red for attack, etc.)
    // TODO: Action menu UI
    // TODO: Damage preview panel
    // TODO: Unit info panel
    
    void Start()
    {
        // TODO: Subscribe to PlayerTurnStateChanged
        // TODO: Subscribe to relevant Brain events for updating displays
    }
    
    // TODO: Method to highlight valid move tiles
    // TODO: Method to highlight valid attack tiles
    // TODO: Method to show damage preview for hovered target
    // TODO: Method to show action menu at unit position
    // TODO: Method to update cursor position
}
```

**Task:** Sketch the UI layout. What panels exist? What shows when? Draw it out or use a tool like Figma.

---

## Phase 3: Integration with Existing Systems

### Step 3.1: Modify TurnRotisserie for Player Turns
**Location:** `Assets/TurnrootFramework/Gameplay/Brain/Components/Battle/TurnRotisserie.cs`

**Changes needed:**
```csharp
// In ActivateCurrentUnit():
private void ActivateCurrentUnit()
{
    var activeUnit = units[_currentRosterIndex];
    // ... existing code ...
    
    // NEW: Check if player-controlled
    if (_currentTurnOrder is TurnOrder.PlayerStart or TurnOrder.PlayerEnd)
    {
        // Publish player activation event instead of AI
        _brain?.PublishPlayerUnitActivated(activeUnit);
        // DON'T call Progress() - wait for player input brain to signal completion
    }
    else
    {
        // Existing AI logic unchanged
        ChangeBattleContextData(activeUnit);
    }
}
```

**Task:** Trace through what happens after `PublishPlayerUnitActivated`. Who catches that event? What state transitions happen?

---

### Step 3.2: Subscribe PlayerInputBrain to Turn Completion
**Location:** Back in `PlayerInputBrain.cs`

```csharp
protected override void SubscribeToBrainEvents()
{
    _brain.OnPlayerTurnCompleted += HandlePlayerTurnCompleted;
    // ... other subscriptions
}

private void HandlePlayerTurnCompleted(CharacterInstance unit)
{
    // Clear cached tile data
    _validMoveTiles.Clear();
    _validAttackTiles.Clear();
    
    // Signal TurnRotisserie to continue
    // TODO: Decide mechanism - direct call or event?
    var turnRotisserie = _brain.battleBrain.GetComponent<TurnRotisserie>();
    turnRotisserie.Progress(); // or publish event that TurnRotisserie subscribes to
}
```

**Task:** Should this be a direct call or another Brain event? Justify your choice. (Hint: Think about what else might need to know when a player turn ends.)

---

## Phase 4: Action Preview & Confirmation

### Step 4.1: Damage Preview System
**Location:** `PlayerInputBrain.cs` (logic) + `BattleUIController.cs` (display)

```csharp
// In PlayerInputBrain:
public (int damageToTarget, int counterDamage, bool wouldKill, bool wouldBeKilled) 
    CalculateAttackPreview(CharacterInstance attacker, CharacterInstance target)
{
    var context = _brain.battleBrain.BattleObject.Context;
    var weaponItem = attacker.GetEquippedWeapon();
    
    int damage = DamageCalculator.CalculatePotentialDamage(attacker, target, weaponItem, context);
    bool wouldKill = DamageCalculator.WouldKill(attacker, target, weaponItem, context);
    
    // TODO: Calculate counter damage if target can retaliate
    // TODO: Check if counter would kill attacker
    
    return (damage, counterDamage, wouldKill, wouldBeKilled);
}
```

**Task:** What formula should you use for hit/crit display? Your `DamageCalculator` already has `CalculateHitChance` and `CalculateCriticalChance` - wire those up.

---

### Step 4.2: Movement Path Preview
**Location:** You'll need a new component - `BattlePathVisualizer.cs`

```csharp
public class BattlePathVisualizer : MonoBehaviour
{
    // TODO: LineRenderer or sprite chain to show path
    
    public void ShowPath(MapGridPoint start, MapGridPoint end, MapGrid grid)
    {
        // TODO: Use AStar to get actual path
        // TODO: Visualize it with arrows or line
    }
    
    public void ClearPath()
    {
        // TODO: Hide visualization
    }
}
```

**Task:** Decide visual style. Will you use arrows on each tile (like FE), a continuous line, or something else?

---

### Step 4.3: Implement Action Confirmation Flow
**Location:** `PlayerTurnFlow.cs`

```csharp
// When entering ConfirmingAction state:
private void EnterConfirmingActionState()
{
    // Execute command as preview
    var command = BuildCurrentActionCommand();
    _brain.ExecuteCommand(command);
    
    // Take snapshot so we can undo if cancelled
    _brain.TakeSnapshot();
    
    // UI shows "Confirm? Yes/No"
    // Wait for player input...
}

private void OnPlayerConfirmsAction()
{
    // Command is already executed, just transition state
    TransitionTo(PlayerTurnState.ExecutingAction);
    // Play animations, then transition to TurnComplete
}

private void OnPlayerCancelsAction()
{
    // Restore snapshot to undo preview
    _brain.RestoreSnapshot();
    
    // Track undo count for timewheel
    _brain.PublishPlayerUndoAction();
    
    // Return to previous state
    TransitionTo(PlayerTurnState.UnitSelected);
}
```

**Task:** Think about edge cases. What if the player undoes, moves somewhere else, then confirms *that* action? Do you need to track a stack of actions, or is the snapshot system sufficient?

---

## Phase 5: Polish & Edge Cases

### Step 5.1: Handle Invalid Inputs Gracefully
```csharp
// In PlayerInputBrain or BattleInputController:
private bool ValidateTargetSelection(CharacterInstance target)
{
    // TODO: Is target in range?
    // TODO: Is target on correct team (enemy for attack, ally for heal)?
    // TODO: Does active unit have a weapon/item that can reach?
    
    if (!isValid)
    {
        // TODO: Play error sound, show message
        return false;
    }
    return true;
}
```

---

### Step 5.2: Timewheel Undo Tracking
**Location:** New component - `TimewheelTracker.cs` (or add to an existing system)

```csharp
public class TimewheelTracker : MonoBehaviour
{
    private int _undoCount = 0;
    
    void Start()
    {
        var brain = FindObjectOfType<Brain>();
        brain.OnPlayerUndoAction += IncrementUndoCount;
    }
    
    private void IncrementUndoCount()
    {
        _undoCount++;
        // TODO: UI update, check against timewheel limit
    }
}
```

**Task:** What's the gameplay consequence of too many undos? Does the timewheel penalize the player? Design that system before implementing.

---

### Step 5.3: Handle Special Cases
Things to think about now (implement later):

- **Canto movement** (FE units that can move after attacking) - how does that fit into your state machine?
- **Wait command** (end turn without moving) - trivial, but needs a menu option
- **Item use** - very similar to attack flow, might share a state
- **Trading items between units** - new state or separate menu flow?
- **Rescue/Drop** (if you have that mechanic) - requires target selection like attack

**Task:** For each, write one sentence describing how it fits into `PlayerTurnState`. Example: "Canto: After `ExecutingAction` for an attack, if unit has canto, transition to `SelectingMoveDestination` again with reduced movement range."

---

## Implementation Order Recommendation

**Week 1:** Phase 1 (Steps 1.1-1.3) - Core infrastructure, get state machine working with debug logs

**Week 2:** Phase 2 (Steps 2.1-2.2) - Input handling and basic UI, even if ugly

**Week 3:** Phase 3 (Steps 3.1-3.2) - Integration with turn flow, get one full player turn working end-to-end

**Week 4:** Phase 4 (Steps 4.1-4.3) - Previews and confirmation, make it feel good

**Week 5:** Phase 5 - Polish, edge cases, playtesting

---

## Your Next Immediate Actions

1. **Design Session:** Spend 30 minutes with pen and paper drawing the state machine. Every state, every transition, every trigger.

2. **Control Scheme Doc:** Write down your input mapping for every state. This will guide `BattleInputController` implementation.

3. **Create Skeletons:** Make the three main files (`PlayerTurnFlow.cs`, extend `PlayerInputBrain.cs`, add events to `Brain.cs`) with just method signatures and TODOs.

4. **First Test:** Get `PlayerTurnFlow` to log "Player unit activated" when it's the player's turn.