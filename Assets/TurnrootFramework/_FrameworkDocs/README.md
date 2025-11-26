# Turnroot Framework — Operational Docs

This folder contains concise operational documentation for the framework systems related to:

- Serialization, encoding/decoding and anti-tamper guard rails
- Tamper policies and LongTermMemory ledger behavior
- Unique instance handling for templates marked `IsUnique`
- Tests, editor utilities and maintenance checklists

Use these notes while maintaining, debugging, or extending the GamewideContext serialization/anti-tamper systems.

Contents
- Gameplay/
  - GamewideContext/Serialization.md — wrappers, payload and hashing details
  - GamewideContext/TamperPolicy.md — ledger & tamper policy rules and troubleshooting
  - GamewideContext/Samples.md — copy-paste examples for encode/decode and testing
- Characters/
  - UniqueInstances.md — rules & usage for CharacterInstance.Create and UniqueInstanceRegistry
- Guides/
  - Testing.md — tests and CI guidance for serialization & uniqueness systems
  - Checklists.md — operational checklists and migration tips
# Prototype Systems Documentation

Complete API reference for the Turnroot Framework assets.

## Core Systems

### Character System
- **[CharacterData](Characters/Character.md)** - Main character configuration asset
- **[CharacterStats](Characters/CharacterStats.md)** - Stat system (bounded and unbounded)
- **[CharacterComponents](Characters/CharacterComponents.md)** - Pronouns, relationships, traits
- **[CharacterInventory](Characters/CharacterInventory.md)** - Multi-slot equipment and inventory
### Brain System
- **[GamewideContextBrain](Gameplay/GamewideContextBrain.md)** - Runtime instance factory, serialization and rehydration

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
 - **[Map Grid Editor](Maps/MapGridEditorWindow.md)** - Map painting, features, movement testing

### Map Systems
- **[MapGrid](Maps/MapGrid.md)** - Grid container for tiles
- **[MapGridPoint](Maps/MapGridPoint.md)** - Per-tile properties and feature storage
- **[MapGridEditorSettings](Maps/MapGridEditorSettings.md)** - Editor colors and layout settings
- **[MapGridPropertyBase](Maps/Property Components/MapGridPropertyBase.md)** - Typed property containers
- **[MapGridPointFeature](Maps/Property Components/MapGridPointFeature.md)** - Feature enum/ID helpers
- **[MapGridFeatureProperties](Maps/Property Components/MapGridFeatureProperties.md)** - Feature default assets

---

## DOCUMENTATION LOCATION
- **Base Path**: `Assets/TurnrootFramework/_FrameworkDocs/`
- **Entry Point**: `README.md` (index with quick reference)

## CURRENT DOCUMENTATION STRUCTURE

### Files and Their Coverage (short)
```
TurnrootFramework/_FrameworkDocs/
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
  ├── Templates/                                   # Useful templates for adding new systems
  │   └── DataToInstanceChecklist.md               # Checklist & guide for adding Data->Instance pairs
```

## DOCUMENTATION MAPPING TO SOURCE FILES

### Characters/Character.md
**Source**: `Assets/TurnrootFramework/Characters/CharacterData.cs`
Summary: main ScriptableObject for character templates (stats, portraits, skills). See `CharacterData` API in the doc.

### Characters/Portraits/Portrait.md
**Source**: `Assets/TurnrootFramework/Characters/Components/Portrait.cs`
Summary: portrait composition (ImageStack), tinting and rendering to PNG; used by `CharacterData` and the `PortraitEditorWindow`.

### Characters/Portraits/ImageStack.md
**Sources**: `Assets/TurnrootFramework/Graphics2D/Components/ImageStack.cs`, `Assets/TurnrootFramework/Graphics2D/Components/ImageStackLayer.cs`
Summary: ImageStack and ImageStackLayer define layer lists used by Portrait/SkillBadge composition.

### Tools/ImageCompositor.md
**Source**: `Assets/TurnrootFramework/AbstractScripts/Graphics2D/ImageCompositor.cs`
Summary: low-level compositor used by `StackedImage.Render()`; includes mask-based tinting API.

