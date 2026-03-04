using UnityEditor;
using UnityEngine;

namespace Turnroot.Gameplay.Audio.Editor
{
    /// <summary>
    /// Custom editor window displaying comprehensive help documentation for the AudioController system.
    /// </summary>
    public class AudioControllerHelpWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _exampleStyle;
        private GUIStyle _codeStyle;

        [MenuItem("Window/Turnroot/Audio System Help")]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioControllerHelpWindow>("Audio System Help");
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
                pixels[i] = color;

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnGUI()
        {
            if (_headerStyle == null)
                InitializeStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawArchitecture();
            DrawWorkflow();
            DrawExample1();
            DrawExample2();
            DrawExample3();
            DrawReroutes();
            DrawTips();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("🎵 AUDIO SYSTEM GUIDE", _headerStyle);
            EditorGUILayout.LabelField(
                "Complete documentation for AudioController, profiles, and scene integration",
                _bodyStyle
            );
            DrawSeparator();
        }

        private void DrawArchitecture()
        {
            EditorGUILayout.LabelField("ARCHITECTURE OVERVIEW", _sectionStyle);

            EditorGUILayout.LabelField(
                "• <b>AudioSegmentProfile</b> (ScriptableObject)",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "  └─ Reusable list of audio actions (assets you create)",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>AudioAction</b> (data)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Single instruction: Play, FadeIn, FadeOut, Stop, etc.",
                _bodyStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>AudioController</b> (component)", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Manages AudioSources by group (Music/SFX/Voices)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("  └─ Maps segment indices to profiles", _bodyStyle);
            EditorGUILayout.LabelField("  └─ Handles conditional profile selection", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("• <b>DynamicSceneFlow</b> segments", _bodyStyle);
            EditorGUILayout.LabelField(
                "  └─ Your scene flow states that trigger audio",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawWorkflow()
        {
            EditorGUILayout.LabelField("SETUP WORKFLOW", _sectionStyle);

            EditorGUILayout.LabelField("<b>1. Create AudioSegmentProfile assets:</b>", _bodyStyle);
            GUILayout.Label(
                "   Right-click → Create → Turnroot/Audio/Audio Segment Profile",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<b>2. Add AudioSources to AudioController:</b>",
                _bodyStyle
            );
            EditorGUILayout.LabelField(
                "   • Drag AudioSources into Music/SFX/Voices groups",
                _bodyStyle
            );
            EditorGUILayout.LabelField("   • Need 2+ Music sources for crossfade", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>3. Configure Audio Segments:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "   • Create segment configs in AudioController",
                _bodyStyle
            );
            EditorGUILayout.LabelField("   • Assign profiles to segments", _bodyStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>4. Call from DynamicSceneFlow:</b>", _bodyStyle);
            EditorGUILayout.LabelField(
                "   • Use PlaySegmentAudio(index) in segment events",
                _bodyStyle
            );

            DrawSeparator();
        }

        private void DrawExample1()
        {
            EditorGUILayout.LabelField("EXAMPLE 1: Battle Intro", _sectionStyle);

            EditorGUILayout.LabelField("<b>Profile</b> 'Battle_Intro_Music':", _exampleStyle);
            GUILayout.Label("Action 0: FadeOut, Group=Music, Duration=1s", _codeStyle);
            GUILayout.Label(
                "Action 1: Play, Group=Music, Clip=BattleTheme, Loop=true, Delay=1s",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>AudioController:</b>", _exampleStyle);
            GUILayout.Label("Segment 0: 'BattleStart' → Battle_Intro_Music", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>DynamicSceneFlow Segment 0 event:</b>", _exampleStyle);
            GUILayout.Label("→ PlaySegmentAudio(0)", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<color=#90EE90>Result: Fades out map music, plays battle theme</color>",
                _exampleStyle
            );

            DrawSeparator();
        }

        private void DrawExample2()
        {
            EditorGUILayout.LabelField("EXAMPLE 2: Conditional Music (Boss)", _sectionStyle);

            EditorGUILayout.LabelField("<b>Profiles:</b>", _exampleStyle);
            EditorGUILayout.LabelField("  • Battle_Normal_Music", _exampleStyle);
            EditorGUILayout.LabelField("  • Battle_Boss_Music", _exampleStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>AudioController Segment Config:</b>", _exampleStyle);
            GUILayout.Label("Name: 'BattleStart'", _codeStyle);
            GUILayout.Label("Default: Battle_Normal_Music", _codeStyle);
            GUILayout.Label(
                "Conditional: Key='isBossBattle', Profile=Battle_Boss_Music",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField("<b>Code:</b>", _exampleStyle);
            GUILayout.Label(
                "if (isBoss) sceneFlow.SetAudioCondition(\"isBossBattle=true\");",
                _codeStyle
            );
            GUILayout.Label("sceneFlow.PlaySegmentAudio(0);", _codeStyle);
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<color=#90EE90>Result: Plays boss music if condition true, else normal</color>",
                _exampleStyle
            );

            DrawSeparator();
        }

        private void DrawExample3()
        {
            EditorGUILayout.LabelField("EXAMPLE 3: Cutscene with Voice & SFX", _sectionStyle);

            EditorGUILayout.LabelField("<b>Profile</b> 'Cutscene_Intro':", _exampleStyle);
            GUILayout.Label("Action 0: FadeOut, Group=Music, Duration=2s", _codeStyle);
            GUILayout.Label(
                "Action 1: Play, Group=Voices, Clip=Hero_Line1, Delay=0.5s",
                _codeStyle
            );
            GUILayout.Label("Action 2: Play, Group=SFX, Clip=DoorSlam, Delay=3s", _codeStyle);
            GUILayout.Label(
                "Action 3: Play, Group=Voices, Clip=Hero_Line2, Delay=3.5s",
                _codeStyle
            );
            GUILayout.Label(
                "Action 4: FadeIn, Group=Music, Clip=TownTheme, Duration=2s, Delay=6s",
                _codeStyle
            );
            GUILayout.Space(4);

            EditorGUILayout.LabelField(
                "<color=#90EE90>Result: Orchestrated sequence with precise timing</color>",
                _exampleStyle
            );

            DrawSeparator();
        }

        private void DrawReroutes()
        {
            EditorGUILayout.LabelField("UNITY EVENT REROUTES (DynamicSceneFlow)", _sectionStyle);

            EditorGUILayout.LabelField("<b>Available methods for Unity Events:</b>", _bodyStyle);
            GUILayout.Space(4);

            GUILayout.Label("PlaySegmentAudio(int) - Play segment by index", _codeStyle);
            GUILayout.Label("PlaySegmentAudioByName(string) - Play by name", _codeStyle);
            GUILayout.Label("PlayVoiceClip(AudioClip) - One-shot voice", _codeStyle);
            GUILayout.Label("PlaySfxClip(AudioClip) - One-shot SFX", _codeStyle);
            GUILayout.Label("CrossfadeMusic(AudioClip) - Smooth music transition", _codeStyle);
            GUILayout.Label("FadeOutMusic() - Fade music over 2s", _codeStyle);
            GUILayout.Label("StopAllMusic() - Immediate stop all music", _codeStyle);
            GUILayout.Label("StopAllSFX() - Immediate stop all SFX", _codeStyle);
            GUILayout.Label("StopAllVoices() - Immediate stop all voices", _codeStyle);
            GUILayout.Label("SetAudioCondition(string) - Format: 'key=true'", _codeStyle);

            DrawSeparator();
        }

        private void DrawTips()
        {
            EditorGUILayout.LabelField("💡 TIPS", _sectionStyle);

            EditorGUILayout.LabelField(
                "• Use 2+ Music sources for seamless crossfades",
                _bodyStyle
            );
            EditorGUILayout.LabelField("• Set delays on actions for precise timing", _bodyStyle);
            EditorGUILayout.LabelField(
                "• Use conditions for dynamic music (boss/exploration)",
                _bodyStyle
            );
            EditorGUILayout.LabelField("• Profile assets are reusable across scenes", _bodyStyle);
            EditorGUILayout.LabelField("• FadeIn/FadeOut are smoother than Play/Stop", _bodyStyle);
            EditorGUILayout.LabelField(
                "• Test in play mode to hear timing adjustments",
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
