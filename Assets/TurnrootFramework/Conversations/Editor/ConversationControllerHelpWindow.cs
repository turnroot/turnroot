using UnityEditor;
using UnityEngine;

namespace Turnroot.Conversations.Editor
{
    /// <summary>
    /// Custom editor window displaying comprehensive help documentation for the ConversationController system.
    /// </summary>
    public class ConversationControllerHelpWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _exampleStyle;
        private GUIStyle _codeStyle;

        [MenuItem("Window/Turnroot/Help/Conversation System Help")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConversationControllerHelpWindow>("Conversation System Help");
            window.minSize = new Vector2(600, 400);
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
            DrawLinearExample();
            DrawBranchingExample();
            DrawControllerMethods();
            DrawReroutes();
            DrawTips();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("💬 CONVERSATION SYSTEM GUIDE", _headerStyle);
            EditorGUILayout.LabelField(
                "Complete documentation for ConversationController, linear & branching dialogue",
                _bodyStyle
            );
            DrawSeparator();
        }

        private void DrawArchitecture()
        {
            EditorGUILayout.LabelField("ARCHITECTURE OVERVIEW", _sectionStyle);

            EditorGUILayout.LabelField("• <b>Conversation</b> (ScriptableObject)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Defines dialogue flow (linear or branching)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  └─ Contains ConversationLayers or ConversationGraph",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>ConversationLayer</b> (data)", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Single dialogue segment", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Contains text, speaker info, portraits, events",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "• <b>ConversationInstance</b> (ScriptableObject)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Configuration wrapper for a Conversation", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Can override events per instance", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>ConversationController</b> (component)", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Manages playback and UI", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Handles portrait display", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Processes player choices (branching)", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>ConversationGraph</b> (XNode graph)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Visual node editor for branching conversations",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Nodes contain layers, edges define flow", _bodyStyle);

            DrawSeparator();
        }

