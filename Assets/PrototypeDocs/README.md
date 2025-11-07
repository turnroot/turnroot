# Prototype Systems Documentation

Complete API reference for Assets/Prototypes systems.

## Core Systems

### Character System
- **[Character](Characters/Character.md)** - Main character configuration asset
- **[CharacterStats](Characters/CharacterStats.md)** - Stat system (bounded and unbounded)
- **[CharacterComponents](Characters/CharacterComponents.md)** - Pronouns, relationships, traits
- **[CharacterInventory](Characters/CharacterInventory.md)** - Multi-slot equipment and inventory

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
│   └── Portraits/                              # Portrait sub-system
│       ├── Portrait.md                         # Portrait class (compositable portraits)
│       └── ImageStack.md                       # ImageStack + ImageStackLayer
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
│   ├── DefaultCharacterStats.md                # Default stat initialization
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
- ImageStack: Layer container, `PreRender()`, `Render()` (stub)
- ImageStackLayer: Sprite, Mask, Offset, Scale, Rotation, Order
- Mask-based tinting (RGB channels)
- Transform application order

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
**Source**: `Assets/Prototypes/Characters/Components/Inventory/CharacterInventory.cs`
**Documents**:
- ScriptableObject for inventory management
- **Equipment System**: 3-slot array (Weapon/Shield/Accessory)
- Properties: InventoryItems, Capacity, CurrentItemCount, CurrentWeight
- Equipment state: EquippedItemIndices array, IsWeaponEquipped/Shield/Accessory flags
- Methods: GetEquippedItemIndex(), IsItemEquipped(), AddToInventory(), RemoveFromInventory(), EquipItem(), UnequipItem(int), UnequipAllItems(), ReorderItem()
- **Slot Mapping**: ObjectItemType → slot index (Weapon=0, Shield=1, Accessory=2)
- **Auto-Unequip**: Equipping same type replaces previous item in slot
- **Index Tracking**: Adjusts equipped indices on remove/reorder operations
- Integration with ObjectItem and ObjectItemType enum

### Skills/Nodes/README.md
**Sources**:
- `Assets/Prototypes/Skills/Nodes/Core/*.cs`
- `Assets/Prototypes/Skills/Nodes/Nodes/Events/*.cs`
**Documents**:
- SkillGraph: NodeGraph container, Execute(context), validation
- SkillNode: Base class with Execute(context), port system
- SkillExecutionContext: UnitInstance, Targets, CustomData
- Port types: ExecutionFlow, BoolValue, FloatValue, StringValue
- Event nodes: 15+ nodes for stat modification, damage, combat effects
- Multi-target pattern: BoolValue affectAllTargets input
- Node categories and color coding

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
- Abstract methods: `UpdateTintColorsFromOwner()`, `GetSaveSubdirectory()`
- **Rendering Pipeline**: Render() → CompositeLayers() → SaveToFile() → LoadSavedSprite()
- **Lifecycle**: Constructor, OnAfterDeserialize() for GUID persistence
- Tint color system (3-color RGB mask channels)
- File path structure and editor-only features
- Implementations: SkillBadge, Portrait