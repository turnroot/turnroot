# Scene Flow System - Usage Guide

## Overview
The Scene Flow System provides a graph-based (non-linear) approach to managing scene transitions, perfect for games with hub scenes and branching paths.

## Key Components

### 1. SceneFlowGraph (ScriptableObject)
Create via: `Assets > Create > Turnroot/Scene Flow/Scene Flow Graph`

Defines:
- **Scene Nodes**: Each scene appears once with a unique ID
- **Transitions**: Connections between scenes with conditions
- **Starting Scene**: Where the flow begins

### 2. SceneFlowBrain (Brain Component)
Automatically added to your TurnrootBrain GameObject.

Manages:
- Current scene tracking
- Scene navigation history
- Condition evaluation
- Loading/unloading scenes

### 3. Brain Events
New events available:
- `OnSceneTransitionStarted`
- `OnSceneTransitionCompleted`
- `OnSceneTransitionBlocked`
- `OnSceneChanged`
- `OnSceneLoadProgress`

## Quick Start

### 1. Create Your Graph

```
1. Create new SceneFlowGraph asset
2. Add Scene Nodes for each scene:
   - Hub (isHub = true)
   - WorldMap  
   - Shop
   - Battle
3. Add Transitions between scenes:
   - Hub → WorldMap (label: "Go to World Map"  )
   - Hub → Shop (label: "Visit Shop")
   - WorldMap → Battle (label: "Start Battle", conditions: has flag "battle_unlocked")
   - All scenes → Hub (isBidirectional for hub scenes)
```

### 2. Assign to Brain

```csharp
// In your TurnrootBrain GameObject inspector:
// Assign your SceneFlowGraph to the SceneFlowBrain component
```

### 3. Trigger Transitions from Inspector

The SceneFlowBrain provides UnityEvent-compatible methods:

**In DynamicSceneFlow or UI buttons:**

```csharp
// Transition to specific scene
sceneFlowBrain.TransitionToScene("world_map");

// Or by Unity scene name
sceneFlowBrain.TransitionToSceneByName("WorldMapScene");

// Go back to previous scene
sceneFlowBrain.GoBackToPreviousScene();

// Return to hub
sceneFlowBrain.ReturnToHub();
```

### 4. Set Conditions Dynamically

```csharp
// Set flags for conditional transitions
sceneFlowBrain.SetCustomFlag("battle_unlocked", true);
sceneFlowBrain.SetCustomIntValue("chapter", 2);
sceneFlowBrain.SetCustomStringValue("current_location", "forest");
```

### 5. Query Available Scenes

```csharp
// Get all currently available transitions (UI buttons, etc.)
var options = sceneFlowBrain.GetAvailableScenes();

foreach (var option in options)
{
    Debug.Log($"Can go to: {option.displayName} via {option.label}");
    // Create button with: onClick -> TransitionToScene(option.sceneId)
}
```

## Example: Hub-Based Flow

```
Graph Structure:
    [Intro] → [Hub] ⟷ [World Map]
                ↓↑
          [Shop] [Battle A]
                     ↓
                 [Hub] (returns)
```

**Setup:**
1. Create nodes: Intro, Hub (isHub=true), WorldMap, Shop, BattleA
2. Add transitions:
   - Intro → Hub (always available)
   - Hub ⟷ WorldMap (bidirectional)
   - Hub ⟷ Shop (bidirectional)
   - Hub → BattleA (condition: "chapter1_unlocked")
   - BattleA → Hub (always, marks battle complete)

**In Battle Complete Event:**
```csharp
void OnBattleCompleted()
{
    brain.sceneFlowBrain.SetCustomFlag("chapter1_complete", true);
    brain.sceneFlowBrain.SetCustomFlag("chapter2_unlocked", true);
    brain.sceneFlowBrain.GoBackToPreviousScene(); // Returns to hub
}
```

## Condition Types

### BrainStateBool (StateBrain Integration)
Check if a specific brain state is currently active.

```csharp
// Example: Only allow transition to shop when in Hub state
// Transition condition setup:
// conditionType = BrainStateBool
// conditionKey = "Hub"  // or "Combat.Battle" for child states
// expectedBoolValue = true

// The condition will pass when:
// - Brain's current state is "Hub", OR
// - Brain's current state is a child of "Hub"
```

**Use cases:**
- Only allow shop access when in Hub state
- Block world map while in Combat state
- Enable specific scenes during Cutscene state

**State names from BrainStateNames:**
- `Hub`, `Combat`, `Paused`, `Cutscene`, `WorldMap`, `MainMenu`
- `Combat.PreBattle`, `Combat.Battle`, `Combat.PostBattle`
- `GameStart.ChooseSaveFile`

### BrainStateString (StateBrain Integration)
Check the current brain state name or path.

```csharp
// Example: Check if we're in a specific state
// conditionType = BrainStateString
// conditionKey = "CurrentStateName"  // or "CurrentStatePath"
// expectedStringValue = "Battle"     // or "Combat.Battle"

// CurrentStateName checks: state.Name
// CurrentStatePath checks: state.GetFullPath()
```

