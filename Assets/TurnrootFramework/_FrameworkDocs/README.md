# Prototype Systems Documentation

Complete API reference for Assets/Prototypes systems.

## Core Systems

### Character System
- **[Character](Characters/Character.md)** - Main character configuration asset
- **[CharacterStats](Characters/CharacterStats.md)** - Stat system (bounded and unbounded)
- **[CharacterComponents](Characters/CharacterComponents.md)** - Pronouns, relationships, traits
- **[CharacterInventory](Characters/CharacterInventory.md)** - Multi-slot equipment and inventory

### Gameplay Objects System
- **[ObjectItem](Gameplay/ObjectItem.md)** - Item assets (weapons, magic, consumables, gifts, etc.)
- **[ObjectSubtype](Gameplay/ObjectSubtype.md)** - Dynamic item type validation system

### Portrait System
- **[Portrait](Characters/Portraits/Portrait.md)** - Compositable character portraits
- **[ImageStack](Characters/Portraits/ImageStack.md)** - Layer container and ImageStackLayer
- **[ImageCompositor](Tools/ImageCompositor.md)** - Static compositor utility

### Skills System
- **[Skill](Skills/Skill.md)** - Skill container asset with badge graphics
- **[SkillBadge](Skills/SkillBadge.md)** - Compositable skill badge graphics
- **[SkillBadgeEditorWindow](Skills/SkillBadgeEditorWindow.md)** - Badge editor tool
- **[Skill Node System](Skills/Nodes/README.md)** - xNode-based visual skill behavior graphs
  - SkillGraph execution system
  - Event nodes (stat modification, damage, combat effects)
  - Port types and execution flow
  - Multi-target patterns

### Graphics2D System
- **[StackedImage](Graphics2D/StackedImage.md)** - Abstract base for layered compositable images

### Configuration
- **[Settings](Configurations/Settings.md)** - CharacterPrototypeSettings, GraphicsPrototypesSettings
- **[DefaultCharacterStats](Characters/DefaultCharacterStats.md)** - Default stat initialization
- **[ExperienceTypes](Configurations/Components/ExperienceTypes.md)** - Combat experience and weapon types

### Editor Tools
- **[PortraitEditorWindow](Tools/PortraitEditorWindow.md)** - Portrait editing interface

---

## DOCUMENTATION LOCATION
- **Base Path**: `Assets/PrototypeDocs/`
- **Entry Point**: `README.md` (index with quick reference)

## CURRENT DOCUMENTATION STRUCTURE

### Files and Their Coverage
```
PrototypeDocs/
├── README.md                                    # Index, quick ref, architecture, troubleshooting
├── Characters/                                  # Character system documentation
│   ├── Character.md                            # Character class (main asset)
│   ├── CharacterStats.md                       # CharacterStat + BoundedCharacterStat
│   ├── CharacterComponents.md                  # Pronouns, SupportRelationship, HereditaryTraits, CharacterWhich
│   ├── CharacterInventory.md                   # Multi-slot equipment and inventory system
│   ├── DefaultCharacterStats.md                # Default stat initialization
│   └── Portraits/                              # Portrait sub-system
│       ├── Portrait.md                         # Portrait class (compositable portraits)
│       └── ImageStack.md                       # ImageStack + ImageStackLayer
├── Gameplay/                                    # Gameplay objects system
│   ├── ObjectItem.md                           # ObjectItem ScriptableObject (items, weapons, etc.)
│   └── ObjectSubtype.md                        # Dynamic item type validation
├── Skills/                                      # Skills system documentation
│   ├── Skill.md                                # Skill asset with badge graphics
│   ├── SkillBadge.md                           # SkillBadge (StackedImage<Skill>)
│   ├── SkillBadgeEditorWindow.md               # Badge editor window
│   └── Nodes/                                  # xNode skill execution system
│       └── README.md                           # Node architecture, Event nodes, execution flow
├── Graphics2D/                                  # Graphics2D base systems
│   └── StackedImage.md                         # StackedImage<TOwner> abstract base class
├── Configurations/                              # Settings and configuration systems
│   ├── Settings.md                             # CharacterPrototypeSettings + GraphicsPrototypesSettings
│   └── Components/                             # Configuration components
│       └── ExperienceTypes.md                  # ExperienceType + WeaponType (combat system)
└── Tools/                                       # Editor tools and utilities
    ├── PortraitEditorWindow.md                 # Portrait editor window
    ├── StackedImageEditorWindow.md             # Base editor for StackedImage (documented inline)
    └── ImageCompositor.md                      # Static compositor utility
```

