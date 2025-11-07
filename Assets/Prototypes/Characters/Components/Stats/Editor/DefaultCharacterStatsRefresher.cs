#if UNITY_EDITOR
using Assets.Prototypes.Characters;
using Assets.Prototypes.Characters.Stats;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class DefaultCharacterStatsRefresher
{
    [MenuItem("Turnroot/Tools/Refresh Unit Stats")]
    public static void RefreshFromMenu()
    {
        var gameplaySettings = GameplayGeneralSettings.Instance;
        var defaultStats = DefaultCharacterStats.Instance;

        if (gameplaySettings == null)
        {
            Debug.LogError("GameplayGeneralSettings not found!");
            return;
        }

        if (defaultStats == null)
        {
            Debug.LogError("DefaultCharacterStats not found!");
            return;
        }

        RefreshStats(defaultStats, gameplaySettings);
        EditorUtility.SetDirty(defaultStats);
        AssetDatabase.SaveAssets();

        Debug.Log("Unit stats refreshed successfully!");
    }

    public static void RefreshStats(
        DefaultCharacterStats defaultStats,
        GameplayGeneralSettings gameplaySettings
    )
    {
        // Get the serialized object to access private fields
        var serializedObject = new SerializedObject(defaultStats);
        var boundedStatsProperty = serializedObject.FindProperty("_defaultBoundedStats");
        var unboundedStatsProperty = serializedObject.FindProperty("_defaultUnboundedStats");

        // Core stats that should always exist
        var coreUnboundedStats = new[]
        {
            UnboundedStatType.Strength,
            UnboundedStatType.Defense,
            UnboundedStatType.Magic,
            UnboundedStatType.Resistance,
            UnboundedStatType.Skill,
            UnboundedStatType.Speed,
            UnboundedStatType.Dexterity,
            UnboundedStatType.Charm,
            UnboundedStatType.Movement,
            UnboundedStatType.Endurance,
        };

        // Build list of unbounded stats that should exist
        var requiredUnboundedStats = new List<UnboundedStatType>(coreUnboundedStats);

        if (gameplaySettings.UseLuck)
            requiredUnboundedStats.Add(UnboundedStatType.Luck);

        if (gameplaySettings.UseSeparateCriticalAvoidance)
            requiredUnboundedStats.Add(UnboundedStatType.CriticalAvoidance);

        if (gameplaySettings.UseAuthority)
            requiredUnboundedStats.Add(UnboundedStatType.Authority);

        // Get existing stats
        var existingUnboundedStats = new List<UnboundedStatType>();
        for (int i = 0; i < unboundedStatsProperty.arraySize; i++)
        {
            var element = unboundedStatsProperty.GetArrayElementAtIndex(i);
            var statType = (UnboundedStatType)
                element.FindPropertyRelative("StatType").enumValueIndex;
            existingUnboundedStats.Add(statType);
        }

        // Remove stats that shouldn't exist
        for (int i = unboundedStatsProperty.arraySize - 1; i >= 0; i--)
        {
            var element = unboundedStatsProperty.GetArrayElementAtIndex(i);
            var statType = (UnboundedStatType)
                element.FindPropertyRelative("StatType").enumValueIndex;

            if (!requiredUnboundedStats.Contains(statType))
            {
                unboundedStatsProperty.DeleteArrayElementAtIndex(i);
                Debug.Log($"Removed stat: {statType}");
            }
        }

        // Add missing stats
        foreach (var requiredStat in requiredUnboundedStats)
        {
            if (!existingUnboundedStats.Contains(requiredStat))
            {
                unboundedStatsProperty.arraySize++;
                var newElement = unboundedStatsProperty.GetArrayElementAtIndex(
                    unboundedStatsProperty.arraySize - 1
                );
                newElement.FindPropertyRelative("StatType").enumValueIndex = (int)requiredStat;
                newElement.FindPropertyRelative("Current").floatValue = GetDefaultValueForStat(
                    requiredStat
                );
                Debug.Log($"Added stat: {requiredStat}");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static float GetDefaultValueForStat(UnboundedStatType statType)
    {
        // Return sensible defaults for different stat types
        return statType switch
        {
            UnboundedStatType.Luck => 5f,
            UnboundedStatType.CriticalAvoidance => 0f,
            UnboundedStatType.Authority => 5f,
            _ => 10f,
        };
    }
}
#endif