### Characters/CharacterStats.md
**Sources**: `Assets/TurnrootFramework/Characters/Components/Stats/*`
Summary: Bounded & unbounded stat containers and helper conversions used in characters.

### Characters/CharacterComponents.md
**Sources**: `Assets/TurnrootFramework/Characters/Components/*`
Summary: small data containers & serializable types used by `CharacterData` (Pronouns, SupportRelationship, HereditaryTraits).

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
- Integration with CharacterInventoryInstance, ObjectSubtype, WeaponType, Aptitude

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
**Sources**: `Assets/TurnrootFramework/Skills/Nodes/*`
Summary: Visual xNode graph system for skill behavior - nodes for events, conditions and flow execution.

### Configurations/Settings.md
**Sources**: `Assets/TurnrootFramework/Configurations/*` and `Resources/GameSettings/*`
Summary: Global ScriptableObjects that configure character & graphics pipeline; provides editor `OnValidate()` hooks.

### Configurations/DefaultCharacterStats.md
**Source**: `Assets/TurnrootFramework/Characters/DefaultCharacterStats.cs`
**Documents**:
- DefaultCharacterStats: ScriptableObject for default stat initialization
- DefaultBoundedStat and DefaultUnboundedStat nested classes
- CreateBoundedStats() and CreateUnboundedStats() methods
- **Integration**: Character.OnEnable() auto-loads from Resources
- **Initialization**: Only applies if character has no stats

### Configurations/Components/ExperienceTypes.md
**Sources**:
- `Assets/TurnrootFramework/Gameplay/Combat/FundamentalComponents/ExperienceType.cs`
- `Assets/TurnrootFramework/Gameplay/Combat/Objects/Components/WeaponType.cs`
- `Assets/TurnrootFramework/Gameplay/Combat/FundamentalComponents/Editor/ExperienceTypeDrawer.cs`
**Documents**:
- ExperienceType: Name, Id (auto-generated), Enabled, HasWeaponType, AssociatedWeaponType
- **ID Generation**: Lowercase, space-stripped from Name, private setter
- WeaponType: Name, Icon, Id, Ranges[], DefaultRange
- ExperienceTypeDrawer: Custom property drawer with conditional visibility, auto-ID generation
- **Serialization**: Both use [SerializeField] backing fields with property wrappers
- Integration with GameplayGeneralSettings

### Tools/PortraitEditorWindow.md
**Source**: `Assets/TurnrootFramework/Characters/Components/Editor/PortraitEditorWindow.cs`
Summary: Editor UI for composing and rendering portraits with live preview.

### Skills/Skill.md
**Source**: `Assets/TurnrootFramework/Skills/Skill.cs`
Summary: Skill asset containing behavior graph, UI accent colors, and badge assets.

### Skills/SkillBadge.md
**Source**: `Assets/TurnrootFramework/Skills/Components/Badges/SkillBadge.cs`
**Documents**:
- Inherits `StackedImage<Skill>` - specialized for skill badge graphics
- `GetSaveSubdirectory()` - returns "SkillBadges"
- `UpdateTintColorsFromOwner()` - syncs from Skill.AccentColor1/2/3
- Save path: `Assets/Resources/GameContent/Graphics/SkillBadges/{Key}.png`
- Tint color array safety (always 3 elements, white default)

### Skills/SkillBadgeEditorWindow.md
**Source**: `Assets/TurnrootFramework/Skills/Components/Badges/Editor/SkillBadgeEditorWindow.cs`
**Documents**:
- Inherits `StackedImageEditorWindow<Skill, SkillBadge>`
- MenuItem: "Turnroot/Editors/Skill Badge Editor"
- `OpenSkillBadge(Skill, int)` - static method to open with pre-loaded skill
- `GetImagesFromOwner(Skill)` - returns array with skill's Badge
- Window properties: WindowTitle, OwnerFieldLabel
- Opened via reflection from Skill.CreateNewBadge()

### Graphics2D/StackedImage.md
**Source**: `Assets/TurnrootFramework/Graphics2D/Components/StackedImage.cs`
Summary: Base class for layered image composition with rendering to PNG and editor integration.