## DOCUMENTATION MAPPING TO SOURCE FILES

### Characters/Character.md
**Source**: `Assets/Prototypes/Characters/Character.cs`
**Documents**:
- All properties (Identity, Demographics, Description, Flags, Visual, Stats, etc.)
- Lifecycle: `OnEnable()` - loads CharacterPrototypeSettings
- Methods: `GetBoundedStat()`, `GetUnboundedStat()`, `GetClassExp()`, `SetClassExp()`, support relationship methods
- Integration with accent colors → Portrait tinting

### Characters/Portraits/Portrait.md
**Source**: `Assets/Prototypes/Characters/Components/Portrait.cs`
**Documents**:
- Properties: Owner, ImageStack, Key, RuntimeSprite, SavedSprite, Id, TintColors
- Constructor + `OnAfterDeserialize()`
- Methods: `SetOwner()`, `SetKey()`, `UpdateTintColorsFromOwner()`, `Render()`, `CompositeLayers()`
- **Rendering Pipeline** (critical):
  1. CompositeLayers() → compositor
  2. SaveToFile() → PNG to disk
  3. LoadSavedSprite() → configure texture importer + load asset
- **Tinting System**: RGB mask channels → 3 tint colors

### Characters/Portraits/ImageStack.md
**Sources**: 
- `Assets/Prototypes/Graphics2D/Components/Portrait/ImageStack.cs`
- `Assets/Prototypes/Graphics2D/Components/ImageStackLayer.cs`
**Documents**:
- ImageStack: Layer container with `Layers` list and optional `OwnerCharacter` reference
- ImageStackLayer: Sprite, Mask, Offset, Scale, Rotation, Order
- Mask-based tinting (RGB channels)
- Transform application order
- **Note**: Rendering logic handled by `StackedImage<TOwner>.CompositeLayers()`, not ImageStack methods

### Tools/ImageCompositor.md
**Source**: `Assets/AbstractScripts/Graphics2D/ImageCompositor.cs`
**Documents**:
- Static methods: `CreateSpriteFromTexture()`, `TintSpritePixels()`, `CompositeImageStackLayers()`
- **Tinting Algorithm**: 
  - Normalize RGB weights
  - Blend 3 tint colors proportionally
  - Lerp by totalStrength
- **Compositing Process**: Sort by Order → tint → scale → offset → alpha blend
- **Scaling**: Nearest-neighbor, iterate destination pixels

### Characters/CharacterStats.md
**Sources**:
- `Assets/Prototypes/Characters/Components/Stats/CharacterStat.cs`
- `Assets/Prototypes/Characters/Components/Stats/BoundedCharacterStat.cs`
- `Assets/Prototypes/Characters/Components/Stats/StatTypes.cs`
**Documents**:
- CharacterStat: Unbounded, Current + Bonus
- BoundedCharacterStat: Min/Max bounds, Ratio for progress bars
- Both: Implicit int conversion
- StatTypes enums: UnboundedStatType, BoundedStatType

### Characters/CharacterComponents.md
**Sources**:
- `Assets/Prototypes/Characters/Components/Pronouns.cs`
- `Assets/Prototypes/Characters/Components/SupportRelationship.cs`
- `Assets/Prototypes/Characters/Components/HereditaryTraits.cs`
- `Assets/Prototypes/Characters/Components/CharacterWhich.cs`
**Documents**:
- Pronouns: they/she/he sets, `Use()` for template replacement
- SupportRelationship: Character ref, levels (None/C/B/A/S), points, speed
- HereditaryTraits: Colors, passed skills/traits, growth bonuses
- CharacterWhich enum: Player/Enemy/Ally/Neutral

### Characters/CharacterInventory.md
**Source**: `Assets/TurnrootFramework/Characters/Components/Inventory/CharacterInventoryInstance.cs`
**Documents**:
- Serializable class for per-character inventory management
- **Equipment System**: 3-slot array (Weapon/Shield/Accessory)
- Properties: InventoryItems, Capacity, CurrentItemCount, CurrentWeight
- Equipment state: EquippedItemIndices array, IsWeaponEquipped/Shield/Accessory flags
- Methods: GetEquippedItemIndex(), IsItemEquipped(), AddToInventory(), RemoveFromInventory(), EquipItem(), UnequipItem(int), UnequipAllItems(), ReorderItem()
- **Slot Mapping**: Uses ObjectSubtype and EquipableObjectType for slot determination
- **Auto-Unequip**: Equipping same type replaces previous item in slot
- **Index Tracking**: Adjusts equipped indices on remove/reorder operations
- Integration with ObjectItem and ObjectSubtype

