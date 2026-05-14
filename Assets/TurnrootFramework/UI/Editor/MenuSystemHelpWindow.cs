using UnityEditor;
using UnityEngine;

namespace Turnroot.UI.Editor
{
    /// <summary>
    /// Custom editor window displaying comprehensive help documentation for the Menu System.
    /// </summary>
    public class MenuSystemHelpWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _exampleStyle;
        private GUIStyle _codeStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _successStyle;

        [MenuItem("Window/Turnroot/Help/Menu System Help")]
        public static void ShowWindow()
        {
            var window = GetWindow<MenuSystemHelpWindow>("Menu System Help");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(0, 0, 10, 10),
                normal = { textColor = new Color(0.8f, 0.9f, 1f) },
            };

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                margin = new RectOffset(0, 0, 8, 4),
                normal = { textColor = new Color(1f, 0.85f, 0.4f) },
            };

            _bodyStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true,
                margin = new RectOffset(10, 10, 2, 6),
            };

            _exampleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true,
                margin = new RectOffset(20, 10, 2, 6),
                normal = { textColor = new Color(0.7f, 0.9f, 0.7f) },
            };

            _codeStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                wordWrap = false,
                richText = false,
                margin = new RectOffset(30, 10, 1, 1),
                padding = new RectOffset(8, 8, 4, 4),
                normal =
                {
                    textColor = new Color(0.9f, 0.9f, 0.9f),
                    background = MakeTexture(2, 2, new Color(0.2f, 0.2f, 0.25f)),
                },
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true,
                margin = new RectOffset(15, 10, 4, 4),
                padding = new RectOffset(8, 8, 6, 6),
                normal =
                {
                    textColor = new Color(1f, 0.9f, 0.6f),
                    background = MakeTexture(2, 2, new Color(0.4f, 0.3f, 0.1f, 0.3f)),
                },
            };

            _successStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true,
                margin = new RectOffset(15, 10, 4, 4),
                padding = new RectOffset(8, 8, 6, 6),
                normal =
                {
                    textColor = new Color(0.6f, 1f, 0.6f),
                    background = MakeTexture(2, 2, new Color(0.1f, 0.3f, 0.1f, 0.3f)),
                },
            };
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (_headerStyle == null)
            {
                InitializeStyles();
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawArchitecture();
            DrawMenuTypes();
            DrawSetupWorkflow();
            DrawListMenuSetup();
            DrawRadialMenuSetup();
            DrawSubmenuNavigation();
            DrawRouteRegistration();
            DrawExamples();
            DrawBackButton();
            DrawDetailsButton();
            DrawTroubleshooting();
            DrawTips();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("📋 MENU SYSTEM GUIDE", _headerStyle);
            EditorGUILayout.LabelField(
                "Complete documentation for setting up menus, submenus, and navigation in the Turnroot Framework",
                _bodyStyle
            );
            DrawSeparator();
        }

        private void DrawArchitecture()
        {
            EditorGUILayout.LabelField("ARCHITECTURE OVERVIEW", _sectionStyle);

            EditorGUILayout.LabelField(
                "• <b>GamewideUiSettings</b> (Singleton ScriptableObject)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Central configuration for all menus", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Contains List<MenuEntry> defining menu prefab mappings",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  └─ Stores prefabs, styles, colors, and timing settings",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>MenuEntry</b> (Serializable class)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Defines a single menu (name + prefab + style)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Properties:", _bodyStyle);
            EditorGUILayout.LabelField("     • MenuName (enum identifier)", _bodyStyle);
            EditorGUILayout.LabelField(
                "     • MenuStyle (List, Pie, Grid, Filmstrip, None)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("     • GameObject prefab (the actual menu)", _bodyStyle);
            EditorGUILayout.LabelField("     • activeInstance (runtime GameObject)", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "• <b>MenuTransitionManager</b> (Runtime system)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Instantiates menu prefabs", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Handles fade transitions between menus", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Wires up input actions and event handlers",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Applies menu colors based on style", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Cleans up destroyed menus", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>UiBrain</b> (MonoBehaviour component)", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Central UI controller for game state", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Manages menu depth tracking", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Creates Back and Details buttons", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Coordinates transitions between menus", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>MenuRouteHandler</b> (Runtime routing)", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Maps MenuItem names to actions", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Routes selections to appropriate handlers",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Extensible via AddRoute/RemoveRoute", _bodyStyle);

            DrawSeparator();
        }

        private void DrawMenuTypes()
        {
            EditorGUILayout.LabelField("MENU TYPES", _sectionStyle);

            EditorGUILayout.LabelField("<b>ListMenu</b> (Vertical list navigation)", _bodyStyle);
            EditorGUILayout.LabelField("  • Component: ListMenu (extends MenuBase)", _bodyStyle);
            EditorGUILayout.LabelField("  • Child items: ListMenuItem components", _bodyStyle);
            EditorGUILayout.LabelField("  • Navigation: Up/Down arrow keys or gamepad", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Best for: Linear navigation, settings menus",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>RadialMenu</b> (Pie/wheel menu)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Component: RadialMenu (extends MonoBehaviour)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Child items: RadialMenuItem components", _bodyStyle);
            EditorGUILayout.LabelField("  • Navigation: Directional input or mouse", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Best for: Quick access, radial selection",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>GridMenu</b> (2D grid navigation)", _bodyStyle);
            EditorGUILayout.LabelField("  • Component: GridMenu (extends MenuBase)", _bodyStyle);
            EditorGUILayout.LabelField("  • Child items: GridMenuItem components", _bodyStyle);
            EditorGUILayout.LabelField("  • Navigation: 4-directional input", _bodyStyle);
            EditorGUILayout.LabelField("  • Best for: Unit selection, inventory grids", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>FilmstripMenu</b> (Horizontal scrolling)", _bodyStyle);
            EditorGUILayout.LabelField("  • Similar to ListMenu but horizontal", _bodyStyle);
            EditorGUILayout.LabelField("  • Navigation: Left/Right input", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Best for: Character selection, card browsing",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawSetupWorkflow()
        {
            EditorGUILayout.LabelField("SETUP WORKFLOW", _sectionStyle);

            EditorGUILayout.LabelField(
                "<b>STEP 1: Add to MenuName enum (GamewideUiSettings.cs)</b>",
                _bodyStyle
            );
            GUILayout.Label("public enum MenuName", _codeStyle);
            GUILayout.Label("{", _codeStyle);
            GUILayout.Label("    // ... existing entries", _codeStyle);
            GUILayout.Label("    YourNewMenu,  // Add your menu here", _codeStyle);
            GUILayout.Label("}", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>STEP 2: Create the menu prefab</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Create GameObject in Unity hierarchy", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Add menu component (ListMenu, RadialMenu, etc.)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Add child GameObjects with menu item components",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Configure visuals, layout, colors", _bodyStyle);
            EditorGUILayout.LabelField("  • Save as prefab in Prefabs folder", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>STEP 3: Register in GamewideUiSettings</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Select GamewideUiSettings asset", _bodyStyle);
            EditorGUILayout.LabelField("  • Find 'All Possible Menu Locations' list", _bodyStyle);
            EditorGUILayout.LabelField("  • Add new element:", _bodyStyle);
            EditorGUILayout.LabelField("     - Menu Name: YourNewMenu", _bodyStyle);
            EditorGUILayout.LabelField("     - Parent Menu Name: (parent or None)", _bodyStyle);
            EditorGUILayout.LabelField("     - Style: List, Pie, Grid, etc.", _bodyStyle);
            EditorGUILayout.LabelField("     - Prefab: Drag your prefab here", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>STEP 4: Add helper method (optional)</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • In GamewideUiSettings.cs, add getter:", _bodyStyle);
            GUILayout.Label(
                "public MenuEntry GetYourMenu() => GetMenuEntry(MenuName.YourNewMenu);",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>STEP 5: Register route (for navigation TO this menu)</b>",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • See 'ROUTE REGISTRATION' section below", _bodyStyle);

            DrawSeparator();
        }

        private void DrawListMenuSetup()
        {
            EditorGUILayout.LabelField("LIST MENU SETUP (DETAILED)", _sectionStyle);

            EditorGUILayout.LabelField("<b>1. Create the Menu GameObject</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • GameObject → Create Empty", _bodyStyle);
            EditorGUILayout.LabelField("  • Rename to 'YourMenuName'", _bodyStyle);
            EditorGUILayout.LabelField("  • Add Component → ListMenu", _bodyStyle);
            EditorGUILayout.LabelField("  • Add RectTransform (auto-added with UI)", _bodyStyle);
            EditorGUILayout.LabelField("  • Add CanvasGroup (required for fade)", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>2. Create Menu Items</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Create child GameObjects under menu", _bodyStyle);
            EditorGUILayout.LabelField("  • Add Component → ListMenuItem to each", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • SimpleButton is auto-added by ListMenuItem",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Set Item Name field on each ListMenuItem", _bodyStyle);
            EditorGUILayout.LabelField("     → This MUST match route name later!", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>3. Add Visual Elements</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Add Text (TextMeshPro) or Image to each item",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Configure button visuals", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Set up layout (Vertical Layout Group, etc.)",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>4. Configure Input (optional)</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • ListMenu has navigateUpAction, navigateDownAction",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • These are auto-configured by UiBrain", _bodyStyle);
            EditorGUILayout.LabelField("  • selectAction for item selection", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>5. Save as Prefab</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Drag to Prefabs folder", _bodyStyle);
            EditorGUILayout.LabelField("  • Delete from Hierarchy (optional)", _bodyStyle);

            EditorGUILayout.LabelField(
                "<color=#90EE90>✓ ListMenu ready to use!</color>",
                _successStyle
            );

            DrawSeparator();
        }

        private void DrawRadialMenuSetup()
        {
            EditorGUILayout.LabelField("RADIAL MENU SETUP (DETAILED)", _sectionStyle);

            EditorGUILayout.LabelField("<b>RadialMenu Overview</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • RadialMenu dynamically generates segments", _bodyStyle);
            EditorGUILayout.LabelField("  • You define RadialMenuItems as children", _bodyStyle);
            EditorGUILayout.LabelField("  • System calculates angles and positions", _bodyStyle);
            EditorGUILayout.LabelField("  • Supports center item for primary action", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Setup Steps</b>", _bodyStyle);
            EditorGUILayout.LabelField("  1. Create GameObject + RadialMenu component", _bodyStyle);
            EditorGUILayout.LabelField(
                "  2. Add RadialMenuItem children (one per segment)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  3. Set item names to match routes", _bodyStyle);
            EditorGUILayout.LabelField("  4. Optionally add center item for 'Start'", _bodyStyle);
            EditorGUILayout.LabelField("  5. Configure radius, colors in RadialMenu", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Configuration Options</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Colors pulled from GamewideUiSettings", _bodyStyle);
            EditorGUILayout.LabelField("  • RadialMenuDefaultRadiusPixels", _bodyStyle);
            EditorGUILayout.LabelField("  • RadialMenuInnerRadius (0-1)", _bodyStyle);
            EditorGUILayout.LabelField("  • RadialMenuSegmentGap", _bodyStyle);
            EditorGUILayout.LabelField("  • Show icons/labels options", _bodyStyle);

            DrawSeparator();
        }

        private void DrawSubmenuNavigation()
        {
            EditorGUILayout.LabelField("SUBMENU NAVIGATION", _sectionStyle);

            EditorGUILayout.LabelField("<b>How Submenus Work (Automatic System)</b>", _bodyStyle);
            EditorGUILayout.LabelField("  1. User clicks a menu item", _bodyStyle);
            EditorGUILayout.LabelField("  2. MenuItem raises OnItemSelected event", _bodyStyle);
            EditorGUILayout.LabelField("  3. UiBrain receives selection", _bodyStyle);
            EditorGUILayout.LabelField("  4. MenuRouteHandler looks up item name", _bodyStyle);
            EditorGUILayout.LabelField("  5. Route action calls OpenSubmenu()", _bodyStyle);
            EditorGUILayout.LabelField("  6. TransitionToSubmenu() starts coroutine", _bodyStyle);
            EditorGUILayout.LabelField(
                "  7. MenuTransitionManager.TransitionBetween():",
                _bodyStyle
            );
            EditorGUILayout.LabelField("     - Fades out current menu", _bodyStyle);
            EditorGUILayout.LabelField("     - Destroys current menu instance", _bodyStyle);
            EditorGUILayout.LabelField("     - Instantiates new menu prefab", _bodyStyle);
            EditorGUILayout.LabelField("     - Wires up events and input", _bodyStyle);
            EditorGUILayout.LabelField("     - Applies colors", _bodyStyle);
            EditorGUILayout.LabelField("     - Fades in new menu", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Menu Depth Tracking</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • MenuDepthTracker maintains hierarchy stack",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Depth 1 = Root menu (PreBattle, MainMenu)", _bodyStyle);
            EditorGUILayout.LabelField("  • Depth 2+ = Submenus", _bodyStyle);
            EditorGUILayout.LabelField("  • Back button appears when depth > 1", _bodyStyle);
            EditorGUILayout.LabelField("  • Back navigates to previous menu", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Parent-Child Relationships</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Set via MenuName in MenuPrefabs", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Automatically resolved by GamewideUiSettings",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Used for hierarchical organization", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Circular references detected and prevented",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawRouteRegistration()
        {
            EditorGUILayout.LabelField("ROUTE REGISTRATION", _sectionStyle);

            EditorGUILayout.LabelField("<b>What is a Route?</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Maps menu item name (string) to action (delegate)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • When MenuItem 'Team' is selected → Open team menu",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Registered in MenuRouteHandler.InitializeRoutes()",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>STEP 1: Add route in MenuRouteHandler.cs</b>",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Open: Gameplay/Brain/Components/UI/Menus/MenuRouteHandler.cs",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Find: InitializeRoutes() method", _bodyStyle);
            EditorGUILayout.LabelField("  • Add your route:", _bodyStyle);
            GUILayout.Space(2);
            GUILayout.Label("private void InitializeRoutes()", _codeStyle);
            GUILayout.Label("{", _codeStyle);
            GUILayout.Label("    // Existing routes...", _codeStyle);
            GUILayout.Label(
                "    _menuActionRoutes[MenuRouteNames.Team] = _ => _brain.OpenPreBattleUnitsMenu();",
                _codeStyle
            );
            GUILayout.Label("    ", _codeStyle);
            GUILayout.Label("    // Your new route:", _codeStyle);
            GUILayout.Label(
                "    _menuActionRoutes[\"YourItemName\"] = _ => _brain.OpenYourMenu();",
                _codeStyle
            );
            GUILayout.Label("}", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>STEP 2: Add open method in SettingsMenuHelpers.cs or UiBrain partial</b>",
                _bodyStyle
            );
            GUILayout.Label("public void OpenYourMenu()", _codeStyle);
            GUILayout.Label(
                "    => OpenSubmenu(uiSettings.GetYourMenu(), \"your menu\");",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>STEP 3: MenuItem name MUST match route key</b>",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • ListMenuItem.ItemName = \"YourItemName\" (Inspector or code)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • RadialMenuItem.ItemName = \"YourItemName\"",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Exact string match required!", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<color=#FFA500>⚠️ IMPORTANT: Route names are case-sensitive!</color>",
                _warningStyle
            );

            DrawSeparator();
        }

        private void DrawExamples()
        {
            EditorGUILayout.LabelField("EXAMPLE: Adding a Shop Menu", _sectionStyle);

            EditorGUILayout.LabelField("<b>1. Add to MenuName enum:</b>", _exampleStyle);
            GUILayout.Label("ShopMenu,  // In MenuName enum", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>2. Create prefab with ListMenu:</b>", _exampleStyle);
            EditorGUILayout.LabelField("  • ShopMenu GameObject", _exampleStyle);
            EditorGUILayout.LabelField("     └─ ListMenu component", _exampleStyle);
            EditorGUILayout.LabelField("     └─ Child: BuyItem (ListMenuItem)", _exampleStyle);
            EditorGUILayout.LabelField("     └─ Child: SellItem (ListMenuItem)", _exampleStyle);
            EditorGUILayout.LabelField("     └─ Child: Exit (ListMenuItem)", _exampleStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>3. Register in GamewideUiSettings:</b>", _exampleStyle);
            EditorGUILayout.LabelField("  • MenuName: ShopMenu", _exampleStyle);
            EditorGUILayout.LabelField("  • ParentMenuName: MainMenu", _exampleStyle);
            EditorGUILayout.LabelField("  • Style: List", _exampleStyle);
            EditorGUILayout.LabelField("  • Prefab: ShopMenuPrefab", _exampleStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>4. Add route in MenuRouteHandler:</b>", _exampleStyle);
            GUILayout.Label(
                "_menuActionRoutes[\"Shop\"] = _ => _brain.OpenShopMenu();",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>5. Add open method:</b>", _exampleStyle);
            GUILayout.Label(
                "public void OpenShopMenu() => OpenSubmenu(uiSettings.GetShopMenu(), \"shop\");",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>6. Create menu item in parent that opens it:</b>",
                _exampleStyle
            );
            EditorGUILayout.LabelField(
                "  • In MainMenu, add ListMenuItem with name \"Shop\"",
                _exampleStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<color=#90EE90>✓ Done! Selecting 'Shop' in MainMenu opens ShopMenu</color>",
                _successStyle
            );

            DrawSeparator();
        }

        private void DrawBackButton()
        {
            EditorGUILayout.LabelField("BACK BUTTON SYSTEM", _sectionStyle);

            EditorGUILayout.LabelField("<b>Status: ✓ FULLY IMPLEMENTED</b>", _successStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>How It Works:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Automatically appears in submenus (depth > 1)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Created from GamewideUiSettings.MenuCanvasPrefab",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Uses SimpleButton with Role.Back", _bodyStyle);
            EditorGUILayout.LabelField("  • Input: Escape key or gamepad B/Circle", _bodyStyle);
            EditorGUILayout.LabelField("  • Navigates to previous menu in depth stack", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Configuration:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Set MenuCanvasPrefab in GamewideUiSettings",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Prefab should contain SimpleButton with Role = Back",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Position and style in prefab", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • UiBrain handles creation/destruction automatically",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Behavior:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Depth 1 (root): Back goes to previous state",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Depth 2+: Back goes to parent menu in stack",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Triggers fade transition automatically", _bodyStyle);

            DrawSeparator();
        }

        private void DrawDetailsButton()
        {
            EditorGUILayout.LabelField("DETAILS BUTTON SYSTEM", _sectionStyle);

            EditorGUILayout.LabelField("<b>Status: ⚠️ PARTIALLY IMPLEMENTED</b>", _warningStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>What's Implemented:</b>", _bodyStyle);
            EditorGUILayout.LabelField("  ✓ Button creation/destruction", _bodyStyle);
            EditorGUILayout.LabelField("  ✓ Input action binding", _bodyStyle);
            EditorGUILayout.LabelField("  ✓ State-based visibility logic", _bodyStyle);
            EditorGUILayout.LabelField("  ✓ SimpleButton integration", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>What's NOT Implemented:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  ✗ Details panel/view UI (HandleDetailsButtonPressed is TODO)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  ✗ Content display system", _bodyStyle);
            EditorGUILayout.LabelField("  ✗ Details panel prefab", _bodyStyle);
            EditorGUILayout.LabelField("  ✗ Integration with menu items", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Current Implementation Location:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • BackAndDetailHelper.cs:247 - HandleDetailsButtonPressed()",
                _bodyStyle
            );
            GUILayout.Label("// Current code just logs:", _codeStyle);
            GUILayout.Label(
                "\"UiBrain: Details button pressed - TODO: Implement details view\"",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>To Complete Implementation:</b>", _bodyStyle);
            EditorGUILayout.LabelField("  1. Create details panel prefab", _bodyStyle);
            EditorGUILayout.LabelField(
                "  2. Add DetailsPanelPrefab field to GamewideUiSettings",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  3. Implement HandleDetailsButtonPressed() to:",
                _bodyStyle
            );
            EditorGUILayout.LabelField("     - Instantiate details panel", _bodyStyle);
            EditorGUILayout.LabelField("     - Populate with selected item data", _bodyStyle);
            EditorGUILayout.LabelField("     - Handle close/dismiss", _bodyStyle);
            EditorGUILayout.LabelField(
                "  4. Add IDetailProvider interface to menu items",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  5. Track currently selected/hovered item for details",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawTroubleshooting()
        {
            EditorGUILayout.LabelField("TROUBLESHOOTING", _sectionStyle);

            EditorGUILayout.LabelField("<b>Menu doesn't appear:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Check prefab assigned in GamewideUiSettings",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Verify menu prefab is assigned in GamewideUiSettings -> MenuPrefabs",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Check Console for error messages", _bodyStyle);
            EditorGUILayout.LabelField("  • Ensure CanvasGroup on root GameObject", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Menu item selection doesn't work:</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Verify ItemName matches route key exactly", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Check route registered in MenuRouteHandler",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Ensure SimpleButton component on list item",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Check OnItemSelected event is wired up (auto)",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Navigation doesn't work:</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Check menu component on root GameObject", _bodyStyle);
            EditorGUILayout.LabelField("  • Verify input actions enabled", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Ensure MenuBase.RefreshMenuItems() called (auto)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Check EventSystem exists in scene", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Fade transition issues:</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • UIFade component added automatically", _bodyStyle);
            EditorGUILayout.LabelField("  • Check MenuFadeTime in GamewideUiSettings", _bodyStyle);
            EditorGUILayout.LabelField("  • Verify CanvasGroup on menu root", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Back button doesn't appear:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Set MenuCanvasPrefab in GamewideUiSettings",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Check menu depth > 1 (root doesn't show back)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Verify SimpleButton in MenuCanvasPrefab has Role.Back",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawTips()
        {
            EditorGUILayout.LabelField("💡 TIPS & BEST PRACTICES", _sectionStyle);

            EditorGUILayout.LabelField(
                "• <b>ALWAYS set CanvasGroup</b> on menu root for fades",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• <b>ItemName MUST match route key</b> exactly (case-sensitive)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use MenuStyle.List for most menus (simplest)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use MenuStyle.Pie for quick radial selection",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Register ALL routes in MenuRouteHandler.InitializeRoutes()",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Parent-child relationships defined via parentMenuName",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• MenuTransitionManager handles ALL instantiation - don't manually instantiate",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use GetMenuLocation() helpers for clean code",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Test navigation depth by pressing Back button",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• UIFade is automatic - set timing in GamewideUiSettings",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• MenuDepthTracker maintains state - don't manage manually",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Colors applied automatically based on MenuStyle",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Input actions wired automatically - don't subscribe manually",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• For custom behavior, extend MenuItemBase or add routes",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Check Console for validation warnings on menu setup",
                _bodyStyle
            );

            GUILayout.Space(20);
        }

        private void DrawSeparator()
        {
            GUILayout.Space(8);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            GUILayout.Space(8);
        }
    }
}
