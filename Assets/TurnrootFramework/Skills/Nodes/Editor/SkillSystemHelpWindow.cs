using UnityEditor;
using UnityEngine;

namespace Turnroot.Skills.Nodes.Editor
{
    /// <summary>
    /// Custom editor window displaying comprehensive help documentation for the Skill system.
    /// </summary>
    public class SkillSystemHelpWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _exampleStyle;
        private GUIStyle _codeStyle;
        private GUIStyle _warningStyle;

        [MenuItem("Window/Turnroot/Help/Skill System Help")]
        public static void ShowWindow()
        {
            var window = GetWindow<SkillSystemHelpWindow>("Skill System Help");
            window.minSize = new Vector2(650, 400);
            window.Show();
        }

        public static void ShowWindowFromButton()
        {
            ShowWindow();
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
            DrawWorkflow();
            DrawGraphBasics();
            DrawNodeTypes();
            DrawExample1();
            DrawExample2();
            DrawExecutionFlow();
            DrawTips();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("⚔️ SKILL SYSTEM GUIDE", _headerStyle);
            EditorGUILayout.LabelField(
                "Complete documentation for Skills, SkillGraphs, and visual node-based skill design",
                _bodyStyle
            );
            DrawSeparator();
        }

        private void DrawArchitecture()
        {
            EditorGUILayout.LabelField("ARCHITECTURE OVERVIEW", _sectionStyle);

            EditorGUILayout.LabelField("• <b>Skill</b> (ScriptableObject)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Template defining skill appearance and behavior",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  └─ Contains name, description, badge, colors",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ <b>MUST</b> have a SkillGraph assigned", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>SkillGraph</b> (XNode graph)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Visual node editor defining skill behavior",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Contains connected SkillNodes", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Executes from entry nodes (nodes with no inputs)",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>SkillNode</b> (graph node)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Individual action or condition in skill flow",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  └─ Connected via ExecutionFlow outputs/inputs",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>SkillInstance</b> (runtime)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Runtime wrapper tracking equipped/ready state",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Executes skill via BattleContext", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>SkillGraphExecutor</b> (runtime)", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Executes graph nodes in sequence", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Handles async operations and execution flow",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawWorkflow()
        {
            EditorGUILayout.LabelField("SETUP WORKFLOW", _sectionStyle);

            EditorGUILayout.LabelField("<b>1. Create a SkillGraph:</b>", _bodyStyle);
            GUILayout.Label("   Right-click → Create → Turnroot/Skills/Skill Graph", _codeStyle);
            GUILayout.Space(2);

            EditorGUILayout.LabelField(
                "<color=#FFA500>⚠️ CRITICAL: Always create the graph FIRST</color>",
                _warningStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>2. Open the SkillGraph editor:</b>", _bodyStyle);
            EditorGUILayout.LabelField("   • Double-click the SkillGraph asset", _bodyStyle);
            EditorGUILayout.LabelField("   • Or Window → XNode Editor", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>3. Add nodes to define behavior:</b>", _bodyStyle);
            EditorGUILayout.LabelField("   • Right-click in graph → Create Node", _bodyStyle);
            EditorGUILayout.LabelField(
                "   • Flow nodes: Entry points (Battle Starts, Turn Starts)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "   • Event nodes: Actions (Damage, Heal, Move)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("   • Condition nodes: Logic (If, Comparisons)", _bodyStyle);
            EditorGUILayout.LabelField("   • Math nodes: Calculations (Add, Multiply)", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>4. Connect nodes:</b>", _bodyStyle);
            EditorGUILayout.LabelField("   • Drag from output ports to input ports", _bodyStyle);
            EditorGUILayout.LabelField(
                "   • ExecutionFlow connections control sequence",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "   • Value connections pass data between nodes",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>5. Create a Skill asset:</b>", _bodyStyle);
            GUILayout.Label("   Right-click → Create → Turnroot/Skills/Skill", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>6. Assign the SkillGraph to the Skill:</b>", _bodyStyle);
            EditorGUILayout.LabelField("   • Select your Skill asset", _bodyStyle);
            EditorGUILayout.LabelField(
                "   • In the Behavior section, drag your SkillGraph",
                _bodyStyle
            );
            GUILayout.Space(2);

            EditorGUILayout.LabelField(
                "<color=#FFA500>⚠️ REQUIRED: Skill CANNOT execute without a SkillGraph</color>",
                _warningStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>7. Configure skill appearance:</b>", _bodyStyle);
            EditorGUILayout.LabelField("   • Set name and description", _bodyStyle);
            EditorGUILayout.LabelField("   • Choose accent colors", _bodyStyle);
            EditorGUILayout.LabelField("   • Create badge (optional)", _bodyStyle);

            DrawSeparator();
        }

        private void DrawGraphBasics()
        {
            EditorGUILayout.LabelField("SKILLGRAPH BASICS", _sectionStyle);

            EditorGUILayout.LabelField("<b>Entry Points:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Nodes with no input connections are entry points",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Graph executes from all entry points", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Common entries: Battle Starts, Turn Starts, Unit Attacks",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Node Connections:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • <b>ExecutionFlow</b> (white): Controls sequence of execution",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • <b>Number</b> (green): Passes numeric values between nodes",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • <b>Conditional</b> (blue): Passes boolean values for conditions",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Execution Order:</b>", _bodyStyle);
            EditorGUILayout.LabelField("  1. Find all entry nodes", _bodyStyle);
            EditorGUILayout.LabelField("  2. Execute each entry node", _bodyStyle);
            EditorGUILayout.LabelField("  3. Follow ExecutionFlow connections", _bodyStyle);
            EditorGUILayout.LabelField("  4. Execute connected nodes in sequence", _bodyStyle);
            EditorGUILayout.LabelField("  5. Stop when no more connections", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Async Operations:</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Some nodes wait (animations, delays)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Call graph.Proceed() to continue execution",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Useful for animation events", _bodyStyle);

            DrawSeparator();
        }

        private void DrawNodeTypes()
        {
            EditorGUILayout.LabelField("NODE CATEGORIES", _sectionStyle);

            EditorGUILayout.LabelField("<b>Flow Nodes (Entry Points & Triggers):</b>", _bodyStyle);
            GUILayout.Label("  • Battle Starts - Runs once at battle start", _codeStyle);
            GUILayout.Label("  • Turn Starts - Runs at start of unit's turn", _codeStyle);
            GUILayout.Label("  • Turn Ends - Runs at end of unit's turn", _codeStyle);
            GUILayout.Label("  • Unit Attacks - Triggers when unit attacks", _codeStyle);
            GUILayout.Label("  • Enemy Attacks - Triggers when enemy attacks", _codeStyle);
            GUILayout.Label("  • Enemy Defeated - Triggers when enemy dies", _codeStyle);
            GUILayout.Label("  • Flow If - Conditional branching", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Event Nodes (Actions & Effects):</b>", _bodyStyle);
            GUILayout.Label("  • Damage - Deal damage to targets", _codeStyle);
            GUILayout.Label("  • Heal - Restore HP to targets", _codeStyle);
            GUILayout.Label("  • Warp - Teleport unit", _codeStyle);
            GUILayout.Label("  • Reposition - Move unit to new location", _codeStyle);
            GUILayout.Label("  • Swap - Exchange positions with target", _codeStyle);
            GUILayout.Label("  • Steal - Take item from target", _codeStyle);
            GUILayout.Label("  • Take Another Turn - Grant extra turn", _codeStyle);
            GUILayout.Label("  • Reduce Damage - Mitigate incoming damage", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>Condition Nodes (Checks & Comparisons):</b>",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  • Check stats, health, position", _bodyStyle);
            EditorGUILayout.LabelField("  • Compare values (>, <, ==)", _bodyStyle);
            EditorGUILayout.LabelField("  • Boolean operations (AND, OR, NOT)", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Math Nodes (Calculations):</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Number operations (+, -, *, /)", _bodyStyle);
            EditorGUILayout.LabelField("  • Number inputs (constants)", _bodyStyle);
            EditorGUILayout.LabelField("  • Comparisons for conditions", _bodyStyle);

            DrawSeparator();
        }

        private void DrawExample1()
        {
            EditorGUILayout.LabelField("EXAMPLE 1: Simple Attack Skill", _sectionStyle);

            EditorGUILayout.LabelField(
                "<b>Scenario:</b> Deal 10 damage when unit attacks",
                _exampleStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>SkillGraph Setup:</b>", _exampleStyle);
            GUILayout.Label("Nodes:", _codeStyle);
            GUILayout.Label("  1. Unit Attacks (entry node)", _codeStyle);
            GUILayout.Label("     └─> 2. Damage Node", _codeStyle);
            GUILayout.Label("            - Damage: 10", _codeStyle);
            GUILayout.Label("            - Target: From Context", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Execution:</b>", _exampleStyle);
            EditorGUILayout.LabelField("  • When unit attacks, trigger fires", _exampleStyle);
            EditorGUILayout.LabelField("  • Damage node executes", _exampleStyle);
            EditorGUILayout.LabelField("  • 10 damage applied to target", _exampleStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<color=#90EE90>Result: Simple direct damage skill</color>",
                _exampleStyle
            );

            DrawSeparator();
        }

        private void DrawExample2()
        {
            EditorGUILayout.LabelField("EXAMPLE 2: Conditional Passive Skill", _sectionStyle);

            EditorGUILayout.LabelField(
                "<b>Scenario:</b> Heal self by 5 HP at turn start if HP < 50%",
                _exampleStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>SkillGraph Setup:</b>", _exampleStyle);
            GUILayout.Label("Nodes:", _codeStyle);
            GUILayout.Label("  1. Turn Starts (entry)", _codeStyle);
            GUILayout.Label("     └─> 2. Check HP Condition", _codeStyle);
            GUILayout.Label("            └─> 3. Flow If (HP < 50%)", _codeStyle);
            GUILayout.Label("                   └─> True: 4. Heal Node (5 HP)", _codeStyle);
            GUILayout.Label("                   └─> False: (nothing)", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Execution:</b>", _exampleStyle);
            EditorGUILayout.LabelField("  • At turn start, check current HP", _exampleStyle);
            EditorGUILayout.LabelField("  • If HP below 50%, flow continues", _exampleStyle);
            EditorGUILayout.LabelField("  • Heal node restores 5 HP", _exampleStyle);
            EditorGUILayout.LabelField("  • If HP above 50%, skill does nothing", _exampleStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<color=#90EE90>Result: Conditional passive with self-healing</color>",
                _exampleStyle
            );

            DrawSeparator();
        }

        private void DrawExecutionFlow()
        {
            EditorGUILayout.LabelField("EXECUTION & CONTEXT", _sectionStyle);

            EditorGUILayout.LabelField("<b>BattleContext:</b>", _bodyStyle);
            EditorGUILayout.LabelField("  • Passed to all nodes during execution", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • Contains: Unit, Targets, Brain, Battle state",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • Nodes read/modify context during execution",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Skill Execution Path:</b>", _bodyStyle);
            GUILayout.Label("  1. Battle system triggers skill", _codeStyle);
            GUILayout.Label("  2. SkillInstance.ExecuteSkill(context)", _codeStyle);
            GUILayout.Label("  3. Skill.ExecuteSkill(context)", _codeStyle);
            GUILayout.Label("  4. SkillGraph.Execute(context)", _codeStyle);
            GUILayout.Label("  5. SkillGraphExecutor runs nodes", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Common Patterns:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "  • <b>Passive skills:</b> Battle Starts → Check conditions → Effects",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • <b>Active skills:</b> Unit Attacks → Calculate damage → Apply effects",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • <b>Reactive skills:</b> Enemy Attacks → Reduce damage → Counter",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  • <b>Turn-based:</b> Turn Starts/Ends → Apply buffs/debuffs",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawTips()
        {
            EditorGUILayout.LabelField("💡 TIPS & BEST PRACTICES", _sectionStyle);

            EditorGUILayout.LabelField(
                "• <b>ALWAYS create SkillGraph BEFORE Skill asset</b>",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• <b>ALWAYS assign SkillGraph to Skill</b> - execution will fail without it",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use clear node names to document skill behavior",
                _bodyStyle
            );
            EditorGUILayout.LabelField("• Entry nodes need no input connections", _bodyStyle);
            EditorGUILayout.LabelField("• Use Flow If nodes for conditional behavior", _bodyStyle);
            EditorGUILayout.LabelField(
                "• Multiple entry nodes can exist in one graph (Battle Starts + Turn Starts)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("• Test skills in Play Mode with battle system", _bodyStyle);
            EditorGUILayout.LabelField(
                "• Graph.Proceed() advances execution after async operations",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use Number/Conditional ports to pass data between nodes",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• ExecutionFlow (white) controls the sequence of node execution",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Circular execution chains are detected and stopped",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• SkillGraphRepair auto-fixes graph issues on import",
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
