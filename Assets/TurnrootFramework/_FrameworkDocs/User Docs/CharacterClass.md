# ⚔️ Designing Character Classes

Classes define what your characters *do*. A knight in heavy armor plays differently than a nimble thief or a powerful mage. This guide covers how to create classes that feel distinct and satisfying.

---

## What Classes Actually Do

When a character has a class, that class affects almost everything about them:

**Combat identity.** What weapons can they use? Are they melee or ranged? Physical or magical?

**Stats.** Classes can boost stats, cap them, modify growth rates, and grant one-time bonuses when the class is first taken.

**Movement.** A mounted class covers ground quickly. An armored class plods along. A flying class ignores terrain entirely.

**Skills.** Classes grant abilities—some immediately, some as mastery rewards for sticking with the class.

In short: a character's class is their job description for combat.

---

## Creating a Class

Right-click in your Project window and choose **Create → Turnroot → Character → Class Data**. Name it something clear like `Class_Cavalier` or `Class_DarkMage`.

The Inspector shows several sections. Let's walk through them.

---

## Identity: The Basics

### Name and Description

Give your class a **ClassName** that players will recognize, plus a short **Description** that sells the fantasy. "Mounted knights who excel at hit-and-run tactics" tells players what to expect.

### Class Tier

Classes fit into progression tiers:

- **Starter** — Tutorial classes, limited options
- **Base** — Standard starting classes
- **Advanced** — Promoted classes with better stats
- **Master** — Elite late-game classes
- **Expert** — Ultimate endgame options

This helps organize your class system and can affect gameplay rules.

### Movement Type

How does this class get around?

- **Infantry** — Standard foot movement, normal terrain costs
- **Cavalry** — Fast on open ground, struggles in forests, can't cross mountains
- **Flying** — Ignores terrain (everything costs 1), but might have weaknesses
- **Armored** — Slow but sturdy, terrain costs are harsh
- **Mage** — Often like infantry, but might have special warp abilities

Movement type is one of the biggest factors in how a class *feels* to play. Cavalry and fliers cover maps quickly but have terrain limitations. Infantry is reliable but slow. Choose based on the fantasy you're building.

### Special Flags

A couple of toggles:
- **IsMagic** — Is this a magic-focused class? Affects some calculations.
- **IsUnique** — Can only one character have this class at a time?

---

## Stats: Shaping Growth

This is where you tune how the class affects combat performance.

### Stat Bonuses

Flat bonuses applied while the class is equipped. A heavy armor class might give +4 Defense but -2 Speed. A thief class might boost Skill and Speed while leaving Strength alone.

These are immediate—the moment someone takes this class, they get these adjustments.

### Stat Caps

Maximum values this class allows. An armored class might cap Speed at 20 (they're just too heavy to get faster). A magic class might cap Strength at 15 (they're not built for physical power).

Caps create meaningful differences between class paths. If one class caps Strength at 40 and another at 25, that matters for long-term builds.

### Stat Minimums

Floor values the class enforces. A mage class might require minimum Magic of 8—if a character's Magic would drop below that, it stays at 8 instead.

Use this sparingly. It's mainly for preventing weird edge cases.

### Growth Rate Modifiers

Percentage adjustments to growth rates while in this class. A warrior class might give +20% Strength growth and +15% HP growth, but -10% Magic and -5% Resistance.

This is *huge* for long-term character building. Two characters with identical personal growths will develop differently based on which classes they spend time in.

### Class Change Bonuses

One-time stat boosts when first taking this class. Often used for promotions—when a Cavalier promotes to Paladin, they might get +2 Strength, +1 Speed, +3 HP immediately.

This makes promotion feel rewarding and marks a clear power jump.

---

## Requirements: Who Can Take This Class?

### Weapon Types

The **Allowed Weapon Types** list determines what this class can equip. A Cavalier might use Swords and Lances. A Mage uses Tomes. A Fighter uses Axes.

Leave this empty to mean "no restrictions," though that's unusual.

### Level Requirement

