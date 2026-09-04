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

            EditorGUILayout.LabelField("Direction", _sectionStyle);
            EditorGUILayout.LabelField(
                "Every conversation file begins with a direction line. TD means the chart reads from top to bottom.",
                _bodyStyle
            );
            GUILayout.Label("flowchart TD", _codeStyle);
            DrawSeparator();

            EditorGUILayout.LabelField("Node IDs", _sectionStyle);
            EditorGUILayout.LabelField(
                "A node is one step in the conversation. Each line has an ID and a body inside square brackets. The ID is how nodes connect to each other, and it must be unique within the conversation.",
                _bodyStyle
            );

            GUILayout.Label("PART1_Aubrey_NEUTRAL-Greeting[\"Aubrey: Hello!\"]", _codeStyle);
            GUILayout.Label(
                "|------------- ID ----------------| |-- brackets --| |-- player text --|",
                _codeStyle
            );
            GUILayout.Label("PART1_Aubrey_NEUTRAL-Greeting", _codeStyle);
            GUILayout.Label("PART1_Aubrey_NEUTRAL-Reply", _codeStyle);
            GUILayout.Label("PART1_LadyOfTheLake_MYSTERIOUS-Intro", _codeStyle);
            EditorGUILayout.LabelField(
                "Dialogue IDs use the format <b>PART&lt;Number&gt;_&lt;Speaker&gt;_&lt;Emotion&gt;-&lt;Descriptor&gt;</b>. The descriptor is a short, unique name so a character can speak more than once with the same emotion. After the part number there must be exactly one underscore (between speaker and emotion) and exactly one hyphen (between emotion and descriptor). Multi-word speaker names remove spaces rather than using underscores; no other underscores or hyphens are allowed inside the speaker, emotion, or descriptor.",
                _bodyStyle
            );
            DrawSeparator();

            EditorGUILayout.LabelField("Connections", _sectionStyle);
            EditorGUILayout.LabelField(
                "Connect nodes with arrows so the conversation knows what to play next. When one node points to several Choice nodes, the game shows them as buttons.",
                _bodyStyle
            );
            GUILayout.Label(
                "PART1_Aubrey_NEUTRAL-Greeting --> PART1_Player_NEUTRAL-Reply",
                _codeStyle
            );
            GUILayout.Label("|---- first node ----| |arrow| |---- next node ----|", _codeStyle);
            DrawSeparator();

            EditorGUILayout.LabelField("Start", _sectionStyle);
            GUILayout.Label("PART1_Start[\"Hub conversation\"]", _codeStyle);
            EditorGUILayout.LabelField(
                "Every conversation must have exactly one Start node. This is where PlayConversationById begins. If a file has several Starts, use StartConversationById(id, nodeId) to begin from a specific one.",
                _bodyStyle
            );
            DrawSeparator();

            EditorGUILayout.LabelField("Dialogue", _sectionStyle);
            GUILayout.Label("PART1_Aubrey_NEUTRAL-Greeting[\"Aubrey: Hello.\"]", _codeStyle);
            EditorGUILayout.LabelField(
                "Displays the quoted text and shows the speaker's portrait using the emotion keyword. The speaker name is matched to a CharacterData asset through the Conversation's People list.",
                _bodyStyle
            );
            DrawSeparator();

            EditorGUILayout.LabelField("Emotion keywords", _sectionStyle);
            GUILayout.Label("PART1_Aubrey_ANGRY-Warning", _codeStyle);
            GUILayout.Label("PART1_Aubrey_SAD-Goodbye", _codeStyle);
            GUILayout.Label("PART1_Aubrey_HAPPY-Celebration", _codeStyle);
            EditorGUILayout.LabelField(
                "The word between the last underscore and the hyphen is the emotion keyword. It selects which portrait or expression to show. You can use any keyword as long as the CharacterData has a matching portrait. Common examples: NEUTRAL, ANGRY, SAD, HAPPY, ANNOYED, EXPLAINING.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "If the portrait key is missing, the system tries a case-insensitive match, then falls back to a portrait named <b>default</b>.",
                _bodyStyle
            );
            DrawSeparator();

            EditorGUILayout.LabelField("Choice", _sectionStyle);
            GUILayout.Label("PART1_Choice_Agree[\"OPTION A: I agree.\"]", _codeStyle);
            EditorGUILayout.LabelField(
                "The quoted text becomes the choice button label. A Choice node must have exactly one outgoing arrow, which points to the node that plays after the player picks it.",
                _bodyStyle
            );
            DrawSeparator();

            EditorGUILayout.LabelField("Dialogue To Choices", _sectionStyle);
            EditorGUILayout.LabelField(
                "Create several Choice nodes and point one dialogue node at all of them. The controller will pause the conversation and show each choice as a button.",
                _bodyStyle
            );
            GUILayout.Label(
                "PART1_Aubrey_NEUTRAL-Question[\"Aubrey: Which way should we go?\"]",
                _codeStyle
            );
            GUILayout.Label("PART1_Choice_Left[\"Left into the forest.\"]", _codeStyle);
            GUILayout.Label("PART1_Choice_Right[\"Right toward the river.\"]", _codeStyle);
            GUILayout.Label(
                "PART1_Aubrey_NEUTRAL-GoLeft[\"Aubrey: The forest it is.\"]",
                _codeStyle
            );
            GUILayout.Label(
                "PART1_Aubrey_HAPPY-GoRight[\"Aubrey: I love the river!\"]",
                _codeStyle
            );
            GUILayout.Label("PART1_Aubrey_NEUTRAL-Question --> PART1_Choice_Left", _codeStyle);
            GUILayout.Label("PART1_Aubrey_NEUTRAL-Question --> PART1_Choice_Right", _codeStyle);
            GUILayout.Label("PART1_Choice_Left --> PART1_Aubrey_NEUTRAL-GoLeft", _codeStyle);
            GUILayout.Label("PART1_Choice_Right --> PART1_Aubrey_HAPPY-GoRight", _codeStyle);
            EditorGUILayout.LabelField(
                "The dialogue node plays first, then the player sees two buttons. Each choice leads to its own follow-up dialogue node.",
                _bodyStyle
            );
            DrawSeparator();

            EditorGUILayout.LabelField("Action", _sectionStyle);
            GUILayout.Label(
                "PART1_Action_GainSupport_Aubrey_PP[\"GAIN ++ SUPPORT WITH AUBREY\"]",
                _codeStyle
            );
            GUILayout.Label(
                "PART1_Action_LoseSupport_Aubrey_M[\"LOSE - SUPPORT WITH AUBREY\"]",
                _codeStyle
            );
            GUILayout.Label(
                "PART1_Action_UnlockBattle_TakeOutTheTrash[\"UNLOCK BATTLE\"]",
                _codeStyle
            );
            GUILayout.Label(
                "PART1_Action_PlayerGainsItem_IronSword[\"GAIN IRON SWORD\"]",
                _codeStyle
            );
            GUILayout.Label(
                "PART1_Action_PlayerLosesItem_RustyKey[\"LOSE RUSTY KEY\"]",
                _codeStyle
            );
            GUILayout.Label(
                "PART1_Action_CharacterJoinsTeam_Aubrey[\"AUBREY JOINS TEAM\"]",
                _codeStyle
            );
            GUILayout.Label(
                "PART1_Action_CharacterLeavesTeam_TempAlly[\"TEMP ALLY LEAVES\"]",
                _codeStyle
            );
            EditorGUILayout.LabelField(
                "The ID tells the game what side effect to apply. The text inside the brackets is optional and is never shown to the player; it is only a note for the writer.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "Support strength suffixes: PP/PlusPlus (+20), P/Plus (+10), MM/MinusMinus (-20), M/Minus (-10). Items load from Resources/Items. Characters load from Resources or the active roster.",
                _bodyStyle
            );
            DrawSeparator();

            EditorGUILayout.LabelField("Condition", _sectionStyle);
            GUILayout.Label("PART2_Condition_FirstIsopod[FIRST ISOPOD ENCOUNTER]", _codeStyle);
            EditorGUILayout.LabelField(
                "Pauses the conversation until gameplay reports this moment. The condition name is the part after Condition_, not the full ID. For this node the condition name is <b>FirstIsopod</b>.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "From code, use the active Brain's ConversationalBrain:",
                _bodyStyle
            );
            GUILayout.Label(
                "brain.ConversationalBrain.NotifyConversationCondition(conversation, \"FirstIsopod\");",
                _codeStyle
            );
            EditorGUILayout.LabelField(
                "Conditions can also be chained. If a node points to several Condition nodes, reporting the name of any of them will jump straight to that condition's target, skipping the others.",
                _bodyStyle
            );
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
            GUILayout.Label(
                "Changes avatar-to-character support points. Target is a speaker name from the conversation's People list. Strength suffixes (PP/P/MM/M) map to +/- 20/10.",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>UnlockBattle</b>", _bodyStyle);
            GUILayout.Label("Unlocks a battle scene id for scene-flow selection.", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>PlayerGainsItem / PlayerLosesItem</b>", _bodyStyle);
            GUILayout.Label(
                "Adds or removes an item in the avatar's inventory. Target is the ObjectItem asset name under Resources/Items. Lose removes the first matching instance.",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>CharacterJoinsTeam / CharacterLeavesTeam</b>",
                _bodyStyle
            );
            GUILayout.Label(
                "Adds or removes a character from the player roster. Target is a CharacterData asset name (Resources) or a display name in the active roster. Join creates the instance if needed.",
                _codeStyle
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
                "• Speaker names in IDs can't have spaces or underscores. Remove spaces for multi-word names (LadyOfTheLake) and map them to CharacterData for real display names.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Every dialogue ID must end with <b>-Descriptor</b>. Two lines from the same character and emotion must use different descriptors.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Emotion keywords are portrait keys; unknown keys fall back to 'default'.",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "• Use <b>Action_</b> nodes to fire brain events and <b>Condition_</b> nodes to wait for triggers.",
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
