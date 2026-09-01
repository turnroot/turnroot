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

        public static void ShowWindowFromButton() => ShowWindow();

        private void OnEnable() => InitializeStyles();

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
                fontSize = 14,
                margin = new RectOffset(0, 0, 8, 4),
                normal = { textColor = new Color(1f, 0.85f, 0.4f) },
            };

            _bodyStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true,
                margin = new RectOffset(10, 10, 2, 6),
            };

            _exampleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true,
                margin = new RectOffset(20, 10, 2, 6),
                normal = { textColor = new Color(0.7f, 0.9f, 0.7f) },
            };

            _codeStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
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
            DrawSyntaxReference();
            DrawActionsReference();
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
                "Complete documentation for ConversationController and Mermaid-based dialogue",
                _bodyStyle
            );
            DrawSeparator();
        }

        private void DrawArchitecture()
        {
            EditorGUILayout.LabelField("HOW IT WORKS", _sectionStyle);

            EditorGUILayout.LabelField(
                "A <b>Conversation</b> asset points to a Mermaid text file. At runtime the file is parsed into a graph of dialogue lines, choices, actions, conditions, and signals. The <b>ConversationController</b> walks that graph and drives the UI.",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawWorkflow()
        {
            EditorGUILayout.LabelField("SETUP", _sectionStyle);

            GUILayout.Label("1. Create a Conversation asset (Turnroot/Conversation).", _codeStyle);
            GUILayout.Label(
                "2. Put the Conversation asset and its .mermaid TextAsset in Resources/Conversations.",
                _codeStyle
            );
            GUILayout.Label("3. Write the Mermaid graph (see Syntax below).", _codeStyle);
            GUILayout.Label("4. Assign the TextAsset and click Parse & Update People.", _codeStyle);
            GUILayout.Label("5. Map any unmatched speakers to CharacterData assets.", _codeStyle);
            GUILayout.Label(
                "6. Trigger it from code/events with PlayConversationById(\"MyConversation\").",
                _codeStyle
            );

            DrawSeparator();
        }

        private void DrawSyntaxReference()
        {
            EditorGUILayout.LabelField("SYNTAX", _sectionStyle);

            GUILayout.Label("Node IDs: PART<Number>_<Kind>_<Name>[_<Qualifier>]", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Dialogue</b>", _bodyStyle);
            GUILayout.Label("PART1_Aubrey_NEUTRAL[\"Aubrey: Hello.\"]", _codeStyle);
            GUILayout.Label(
                "Speaker comes from the ID; display name uses CharacterData.",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Choice</b>", _bodyStyle);
            GUILayout.Label("PART1_Choice_Agree[\"OPTION A: I agree.\"]", _codeStyle);
            GUILayout.Label("Body text becomes the choice button label.", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Action</b>", _bodyStyle);
            GUILayout.Label("PART1_Action_GainSupport_Aubrey_PP", _codeStyle);
            GUILayout.Label("PART1_Action_LoseSupport_Aubrey_M", _codeStyle);
            GUILayout.Label("PART1_Action_UnlockBattle_TakeOutTheTrash", _codeStyle);
            GUILayout.Label("PART1_Action_PlayerGainsItem_IronSword", _codeStyle);
            GUILayout.Label("PART1_Action_PlayerLosesItem_RustyKey", _codeStyle);
            GUILayout.Label("PART1_Action_CharacterJoinsTeam_Aubrey", _codeStyle);
            GUILayout.Label("PART1_Action_CharacterLeavesTeam_TempAlly", _codeStyle);
            GUILayout.Label(
                "Support strength suffixes: PP/PlusPlus, P/Plus, MM/MinusMinus, M/Minus.",
                _bodyStyle
            );
            GUILayout.Label(
                "Items load from Resources/Items. Characters load from Resources or the active roster.",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Condition</b>", _bodyStyle);
            GUILayout.Label("PART2_Condition_FirstIsopod", _codeStyle);
            GUILayout.Label(
                "Pauses until code fires NotifyConversationCondition(...).",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Signal</b>", _bodyStyle);
            GUILayout.Label("PART1_Signal_StartBattle", _codeStyle);
            GUILayout.Label("Fires OnConversationSignal so other systems can react.", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Start / Finish</b>", _bodyStyle);
            GUILayout.Label("PART1_Start([START])", _codeStyle);
            GUILayout.Label("PART1_Finish_NoUnlock([FINISH])", _codeStyle);
            GUILayout.Label("Use Finish, not End — End is a Mermaid reserved word.", _bodyStyle);

            DrawSeparator();
        }

        private void DrawControllerMethods()
        {
            EditorGUILayout.LabelField("CONTROLLER API", _sectionStyle);

            GUILayout.Label(
                "PlayConversationById(string id) — play from Resources/Conversations",
                _codeStyle
            );
            GUILayout.Label(
                "StartConversationById(string id, string nodeId) — resume at a node",
                _codeStyle
            );
            GUILayout.Label("PlayConversationDirect(Conversation) — play any asset", _codeStyle);
            GUILayout.Label("PlayConversationDirectFromNode(Conversation, nodeId)", _codeStyle);
            GUILayout.Label("NextLayer() / Advance() — advance dialogue", _codeStyle);
            GUILayout.Label("ChooseBranchTarget(string nodeId) — pick a choice", _codeStyle);

            DrawSeparator();
        }

        private void DrawActionsReference()
        {
            EditorGUILayout.LabelField("ACTIONS", _sectionStyle);

            EditorGUILayout.LabelField("<b>GainSupport / LoseSupport</b>", _bodyStyle);
            GUILayout.Label("Changes avatar-to-character support points.", _codeStyle);
            GUILayout.Label(
                "Target is a speaker name from the conversation's People list. Strength suffixes (PP/P/MM/M) map to +/- 20/10.",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>UnlockBattle</b>", _bodyStyle);
            GUILayout.Label("Unlocks a battle scene id for scene-flow selection.", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>PlayerGainsItem / PlayerLosesItem</b>", _bodyStyle);
            GUILayout.Label("Adds or removes an item in the avatar's inventory.", _codeStyle);
            GUILayout.Label(
                "Target is the ObjectItem asset name under Resources/Items. Lose removes the first matching instance.",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>CharacterJoinsTeam / CharacterLeavesTeam</b>",
                _bodyStyle
            );
            GUILayout.Label("Adds or removes a character from the player roster.", _codeStyle);
            GUILayout.Label(
                "Target is a CharacterData asset name (Resources) or a display name in the active roster. Join creates the instance if needed.",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawReroutes()
        {
            EditorGUILayout.LabelField("DYNAMICSCENEFLOW REROUTES", _sectionStyle);

            GUILayout.Label("StartConversation(string id)", _codeStyle);
            GUILayout.Label("StartConversationFromNode(string id, string nodeId)", _codeStyle);
            GUILayout.Label("AdvanceConversation()", _codeStyle);
            GUILayout.Label("ChooseBranch(string nodeId)", _codeStyle);

            DrawSeparator();
        }

        private void DrawTips()
        {
            EditorGUILayout.LabelField("TIPS", _sectionStyle);

            EditorGUILayout.LabelField(
                "• Speaker names in IDs can't have spaces; map them to CharacterData for real display names.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Emotion suffixes are portrait keys; unknown keys fall back to 'default'.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Never use <b>End</b> in a node ID — it's a Mermaid reserved word. Use <b>Finish</b>.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use <b>Signal_</b> nodes to fire brain events and <b>Condition_</b> nodes to wait for triggers.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Resume sub-conversations after scene changes with StartConversationById(id, nodeId).",
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
