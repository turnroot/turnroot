# Developer Notes: Architecture Patterns

## Table of Contents
1. [StackedImage Inheritance Architecture](#stackedimage-inheritance-architecture)
2. [ScriptableObject Instance Data Pattern](#scriptableobject-instance-data-pattern)

---

# StackedImage Inheritance Architecture

## Overview
This document explains the inheritance and abstraction patterns used in the StackedImage system, a flexible framework for creating composite graphics (like character portraits) from layered image data in Unity.

## System Purpose
The StackedImage system allows us to:
1. **Composite multiple image layers** with masks and tints into a single sprite
2. **Apply owner-specific customization** (like character accent colors)
3. **Provide specialized editor tooling** for different types of stacked images
4. **Reuse common functionality** while allowing domain-specific behavior

---

## Architecture: Three-Level Inheritance Hierarchy

### Level 1: Abstract Generic Base (`StackedImage<TOwner>`)
**Location:** `Assets/Prototypes/Graphics2D/Components/StackedImage.cs`

This is the **generic abstract base class** that provides all core functionality for any stacked image system.

#### Generic Type Parameter Pattern
```csharp
public abstract class StackedImage<TOwner>
    where TOwner : UnityEngine.Object
```

**What this means:**
- `<TOwner>` is a **generic type parameter** - a placeholder for any concrete type
- `where TOwner : UnityEngine.Object` is a **generic constraint** - it restricts `TOwner` to Unity objects (ScriptableObjects, MonoBehaviours, etc.)
- This allows the class to work with ANY Unity object type as an "owner" while maintaining type safety

**Why use generics here?**
- **Reusability:** The same base class works for character portraits, item icons, UI elements, etc.
- **Type Safety:** Each subclass knows the exact type of its owner at compile time
- **Flexibility:** New StackedImage types can be created for different owner types without modifying the base class

#### Core Responsibilities
The base class handles:

1. **State Management**
```csharp
[SerializeField]
protected TOwner _owner;              // Reference to owner object (generic type)

[SerializeField]
private ImageStack _imageStack;       // Collection of layers to composite

[SerializeField, HideInInspector]
private string _key;                  // Unique identifier/filename

[SerializeField, HideInInspector]
protected Color[] _tintColors;        // Colors for masking/tinting layers

private Guid _id;                     // Unique runtime identifier
```

**Serialization Pattern:**
- `[SerializeField]` makes private fields appear in Unity's Inspector and get saved
- `[HideInInspector]` keeps them serialized but hidden from the Inspector UI
- This gives us control over data persistence without cluttering the Inspector

2. **Composition Pipeline**
```csharp
public void Render()
{
    // 1. Validate ImageStack exists
    // 2. Ensure key is set (auto-generate if needed)
    // 3. Composite layers into a single Texture2D
    Texture2D composited = CompositeLayers();
    
    // 4. Create runtime Sprite
    _runtimeSprite = Sprite.Create(composited, ...);
    
    // 5. Save to file system
    SaveToFile(composited);
    
    // 6. Load as Unity asset
    LoadSavedSprite();
}
```

This method orchestrates the entire rendering pipeline in a **template pattern** - the base class defines the algorithm structure, but some steps can be customized by subclasses.

3. **Abstract Hook Method**
```csharp
public abstract void UpdateTintColorsFromOwner();
```

**What this pattern achieves:**
- The **base class** defines the contract: "subclasses MUST provide a way to update tint colors"
- **Subclasses** implement the specific logic: "for characters, use their accent colors"
- This is the **Template Method pattern** - the base class defines the skeleton, subclasses fill in specific steps

**Why abstract instead of virtual?**
- `abstract`: Forces subclasses to implement (no default behavior makes sense)
- `virtual`: Provides default behavior that subclasses can optionally override
- Here, each owner type has different color sources, so we require implementation

4. **Unity Lifecycle Hook**
```csharp
private void OnAfterDeserialize()
{
    // Restore Guid from string (Guid isn't serializable by default)
    if (!string.IsNullOrEmpty(_idString))
        _id = Guid.Parse(_idString);
    
    // Auto-generate key if missing
    if (string.IsNullOrEmpty(_key))
        _key = $"stackedImage_{_id}";
    
    // Initialize tint colors array
    if (_tintColors == null || _tintColors.Length < 3)
        _tintColors = new Color[3] { Color.white, Color.white, Color.white };
    
    // Update from owner
    UpdateTintColorsFromOwner();
}
```

**Unity Serialization Lifecycle:**
- Unity's serializer doesn't natively support `Guid` type
- Solution: Store as `string` (`_idString`), convert to `Guid` after deserialization
- `OnAfterDeserialize()` is called by Unity whenever the object is loaded from disk
- This ensures our data is always in a valid state after loading

---

### Level 2: Concrete Implementation (`Portrait`)
**Location:** `Assets/Prototypes/Characters/Components/Portrait.cs`

This is a **concrete specialization** of StackedImage for character portraits.

```csharp
[Serializable]
public class Portrait : StackedImage<CharacterData>
{
    public override void UpdateTintColorsFromOwner()
    {
        // Defensive programming: ensure array exists
        if (_tintColors == null || _tintColors.Length < 3)
        {
            _tintColors = new Color[3] { Color.white, Color.white, Color.white };
        }

        // Extract colors from CHARACTER-SPECIFIC properties
        if (_owner != null)
        {
            _tintColors[0] = _owner.AccentColor1;
            _tintColors[1] = _owner.AccentColor2;
            _tintColors[2] = _owner.AccentColor3;
        }
    }
}
```

**Key Design Decisions:**

1. **Type Substitution:**
   - Base class: `StackedImage<TOwner>`
   - Portrait: `StackedImage<CharacterData>`
   - This means everywhere the base class uses `TOwner`, Portrait uses `CharacterData`
   - Result: `_owner` is now typed as `CharacterData` specifically

2. **`[Serializable]` Attribute:**
   - Makes Portrait work with Unity's serialization system
   - Without this, arrays of Portraits (like in CharacterData) wouldn't save properly
   - This is a **Unity requirement**, not a C# language requirement

3. **Minimal Implementation:**
   - Only implements what's specific to portraits: color extraction from characters
   - Inherits all composition, rendering, and file management from base class
   - This follows the **Don't Repeat Yourself (DRY)** principle

**How it's used:**
```csharp
// In CharacterData.cs
[Foldout("Visual"), SerializeField]
private Portrait[] _portraits;

public Portrait[] Portraits => _portraits;
```

Each CharacterData can have multiple Portraits, each compositing different ImageStacks with the character's accent colors.

---

### Level 3: Editor Tools (Abstract Generic Editor Window)
**Location:** `Assets/Prototypes/Graphics2D/Components/Editor/StackedImageEditorWindow.cs`

This is another **generic abstract base**, but for editor tooling rather than runtime functionality.

```csharp
public abstract class StackedImageEditorWindow<TOwner, TStackedImage> : EditorWindow
    where TOwner : UnityEngine.Object
    where TStackedImage : StackedImage<TOwner>
```

**Two Generic Type Parameters:**
- `TOwner`: The type that owns the stacked images (e.g., CharacterData)
- `TStackedImage`: The specific StackedImage implementation (e.g., Portrait)
- Second constraint: `TStackedImage : StackedImage<TOwner>` ensures they match up correctly

**Why is this powerful?**
This creates a **type relationship chain**:
```
StackedImageEditorWindow<TOwner, TStackedImage>
    where TStackedImage : StackedImage<TOwner>
```

When we write:
```csharp
class PortraitEditorWindow : StackedImageEditorWindow<CharacterData, Portrait>
```

The compiler verifies:
- Portrait must be a StackedImage\<CharacterData\> ✓
- This catches type mismatches at compile time instead of runtime

#### Editor Window Responsibilities

1. **Abstract Contract for Subclasses:**
```csharp
protected abstract string WindowTitle { get; }
protected abstract string OwnerFieldLabel { get; }
protected abstract TStackedImage[] GetImagesFromOwner(TOwner owner);
```

These **abstract properties and methods** define what subclasses must provide:
- `WindowTitle`: "Portrait Editor" vs "Item Icon Editor"
- `OwnerFieldLabel`: "Character" vs "Item"
- `GetImagesFromOwner`: How to extract images from the owner object

This is the **Strategy pattern** - different strategies for accessing images depending on owner type.

2. **Shared UI Components:**
```csharp
protected virtual void DrawControlPanel()
{
    DrawImageMetadataSection();
    DrawImageStackSection();
    DrawOwnerSection();
    DrawTintingSection();
    DrawLayerManagementSection();
}
```

**`virtual` methods:**
- Provide default implementation that works for most cases
- Can be **overridden** by subclasses if they need custom behavior
- Different from `abstract` which provides NO implementation

3. **Generic Field Management:**
```csharp
protected TOwner _currentOwner;           // Works with any owner type
protected TStackedImage _currentImage;    // Works with any StackedImage subclass
```

These fields are typed generically, so the same code works whether we're editing Portraits, ItemIcons, or any future StackedImage type.

---

### Level 4: Concrete Editor Implementation (`PortraitEditorWindow`)
**Location:** `Assets/Prototypes/Characters/Components/Editor/PortraitEditorWindow.cs`

```csharp
public class PortraitEditorWindow : StackedImageEditorWindow<CharacterData, Portrait>
{
    protected override string WindowTitle => "Portrait Editor";
    protected override string OwnerFieldLabel => "Character";

    [MenuItem("Window/Portrait Editor")]
    public static void ShowWindow()
    {
        GetWindow<PortraitEditorWindow>("Portrait Editor");
    }

    public static void OpenPortrait(CharacterData character, int portraitIndex = 0)
    {
        var window = GetWindow<PortraitEditorWindow>("Portrait Editor");
        window._currentOwner = character;
        window._selectedImageIndex = portraitIndex;
        if (character != null && character.Portraits != null && portraitIndex < character.Portraits.Length)
        {
            window._currentImage = character.Portraits[portraitIndex];
            window.RefreshPreview();
        }
    }

    protected override Portrait[] GetImagesFromOwner(CharacterData owner)
    {
        return owner?.Portraits;
    }
}
```

**Notable Patterns:**

1. **`[MenuItem]` Attribute:**
   - Unity editor attribute that adds the window to the menu bar
   - `"Window/Portrait Editor"` creates: Window → Portrait Editor menu item
   - This is how Unity integrates custom tools into the editor

2. **Null-Conditional Operator (`?.`):**
   - `owner?.Portraits` is shorthand for:
     ```csharp
     if (owner != null)
         return owner.Portraits;
     else
         return null;
     ```
   - Prevents null reference exceptions
   - Modern C# syntax for defensive programming

3. **Static Factory Method:**
   ```csharp
   public static void OpenPortrait(CharacterData character, int portraitIndex = 0)
   ```
   - Allows other code to open the editor window programmatically
   - Example: Context menu on CharacterData could call this to jump to portrait editing
   - Optional parameters (`portraitIndex = 0`) provide default values

---

## Supporting Classes

### ImageStack (ScriptableObject)
**Location:** `Assets/Prototypes/Graphics2D/Components/Portrait/ImageStack.cs`

```csharp
[CreateAssetMenu(fileName = "NewImageStack", menuName = "Graphics/Portrait/ImageStack")]
public class ImageStack : ScriptableObject
{
    [SerializeField]
    private List<ImageStackLayer> _layers = new List<ImageStackLayer>();
    
    public List<ImageStackLayer> Layers => _layers;
}
```

**Design Role:**
- **ScriptableObject** = Unity asset that can be created and saved independently
- **Reusability:** Multiple portraits can reference the same ImageStack
- **Separation of Concerns:** Layer data is separate from Portrait-specific data (owner, tints, etc.)

**`[CreateAssetMenu]` Attribute:**
- Adds "Create → Graphics → Portrait → ImageStack" to Unity's asset creation menu
- `fileName` sets default name for new assets
- `menuName` sets location in creation menu hierarchy

### ImageStackLayer (Serializable Data Class)
**Location:** `Assets/Prototypes/Graphics2D/Components/ImageStackLayer.cs`

```csharp
[Serializable]
public class ImageStackLayer
{
    [SerializeField] public Sprite Sprite;    // The image to draw
    [SerializeField] public Sprite Mask;      // Mask for tinting regions
    [SerializeField] public Vector2 Offset;   // Position offset
    [SerializeField] public float Scale = 1f; // Scale multiplier
    [SerializeField] public float Rotation = 0f; // Rotation in degrees
    [SerializeField] public int Order = 0;    // Draw order (higher = on top)
}
```

**Design as Data Class:**
- No methods, just data = **Plain Old Data (POD)** / **Data Transfer Object (DTO)** pattern
- `[Serializable]` makes it work in Unity's Inspector and serialization
- Public fields with `[SerializeField]` for direct Inspector access (Unity convention)
- Default values (`= 1f`, `= 0`) ensure sensible behavior when created

---

## Design Patterns Summary

### 1. **Generic Programming**
**Where:** `StackedImage<TOwner>`, `StackedImageEditorWindow<TOwner, TStackedImage>`

**Purpose:** Write code once that works with many types

**Benefits:**
- Code reuse without sacrificing type safety
- Compiler catches type mismatches
- IntelliSense works correctly (knows `_owner` is CharacterData in Portrait)

### 2. **Template Method Pattern**
**Where:** `StackedImage<TOwner>.Render()` and `UpdateTintColorsFromOwner()`

**Purpose:** Base class defines algorithm structure, subclasses fill in specific steps

**Example:**
```csharp
// Base class defines the template
public void Render()
{
    ValidateImageStack();
    EnsureKey();
    UpdateTintColorsFromOwner();  // <-- Subclass provides this
    Texture2D composited = CompositeLayers();
    CreateSprite(composited);
    SaveToFile(composited);
    LoadSavedSprite();
}

// Subclass provides specific implementation
public override void UpdateTintColorsFromOwner()
{
    _tintColors[0] = _owner.AccentColor1;
    // ...
}
```

### 3. **Strategy Pattern**
**Where:** `GetImagesFromOwner()` in editor windows

**Purpose:** Encapsulate different algorithms (strategies) for accessing data

**Example:**
```csharp
// For characters
protected override Portrait[] GetImagesFromOwner(CharacterData owner)
{
    return owner?.Portraits;
}

// For items (hypothetical)
protected override ItemIcon[] GetImagesFromOwner(ItemData owner)
{
    return owner?.Icons;
}
```

### 4. **Dependency Inversion Principle**
**Where:** StackedImage references ImageStack

**Purpose:** Depend on abstractions (interfaces/base classes) not concrete implementations

**Example:**
```csharp
// StackedImage doesn't create ImageStack, it receives one
[SerializeField]
private ImageStack _imageStack;  // Injected via Inspector
```

Benefits:
- ImageStack can be swapped at runtime
- Multiple StackedImages can share one ImageStack
- Easy to test with mock ImageStacks

### 5. **Single Responsibility Principle**
Each class has one clear job:
- **StackedImage:** Compositing and rendering logic
- **Portrait:** Character-specific color extraction
- **ImageStack:** Layer collection management
- **ImageStackLayer:** Layer data storage
- **StackedImageEditorWindow:** Generic UI framework
- **PortraitEditorWindow:** Character-specific UI integration

---

## C# Language Features Explained

### 1. **Generic Constraints**
```csharp
where TOwner : UnityEngine.Object
```
- Restricts what types can be used for `TOwner`
- Must be Unity.Object or subclass (ScriptableObject, MonoBehaviour, etc.)
- Allows us to use Unity.Object methods on TOwner (like name, GetInstanceID)

### 2. **Property Syntax**
```csharp
public TOwner Owner => _owner;
```
This is an **expression-bodied property**, shorthand for:
```csharp
public TOwner Owner
{
    get { return _owner; }
}
```
- Read-only access to private field
- Encapsulation: internal state is private, external access is controlled

### 3. **Null-Conditional and Null-Coalescing**
```csharp
return owner?.Portraits;  // Returns null if owner is null, otherwise Portraits
_key = key ?? GenerateDefaultKey();  // If key is null, use default
```

### 4. **String Interpolation**
```csharp
_key = $"stackedImage_{_id}";
```
Equivalent to:
```csharp
_key = "stackedImage_" + _id.ToString();
```
More readable and efficient for complex strings.

### 5. **Attributes (Metadata)**
```csharp
[SerializeField]           // Unity: serialize this field
[HideInInspector]          // Unity: don't show in Inspector
[CreateAssetMenu]          // Unity: add to asset creation menu
[MenuItem]                 // Unity: add to editor menu
```

Attributes are **metadata** that changes how code behaves without changing the logic itself. Unity's engine reads these at runtime/compile time to alter behavior.

---

## Extensibility Examples

### Adding a New StackedImage Type
Want to create `ItemIcon` for items? Here's the pattern:

1. **Create ItemData owner class:**
```csharp
public class ItemData : ScriptableObject
{
    public Color PrimaryColor;
    public Color SecondaryColor;
    public ItemIcon[] Icons;
}
```

2. **Create ItemIcon concrete class:**
```csharp
[Serializable]
public class ItemIcon : StackedImage<ItemData>
{
    public override void UpdateTintColorsFromOwner()
    {
        if (_owner != null)
        {
            _tintColors[0] = _owner.PrimaryColor;
            _tintColors[1] = _owner.SecondaryColor;
            _tintColors[2] = Color.white;
        }
    }
}
```

3. **Create ItemIconEditorWindow:**
```csharp
public class ItemIconEditorWindow : StackedImageEditorWindow<ItemData, ItemIcon>
{
    protected override string WindowTitle => "Item Icon Editor";
    protected override string OwnerFieldLabel => "Item";
    
    [MenuItem("Window/Item Icon Editor")]
    public static void ShowWindow()
    {
        GetWindow<ItemIconEditorWindow>("Item Icon Editor");
    }
    
    protected override ItemIcon[] GetImagesFromOwner(ItemData owner)
    {
        return owner?.Icons;
    }
}
```

**That's it!** All the rendering, compositing, file management, and UI comes for free from the base classes.

---

## Key Takeaways

1. **Generics enable type-safe reuse:** One base class works for many owner types
2. **Abstract methods enforce contracts:** Subclasses must implement specific behaviors
3. **Virtual methods provide customization points:** Subclasses can override if needed
4. **Serialization attributes control Unity integration:** `[SerializeField]`, `[CreateAssetMenu]`, etc.
5. **Separation of concerns:** Each class has a focused responsibility
6. **Editor code mirrors runtime code:** Same inheritance structure for consistency

This architecture makes it easy to add new types of stacked images (UI icons, map markers, etc.) while sharing all the complex compositing and rendering logic.

---

## Questions for Self-Study

1. What would happen if we removed the generic constraint `where TOwner : UnityEngine.Object`?
2. Why is `UpdateTintColorsFromOwner()` abstract instead of virtual with a default implementation?
3. How would you add a fourth tint color to the system?
4. What's the difference between `[SerializeField] private` and `public` fields in Unity?
5. Could we make StackedImage a MonoBehaviour instead of a serializable class? What would change?

---

**Document Version:** 2.0  
**Last Updated:** November 6, 2025  
**Author:** GitHub Copilot  

---

# ScriptableObject Instance Data Pattern

## The Critical Problem: Shared State

### Understanding ScriptableObjects at Runtime

ScriptableObjects are **shared, singleton-like assets** that exist as files in your project. When you reference a ScriptableObject in your scene:

- All references point to the **same instance** in memory
- Changes to that instance affect **every object** referencing it
- This behavior persists throughout the entire game session

**Example of the Problem:**

```csharp
// BAD - Don't do this!
public class Enemy : MonoBehaviour 
{
    public CharacterData characterData;  // ScriptableObject reference
    
    void TakeDamage(int amount) 
    {
        // DANGER: This modifies the SHARED ScriptableObject!
        characterData.currentHP -= amount;
        
        // Now EVERY enemy using this CharacterData has reduced HP!
        // The template itself has been modified!
    }
}
```

If you have 3 enemies all using "Goblin" CharacterData:
- Enemy A takes 10 damage → Goblin HP becomes 90
- Enemy B takes 5 damage → Goblin HP becomes 85  
- Enemy C spawns → Already has 85 HP instead of 100!

**The HP is stored in the shared template, not per-enemy!**

## The Solution: Instance Data Pattern

Separate **static data** (shared template) from **runtime data** (per-entity state).

### Pattern Structure

```
ScriptableObject (Asset File)
├─ Read-only template data
├─ Configuration values
└─ References to other templates

Instance Class (Serializable)
├─ Reference to template
├─ Per-entity runtime state
└─ Methods that modify state
```

### Implementation Rules

#### Rule 1: ScriptableObjects are READ-ONLY at Runtime

```csharp
[CreateAssetMenu(menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    // ✅ GOOD - Static configuration
    public string characterName;
    public Sprite portrait;
    public int baseMaxHP;
    public int baseStrength;
    
    // ❌ BAD - Runtime state (will be shared!)
    // public int currentHP;  // Don't do this!
    // public bool isDead;    // Don't do this!
}
```

#### Rule 2: Create Instance Classes for Runtime State

```csharp
[Serializable]
public class CharacterInstance
{
    [SerializeField]
    private CharacterData _template;  // Reference to template
    
    // ✅ GOOD - Per-character runtime state
    [SerializeField]
    private int _currentHP;
    
    [SerializeField]
    private bool _isDead;
    
    public CharacterData Template => _template;
    public int CurrentHP => _currentHP;
    public bool IsDead => _isDead;
    
    public CharacterInstance(CharacterData template)
    {
        _template = template;
        _currentHP = template.baseMaxHP;  // Copy from template
        _isDead = false;
    }
    
    public void TakeDamage(int amount)
    {
        _currentHP -= amount;  // Modifies THIS instance only
        if (_currentHP <= 0)
        {
            _currentHP = 0;
            _isDead = true;
        }
    }
}
```

#### Rule 3: MonoBehaviours Hold Instance Data

```csharp
public class Enemy : MonoBehaviour
{
    // ✅ GOOD - Each enemy has their own instance
    [SerializeField]
    private CharacterInstance _characterInstance;
    
    void Start()
    {
        // Load template and create instance
        CharacterData goblinTemplate = Resources.Load<CharacterData>("Characters/Goblin");
        _characterInstance = new CharacterInstance(goblinTemplate);
    }
    
    void TakeDamage(int amount)
    {
        // Modifies THIS enemy's instance only
        _characterInstance.TakeDamage(amount);
        
        if (_characterInstance.IsDead)
        {
            Die();
        }
    }
}
```

## Project Implementation Examples

### Example 1: SkillInstance

**Template (ScriptableObject):**
```csharp
[CreateAssetMenu(menuName = "Skills/Skill")]
public class Skill : ScriptableObject
{
    public string skillName;
    public Sprite icon;
    public int baseDamage;
    public SkillBehavior behavior;  // Another template
}
```

**Instance (Serializable Class):**
```csharp
[Serializable]
public class SkillInstance
{
    [SerializeField]
    private Skill _skillTemplate;
    
    // Per-character runtime state
    [SerializeField]
    private float _currentCooldown;
    
    [SerializeField]
    private int _useCount;
    
    [SerializeField]
    private bool _isLearned;
    
    public void Use(MonoBehaviour caster)
    {
        if (_currentCooldown > 0 || !_isLearned) return;
        
        // Execute behavior from template
        _skillTemplate.behavior.Execute(caster, this);
        
        // Update instance state
        _currentCooldown = 3f;
        _useCount++;
    }
    
    public void UpdateCooldown(float deltaTime)
    {
        if (_currentCooldown > 0)
            _currentCooldown -= deltaTime;
    }
}
```

**Usage in MonoBehaviour:**
```csharp
public class Character : MonoBehaviour
{
    [SerializeField]
    private List<SkillInstance> _skills = new List<SkillInstance>();
    
    void Start()
    {
        // Create instances from templates
        Skill fireballTemplate = Resources.Load<Skill>("Skills/Fireball");
        _skills.Add(new SkillInstance(fireballTemplate));
    }
    
    void Update()
    {
        // Each character updates their own skill cooldowns
        foreach (var skill in _skills)
        {
            skill.UpdateCooldown(Time.deltaTime);
        }
    }
}
```

### Example 2: CharacterInventoryInstance

**Template (ScriptableObject) - Optional:**
```csharp
[CreateAssetMenu(menuName = "Character/Inventory")]
public class CharacterInventory : ScriptableObject
{
    // Initial configuration only
    public int defaultCapacity = 6;
    public ObjectItem[] startingItems;
}
```

**Instance (Serializable Class):**
```csharp
[Serializable]
public class CharacterInventoryInstance
{
    [SerializeField]
    private List<ObjectItem> _items = new List<ObjectItem>();
    
    [SerializeField]
    private int _capacity;
    
    public void AddItem(ObjectItem item)
    {
        if (_items.Count < _capacity)
        {
            _items.Add(item);
        }
    }
    
    public void RemoveItem(ObjectItem item)
    {
        _items.Remove(item);
    }
}
```

### Example 3: CharacterInstance (Complex)

**Template (ScriptableObject):**
```csharp
[CreateAssetMenu(menuName = "Character/CharacterData")]
public class CharacterData : ScriptableObject
{
    // Static identity
    public string characterName;
    public string fullName;
    public Portrait[] portraits;
    
    // Base stats (templates)
    public int baseMaxHP;
    public int baseStrength;
    public List<BoundedCharacterStat> baseStats;
    
    // Initial configuration
    public int startingLevel = 1;
    public CharacterInventory startingInventory;
}
```

**Instance (Serializable Class):**
```csharp
[Serializable]
public class CharacterInstance
{
    [SerializeField]
    private CharacterData _template;
    
    // Runtime stats
    [SerializeField]
    private int _currentLevel;
    
    [SerializeField]
    private int _currentExp;
    
    [SerializeField]
    private List<BoundedCharacterStat> _runtimeStats;
    
    // Runtime inventory
    [SerializeField]
    private CharacterInventoryInstance _inventory;
    
    // Runtime skills
    [SerializeField]
    private List<SkillInstance> _skills;
    
    // Runtime state
    [SerializeField]
    private bool _isDead;
    
    public CharacterInstance(CharacterData template)
    {
        _template = template;
        
        // Deep copy stats from template
        _runtimeStats = new List<BoundedCharacterStat>();
        foreach (var stat in template.baseStats)
        {
            _runtimeStats.Add(new BoundedCharacterStat(stat));
        }
        
        // Initialize inventory from template
        _inventory = new CharacterInventoryInstance(template.startingInventory);
        
        _currentLevel = template.startingLevel;
        _currentExp = 0;
        _isDead = false;
    }
    
    public void TakeDamage(int amount)
    {
        var hpStat = _runtimeStats.Find(s => s.StatType == BoundedStatType.HP);
        if (hpStat != null)
        {
            hpStat.CurrentValue -= amount;
            if (hpStat.CurrentValue <= 0)
            {
                _isDead = true;
            }
        }
    }
}
```

## When to Use Instance Data vs Direct ScriptableObjects

### ✅ Use ScriptableObjects DIRECTLY (No Instance Needed):

1. **Pure Configuration Data**
   - Game settings (volume, graphics quality)
   - Constant lookup tables (damage type modifiers)
   - Static definitions (weapon types, element types)

2. **Shared Reference Data**
   - Character portraits/sprites (visual assets)
   - Skill icons
   - Item definitions (name, description, sprite)

3. **Stateless Logic**
   - Skill behaviors (pure functions)
   - Calculation formulas
   - Strategy pattern implementations

**Example:**
```csharp
// These DON'T need instances - they're read-only
public class WeaponType : ScriptableObject
{
    public string weaponName;
    public Sprite icon;
    public bool isMagic;
    // No runtime state to track
}

public class GameSettings : ScriptableObject  
{
    public float masterVolume;
    public bool showTutorials;
    // Configuration only, not per-game-session
}
```

### ⚠️ Use INSTANCE DATA Pattern When:

1. **Runtime Modification**
   - Health, mana, stamina (changes during gameplay)
   - Experience points, level
   - Inventory contents
   - Skill cooldowns

2. **Per-Entity State**
   - Multiple enemies of same type need different HP
   - Each character has their own inventory
   - Each player has their own save data

3. **Temporary Runtime Values**
   - Status effects (poisoned, buffed)
   - Combat state (attacking, defending)
   - AI state machine variables

**Example:**
```csharp
// These NEED instances - they have per-entity runtime state
public class CharacterInstance  // Instance for CharacterData
{
    private int _currentHP;      // Changes during combat
    private bool _isDead;        // Runtime state
    private List<StatusEffect> _activeEffects;  // Temporary
}

public class SkillInstance  // Instance for Skill
{
    private float _currentCooldown;  // Ticks down over time
    private int _useCount;           // Increments when used
}
```

## Common Mistakes to Avoid

### ❌ Mistake 1: Modifying ScriptableObject at Runtime

```csharp
// BAD!
public class Character : MonoBehaviour
{
    public CharacterData data;
    
    void TakeDamage(int amount)
    {
        data.currentHP -= amount;  // Modifies the ASSET FILE!
    }
}
```

**Why it's bad:** Changes persist between play sessions in editor, affects all characters using this template.

### ❌ Mistake 2: Not Deep Copying Collections

```csharp
// BAD!
public CharacterInstance(CharacterData template)
{
    _stats = template.baseStats;  // Shallow copy - still shares the list!
}

// GOOD!
public CharacterInstance(CharacterData template)
{
    _stats = new List<Stat>();
    foreach (var stat in template.baseStats)
    {
        _stats.Add(new Stat(stat));  // Deep copy each stat
    }
}
```

### ❌ Mistake 3: Storing Instance Data in ScriptableObject

```csharp
// BAD!
[CreateAssetMenu]
public class Skill : ScriptableObject
{
    public string skillName;  // ✅ Template data
    public float cooldownTime;  // ✅ Template data
    
    private float _currentCooldown;  // ❌ Runtime state - wrong place!
}
```

**Fix:** Move `_currentCooldown` to SkillInstance instead.

## Testing Instance Data Correctly

### Editor Testing Gotcha

In the Unity Editor, changes to ScriptableObjects persist between play sessions. This can mask bugs!

**Test properly:**
1. Make changes during play mode
2. Stop play mode
3. Check if ScriptableObject asset file was modified
4. If it was, you're modifying the template instead of an instance!

**Example:**
```csharp
[CreateAssetMenu]
public class TestData : ScriptableObject
{
    public int value = 100;
}

// In play mode:
testData.value = 50;  // Modifies the asset file!

// After stopping play mode:
// The asset file now shows value = 50 permanently!
```

## Complete Example: Combat System

```csharp
// ============ TEMPLATES (ScriptableObjects) ============

[CreateAssetMenu]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public int baseMaxHP;
    public int baseAttack;
}

[CreateAssetMenu]
public class Skill : ScriptableObject
{
    public string skillName;
    public int damage;
    public float cooldownTime;
    public SkillBehavior behavior;
}

// ============ INSTANCES (Serializable) ============

[Serializable]
public class CharacterInstance
{
    [SerializeField] private CharacterData _template;
    [SerializeField] private int _currentHP;
    [SerializeField] private List<SkillInstance> _skills;
    
    public CharacterInstance(CharacterData template)
    {
        _template = template;
        _currentHP = template.baseMaxHP;
        _skills = new List<SkillInstance>();
    }
    
    public void AddSkill(Skill skillTemplate)
    {
        _skills.Add(new SkillInstance(skillTemplate));
    }
    
    public void TakeDamage(int amount)
    {
        _currentHP -= amount;
    }
}

[Serializable]
public class SkillInstance
{
    [SerializeField] private Skill _template;
    [SerializeField] private float _currentCooldown;
    
    public SkillInstance(Skill template)
    {
        _template = template;
        _currentCooldown = 0f;
    }
    
    public bool CanUse() => _currentCooldown <= 0f;
    
    public void Use(MonoBehaviour caster)
    {
        if (!CanUse()) return;
        
        _template.behavior.Execute(caster, this);
        _currentCooldown = _template.cooldownTime;
    }
    
    public void UpdateCooldown(float deltaTime)
    {
        if (_currentCooldown > 0f)
            _currentCooldown -= deltaTime;
    }
}

// ============ RUNTIME USAGE (MonoBehaviour) ============

public class Enemy : MonoBehaviour
{
    [SerializeField] private CharacterInstance _character;
    
    void Start()
    {
        // Load template and create instance
        var goblinData = Resources.Load<CharacterData>("Characters/Goblin");
        _character = new CharacterInstance(goblinData);
        
        // Add skills
        var fireballSkill = Resources.Load<Skill>("Skills/Fireball");
        _character.AddSkill(fireballSkill);
    }
    
    void Update()
    {
        // Each enemy updates their own skill cooldowns
        foreach (var skill in _character.Skills)
        {
            skill.UpdateCooldown(Time.deltaTime);
        }
    }
}
```

## Key Takeaways

1. **ScriptableObjects = Shared Templates**
   - Read-only at runtime
   - Configuration and static data
   - Shared across all instances

2. **Instance Classes = Per-Entity State**
   - Mutable at runtime
   - Each entity has their own
   - Holds temporary/changing data

3. **Pattern Structure:**
   ```
   ScriptableObject (Asset) → Instance Class (Runtime) → MonoBehaviour (GameObject)
   ```

4. **When in doubt:** If the data changes during gameplay and needs to be different per-entity, it belongs in an instance class.

---

**Document Version:** 2.0  
**Last Updated:** November 6, 2025  
**Author:** GitHub Copilot  
**Related Files:**
- `Assets/Prototypes/Graphics2D/Components/StackedImage.cs`
- `Assets/Prototypes/Characters/Components/Portrait.cs`
- `Assets/Prototypes/Graphics2D/Components/Editor/StackedImageEditorWindow.cs`
- `Assets/Prototypes/Characters/Components/Editor/PortraitEditorWindow.cs`
