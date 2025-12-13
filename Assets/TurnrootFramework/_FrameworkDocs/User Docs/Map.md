# 🗺️ Building Battlefields with Maps

Welcome! This guide will walk you through creating tactical maps in Turnroot. By the end, you'll be designing your own battlefields with interesting terrain, strategic chokepoints, and interactive features.

---

## What You're Building

Think of your map as a game board where every square matters. Each tile can have different terrain that affects movement, provides cover, or triggers special events. The Map Grid Editor lets you paint these tiles visually, like coloring in a grid.

Your map is made of two main pieces:

**The MapGrid** is the container—it defines the size and holds all your tiles together. When you create one, you're essentially saying "I want a battlefield that's 20 tiles wide and 15 tiles tall."

**MapGridPoints** are the individual tiles. Each one knows what kind of terrain it is, whether there's a treasure chest on it, and where units can spawn.

---

## Creating Your First Map

Let's build a simple map together.

### Step 1: Set Up the Grid

Create a new empty GameObject in your scene and give it a descriptive name like `Map_Forest_Ambush`. Add the **MapGrid** component to it.

In the Inspector, you'll see settings for width, height, and scale. Start small—a 12×12 map is plenty while you're learning. You can always resize later.

Hit the **"Create Grid Points"** button. You'll see child objects appear in your hierarchy, one for each tile.

### Step 2: Open the Editor

Go to **Turnroot → Editors → Map Grid Editor**. A new window opens showing your grid as a colored canvas. This is where the fun begins.

### Step 3: Paint Some Terrain

On the left side, you'll see terrain swatches—different colors representing plains, forests, mountains, water, and so on. Click one to select it as your "brush."

Now click and drag on the grid. You're painting terrain! Try creating:
- A grassy clearing in the center
- Forest along the edges for cover
- A river cutting through with a bridge
- Maybe some mountains blocking off a corner

### Step 4: Test Movement

Switch to **Test Movement** mode using the tabs at the top. Click any tile and you'll see a purple overlay showing everywhere a unit could reach from that spot.

Try clicking in the forest versus the open plain. Notice how terrain affects movement range. This is how you'll discover whether your map creates interesting tactical decisions.

---

## Understanding Terrain

Terrain does three things: it costs movement points to enter, it provides combat bonuses, and sometimes it affects HP each turn.

### Movement Costs

Every terrain type has different costs for different movement styles.

When you're designing, think about how these differences create choices. Put forests between the player's starting position and the objective—now they have to decide: take the slow, safe route through cover, or risk the fast open path?

### Combat Bonuses

Tiles can give defense and avoid bonuses. Forests might give +1 defense and +20 avoid. Forts give even more. Thrones are typically the strongest defensive positions.

Place these strategically. A fort overlooking a chokepoint becomes a meaningful objective—do you rush to claim it, or let the enemy have it and work around?

### Health Effects

Some terrain heals or damages units each turn. A healing spring might restore HP. Poison swamps might drain it.

---

## Adding Features

Features are special objects you place on tiles: treasure chests, doors, warp points, villages to visit, and more.

### Placing Features

In the Map Grid Editor, look for the feature tools (icons showing a chest, door, etc.). Select one, then click a tile. A letter appears showing what's there—T for treasure, D for door, and so on.

### Configuring Features

Click a tile with a feature to see its properties in the right panel. For a treasure chest, you can:
- Assign which item it contains
- Mark it as locked (requiring a specific key)
- Add custom events that trigger when opened

Every feature type has its own options. Doors need keys. Warp points need destinations. Villages might have NPCs or shops.

### The Template System

Here's a useful concept: features use templates. You can set defaults (like "all treasure chests contain gold unless specified otherwise"), then override individual chests when needed.

This saves time—set up sensible defaults once, then only configure the special cases.

---

## Setting Up Spawn Points

Before a battle starts, you need to tell the game where units can appear.

Select tiles where you want allies to deploy and mark them as spawn points in the right panel. Do the same for enemy positions. You can also mark special spots for the player's main character or for mid-battle reinforcements.

**A design tip:** Give players a cluster of allied spawn points so they can choose their formation. Don't just give them one forced position. The deployment phase is part of the strategy.

---

## Testing Your Design

The Test Movement mode is your best friend during design. Use it constantly.

Select different movement types and click around your map. Ask yourself:
- Can cavalry actually use those open lanes I created?
- Do flying units have too much freedom, or is there interesting counterplay?
- Are there multiple routes to the objective, or is everyone funneled the same way?
- Is there cover where players will need it?

Watch for dead ends that serve no purpose, chokepoints that are too narrow (creates tedious one-on-one fights), or spawn points that put enemies right on top of the player.

---

## Connecting to 3D Terrain

If you're using a 3D terrain mesh, Turnroot can snap your grid points to match the surface height.

Assign your mesh to the MapGrid's 3D height field, then click **"Connect to 3D Map Height."** The system shoots raycasts downward and adjusts each tile's position to sit on the terrain.

This is optional—plenty of games work great with flat 2D grids. But if you want that 3D look, it's there.

---


## Design Philosophy

The best tactical maps tell a story through terrain. They create interesting decisions, not just obstacles.

Ask yourself: what's the *point* of this battle? If it's a desperate defense, create a position worth defending. If it's an ambush, give the enemy natural cover to spring from. If it's a chase, make the escape route winding and dangerous.

Don't just scatter terrain randomly. Every forest, every river, every mountain should make the player think "how do I use this?" or "how do I deal with this?"

And playtest constantly. What looks great on paper might feel terrible in practice. The Test Movement tool is fast—use it.

Happy mapping! 🗺️


