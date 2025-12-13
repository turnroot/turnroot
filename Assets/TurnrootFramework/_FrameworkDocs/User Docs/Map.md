## 🗺️ Turnroot Map System

The Turnroot Map System is the core tool for building the battlefields in your game. Think of it less like a spreadsheet and more like a **digital diorama** where every piece is functional.

### The **MapGrid**: Your Game Board

The **MapGrid** is the foundation of your battlefield, similar to a chess board or the grid in a tactical RPG.

* **It's the Battle Area:** It defines the size and structure of your map (e.g., 15 rows by 20 columns).
* **It's Data-Rich:** Unlike a simple picture, the grid holds data that directly affects gameplay, such as movement costs, defensive bonuses, and where characters can spawn.
* **It Lives in the Scene:** The map is a real object in your Unity scene, which means you can see it and edit it using standard Unity tools, making map creation visual and immediate.


### **Grid Points**: The Building Blocks of the Map

Every square on your map is a **MapGridPoint**, the fundamental unit of the tactical space.

* **Coordinates and Terrain:** Each point knows exactly where it is (row and column) and what kind of **Terrain Type** it is (e.g., Forest, Mountain, Plain).
* **Occupancy Check:** It tracks who is standing on it. Pathfinding and targeting systems can quickly check the **IsOccupied** flag.
* **Endless Customization (The Property System):** This is the system's power. You can attach any custom gameplay data to a tile, even if the core system doesn't know about it.
    * **Examples:** Add a `FloatProperty` named "DangerLevel," or a `BoolProperty` named "IsSafeZone," or an `EventProperty` that triggers dialogue when a character steps there.

### **Terrain Types**: Defining the Rules of Movement and Combat

**Terrain Types** are the heart of tactical design, defining how the environment interacts with your units.

| Terrain Statistic | What It Does for the Player | Example |
| :--- | :--- | :--- |
| **Movement Cost** | Specifies how many movement points it costs a unit to enter the tile, based on its unit type. | A **Forest** tile might cost **2.0** for infantry (slowing them down) but **1.0** for flying units (no penalty). |
| **Combat Bonuses** | Provides stat buffs to a unit standing on the tile. | A unit in a **Forest** might get **+20 Defense** and **+30 Avoid**, making them harder to hit and hurt. |
| **Health Change** | Affects unit HP at the start or end of a turn. | A **Healing Temple** tile might grant **+10 HP per turn**, while a **Poison Swamp** deals **-5 HP**. |

You define all these rules once in a central **TerrainTypes** asset, and they apply across all your maps.

### **The Map Grid Editor**: Painting Your Battlefield

To build your maps, you use a custom visual tool: the **Map Grid Editor**.

1.  **Create the Grid:** You start by setting the `gridWidth` and `gridHeight` on the **MapGrid** component and clicking **"Create Grid Points"**—this instantly generates the blank canvas.
2.  **Paint the Terrain:** The editor acts like a paint program. You select a terrain type (e.g., Mountain) from a palette and click or drag on the grid to instantly "paint" the tile with that terrain type and its associated rules.
3.  **Place Features:** Use the feature tools (**Treasure, Door, Warp**) to place special interactive elements on tiles.
    * **Template-Instance Pattern for Features:** You define the *default* properties for a "treasure" in a separate asset. When you place one, it inherits those defaults (e.g., *Contents: Iron Sword*), but you can customize *this specific* chest (e.g., change its *Contents: Rare Gem*).

### **Pathfinding** and Movement Range

The system uses a modified **A\*** search algorithm to calculate movement and targeting.

* **Smart Routes:** When a unit moves, the system uses the **Movement Cost** of each tile to find the most efficient route within the unit's **Move** stat budget.
* **Movement & Attack Range:** The **GetReachable** method calculates all the tiles a unit can move to. It can also expand this calculation to show the unit's **attack range** *after* moving, creating the standard tactical overlay.
* **Directional Continuity:** The system slightly favors straighter paths, making unit movement look more natural and less like they're pointlessly zigzagging.

---

## 🎨 Map Grid Editor Tutorial Flow

The Map Grid Editor provides a visual, paint-style interface for building and configuring your battlefields.

### **Phase 1: Setup and Initialization**

