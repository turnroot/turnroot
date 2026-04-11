using System;
using Newtonsoft.Json;
using Turnroot.Serialization;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Characters.CharacterClass
{
    /// <summary>
    /// Runtime instance of a character class, handling visual representation and stat application.
    /// Manages material rendering and provides methods for applying class bonuses/modifiers to characters.
    /// </summary>
    [Serializable]
    public class CharacterClassDataInstance : IPostDeserialize, IDisposable
    {
        #region Fields

        [SerializeField, JsonProperty("_characterData")]
        private CharacterData _characterData;

        [SerializeField, JsonProperty("_classData")]
        private CharacterClassData _classData;

        [NonSerialized]
        private SkinnedMeshRenderer _meshRenderer;

        [SerializeField, JsonProperty("_stats")]
        private ClassStatsInstance _stats = new();

        [SerializeField, JsonProperty("_mastery")]
        private ClassMasteryInstance _mastery = new();

        private bool _disposed = false;

        #endregion

        #region Properties

        public CharacterData CharacterData => _characterData;
        public CharacterClassData ClassData => _classData;
        public SkinnedMeshRenderer MeshRenderer => _meshRenderer;
        public bool IsFirstTimeEquipped => _stats?.IsFirstTimeEquipped ?? true;
        public int BattlesCompleted => _mastery?.BattlesCompleted ?? 0;

        #endregion

        #region Initialization
        public CharacterClassDataInstance(
            CharacterInstance owner,
            CharacterClassData classData,
            SkinnedMeshRenderer meshRenderer = null,
            bool isFirstTimeEquipped = true
        )
        {
            _characterData = owner?.CharacterTemplate;
            _classData = classData;
            _meshRenderer = meshRenderer;

            _stats = new ClassStatsInstance(isFirstTimeEquipped);
            _mastery = new ClassMasteryInstance(owner, classData);
        }

        public CharacterClassDataInstance() { }

        public OperationResult Validate()
        {
            var checks = new (object obj, string name)[]
            {
                (_characterData, nameof(_characterData)),
                (_meshRenderer, nameof(_meshRenderer)),
                (_meshRenderer?.material, "meshRenderer.material"),
                (_classData, nameof(_classData)),
                (_classData.Identity, "classData.Identity"),
                (_classData.Identity.Base, "classData.Identity.Base"),
                (_classData.Identity.MSE, "classData.Identity.MSE"),
                (_classData.Identity.TintMask, "classData.Identity.TintMask"),
            };

            bool ok = ValidationHelper.ValidateNotNull(
                "CharacterClassDataInstance",
                out var missing,
                checks
            );

            if (!ok)
            {
                var msg =
                    $"{nameof(CharacterClassDataInstance)} validation failed: missing {string.Join(", ", missing)}";
                return OperationResult.Failure(msg);
            }

            return OperationResult.Successful();
        }

        public bool Initialize()
        {
            var validation = Validate();
            if (!validation.Success)
            {
                return false;
            }

            var identity = _classData.Identity;

            var mats = _meshRenderer.materials ?? new Material[0];
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!ValidationHelper.ValidateNotNull(m, nameof(m)))
                {
                    continue;
                }

                if (m.HasProperty("_Base") || m.HasProperty("_Tint_Mask") || m.HasProperty("_MSE"))
                {
                    if (identity.Base != null)
                    {
                        m.SetTexture("_Base", identity.Base);
                    }

                    if (identity.MSE != null)
                    {
                        m.SetTexture("_MSE", identity.MSE);
                    }

                    if (identity.TintMask != null)
                    {
                        m.SetTexture("_Tint_Mask", identity.TintMask);
                    }
                }
            }

            return true;
        }

        public void OnAfterDeserialize()
        {
            _mastery?.EnsureMasteryProgressInitialized(_classData);
            if (_characterData != null && _classData != null && _meshRenderer != null)
            {
                Initialize();
            }
        }

        public OperationResult InitializeWithRenderer(SkinnedMeshRenderer meshRenderer)
        {
            var validation = OperationResultGuards.RequireNotNull(
                meshRenderer,
                nameof(meshRenderer)
            );
            if (!validation.Success)
            {
                return validation;
            }

            _meshRenderer = meshRenderer;
            Initialize();
            _mastery?.EnsureMasteryProgressInitialized(_classData);
            return OperationResult.Successful();
        }

        #endregion

        #region Stat Application

        /// <summary>
        /// Apply class stat bonuses to a character instance.
        /// These are persistent bonuses while the class is equipped.
        /// </summary>
        public OperationResult ApplyClassBonuses(CharacterInstance character) =>
            _stats.ApplyClassBonuses(character, _classData);

        /// <summary>
        /// Remove class stat bonuses from a character instance.
        /// Call when changing classes to remove old class bonuses.
        /// </summary>
        public OperationResult RemoveClassBonuses(CharacterInstance character) =>
            _stats.RemoveClassBonuses(character, _classData);

        /// <summary>
        /// Apply one-time class change bonuses (permanent stat increases).
        /// Only applied the first time a character equips this class.
        /// </summary>
        public OperationResult ApplyClassChangeBonuses(CharacterInstance character) =>
            _stats.ApplyClassChangeBonuses(character, _classData);

        public void EnforceStatMinimums(CharacterInstance character) =>
            _stats.EnforceStatMinimums(character, _classData);

        public void ApplyStatCaps(CharacterInstance character) =>
            _stats.ApplyStatCaps(character, _classData);

        public bool IsAboveCaps(CharacterInstance character) =>
            _stats.IsAboveCaps(character, _classData);

        #endregion

        #region Mastery Tracking
        public void IncrementBattleCount(CharacterInstance owner = null, int points = 1) =>
            _mastery.IncrementBattleCount(owner, _classData, points);

        #endregion

        #region Material Management
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        #endregion
    }
}
