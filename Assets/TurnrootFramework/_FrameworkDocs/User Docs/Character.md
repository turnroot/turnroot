# 👤 Creating Characters

Characters are the heart of any tactical RPG. This guide walks you through creating memorable heroes, villains, and everyone in between.

---

## How Characters Work

Turnroot separates characters into two pieces, and understanding this will save you headaches later.

**CharacterData** is the template—a ScriptableObject that defines who a character *is*. Their name, their starting stats, their portrait, their personality. Think of it like a character sheet you'd make before a tabletop campaign.

**CharacterInstance** is the live version—created at runtime from a template. This tracks their *current* state: how much HP they have right now, what level they've reached, what's in their inventory. When you save the game, you're saving instances.

Why the split? Because sometimes you want multiple copies of the same template (a dozen generic soldiers all based on "Soldier Template"), and sometimes you want unique characters who persist across the whole game. The template-instance pattern handles both.

---

## Making Your First Character

### Create the Asset

Right-click in your Project window and go to **Create → Turnroot → Character → CharacterData**. Name it something descriptive like `Character_Elena` or `Enemy_Bandit_Axe`.

### The Inspector Layout

When you select your new character, you'll see foldable sections in the Inspector:

- **Identity** — Their name and what side they're on
- **Demographics** — Physical details like height and birthday
- **Description** — Flavor text and personality notes
- **Stats & Progression** — The numbers that matter in combat
- **Skills & Abilities** — Special moves and passive effects
- **Starting Inventory** — What they're carrying
- **Visual** — Portraits and 3D model settings
- **Support Relationships** — Who they can bond with
- **Behavior** — How the AI controls them (for enemies)

Let's walk through the important parts.

---

## Identity: Who Are They?

### Name Fields

Give them a **Display Name** (what shows in the UI during gameplay) and a **Full Name** (for dialogue and menus). "Marcus" might be the display name, while "Marcus von Brennan" is the full name.

### Character Type

The **Which** field is crucial—it tells the game what kind of character this is:

- **Ally** — Player-controlled, joins your army
- **Enemy** — AI-controlled opponent
- **NPC** — Non-combatant, maybe a villager or quest-giver
- **Avatar** — The player's personal character (if your game has one)

This affects what other options appear. Enemies get AI behavior settings. Allies might get support relationship options.

### Team

The **Team** field groups characters into factions. Usually you'll have "Player" and "Enemy" teams, but you might also have neutral factions or multiple enemy groups that fight each other.

---

## Stats: The Numbers

### Core Stats

These are your bread-and-butter combat stats:

- **HP** — How much damage they can take
- **Strength** — Physical attack power
- **Magic** — Magical attack power  
- **Defense** — Reduces physical damage taken
- **Resistance** — Reduces magical damage taken
- **Speed** — Determines turn order and whether they attack twice
- **Skill** — Affects hit rate and critical chance
- **Luck** — Helps avoid enemy criticals

Each stat has a starting value, a minimum, and a maximum. Most characters won't hit their maximums until late-game (or ever).

### Movement Stats

A few stats aren't bounded:

- **Move** — How many tiles they can travel per turn
- **Constitution** — Their carrying capacity and rescue ability

### Growth Rates

Here's where long-term progression comes in. Growth rates are percentages—when a character levels up, each stat has that percent chance to increase.

A 60% strength growth means they'll gain strength on most level-ups. A 20% resistance growth means it'll rarely go up. Total growth rates around 300-400% make for reasonably strong characters; go higher for protagonists, lower for early-game enemies.

---

## Skills and Abilities

Characters can have two kinds of skills:

**Regular Skills** are learned abilities that can potentially be swapped or unequipped (depending on your game's design). Combat techniques, passive bonuses, that sort of thing.

**Special Skills** are personal, unremovable abilities. Maybe your protagonist has a unique power tied to the story, or a character has a signature fighting style. These go in the Special Skills list.

You'll create Skill assets separately and reference them here.

---

## Starting Inventory

The **Starting Inventory** list defines what items a character has when they first appear.

For each slot, you specify:
- Which item (drag in an ObjectItem asset)
- Whether it's equipped
- How many (for stackable items like healing potions)

A typical starting loadout might be: equipped weapon, backup weapon or healing item, maybe a key or special item for the mission they appear in.

---

## Visual Setup

### Portraits

Characters need portrait images for dialogue, menus, and status screens. Portraits are organized by expression—you might have "default," "happy," "angry," and "sad" variants.

Add entries to the **Portraits** dictionary with the expression name as the key and the sprite as the value.

### Colors

Several color fields let you customize appearance:
- **SkinColor** for skin tone
- **AccentColor1-3** for outfit and accessory tinting (these get applied to the 3D model)

### 3D Model Parts

If you're using 3D models, you'll assign:
- A base body mesh
- Separate head/hands/hair that combine with class outfits
- Blendshape values for body customization

---

## Enemy-Specific Settings

When a character's type is Enemy, you get extra options.

### Is This Enemy Special?

**IsUnique** marks whether this is a named boss versus a generic soldier. Unique enemies usually have portraits, dialogue, and maybe recruitable potential.

**IsRecruitable** means the player might be able to convince this enemy to join their side (through story events, specific conditions, or whatever system you design).

### AI Behavior

The **Behavior Settings** control how the AI plays this character. These are sliders between opposing tendencies:

- **Soldier vs Lone Wolf** — Stay with allies or act independently?
- **Mindless vs Cunning** — Attack the closest target or prioritize strategically?
- **Selfish vs Selfless** — Prioritize own survival or protect allies?
- **Brash vs Wary** — Rush in or hang back cautiously?
- **Bloodthirsty vs Greedy** — Focus on kills or loot?

Mix these to create different enemy personalities. A mindless, brash bandit rushes in recklessly. A cunning, selfish assassin picks off weak targets while avoiding danger.

---

## Support Relationships

The **Support Relationships** list defines who this character can bond with.

For each relationship, you specify:
- The partner character
- Starting rank (if they already know each other)
- Maximum achievable rank
- Reference to the conversation content

**Character Flags** control romance options:
- **CanSSupport** — Can reach the highest support rank (usually romantic)
- **CanSSupportAvatar** — Can romance the player's personal character

---

## Tips for Good Characters

**Stats should match personality.** A scholarly mage shouldn't have great strength. A hotheaded warrior probably has low resistance. Let the numbers tell part of the story.

**Growth rates matter more than base stats.** A character with great growths but mediocre starting stats becomes powerful through investment. That's satisfying for players.

**Give enemies variety.** Don't just copy-paste the same soldier template. Vary their stats, weapons, and AI behavior. A cautious archer feels different from an aggressive one, even with identical numbers.

**Use descriptions.** The short description and likes/dislikes fields make characters memorable. "A cheerful knight who hates mornings" is more interesting than just a stat block.

---

## Common Patterns

**Generic Enemy Template:** Low stats, no portrait, IsUnique = false. You'll spawn dozens of these.

**Recruitable Boss:** Higher stats, full portrait set, IsUnique = true, IsRecruitable = true. Give them dialogue and a compelling reason to defect.

**Main Character:** Strong growths, special personal skill, full visual setup. This is your star—make them shine.

**Supporting Ally:** Solid but not exceptional. Good in their niche, with personality in their description and supports.
