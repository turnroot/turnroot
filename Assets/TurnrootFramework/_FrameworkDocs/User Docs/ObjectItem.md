# 🎒 Creating Items

Items are the loot that makes players excited: powerful weapons, lifesaving healing items, mysterious keys, and everything in between. This guide covers how to create items that feel meaningful.

---

## How Items Work

Like characters, items use a template-instance system.

**ObjectItem** is the template—a ScriptableObject defining what the item *is*. An Iron Sword template says "this weapon has 5 might, 90 hit, weighs 8, and costs 500 gold."

**ObjectItemInstance** is the live version at runtime. When a character picks up an Iron Sword, they get an instance of that template. The instance tracks current durability, who's holding it, and any special modifications.

This means you define each item once, but players can have multiple copies with different states.

---

## Creating an Item

Right-click in your Project window and choose **Create → Turnroot → Objects → Gameplay Item**. Name it descriptively: `Item_IronSword`, `Item_Vulnerary`, `Item_DoorKey`.

The Inspector layout changes based on what type of item you're making. Let's go through the options.

---

## Item Types

### Weapons

Physical weapons for melee and ranged combat. Swords, lances, axes, bows—the tools of war.

Key fields:
- **Weapon Type** — What category (affects proficiency requirements)
- **Might** — Base damage added to attacks
- **Hit** — Accuracy bonus (100 is perfectly accurate)
- **Critical** — Crit chance bonus
- **Weight** — Heavier weapons slow you down

A typical progression: Iron weapons are cheap and reliable. Steel is stronger but heavier. Silver is excellent but expensive. Legendary weapons are rare and powerful.

### Magic

Tomes, staves, and magical implements. Mechanically similar to weapons but scale off Magic instead of Strength.

Spells often have range advantages—attacking from 1-2 distance, or even further with siege magic. Balance this power with limited durability or high cost.

### Consumables

Single-use or limited-use items. Healing potions, stat boosters, keys, and utility items.

Consumables have a use count. Vulneraries might have 3 uses. Door keys have 1. Elixirs restore full HP but are expensive.

Some consumables give permanent effects (stat-boosting items) while others are temporary. Make this clear in the description.

### Equipables

Passive gear that provides bonuses while worn. Shields, rings, accessories, staves for healers.

Unlike weapons, equipables usually don't have attack stats. Instead, they modify the wearer's stats or grant special effects. A shield might give +2 Defense. A ring might prevent poison.

### Gifts

Relationship-building items, if your game has a social system. Flowers, books, jewelry—things characters like or dislike.

Each gift has a quality tier and lists of characters who love or hate it. The right gift for the right person builds relationships faster.

### Lost Items

Returnable objects that belong to specific characters. Finding someone's lost belonging and returning it builds your relationship with them.

---

## Setting Up Common Items

### A Basic Weapon

Let's walk through making an Iron Sword:

1. Create the asset, name it `Item_IronSword`
2. Set **Subtype** to Weapon
3. Set **WeaponType** to Sword
4. Set combat stats: Might 5, Hit 90, Crit 0, Weight 8
5. Set price: 500 gold base, sellable and buyable
6. Set durability: 45 uses
7. Write flavor text: "A reliable sword forged from iron. Nothing fancy, but it gets the job done."

That's a functional weapon! Now players can buy it, use it, and eventually break or sell it.

### A Healing Item

For a Vulnerary:

1. Create asset `Item_Vulnerary`
2. Set **Subtype** to Consumable
3. Set **MaxUses** to 3
4. Set healing effect (however your game handles consumable effects)
5. Set price: 300 gold
6. Flavor text: "A small bottle of medicinal herbs. Restores a modest amount of health."

Healing items are often the most-used consumables. Make them affordable but limited so players have to manage their supply.

### A Key Item

For a Door Key:

1. Create asset `Item_DoorKey`
2. Set **Subtype** to Consumable
3. Set **MaxUses** to 1
4. Mark it as **not sellable** (or very low value)
5. Flavor text: "Opens a single locked door. Fragile construction prevents reuse."

Keys create tactical decisions—who carries them? When do you use them? Making keys single-use matters.

---

## Range (Weapons Only)

Weapons have attack range defined by lower and upper bounds.

