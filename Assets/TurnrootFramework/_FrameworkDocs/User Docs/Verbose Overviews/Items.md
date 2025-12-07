# Understanding the Turnroot Item System

Let me walk you through the item system in the same way we explored characters and classes, starting with the core concepts and building up to how everything works together in practice.

## The Template-Instance Pattern Returns

Just like with characters, the item system uses the template-instance pattern. **ObjectItem** is your template—a ScriptableObject asset you create in your project that defines what an Iron Sword or Health Potion should be. It describes the item's properties, stats, durability, price, and all the rules about how it behaves. This template never changes during gameplay.

**ObjectItemInstance** is the actual item that exists in a character's inventory at runtime. When a character picks up an Iron Sword, the system creates an ObjectItemInstance based on the Iron Sword template. This instance tracks runtime state like current durability (how many uses are left), which character owns it, and its unique identifier. As your game runs, the instance changes—the sword gets used and loses durability, it might get sold or transferred between characters—but the template remains pristine in your project.

This separation is crucial because it means you can have twenty Iron Swords in your game world, each with different durability and owners, all based on a single ObjectItem template. The template describes what an Iron Sword fundamentally is, while each instance represents a specific physical sword that exists in your game right now.

## Understanding Item Subtypes

The first thing to understand about items is that they come in different fundamental categories, controlled by the **ObjectSubtype** system. This isn't just organizational—the subtype determines what fields are available on an item and how the item behaves throughout your game.

**Weapon** items are equippable combat tools. When you mark something as a Weapon, the Inspector reveals combat-specific fields like Might (damage), Hit (accuracy), Critical chance, Weight, and stat bonuses. Weapons have a WeaponType (like Sword, Lance, Axe), they can have durability that degrades with use, and they can be repaired or forged into better versions. A character equips a weapon to use it in combat, and only one weapon can be equipped at a time.

**Magic** items work similarly to weapons but represent spell tomes or magical implements. They follow most of the same rules as weapons—they have combat stats, durability, and can be equipped—but they're conceptually separate. Your game might treat physical weapons and magic differently in combat calculations or class restrictions.

**Equipable** items are non-weapon equipment like shields, accessories, or rings. These have an **EquipableObjectType** that further categorizes them. A shield might provide defensive bonuses, while an accessory might boost stats or grant special abilities. Unlike weapons, multiple equipable items can often be worn at once, depending on your game's rules. The number of non-weapon equipment slots is controlled by `GameplayGeneralSettings.GetMaxEquippedNonWeaponItems()`, which defaults to two.

**Consumable** items are things like potions or food that get used up when activated. They typically don't have durability in the weapon sense—instead, they exist in your inventory until consumed, then they're gone. You might have a Health Potion that restores HP or a Stat Booster that permanently increases a character's stats.

**Gift** items are presents you can give to characters to improve relationships. Each gift has a rank (determining its effectiveness) and lists of characters who love or hate receiving this particular gift. When you give a character a gift they love, you might gain a large support point bonus. Gifts they hate could damage your relationship. This subtype is optional and controlled by `GameplayGeneralSettings.UseItemsCanBeGifts()`.

**LostItem** items belong to specific characters and can be returned to them. Each lost item has a `BelongsTo` field pointing to a CharacterData. When you find someone's lost item and return it, you might gain support points or unlock special dialogue. Like gifts, this is optional and controlled by `GameplayGeneralSettings.UseItemsCanBeLostItems()`.

The ObjectSubtype class is clever about settings integration. When you look at the dropdown in the Inspector, it only shows subtypes that are currently enabled in your GameplayGeneralSettings. If you've disabled the gift system, Gift won't appear as an option. This prevents you from accidentally creating items of types your game doesn't support.

## Creating Your First Item

Let's walk through creating a weapon to see how everything comes together. Right-click in your Project window and navigate to Create → Turnroot → Objects → Gameplay Item. This creates a new ObjectItem ScriptableObject. Select it and examine the Inspector—you'll see numerous foldout sections, but many are hidden initially.

In the **Identity** section, you set the item's name and flavor text. There's also an `_isUnequippable` flag for weapons and magic—if true, the item can never be dropped, sold, or transferred. This is useful for legendary weapons that should stay with their owner permanently or quest items that must remain in inventory.

The **Type** section is where you choose the subtype. Select "Weapon" and watch as new sections appear in the Inspector. Now you'll see fields for WeaponType, Combat stats, Durability, Repair options, and more. Each subtype reveals its own relevant configuration options.

Choose or create a **WeaponType** for your weapon. WeaponTypes are separate ScriptableObject assets that define categories like Sword, Lance, Axe, Bow, or magic types like Fire, Thunder, Wind. Each WeaponType has a name, icon, unique ID, and a triangle position for the weapon triangle system.