### Gameplay/ObjectItem.md
**Source**: `Assets/TurnrootFramework/Gameplay/Objects/ObjectItem.cs`
**Namespace**: `Turnroot.Gameplay.Objects`
**Documents**:
- ScriptableObject for gameplay items (weapons, magic, consumables, gifts, etc.)
- **Identity**: Name, ID, flavor text, icon, equipable type
- **Type System**: ObjectSubtype integration, WeaponType reference
- **Pricing**: Base price, sellable/buyable flags, sell price reduction
- **Repair System**: Repair costs, item requirements, forge options
- **Lost Items**: Owner tracking, belongs-to character
- **Gift System**: Gift rank, character love/hate preferences
- **Attack Range**: Lower/upper range, stat-based range adjustments
- **Durability**: Uses tracking, max uses, replenish options
- **Stats**: Weight system
- **Aptitude**: Minimum aptitude level for usage
- **Helper Methods**: NaughtyAttributes ShowIf conditions for Inspector organization
- Integration with CharacterInventoryInstance, ObjectSubtype, WeaponType, AptitudeLevel

### Gameplay/ObjectSubtype.md
**Source**: `Assets/TurnrootFramework/Gameplay/Objects/Components/ObjectSubtype.cs`
**Documents**:
- Serializable class for dynamic item type validation
- **Constants**: Weapon, Magic, Consumable, Equipable, Gift, LostItem
- **Dynamic Validation**: IsValid() checks existence, IsEnabled() checks settings
- **Settings Integration**: Gift and LostItem types respect GameplayGeneralSettings
- **Methods**: GetValidValues(), IsValid(), IsEnabled()
- **Properties**: IsWeapon, IsMagic, IsConsumable, IsEquipable, IsGift, IsLostItem
- **Operators**: Implicit string conversion, equality operators
- **Design Pattern**: Class-based constrained string wrapper instead of enum
- Integration with GameplayGeneralSettings, ObjectItem, CharacterInventoryInstance

### Skills/Nodes/README.md
**Sources**:
- `Assets/Prototypes/Skills/Nodes/Core/*.cs`
- `Assets/Prototypes/Skills/Nodes/Nodes/Events/*.cs`
- `Assets/Prototypes/Skills/Nodes/Nodes/Conditions/*.cs`
- `Assets/Prototypes/Gameplay/Combat/FundamentalComponents/Battles/BattleContext.cs`
**Documents**:
- SkillGraph: NodeGraph container, Execute(BattleContext), validation
- SkillNode: Base class with Execute(BattleContext), GetInputFloat/GetInputBool helpers
- BattleContext: UnitInstance, Targets, Allies, AdjacentUnits, CustomData, EnvironmentalConditions, SkillUseCount
- Port types: ExecutionFlow, BoolValue, FloatValue, StringValue
- **Event nodes**: 30+ nodes for stat modification, damage, combat effects, positioning
- **Condition nodes**: 20+ nodes for stat comparisons, unit state, combat state, spatial queries
- Multi-target pattern: BoolValue affectAllTargets input
- Adjacency system: Direction enum with 9-direction grid
- Helper methods for simplified node development

### Configurations/Settings.md
**Sources**:
- `Assets/Prototypes/Characters/CharacterPrototypeSettings.cs`
- `Assets/Prototypes/Graphics2D/GraphicsPrototypesSettings.cs`
**Documents**:
- CharacterPrototypeSettings: `UseAccentColors`, OnValidate propagation
- GraphicsPrototypesSettings: Portrait render dimensions
- **Critical**: OnValidate behavior, EditorApplication.delayCall, IsAssetImportWorkerProcess check
- Loading pattern: `Resources.Load<T>("Path")`

