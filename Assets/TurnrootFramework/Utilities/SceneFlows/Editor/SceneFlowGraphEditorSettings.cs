#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Turnroot.Utilities.SceneFlows.Editor
{
    /// <summary>
    /// Settings asset for customizing the scene flow graph editor appearance and behavior.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SceneFlowGraphEditorSettings",
        menuName = "Turnroot/Editor Settings/Scene Flow Graph Editor Settings"
    )]
    public class SceneFlowGraphEditorSettings : ScriptableObject
    {
        [Header("Node Appearance")]
        [Tooltip("Background color for unselected normal nodes")]
        public Color nodeColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        [Tooltip("Background color for selected normal nodes")]
        public Color nodeSelectedColor = new Color(0.2f, 0.5f, 0.8f, 1f);

        [Tooltip("Background color for unselected hub nodes")]
        public Color hubNodeColor = new Color(0.5f, 0.3f, 0.6f, 1f);

        [Tooltip("Background color for selected hub nodes")]
        public Color hubNodeSelectedColor = new Color(0.6f, 0.4f, 0.8f, 1f);

        [Tooltip("Font size for node labels")]
        [Range(8, 20)]
        public int nodeFontSize = 12;

        [Tooltip("Badge color for chapter numbers on nodes")]
        public Color chapterBadgeColor = new Color(0.4f, 0.6f, 1f, 0.8f);

        [Header("Transition Appearance")]
        [Tooltip("Color for unselected transitions")]
        public Color transitionColor = Color.white;

        [Tooltip("Color for selected transitions")]
        public Color transitionSelectedColor = Color.cyan;

        [Tooltip("Color for bidirectional transitions")]
        public Color transitionBidirectionalColor = new Color(0.5f, 1f, 0.5f);

        [Tooltip("Color for transitions with conditions")]
        public Color transitionConditionalColor = new Color(1f, 0.8f, 0.3f);

        [Tooltip("Color for transitions between different chapters")]
        public Color chapterTransitionColor = new Color(1f, 0.4f, 0.4f);

        [Tooltip("Color for transition being created (during drag)")]
        public Color transitionCreationColor = Color.yellow;

        [Tooltip("Width/thickness of transition arrows")]
        [Range(1f, 10f)]
        public float transitionWidth = 3f;

        [Tooltip("Size of arrowheads on transitions")]
        [Range(5f, 30f)]
        public float arrowSize = 10f;

        [Tooltip("Distance arrows stop before reaching node edge (in pixels)")]
        [Range(10f, 100f)]
        public float arrowNodeOffset = 35f;

        [Header("Grid Appearance")]
        [Tooltip("Color for major grid lines")]
        public Color gridMajorColor = new Color(1f, 1f, 1f, 0.15f);

        [Tooltip("Color for minor grid lines")]
        public Color gridMinorColor = new Color(1f, 1f, 1f, 0.05f);

        [Tooltip("Background color for the graph area")]
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

        /// <summary>
        /// Singleton access to the settings. Will find or create a default instance.
        /// </summary>
        private static SceneFlowGraphEditorSettings _instance;

        public static SceneFlowGraphEditorSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Prefer a project-level override (any asset outside Assets/TurnrootFramework/)
                    // over the package default so users can customise without editing the package.
                    var guids = AssetDatabase.FindAssets("t:SceneFlowGraphEditorSettings");
                    string fallbackPath = null;
                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!path.StartsWith("Assets/TurnrootFramework/"))
                        {
                            _instance = AssetDatabase.LoadAssetAtPath<SceneFlowGraphEditorSettings>(
                                path
                            );
                            return _instance;
                        }
                        fallbackPath ??= path;
                    }
                    _instance =
                        fallbackPath != null
                            ? AssetDatabase.LoadAssetAtPath<SceneFlowGraphEditorSettings>(
                                fallbackPath
                            )
                            : CreateInstance<SceneFlowGraphEditorSettings>();
                }
                return _instance;
            }
        }
    }
}
#endif