#### **1. Scene Setup**
Start in your Unity scene and create a new, empty GameObject. Name it something descriptive, like "Map\_Forest\_Ambush."
* Add the **MapGrid** component to this GameObject.

#### **2. Define Dimensions**
In the MapGrid component's Inspector window:
* Set the **Grid Width** and **Grid Height** (e.g., $15 \times 20$). This defines the size of your battlefield.
* Set the **Grid Scale** (typically $1.0$ for tile-based games) to control the physical spacing between tiles.

#### **3. Generate the Canvas**
* Click the **"Create Grid Points"** button. The system will instantly generate $15 \times 20$ child GameObjects, each representing a blank **MapGridPoint**.
* The Scene View now displays a grid structure, ready for painting.

### **Phase 2: Painting the Terrain**

This is where you sculpt the landscape using the **Map Grid Editor** window (accessible via **Turnroot → Editors → Map Grid Editor**).

#### **1. Select Your Brush**
The left side of the Editor window shows two palettes:
* **Terrain Palette:** Lists all the available **Terrain Types** (Plain, Forest, Mountain, etc.) as colored swatches.
* **Action:** Click a terrain type to select it as your active brush.

#### **2. Paint the Terrain**
* In the main grid canvas, click tiles to apply the selected terrain type, or click and drag to quickly fill large areas.
* **Visual Feedback:** The tiles immediately change color based on the `EditorColor` of the terrain type, giving you an instant visual map of movement costs and bonuses.
* **Pro Tip (Hotkeys):** The editor assigns hotkeys to your terrain types, allowing you to switch brushes instantly without moving the mouse to the palette.

### **Phase 3: Placing Features and Spawn Points**

Once the landscape is set, you add interactive elements and define character starting positions.

#### **1. Placing Interactive Features**
* Switch to the **Tools Palette** in the Editor.
* **Action:** Select a feature tool (e.g., **Treasure**, **Door**, **Warp**).
* **Action:** Click the specific grid point where you want to place the feature.
* **Visual Feedback:** The tile will gain a letter overlay (e.g., 'T' for Treasure) or an icon, marking the interactive element.

#### **2. Defining Spawn Points**
Spawn points are special properties on the grid point that guide unit deployment.
* In the Map Grid Editor, click on a tile where you want a unit to start (or use the **Cursor Tool**).
* The right panel will populate with the tile's specific properties.
* **Action:** Find the **SpawnPoint** settings section.
* **Action:** Check the appropriate flags: **IsPossibleAllySpawnPoint**, **IsPossibleEnemySpawnPoint**, or **IsPossibleAvatarSpawnPoint**.

### **Phase 4: Configuring Tile-Specific Data**

The right-hand panel of the Map Grid Editor lets you fine-tune the properties of the currently selected tile or feature.

#### **1. Customizing Feature Properties (The Template-Instance Pattern)**
If you select a tile with a **Treasure** feature:
* The panel loads the default properties from the **MapGridFeatureProperties** asset (the "template").
* **Action:** Find the **ObjectItemProperty** section. You can now override the default treasure (e.g., changing the contents from "Iron Sword" to "Elixir").
* **Action:** Add a custom `BoolProperty` named **"RequiresKey"** and set it to **True** to make this specific chest locked.

#### **2. Setting Tile Events**
* **Action:** Navigate to the **Event Properties** section.
* **Action:** Use the **FriendlyEnters** or **EnemyEnters** event properties to drag and drop Unity methods.
    * *Example:* Wire the **EnemyEnters** event to a method that triggers a sudden attack or dialogue if an enemy steps on a specific tile.

#### **3. Resizing the Map**
If you need to change the map size during the design phase:
* Use the **"Add Row," "Add Column," "Remove Row,"** and **"Remove Column"** buttons in the MapGrid Inspector. The system automatically preserves your existing features and terrain when resizing.

### **Phase 5: Advanced (Connecting to 3D Terrain)**

If you are using a detailed 3D mesh for your level art (e.g., a bumpy hillside or a multi-tiered fortress) but need a 2D tactical grid:

* **Action:** Assign your 3D mesh to the `_single3dHeightMesh` field on the **MapGrid** component.
* **Action:** Click the **"Connect to 3D Map Height"** button.
* **Result:** The system raycasts down from each grid point to the 3D surface, snapping the 2D grid's world positions to the terrain's slopes. This ensures your tactical grid accurately matches the visual environment.