## The Weapon Triangle System

The weapon triangle is a rock-paper-scissors mechanic common in tactical RPGs. The **TrianglePosition** class handles this. Positions are Top, Left, Right, or NotOnTriangle. The system is circular: Top beats Left, Left beats Right, Right beats Top. When you create a WeaponType, you assign it a triangle position. In combat, if a Sword (Top position) fights a Lance (Left position), the Sword gets an advantage. If it fights an Axe (Right position), the Sword is at a disadvantage.

The TrianglePosition class has methods like `WinsAgainst(other)` and `LosesTo(other)` that you can use in your combat calculations. Magic types might occupy different positions or be NotOnTriangle entirely, exempting them from the system. You configure which position each WeaponType occupies, giving you complete control over your game's balance.

## Combat Statistics and Durability

In the **Combat** foldout, you define the weapon's statistics. **Might** is base damage—how much hurt this weapon inflicts. **Hit** is accuracy—the chance to actually land a blow. **Critical** is critical hit chance. **Weight** affects the wielder's effective speed—heavier weapons slow you down. The StatBonuses dictionary lets you grant additional stat effects. Maybe your Magic Sword gives +3 Resistance, or your Brave Lance provides +2 Strength while equipped.

The **Durability** section controls weapon degradation. If `_durability` is true (the default when `GameplayGeneralSettings.GetWeaponsHaveDurability()` returns true), weapons wear out with use. The `_maxUses` field determines how many times the weapon can be used before breaking. Common weapons might have 40 uses, while legendary weapons might have 100 or be unbreakable entirely by setting an extremely high max uses value.

