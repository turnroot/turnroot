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

        // Core bounded stats that should always exist
        var coreBoundedStats = new[]
        {
            BoundedStatType.Health,
            BoundedStatType.Level,
        };

        // Build list of bounded stats that should exist
        var requiredBoundedStats = new List<BoundedStatType>(coreBoundedStats);

        if (gameplaySettings.UseExperienceSublevels)
            requiredBoundedStats.Add(BoundedStatType.LevelExperience);

        if (gameplaySettings.UseExperienceAptitudes)
            requiredBoundedStats.Add(BoundedStatType.ClassExperience);

        // Get existing bounded stats
        var existingBoundedStats = new List<BoundedStatType>();
        for (int i = 0; i < boundedStatsProperty.arraySize; i++)
        {
            var element = boundedStatsProperty.GetArrayElementAtIndex(i);
            var statType = (BoundedStatType)
                element.FindPropertyRelative("StatType").enumValueIndex;
            existingBoundedStats.Add(statType);
        }

        // Remove bounded stats that shouldn't exist
        for (int i = boundedStatsProperty.arraySize - 1; i >= 0; i--)
        {
            var element = boundedStatsProperty.GetArrayElementAtIndex(i);
            var statType = (BoundedStatType)
                element.FindPropertyRelative("StatType").enumValueIndex;

            if (!requiredBoundedStats.Contains(statType))
            {
                boundedStatsProperty.DeleteArrayElementAtIndex(i);
                Debug.Log($"Removed bounded stat: {statType}");
            }
        }

        // Add missing bounded stats
        foreach (var requiredStat in requiredBoundedStats)
        {
            if (!existingBoundedStats.Contains(requiredStat))
            {
                boundedStatsProperty.arraySize++;
                var newElement = boundedStatsProperty.GetArrayElementAtIndex(
                    boundedStatsProperty.arraySize - 1
                );
                newElement.FindPropertyRelative("StatType").enumValueIndex = (int)requiredStat;
                var (max, current, min) = GetDefaultValuesForBoundedStat(requiredStat);
                newElement.FindPropertyRelative("Max").floatValue = max;
                newElement.FindPropertyRelative("Current").floatValue = current;
                newElement.FindPropertyRelative("Min").floatValue = min;
                Debug.Log($"Added bounded stat: {requiredStat}");
            }
        }

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
                Debug.Log($"Removed unbounded stat: {statType}");
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
                Debug.Log($"Added unbounded stat: {requiredStat}");
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static (float max, float current, float min) GetDefaultValuesForBoundedStat(
        BoundedStatType statType
    )
    {
        // Return sensible defaults for different bounded stat types
        return statType switch
        {
            BoundedStatType.Health => (100f, 100f, 0f),
            BoundedStatType.Level => (99f, 1f, 1f),
            BoundedStatType.LevelExperience => (100f, 0f, 0f),
            BoundedStatType.ClassExperience => (100f, 0f, 0f),
            _ => (100f, 100f, 0f),
        };
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
