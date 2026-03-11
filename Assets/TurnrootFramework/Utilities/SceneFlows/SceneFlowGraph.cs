using System;
using System.Collections.Generic;
using UnityEngine;

namespace Turnroot.Utilities.SceneFlows
{
    /// <summary>
    /// A graph-based scene flow system that defines scenes as a network rather than a linear sequence.
    /// Each scene exists once in the graph with connections to other scenes.
    /// This prevents duplication of hub scenes that appear multiple times in linear flows.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Turnroot/Scene Flow/Scene Flow Graph",
        fileName = "NewSceneFlowGraph"
    )]
    public class SceneFlowGraph : ScriptableObject
    {
        #region Serialized Fields

        [Header("Graph Structure")]
        [Tooltip("All scenes in this flow graph. Each scene should appear only once.")]
        public List<SceneNode> scenes = new();

        [Tooltip("All transitions between scenes. Defines the navigable paths through the graph.")]
        public List<SceneTransition> transitions = new();

        [Header("Starting Point")]
        [Tooltip("The scene ID to start from when this graph is first loaded.")]
        [SerializeField]
        private string _startingSceneId;

        #endregion

        #region Properties

        /// <summary>
        /// The starting scene node. Set via SetStartingScene().
        /// </summary>
        public SceneNode startingScene
        {
            get => GetScene(_startingSceneId);
            set => _startingSceneId = value?.id;
        }

        public string StartingSceneId => _startingSceneId;

        #endregion

        #region Starting Scene Configuration

        public void SetStartingScene(SceneNode scene) => _startingSceneId = scene?.id;

        public void SetStartingSceneById(string sceneId) => _startingSceneId = sceneId;

        #endregion

        #region Query Methods

        public SceneNode GetScene(string sceneId) => scenes.Find(s => s.id == sceneId);

        public SceneNode GetSceneByName(string sceneName) =>
            scenes.Find(s => s.sceneName == sceneName);

        public List<SceneTransition> GetTransitionsFrom(string fromSceneId) =>
            transitions.FindAll(t => t.fromSceneId == fromSceneId);

        public List<SceneTransition> GetTransitionsTo(string toSceneId) =>
            transitions.FindAll(t => t.toSceneId == toSceneId);

        public bool HasTransition(string fromSceneId, string toSceneId) =>
            transitions.Exists(t => t.fromSceneId == fromSceneId && t.toSceneId == toSceneId);

        #endregion

        #region Scene Management

        public void AddScene(SceneNode scene)
        {
            if (!scenes.Contains(scene))
            {
                scenes.Add(scene);
            }
        }

        /// <summary>
        /// Remove a scene from the graph (also removes all transitions involving it).
        /// </summary>
        public void RemoveScene(string sceneId)
        {
            scenes.RemoveAll(s => s.id == sceneId);
            transitions.RemoveAll(t => t.fromSceneId == sceneId || t.toSceneId == sceneId);
        }

        #endregion

        #region Transition Management

        public void AddTransition(SceneTransition transition)
        {
            if (!transitions.Contains(transition))
            {
                transitions.Add(transition);
            }
        }

        public void RemoveTransition(SceneTransition transition) => transitions.Remove(transition);

        #endregion
    }

    /// <summary>
    /// Represents a scene node in the scene flow graph.
    /// </summary>
    [Serializable]
    public class SceneNode
    {
        [Tooltip("Unique identifier for this scene in the graph.")]
        public string id;

#if UNITY_EDITOR
        [Tooltip("The Unity scene asset. Drag a scene here and names will auto-populate.")]
        public UnityEditor.SceneAsset sceneAsset;
#endif

        [Tooltip("The Unity scene name or path to load.")]
        public string sceneName;

        [Tooltip("Display name for UI and editor.")]
        public string displayName;

        [Header("Scene Type")]
        [Tooltip("Is this a hub scene that persists and can be returned to multiple times?")]
        public bool isHub = false;

        [Tooltip("Should this scene stay loaded in the background when leaving?")]
        public bool persistWhenLeaving = false;

        [Header("Chapter Information")]
        [Tooltip("Does this scene belong to a specific chapter?")]
        public bool SpecificChapter = false;

        [Tooltip("The name of the chapter this scene belongs to.")]
        public string ChapterName = "";

        [Tooltip("The number of the chapter this scene belongs to.")]
        public int ChapterNumber = 0;

        [Header("Visual Editor Data")]
        [Tooltip("Position in the visual graph editor (for custom editor window).")]
        public Vector2 editorPosition;

        [TextArea(2, 4)]
        [Tooltip("Notes about this scene for designers/developers.")]
        public string notes;
        public bool TimePasses;
        public Month MonthForThisScene = Month.January;
        public int DayForThisScene = 1;
        public bool HasYear;
        public int YearForThisScene = 1;

        public override string ToString() => $"{displayName} ({sceneName})";
    }

    /// <summary>
    /// Represents a transition/connection between two scenes in the graph.
    /// </summary>
    [Serializable]
    public class SceneTransition
    {
        [Tooltip("ID of the scene this transition starts from.")]
        public string fromSceneId;

        [Tooltip("ID of the scene this transition goes to.")]
        public string toSceneId;

        [Tooltip("Display label for this transition (shown in UI buttons, etc.).")]
        public string label = "Continue";

        [Tooltip("Is this transition bidirectional? (Can go back and forth freely)")]
        public bool isBidirectional = false;

        [Tooltip("Conditions that must be met for this transition to be available.")]
        public List<SceneCondition> conditions = new();

        [Header("Transition Behavior")]
        [Tooltip("Unload the previous scene when transitioning?")]
        public bool unloadPreviousScene = true;

        [Tooltip("Is this a return to a previous scene in the navigation history?")]
        public bool isReturnTransition = false;

        [Tooltip(
            "The brain state to activate when the new scene loads. Leave empty to keep current state."
        )]
        public string targetBrainState;

        [Tooltip(
            "For bidirectional transitions: The brain state to activate when returning (going from 'to' back to 'from'). Leave empty to keep current state."
        )]
        public string targetBrainStateReverse;

        [TextArea(2, 3)]
        [Tooltip("Notes about when/why this transition should be used.")]
        public string notes;

        public bool AreConditionsMet(SceneFlowConditionEvaluator evaluator)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true; // No conditions means always available
            }

            foreach (var condition in conditions)
            {
                if (!evaluator.EvaluateCondition(condition))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            string arrow = isBidirectional ? "↔" : "→";
            return $"{fromSceneId} {arrow} {toSceneId} [{label}]";
        }
    }

    /// <summary>
    /// Defines a condition that must be met for a scene transition to be available.
    /// </summary>
    [Serializable]
    public class SceneCondition
    {
        public SceneConditionType conditionType;

        [Tooltip("The key/name of the condition (e.g., 'chapter_1_complete', 'has_key_item').")]
        public string conditionKey;

        [Tooltip("Expected boolean value (for boolean conditions).")]
        public bool expectedBoolValue = true;

        [Tooltip("Expected integer value (for int comparison conditions).")]
        public int expectedIntValue = 0;

        [Tooltip("Comparison operator for int conditions.")]
        public ComparisonOperator comparisonOperator = ComparisonOperator.GreaterThanOrEqual;

        [Tooltip("Expected string value (for string comparison conditions).")]
        public string expectedStringValue = "";

        public override string ToString()
        {
            return conditionType switch
            {
                SceneConditionType.BrainStateBool => $"State.{conditionKey} == {expectedBoolValue}",
                SceneConditionType.BrainStateInt =>
                    $"State.{conditionKey} {comparisonOperator} {expectedIntValue}",
                SceneConditionType.BrainStateString =>
                    $"State.{conditionKey} == '{expectedStringValue}'",
                SceneConditionType.CustomFlag => $"Flag.{conditionKey} == {expectedBoolValue}",
                SceneConditionType.Always => "Always",
                _ => "Unknown",
            };
        }
    }

    public enum SceneConditionType
    {
        Always,
        BrainStateBool,
        BrainStateInt,
        BrainStateString,
        CustomFlag,
    }

    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
    }

    /// <summary>
    /// Helper class to evaluate scene conditions.
    /// This will be implemented in SceneFlowBrain.
    /// </summary>
    public abstract class SceneFlowConditionEvaluator
    {
        public abstract bool EvaluateCondition(SceneCondition condition);
    }
}