The `_replenishUsesAfterBattle` flag determines if durability resets after each battle. If true, you choose a **ReplenishUseType**: None (don't replenish), Partial (restore some percentage), or Full (reset to max). This creates different strategic considerations—do weapons last across multiple battles, or does each battle start fresh?

When an ObjectItemInstance is created from this template, it tracks `currentUses` internally. Every time `Use()` is called on the instance, it increments this counter and returns the remaining uses. When remaining uses hits zero, the item is broken and usually becomes unusable until repaired or is removed from inventory entirely.

## Repair and Forging Systems

The **Repair** section defines if and how a broken or worn weapon can be restored. If `_repairable` is true, the weapon can visit a blacksmith or use a repair command. The `_repairPricePerUse` sets how much it costs to restore one use. If `_repairNeedsItems` is true, you additionally need specific materials—maybe you need an Iron to repair an Iron Sword. The `_repairItem` field references the required material, and `_repairItemAmountPerUse` determines how many you need per restored use.

The **forging** system lets you upgrade weapons into better versions. If `_forgeable` is true, this weapon can be transformed. The `_forgeInto` array lists possible upgrade paths—maybe an Iron Sword can become a Steel Sword or a Silver Sword. The `_forgePrices` array provides the cost for each upgrade option. If `_forgeNeedsItems` is true, you need materials alongside money. The `_forgeItems` array lists required materials for each forge path.

This creates interesting economic gameplay. Do you keep repairing your reliable Iron Sword, or do you invest in forging it into something stronger? Do you collect rare materials to forge a legendary weapon? The repair and forge systems work hand-in-hand with the durability system to create meaningful item management decisions.

## Range and Targeting

For weapons and magic, the **Range** section defines combat reach. `_lowerRange` and `_upperRange` create a range bracket—a sword might be 1-1 (melee only), a bow might be 2-3 (can't attack adjacent, must have distance), and a javelin might be 1-2 (melee or ranged).

The `_rangeAdjustedByStat` flag enables dynamic range scaling. If true, you specify which stat affects range and by how much. Maybe a powerful character can throw javelins farther, so you set `_rangeAdjustedByStatName` to Strength and `_rangeAdjustedByStatAmount` to represent the scaling formula. Your combat system can read these values and calculate effective range based on the wielder's stats.

## Pricing and Economy

The **Pricing** section controls shop interactions. `_basePrice` is the item's standard cost. `_sellable` determines if players can sell this item—some quest items might not be sellable. `_buyable` controls whether it appears in shops for purchase.

The `_sellPriceDeductedPerUse` creates depreciation—used weapons sell for less. If an Iron Sword costs 500 gold new, and `_sellPriceDeductedPerUse` is 5, then after 20 uses it only sells for 400 gold. This prevents infinite money exploits where you buy weapons, use them slightly, and resell for full price. The ObjectItemInstance calculates the effective sell price by deducting `_sellPriceDeductedPerUse * currentUses` from the base price.

## Gifts and Lost Items

If you've enabled gifts in GameplayGeneralSettings, the **Gift** section appears. The `_giftRank` determines how valuable the gift is—higher ranks might grant more support points. The `_unitsLove` and `_unitsHate` arrays let you specify character preferences. When you give a character a gift they're listed as loving, you gain large support bonuses. Give them something they hate, and you might damage the relationship. This adds personality and strategy to gift-giving—you need to learn what each character likes.

For lost items, the **Lost Items** section has a single critical field: `_belongsTo`, referencing the CharacterData who owns this item. When you find a lost item and return it to its owner, you might gain support points or unlock special content. Lost items create exploration incentives and relationship-building opportunities.

## Creating a WeaponType

Before you can assign a weapon type to your items, you need to create WeaponType assets. These are separate ScriptableObjects created via Create → Turnroot → Game Settings → Gameplay → Weapon Type. Each WeaponType has a name (displayed in UI), an icon (shown in menus), a unique ID (for serialization and comparison), an `_isMagic` flag (distinguishing physical from magical damage types), and a triangle position (for weapon triangle advantages).

You typically create a suite of WeaponTypes during initial project setup—Sword, Lance, Axe, Bow, Fire, Thunder, Wind, Light, Dark, and so on. These get referenced in your GameplayGeneralSettings so the system knows which weapon types exist. Classes can restrict which weapon types they allow, items reference their type, and characters gain experience in different weapon types through the aptitude system.

The WeaponTypeHelpers class provides utilities for working with these. `GetConfiguredWeaponTypes()` returns all weapon types from GameplayGeneralSettings. `Equals(a, b)` compares two WeaponTypes properly—it prefers reference equality but falls back to ID comparison if references differ but IDs match. This handles edge cases where the same weapon type might be loaded from different asset references.

## The Aptitude System

Aptitudes represent a character's proficiency with weapon types. The **Aptitude** class extends LeveledLetteredField, giving you the familiar E, D, C, B, A, S ranking system. When you create an ObjectItem weapon, you can set `_minWeaponTypeAptitude` to require minimum proficiency. A legendary sword might require B-rank sword aptitude, preventing novices from wielding it effectively.

Characters track their weapon aptitudes in the ExperienceRanks system we discussed in the character guide. As they use swords in battle, their sword aptitude grows from E to D to C and beyond. The CharacterClassData can restrict which weapon types a class allows, and within those restrictions, individual weapons can require minimum aptitudes. This creates progression—early-game characters use basic weapons, but as they train and promote to advanced classes, they can wield legendary equipment.

## The Inventory Connection

Items don't exist in a vacuum—they live in inventories. The **CharacterInventoryInstance** class manages a character's items. It maintains a list of ObjectItemInstance objects, tracks capacity limits, and handles equipped items. When you create a CharacterInstance from a CharacterData template, the initialization process looks at the template's `StartingInventory` list, creates ObjectItemInstance objects from those templates, and adds them to the character's inventory.

The inventory has a fixed capacity (default 6 items) and tracks which items are equipped. Equipment slots are organized as an array where slot 0 is always the weapon slot, and subsequent slots are for non-weapon equipment like shields and accessories. The inventory maintains boolean flags (`_isWeaponEquipped`, `_nonWeaponEquippedFlags`) to quickly check what's currently equipped without searching through items.

When you equip a weapon, the inventory calls `EquipItem(index)` with the inventory index. The system determines which slot the item should occupy based on its type (weapon goes to slot 0, shield to slot 1, accessory to slot 2), unequips anything currently in that slot, and marks the new item as equipped. The equipped item indices are stored so the system always knows what's active.

## ObjectItemInstance Operations

The ObjectItemInstance class provides a rich API for item operations, each designed with both validation and execution. This pattern lets you check if an action is valid (for UI updates like enabling/disabling buttons) before attempting it.

**Transfer** operations move items between inventories. You call `CanTransfer(targetInventory)` to check validity—this returns false if the item is unequippable or the target inventory is full. If valid, `Transfer(targetInventory)` performs the move, removing the item from its current owner and adding it to the target. The method returns an **OperationResult** struct with `Success` and `ErrorMessage` fields, letting you provide feedback to the player.

**Discard** removes items from inventory entirely. `CanDiscard()` returns false for unequippable items—you can't accidentally drop your legendary sword. `Discard()` performs the removal and returns a result. This is typically used for inventory management when you need space and want to permanently delete an item.

**Sell** operations interact with the economy system. `CanSell()` checks if the item is sellable and not unequippable. `Sell()` calculates the depreciated price based on current uses, removes the item from inventory, and should add gold to the player (this part is marked TODO, suggesting it needs integration with an economy brain component). The sell price formula is `max(0, basePrice - (sellPriceDeductedPerUse * currentUses))`, ensuring items never sell for negative gold.

**Buy** operations work similarly but in reverse. `CanBuy(buyerInventory)` checks if the item is buyable and the buyer has inventory space. `Buy(buyerInventory)` should deduct gold (another TODO), then adds the item to the buyer's inventory. These methods provide the foundation for shop systems and trading.

**Repair** operations restore durability. `CanRepair(repairUses)` validates that the item is repairable, has durability enabled, the repair amount makes sense, and the player can afford the cost. `Repair(repairUses)` reduces `currentUses` by the specified amount, effectively restoring uses. If the item needs materials, it should check the storehouse (marked TODO). The repair cost is `repairPricePerUse * repairUses`, and if materials are required, you need `repairItemAmountPerUse * repairUses` of the specified material.

## Settings Integration and Dynamic Defaults

The item system is deeply integrated with GameplayGeneralSettings. When you create a new ObjectItem or load one in the editor, the `OnEnable` and `OnValidate` methods call `ApplyGameplayDefaultsFromSettings()`. This method reads settings like whether weapons have durability, can be repaired, or can be forged, and sets the item's corresponding fields to match.

This means when you change a global setting, existing items can automatically update to match (on the next load or validation). If you decide weapons shouldn't have durability in your game, you can disable it in GameplayGeneralSettings, and all items will adjust their `_durability` flag accordingly. This keeps your items in sync with your game's design without manually updating hundreds of assets.

The **ItemSettings** static class provides cached access to these settings, similar to CharacterSettings. It has properties like `CanBeForged`, `CanBeRepaired`, and `HaveDurability` that query GameplayGeneralSettings once, cache the result, and return it on subsequent accesses. Cache invalidation happens automatically when scripts reload or play mode changes, ensuring you always get current values without repeatedly accessing the singleton.

## Practical Workflow: Creating a Complete Weapon

Let me walk through creating an Iron Sword from start to finish. First, ensure you have a Sword WeaponType created. Right-click, Create → Turnroot → Game Settings → Gameplay → Weapon Type, name it "Sword", set its triangle position to Top, mark it as not magic, and give it an appropriate icon.

Now create the sword item. Right-click, Create → Turnroot → Objects → Gameplay Item, name it "IronSword". In the Identity section, set Name to "Iron Sword" and add flavor text like "A sturdy blade of reliable iron, standard issue for soldiers."

In the Type section, set Subtype to Weapon. Set WeaponType to your Sword asset. Keep `_isUnequippable` unchecked—this is a normal weapon that can be traded and sold.

In the Pricing section, set BasePrice to 500. Leave Sellable and Buyable checked. Set SellPriceDeductedPerUse to 5—the sword depreciates slowly.

In the Range section, set LowerRange and UpperRange both to 1—this is a melee weapon. Leave range adjustment disabled.

In the Durability section, ensure `_durability` is checked (should be automatic from settings). Set MaxUses to 40—a standard iron sword lasts forty strikes. Leave `_replenishUsesAfterBattle` unchecked—durability carries between battles.

In the Repair section, check `_repairable`. Set `_repairPricePerUse` to 10. Check `_repairNeedsItems`, create or reference an Iron item (an ObjectItem with Consumable subtype), and set `_repairItemAmountPerUse` to 1—repairing the sword requires iron ore.

Leave `_forgeable` unchecked for now, or if you want upgrades, check it and create a "SteelSword" item, then reference it in the `_forgeInto` array with an appropriate forge price.

In the Combat section, set Weight to 8.0, Might to 6.0, Hit to 85.0, Critical to 10.0. These numbers define the sword's effectiveness—moderate damage, good accuracy, small crit chance, medium weight.

In the Aptitude section, set `_minWeaponTypeAptitude` to E—anyone can use an iron sword, it's basic equipment.

Now you have a complete, functional weapon. When this weapon is instantiated as an ObjectItemInstance and added to a character's inventory, it starts with 0 uses (full durability). Each time it's used in combat, `Use()` increments the use count. After 40 uses, it's broken. The character can visit a blacksmith, pay 400 gold (10 * 40 uses) plus 40 iron ore, and fully restore it. Or they could sell it partially used for a depreciated price.

## How Items Integrate with Characters

Characters interact with items through their CharacterInventoryInstance. During character initialization, the CharacterInstance constructor creates a new inventory and populates it from the CharacterData's `StartingInventory`. Each InventorySlot in the template references an ObjectItem and specifies a slot index (determining initial positioning in the inventory).

The inventory system enforces class restrictions through the CharacterInstance. When you try to equip a weapon, the instance checks `CanEquipWeaponType(weaponType)` on the current class. If the class allows that weapon type, the equip succeeds. If not, the operation fails. This is how you prevent Mages from wielding lances or ensure only special classes can use legendary weapon types.

Stats from equipped items apply through the StatBonuses dictionary on ObjectItem. When an item is equipped, your combat system should read these bonuses and apply them to the character's effective stats. An equipped Magic Sword with +3 Resistance would add 3 to the character's Resistance during combat calculations. When unequipped, that bonus disappears.

The weapon type aptitude requirement gets checked during equipment or class changes. If a character tries to equip a weapon requiring C-rank sword aptitude but only has E-rank, the system should prevent it or apply penalties. As the character uses swords in battle, they gain sword experience through the ExperienceRanks system, eventually reaching higher ranks and unlocking better weapons.

## Serialization and Persistence

ObjectItemInstance uses the same serialization infrastructure as CharacterInstance. The **ObjectItemInstanceJsonConverter** ensures instances are reconstructed properly during deserialization. The converter extracts the template reference from the JSON, creates a new instance using the constructor that takes an ObjectItem, and allows the instance to run its initialization logic.

The IPostDeserialize interface lets ObjectItemInstance perform cleanup after deserialization. The `OnAfterDeserialize()` method clamps `currentUses` to valid ranges—if durability is enabled, uses can't be negative or exceed max uses. This prevents corrupt save data from creating invalid states.

When the GamewideContextBrain saves a unique character, the character's inventory is serialized along with all contained ObjectItemInstance objects. Each instance maintains its `_id` for tracking, its template reference for knowing what it is, and its current uses for durability state. When the character is loaded later, the full inventory state is restored, including partially-used weapons and consumed durability.

## Extending the Item System

The architecture allows extension without modification, following the same patterns we saw with characters. If you want to add custom behavior when items are used, you could subscribe to item-related events (which would need to be added to the Brain event system). If you need additional data on items, you could create a companion ScriptableObject that references ObjectItem and stores your custom data, or extend ObjectItem directly with new fields (being mindful of version control).

The ObjectSubtype system is designed for extension. If you need a new fundamental item category, you'd add a new constant to the ObjectSubtype class, update `GetValidValues()` to include it conditionally based on settings, and extend GameplayGeneralSettings with a flag to enable this new category. The custom property drawer would automatically show it in the Inspector when enabled.

For item operations, the CanX/X pattern (like CanTransfer/Transfer) is extensible. If you need new operations, follow the same pattern: create a validation method that returns bool, create an execution method that returns OperationResult, and handle all the logic internally. This keeps the API consistent and makes UI integration straightforward.

## The OperationResult Pattern

The **OperationResult** struct is a lightweight result type that carries success state and error messages. It's defined as a simple struct with a `Success` boolean and an optional `ErrorMessage` string. You create success results with `OperationResult.SuccessResult()` and failures with `OperationResult.Failure("reason")`.

This pattern eliminates exception handling for expected failure cases. When a transfer fails because the inventory is full, that's not an exceptional circumstance—it's a normal game state that should be handled gracefully. The calling code checks `result.Success`, and if false, displays `result.ErrorMessage` to the player. This creates clear failure paths without try-catch blocks cluttering your code.

You can extend this pattern throughout your game. Any operation that can fail in expected ways should return an OperationResult. Combat actions, conversation choices, quest objectives—all can use this pattern for consistent error handling and user feedback.

## Integration with the Brain System

While the current files show item operations happening directly on ObjectItemInstance, a complete integration with the Brain system would publish events for item-related actions. You might have events like `OnItemUsed`, `OnItemTransferred`, `OnItemBroken`, `OnItemRepaired`, or `OnItemForged`. These would follow the same pattern as character events—a specialized ItemBrain or InventoryBrain component would publish events through the main Brain, and other systems could subscribe to react.

// TODO: This!!!! ^

This would let you layer behavior without modifying item code. Maybe your achievement system subscribes to `OnItemForged` to track forged weapons. Your audio system subscribes to `OnItemBroken` to play a breaking sound. Your UI system subscribes to `OnItemTransferred` to update inventory displays. The farfalle architecture applies equally well to items as to characters.

The TODO comments in the ObjectItemInstance code highlight integration points. Gold economy operations need connection to an EconomyBrain or CurrencySystem. Storehouse/convoy operations need connection to a shared storage system. These integrations would follow the established patterns—reference the Brain, call methods on specialized components, publish events when state changes, and let subscribers react appropriately.