**Melee weapons** are typically 1-1 (adjacent only).
**Javelins and hand axes** might be 1-2 (adjacent or one tile away).
**Bows** are usually 2-2 or 2-3 (can't attack adjacent, but reach further).
**Siege weapons** might be 3-10 (artillery).
**Magic** varies, often 1-2 for basic spells.

Range is powerful. Being able to attack without counterattack (like an archer against a melee enemy) is a big advantage. Balance ranged weapons by giving them lower might or other tradeoffs.

---

## Durability and Repair

### How Durability Works

Most weapons and staves have limited uses. Each attack or spell consumes one durability. When durability hits zero, the item breaks (or becomes unusable until repaired, depending on your settings).

Durability creates resource management. That legendary sword is powerful, but do you save it for the boss or use it liberally?

### Repair Options

If your game allows repairs:
- **Repair Price Per Use** — Gold cost to restore one durability point
- **Repair Needs Items** — Requires specific materials (iron ore, silver ore, etc.)

Make powerful weapons expensive to maintain. A legendary blade might cost 200 gold per durability point to repair, compared to 10 for an iron sword.

### Auto-Replenish

Some items restore durability automatically after battle. Useful for unique weapons you don't want players to permanently lose.

---

## Forging

If you want players to upgrade weapons:

### Setting Up Forge Paths

Each item can have **Forge Options** listing what it can become:

- **Forge Into** — The resulting item
- **Price** — Gold cost
- **Materials** — Required items and quantities

Example: Iron Sword can forge into Steel Sword (500 gold + 3 Iron Ore) or into Killer Sword (1000 gold + 1 Mithril).

### Designing Forge Trees

Think of forging as a tech tree. Early weapons lead to multiple upgrade paths. Do you go for raw power (Steel → Silver) or specialized effects (Iron → Killer → Brave)?

Make materials meaningful. Common ore lets you upgrade basic weapons. Rare materials unlock special paths. This gives exploration and shop choices more weight.

---

## Writing Good Item Descriptions

The flavor text field isn't just decoration—it helps players understand items and adds personality to your world.

**Be concise.** You have limited UI space. Get to the point.

**Hint at function.** "Strikes with blinding speed" suggests this weapon enables double attacks. "Unreliable but devastating when it connects" hints at low hit rate but high crit.

**Add world flavor.** "Forged in the mountain forges of Ironhold" is more interesting than "A steel sword."

**Vary by rarity.** Common items can be plain. Rare items deserve backstory. Legendary weapons should feel legendary.

Examples:
- **Common:** "A standard iron blade. Dependable."
- **Uncommon:** "Quality steel from the Goldcrest smiths."
- **Rare:** "Silver blessed by the temple priests. Gleams in moonlight."
- **Legendary:** "The fang of the dragon who burned the old capital. Handle with reverence."

---

## Economy Tips

### Pricing Weapons

- **Early game** (Iron tier): 500-1000 gold
- **Mid game** (Steel tier): 1500-3000 gold  
- **Late game** (Silver tier): 5000-8000 gold
- **Legendary**: 15000+ or not purchasable

Sell prices are typically 50% of buy prices. Factor in durability loss if you use that system.

### Pricing Consumables

Healing items should be affordable—players need to use them. Keys and utility items can be moderate. Stat boosters should feel expensive because they're permanent.

### Drop Rates and Treasure

When enemies drop items or chests contain them, think about player expectations. A tough boss should drop something worth the effort. Regular enemies might drop common consumables or nothing.

Put good treasure behind challenges. A locked chest should contain something better than an unlocked one. A chest guarded by enemies should reward the risk.

---

## Common Patterns

**Starter Weapon:** Low might, high durability, cheap to replace. Players will discard these as soon as they find upgrades, and that's fine.

**Endgame Weapon:** High stats across the board, limited availability, expensive or impossible to repair. Using it should feel special.

**Utility Consumable:** Solves a specific problem (poison cure, door key, teleport stone). Stock limited quantities to force tough choices.

**Permanent Booster:** Expensive single-use item that permanently raises a stat. Players agonize over who gets these—that's intentional.

**Unique/Legendary Weapon:** One per playthrough, irreplaceable, defines a character's identity. Make it memorable.