**Minimum Level Requirement** sets the floor for taking this class. Base classes are usually level 1. Promoted classes might require level 10+. Master classes might need level 20+.

This naturally gates progression—players can't rush to endgame classes without developing their characters.

### Promotion Paths

If you're using a promotion-based system, the **Promotion Paths** list shows what this class can become. A Cavalier might promote into Paladin or Great Knight.

You can create branching paths this way—one base class leading to multiple advanced options, letting players specialize.

### Certification Item

Some systems require an item to unlock a class (a seal, a certificate, etc.). Set this if your game uses that approach.

---

## Skills: Class Abilities

### Innate Skills

Skills automatically granted when equipping this class. A Cavalier might get "Canto" (move again after attacking) immediately. A Thief gets "Lockpick" for opening chests without keys.

These define what makes the class special mechanically.

### Weapon Level Bonuses

Skills granted when reaching weapon proficiency milestones *while in this class*. Maybe Sword rank B unlocks "Swordfaire" (+5 damage with swords), and Sword rank A unlocks "Astra" (a multi-hit special attack).

This rewards players for investing in both a class and a weapon type together.

### Mastery System

If enabled, the class tracks mastery progress. Characters might gain mastery through battles or levels, eventually "mastering" the class and earning a permanent reward—often a skill they keep even when switching to a different class.

This encourages sampling multiple classes without feeling like wasted time. Even if you leave a class, the mastery reward stays with you.

---

## Designing Good Classes

### Start With Fantasy

Before touching numbers, ask: what's the *feel* of this class? A berserker should feel reckless and powerful. A sniper should feel precise and deadly from range. A paladin should feel protective and righteous.

The numbers should support that fantasy. A berserker has high Strength, risky low Defense. A sniper has excellent Skill and range. A paladin has balanced stats with good Defense.

### Create Meaningful Tradeoffs

Every strength should come with a weakness. Flying ignores terrain but takes extra damage from archers. Armor has great Defense but moves slowly. Cavalry covers ground fast but struggles in forests.

If a class is just "better" with no downside, it breaks your game balance. Players will always pick the strictly superior option.

### Think About Movement

Movement type might be the most important choice. It determines where a class can go, how fast they get there, and what terrain they care about.

Don't give every class cavalry or flying movement. Those should feel special. Infantry is the baseline—reliable, predictable, good at using terrain cover.

### Plan Promotion Branches

Give players interesting choices at promotion time. A Fighter might become:
- **Warrior** — Double down on raw Strength
- **Hero** — Balanced growth, gains sword access
- **Berserker** — Extreme offense, risky defenses

Each path should feel viable and distinct. Avoid "one is clearly best" situations.

---

## Balancing Tips

**Physical classes** typically have high Strength, HP, Defense, and low Magic, Resistance. Their stat totals tend toward durability and damage.

**Magic classes** flip that—high Magic, Resistance, Skill, and lower HP, Defense. They're powerful but fragile.

**Hybrid classes** try to do both, usually at the cost of not excelling at either. These should have some unique advantage to justify the compromise.

**Mounted classes** trade stat power for mobility. They often have lower caps than infantry equivalents—the movement *is* the advantage.

**Flying classes** are premium mobility. Balance them with low Defense, vulnerability to certain attacks, or limited weapon options.

---

## Common Patterns

**Base Physical Class:** Infantry movement, one or two weapon types, decent HP/Strength/Defense growths, promotes into 2-3 options.

**Base Magic Class:** Infantry or unique movement, tome weapons, Magic/Resistance focused, fragile. Promotes into offensive or supportive paths.

**Mounted Class:** Cavalry movement, lance and maybe sword access, good Strength and Speed but modest Defense caps. Premium mobility.

**Flying Class:** Best mobility in the game, but low HP and Defense. Often has a specific weakness (arrows, wind magic).

**Armored Class:** Worst mobility in the game, but excellent Defense and HP. Needs support from allies to get anywhere useful.

**Hybrid Class:** Tries to do two things (physical + magic, melee + range). Has a unique niche but not the best at either specialty.