### Configurations/DefaultCharacterStats.md
**Source**: `Assets/Prototypes/Characters/DefaultCharacterStats.cs`
**Documents**:
- DefaultCharacterStats: ScriptableObject for default stat initialization
- DefaultBoundedStat and DefaultUnboundedStat nested classes
- CreateBoundedStats() and CreateUnboundedStats() methods
- **Integration**: Character.OnEnable() auto-loads from Resources
- **Initialization**: Only applies if character has no stats

### Configurations/Components/ExperienceTypes.md
**Sources**:
- `Assets/Prototypes/Gameplay/Combat/FundamentalComponents/ExperienceType.cs`
- `Assets/Prototypes/Gameplay/Combat/Objects/Components/WeaponType.cs`
- `Assets/Prototypes/Gameplay/Combat/FundamentalComponents/Editor/ExperienceTypeDrawer.cs`
**Documents**:
- ExperienceType: Name, Id (auto-generated), Enabled, HasWeaponType, AssociatedWeaponType
- **ID Generation**: Lowercase, space-stripped from Name, private setter
- WeaponType: Name, Icon, Id, Ranges[], DefaultRange
- ExperienceTypeDrawer: Custom property drawer with conditional visibility, auto-ID generation
- **Serialization**: Both use [SerializeField] backing fields with property wrappers
- Integration with GameplayGeneralSettings

### Tools/PortraitEditorWindow.md
**Source**: `Assets/Prototypes/Characters/Components/Editor/PortraitEditorWindow.cs`
**Documents**:
- Editor window: Tools > Portrait Editor
- UI layout and controls
- Workflow: character selection → portrait config → layer management → tinting → save
- Reflection usage for private field assignment
- Live preview system
- Integration points with Portrait.Render()

### Skills/Skill.md
**Source**: `Assets/Prototypes/Skills/Skill.cs`
**Documents**:
- Properties: AccentColor1/2/3, Badge, SkillName, Description
- UnityEvents: ReadyToFire, SkillTriggered, ActionCompleted, SkillEquipped, SkillUnequipped
- Method: `CreateNewBadge()` - creates SkillBadge and opens editor window via reflection
- Integration with Character.Skills and Character.SpecialSkills
- NaughtyAttributes usage for inspector UI
- Incomplete features: NodeConnections, EventNodes (commented out)

### Skills/SkillBadge.md
**Source**: `Assets/Prototypes/Skills/Components/Badges/SkillBadge.cs`
**Documents**:
- Inherits `StackedImage<Skill>` - specialized for skill badge graphics
- `GetSaveSubdirectory()` - returns "SkillBadges"
- `UpdateTintColorsFromOwner()` - syncs from Skill.AccentColor1/2/3
- Save path: `Assets/Resources/GameContent/Graphics/SkillBadges/{Key}.png`
- Tint color array safety (always 3 elements, white default)

### Skills/SkillBadgeEditorWindow.md
**Source**: `Assets/Prototypes/Skills/Components/Badges/Editor/SkillBadgeEditorWindow.cs`
**Documents**:
- Inherits `StackedImageEditorWindow<Skill, SkillBadge>`
- MenuItem: "Window/Turnroot/Skill Badge Editor"
- `OpenSkillBadge(Skill, int)` - static method to open with pre-loaded skill
- `GetImagesFromOwner(Skill)` - returns array with skill's Badge
- Window properties: WindowTitle, OwnerFieldLabel
- Opened via reflection from Skill.CreateNewBadge()

### Graphics2D/StackedImage.md
**Source**: `Assets/Prototypes/Graphics2D/Components/StackedImage.cs`
**Documents**:
- Abstract generic class `StackedImage<TOwner>` where TOwner : UnityEngine.Object
- Properties: Owner, ImageStack, Key, RuntimeSprite, SavedSprite, Id, TintColors
- Methods: `SetOwner()`, `SetKey()`, `Render()`, `CompositeLayers()`, `ToString()`, `Identify()`
- Internal helper: `EnsureKeyInitialized()` - consolidates key generation logic
- Abstract methods: `UpdateTintColorsFromOwner()`, `GetSaveSubdirectory()`
- **Rendering Pipeline**: Render() → CompositeLayers() → SaveToFile() → LoadSavedSprite()
- **CompositeLayers()**: Loads dimensions from `GraphicsPrototypesSettings`, creates base texture, calls ImageCompositor
- **Lifecycle**: Constructor, OnAfterDeserialize() for GUID persistence
- Tint color system (3-color RGB mask channels)
- File path structure and editor-only features
- Implementations: SkillBadge, Portrait