**Use cases:**
- Unlock content based on reaching specific states
- Different hub options based on state path
- Conditional navigation from complex state hierarchies

### BrainStateInt
StateBrain doesn't use integers, so this falls back to **custom int values**.

```csharp
// Use SetCustomIntValue instead
sceneFlowBrain.SetCustomIntValue("level", 5);

// Condition:
// conditionType = BrainStateInt
// conditionKey = "level"
// comparisonOperator = GreaterThanOrEqual
// expectedIntValue = 5
```

### CustomFlag (Simple Game Flags)
Best for straightforward boolean conditions.

```csharp
// Set in code
sceneFlowBrain.SetCustomFlag("has_key", true);

// Check in transition conditions
// conditionKey = "has_key"
// expectedBoolValue = true
```

## Example: State-Based Transitions

### Scenario: Shop only accessible from Hub, Battles only from Combat state

**Graph Setup:**
```
Hub (isHub=true)
├→ WorldMap
│  └→ BattleForest (starts Combat state)
├→ Shop (condition: BrainStateBool "Hub" = true)
└→ Armory

BattleForest returns to → Hub
```

**Transition Conditions:**
```
1. Hub → Shop
   - Type: BrainStateBool
   - Key: "Hub"
   - Expected: true
   - Result: Only accessible when actually in Hub state

2. WorldMap → BattleForest
   - Type: CustomFlag
   - Key: "forest_unlocked"
   - Expected: true

3. Any scene attempting direct access to Shop
   - Blocked if not in Hub state
```

**Code Integration:**
```csharp
// When entering hub scene
void OnHubSceneLoaded()
{
    brain.stateBrain.ActivateHighLevelState("Hub");
    // Shop transition now available
}

// When starting battle
void OnBattleStart()
{
    brain.stateBrain.ActivateHighLevelState("Combat");
    brain.stateBrain.ActivateChildState("Battle");
    // Shop transition now blocked
}

// Check what's available
var options = brain.sceneFlowBrain.GetAvailableScenes();
// Will only show scenes whose conditions pass
```

##`

### BrainStateBool/Int/String
These are placeholders for future integration with StateBrain.
For now, they fall back to custom flags/values.

## Inspector Integration

### DynamicSceneFlow Events

```
Example UnityEvent sequence:
1. onSegmentReached[0]:
   - SetCustomFlag(string "intro_seen", bool true)
   - TransitionToScene(string "hub")

2. Button onClick:
   - TransitionToScene(string "world_map")
```

### Conditional UI

```csharp
public class SceneNavigationUI : MonoBehaviour
{
    public SceneFlowBrain sceneFlowBrain;
    public GameObject buttonPrefab;
    public Transform buttonContainer;
    
    void RefreshButtons()
    {
        // Clear existing
        foreach(Transform child in buttonContainer)
            Destroy(child.gameObject);
            
        // Create buttons for available scenes
        var options = sceneFlowBrain.GetAvailableScenes();
        foreach(var option in options)
        {
            var btn = Instantiate(buttonPrefab, buttonContainer);
            btn.GetComponentInChildren<Text>().text = option.label;
            btn.GetComponent<Button>().onClick.AddListener(() => 
            {
                sceneFlowBrain.TransitionToScene(option.sceneId);
            });
        }
    }
}
```

## Benefits Over Linear Flow

### Problem: Hub Duplication
**Linear system:**
```
Intro → Hub1 → WorldMap → Hub2 → Shop → Hub3 → Battle
```
3 separate "Hub" nodes that must be kept in sync!

**Graph system:**
```
        ┌─ World Map
Intro → Hub ┼─ Shop
        └─ Battle
```
Single Hub node, referenced multiple times.

## Next Steps

1. **Visual Editor** (Future): Custom editor window to visualize the graph
2. **StateBrain Integration** (Future): Conditions based on actual game state
3. **Save/Load Integration** (Future): Persist current scene in save file
4. **Advanced Transitions** (Future): Fade effects, loading screens, etc.

## Tips

- **Hub scenes**: Set `isHub = true` for main menu/hub scenes
- **Scene IDs**: Use descriptive IDs like "battle_forest_1" not generic names
- **Labels**: Make transition labels match what players see ("Enter Shop" not "shop_scene")
- **Bidirectional**: Use for hub↔locations, not story progression
- **Conditions**: Keep simple at first, add complexity as needed

## Troubleshooting

**"No transition defined" warning:**
- Add missing transition in graph, or
- Ignore if you want to allow direct transition anyway

**"Conditions not met" warning:**
- Check SetCustomFlag was called before transition
- Verify flag name matches exactly (case-sensitive)
- Use GetCustomFlag to debug current state

**Scene doesn't load:**
- Verify scene name matches exactly (case-sensitive)
- Ensure scene is added to Build Settings
- Check Unity console for detailed error messages
