using System.Collections.Generic;
using Turnroot.Characters.CharacterClass;
using Turnroot.Characters.Components.Support;
using Turnroot.Characters.Stats;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters
{
    public partial class CharacterData : ScriptableObject, IHasStats
    {
        public void InvalidatePortraitArrayCache() => _portraitArrayCache = null;

        public void SaveDefaults()
        {
            TaggedLayerDefaults.Clear();
            if (Portraits != null)
            {
                CharacterHelpers.ForEachPortraitLayer(
                    Portraits,
                    layer =>
                    {
                        if (!string.IsNullOrEmpty(layer.Tag))
                        {
                            TaggedLayerDefaults[layer.Tag] = new TaggedLayerDefault
                            {
                                Tag = layer.Tag,
                                Sprite = layer.Sprite,
                                Offset = layer.Offset,
                                Scale = layer.Scale,
                                Tint = layer.Tint,
                            };
                        }
                    }
                );
            }
        }

        public void LoadDefaults()
        {
            if (Portraits != null)
            {
                CharacterHelpers.ForEachPortraitLayer(
                    Portraits,
                    layer =>
                    {
                        if (
                            !string.IsNullOrEmpty(layer.Tag)
                            && TaggedLayerDefaults.TryGetValue(layer.Tag, out var def)
                        )
                        {
                            layer.Sprite = def.Sprite;
                            layer.Offset = def.Offset;
                            layer.Scale = def.Scale;
                            layer.Tint = def.Tint;
                        }
                    }
                );
            }
            InvalidatePortraitArrayCache();
        }

        // Helper methods to get stats by type
        public BoundedCharacterStat GetBoundedStat(BoundedStatType type) =>
            StatHelpers.GetBoundedStat(BoundedStats, type);

        public CharacterStat GetUnboundedStat(UnboundedStatType type) =>
            StatHelpers.GetUnboundedStat(UnboundedStats, type);

        public ExperienceRank GetExperienceRank(string experienceTypeId) =>
            ExperienceRanks?.Find(e => e.ExperienceTypeId == experienceTypeId);

        /* ----------------------------- Core Functions ----------------------------- */

        private void OnEnable()
        {
            // Load settings from centralized cache
            var settings = CharacterSettings.PrototypeSettings;
            if (settings == null)
            {
                return;
            }

            // Initialize stats from defaults if stats are empty
            if (BoundedStats.Count == 0 && UnboundedStats.Count == 0)
            {
                var defaultStats = CharacterSettings.DefaultStats;
                if (defaultStats != null)
                {
                    BoundedStats = defaultStats.CreateBoundedStats();
                    UnboundedStats = defaultStats.CreateUnboundedStats();
                }
            }

            // Initialize personal growth rates if empty (set all unbounded stats to 0% growth)
            if (PersonalGrowthRates.Count == 0 && UnboundedStats.Count > 0)
            {
                foreach (var stat in UnboundedStats)
                {
                    PersonalGrowthRates.Add(new UnboundedStatModifier(stat.StatType, 0f));
                }
            }
        }

        private void OnValidate()
        {
            // Reset cached portrait array so changes in the inspector are reflected
            _portraitArrayCache = null;
            // Ensure that the character's name is not empty
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                DisplayName = "New Unit";
            }

            // Ensure that the full name is not empty
            if (string.IsNullOrWhiteSpace(FullName))
            {
                FullName = DisplayName;
            }

            // Validate support relationships - remove any that reference this character
            if (SupportRelationships != null)
            {
                var removed = SupportRelationship.SanitizeForCharacter(this, SupportRelationships);
                foreach (var r in removed)
                {
                    TurnrootLogger.Log(
                        $"Removed invalid support relationship: {name} cannot have a support relationship with themselves ({r.Character?.name})",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }

            // Editor-time validation for rigging properties
            if (HasExtraBoneLayer)
            {
                if (AdditionalBonesMask == null)
                {
                    TurnrootLogger.Log(
                        $"{name}: 'HasExtraBoneLayer' is true but 'AdditionalBonesMask' is not set. This may cause Animator layering misconfiguration.",
                        TurnrootLogger.LogLevel.Warning
                    );
                }

                if (
                    (AdditionalBoneNames == null || AdditionalBoneNames.Length == 0)
                    && AdditionalBonesMask == null
                )
                {
                    TurnrootLogger.Log(
                        $"{name}: No additional bone names or AvatarMask were provided for the extra bone layer. Add names or an AvatarMask for tooling/runtime mapping.",
                        TurnrootLogger.LogLevel.Warning
                    );
                }
            }

#if UNITY_EDITOR
            // Validate that assigned skinned meshes contain required blendshapes; if not, clear the assignment and log an error.
            ValidateRendererBlendshapes(CharacterDefaultModel, nameof(CharacterDefaultModel));
            ValidateRendererBlendshapes(
                CharacterHeadHandsAndHair,
                nameof(CharacterHeadHandsAndHair)
            );
#endif
        }

#if UNITY_EDITOR
        private void ValidateRendererBlendshapes(SkinnedMeshRenderer renderer, string propertyName)
        {
            if (renderer == null)
            {
                return;
            }

            var mesh = renderer.sharedMesh;
            if (mesh == null)
            {
                Debug.LogError(
                    $"{name}: Assigned {propertyName} has no sharedMesh. Clearing assignment."
                );
                if (propertyName == nameof(CharacterDefaultModel))
                {
                    CharacterDefaultModel = null;
                }
                else if (propertyName == nameof(CharacterHeadHandsAndHair))
                {
                    CharacterHeadHandsAndHair = null;
                }
                return;
            }

            var required = Blendshapes.BlendshapeNames;
            var missing = new List<string>();
            if (required != null)
            {
                foreach (var name in required)
                {
                    if (mesh.GetBlendShapeIndex(name) < 0)
                    {
                        missing.Add(name);
                    }
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogError(
                    $"{name}: Assigned {propertyName} mesh '{mesh.name}' is missing blendshapes: {string.Join(", ", missing)}. Clearing assignment."
                );
                if (propertyName == nameof(CharacterDefaultModel))
                {
                    CharacterDefaultModel = null;
                }
                else if (propertyName == nameof(CharacterHeadHandsAndHair))
                {
                    CharacterHeadHandsAndHair = null;
                }
            }
        }
#endif
    }
}