        private void DrawWorkflow()
        {
            EditorGUILayout.LabelField("SETUP WORKFLOW", _sectionStyle);

            EditorGUILayout.LabelField("<b>1. Create a Conversation asset:</b>", _bodyStyle);
            GUILayout.Label("   Right-click → Create → Turnroot/Conversation", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>2. Choose conversation type:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "   • <b>Linear:</b> Uncheck 'Branching Conversation', add layers",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "   • <b>Branching:</b> Keep checked, assign ConversationGraph",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>3. Configure ConversationLayers:</b>", _bodyStyle);
            EditorGUILayout.LabelField("   • Set speaker (CharacterData)", _bodyStyle);
            EditorGUILayout.LabelField("   • Choose portrait key", _bodyStyle);
            EditorGUILayout.LabelField("   • Write dialogue text", _bodyStyle);
            EditorGUILayout.LabelField("   • Add optional events", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>4. Create ConversationInstance:</b>", _bodyStyle);
            GUILayout.Label("   Right-click → Create → Turnroot/Conversation Instance", _codeStyle);
            EditorGUILayout.LabelField("   • Assign your Conversation", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>5. Setup ConversationController in scene:</b>",
                _bodyStyle
            );
            EditorGUILayout.LabelField("   • Add component to GameObject", _bodyStyle);
            EditorGUILayout.LabelField(
                "   • Assign UI references (dialogue text, portraits)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("   • Add ConversationInstances to list", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>6. Start conversation:</b>", _bodyStyle);
            EditorGUILayout.LabelField("   • Call StartConversation() in play mode", _bodyStyle);
            EditorGUILayout.LabelField("   • Or use DynamicSceneFlow reroutes", _bodyStyle);

            DrawSeparator();
        }

        private void DrawLinearExample()
        {
            EditorGUILayout.LabelField("EXAMPLE 1: Linear Conversation", _sectionStyle);

            EditorGUILayout.LabelField(
                "<b>Scenario:</b> Two characters talk in sequence",
                _exampleStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Conversation Setup:</b>", _exampleStyle);
            GUILayout.Label("Branching Conversation: Unchecked", _codeStyle);
            GUILayout.Label("Layers:", _codeStyle);
            GUILayout.Label("  Layer 0:", _codeStyle);
            GUILayout.Label("    Speaker: Hero", _codeStyle);
            GUILayout.Label("    Portrait: Neutral", _codeStyle);
            GUILayout.Label("    Text: \"I need to find the ancient sword.\"", _codeStyle);
            GUILayout.Label("  Layer 1:", _codeStyle);
            GUILayout.Label("    Speaker: Mentor", _codeStyle);
            GUILayout.Label("    Portrait: Wise", _codeStyle);
            GUILayout.Label("    Text: \"The sword lies beyond the mountains.\"", _codeStyle);
            GUILayout.Label("  Layer 2:", _codeStyle);
            GUILayout.Label("    Speaker: Hero", _codeStyle);
            GUILayout.Label("    Portrait: Determined", _codeStyle);
            GUILayout.Label("    Text: \"I'll begin my journey at dawn.\"", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Playback:</b>", _exampleStyle);
            EditorGUILayout.LabelField(
                "  • Call StartConversation() → Shows Layer 0",
                _exampleStyle
            );
            EditorGUILayout.LabelField("  • Player clicks/advances → Layer 1", _exampleStyle);
            EditorGUILayout.LabelField("  • Player clicks/advances → Layer 2", _exampleStyle);
            EditorGUILayout.LabelField(
                "  • Player clicks/advances → Conversation ends",
                _exampleStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<color=#90EE90>Result: Simple linear dialogue sequence</color>",
                _exampleStyle
            );

            DrawSeparator();
        }

        private void DrawBranchingExample()
        {
            EditorGUILayout.LabelField("EXAMPLE 2: Branching Conversation", _sectionStyle);

            EditorGUILayout.LabelField(
                "<b>Scenario:</b> Player makes choices that affect dialogue flow",
                _exampleStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>ConversationGraph Setup:</b>", _exampleStyle);
            GUILayout.Label("Node 1 (Entry):", _codeStyle);
            GUILayout.Label("  Layer: \"Will you help us?\"", _codeStyle);
            GUILayout.Label("  Choices:", _codeStyle);
            GUILayout.Label("    → \"Yes, I'll help\" → Node 2", _codeStyle);
            GUILayout.Label("    → \"No, I refuse\" → Node 3", _codeStyle);
            GUILayout.Label("", _codeStyle);
            GUILayout.Label("Node 2 (Help Path):", _codeStyle);
            GUILayout.Label("  Layer: \"Thank you! The village is saved!\"", _codeStyle);
            GUILayout.Label("  Next: End", _codeStyle);
            GUILayout.Label("", _codeStyle);
            GUILayout.Label("Node 3 (Refuse Path):", _codeStyle);
            GUILayout.Label("  Layer: \"We'll have to manage alone then...\"", _codeStyle);
            GUILayout.Label("  Next: End", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Playback:</b>", _exampleStyle);
            EditorGUILayout.LabelField(
                "  • Shows layer with choice buttons at bottom",
                _exampleStyle
            );
            EditorGUILayout.LabelField(
                "  • Player clicks choice → Conversation branches",
                _exampleStyle
            );
            EditorGUILayout.LabelField(
                "  • Different outcome based on player decision",
                _exampleStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<color=#90EE90>Result: Player agency through branching paths</color>",
                _exampleStyle
            );

            DrawSeparator();
        }

        private void DrawControllerMethods()
        {
            EditorGUILayout.LabelField("CONVERSATIONCONTROLLER PUBLIC API", _sectionStyle);

            EditorGUILayout.LabelField("<b>Playback Control:</b>", _bodyStyle);
            GUILayout.Space(4);

            GUILayout.Label("StartConversation() - Starts current conversation", _codeStyle);
            GUILayout.Label(
                "StartConversationAtIndex(int) - Start specific conversation",
                _codeStyle
            );
            GUILayout.Label("NextLayer() - Advance to next layer", _codeStyle);
            GUILayout.Label("Advance() - Alias for NextLayer()", _codeStyle);
            GUILayout.Label("Proceed() - Alias for NextLayer()", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Conversation Selection:</b>", _bodyStyle);
            GUILayout.Space(4);

            GUILayout.Label("IncrementConversationIndex() - Next in list", _codeStyle);
            GUILayout.Label("DecrementConversationIndex() - Previous in list", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Branching Control:</b>", _bodyStyle);
            GUILayout.Space(4);

            GUILayout.Label("ChooseBranchTarget(int) - Select choice by node ID", _codeStyle);
            GUILayout.Label("GetCurrentChoices() - Get available choices", _codeStyle);

            DrawSeparator();
        }

        private void DrawReroutes()
        {
            EditorGUILayout.LabelField("UNITY EVENT REROUTES (DynamicSceneFlow)", _sectionStyle);

            EditorGUILayout.LabelField("<b>Available methods for Unity Events:</b>", _bodyStyle);
            GUILayout.Space(4);

            GUILayout.Label("StartConversation() - Start current conversation", _codeStyle);
            GUILayout.Label("AdvanceConversation() - Advance to next layer", _codeStyle);
            GUILayout.Label("StartConversationAtIndex(int) - Start by index", _codeStyle);
            GUILayout.Label("NextConversation() - Increment index", _codeStyle);
            GUILayout.Label("PreviousConversation() - Decrement index", _codeStyle);
            GUILayout.Label("ChooseBranch(int) - Select branch choice", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Usage Example:</b>", _exampleStyle);
            GUILayout.Label("DynamicSceneFlow Segment event:", _codeStyle);
            GUILayout.Label("  → StartConversation()", _codeStyle);
            GUILayout.Label("", _codeStyle);
            GUILayout.Label("Button Press event:", _codeStyle);
            GUILayout.Label("  → AdvanceConversation()", _codeStyle);

            DrawSeparator();
        }

        private void DrawTips()
        {
            EditorGUILayout.LabelField("💡 TIPS & BEST PRACTICES", _sectionStyle);

            EditorGUILayout.LabelField(
                "• <b>Linear conversations</b> are simpler for cutscenes and narration",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• <b>Branching conversations</b> give players agency and replay value",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use <b>ConversationInstances</b> to reuse same Conversation with different events",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Set <b>portrait keys</b> in CharacterData for automatic portrait display",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use <b>OnLayerStart/OnLayerEnd</b> events for mid-conversation effects",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• <b>Choice buttons</b> are auto-generated from graph edges (branching)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Test in Play Mode - use inspector buttons to advance dialogue",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• <b>Secondary portraits</b> can show multiple speakers simultaneously",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use <b>Graphics2DSettings</b> to configure portrait transition effects",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Entry nodes in branching graphs are nodes with no incoming connections",
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
