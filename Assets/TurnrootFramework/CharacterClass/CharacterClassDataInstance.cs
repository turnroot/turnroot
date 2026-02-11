using System;
using System.Collections.Generic;
using Turnroot.Serialization;
using Turnroot.Skills;
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

        [SerializeField]
        private CharacterData _characterData;

        [SerializeField]
        private CharacterClassData _classData;

        [SerializeField]
        private SkinnedMeshRenderer _meshRenderer;

        [SerializeField]
        private bool _isFirstTimeEquipped = true;

        [SerializeField]
        private int _battlesCompleted = 0;

        [SerializeField]
        private int _levelWhenEquipped = 1;

        [SerializeField]
        private List<Skill> _masteredSkills = new();

        private bool _disposed = false;

        #endregion

        #region Properties

        public CharacterData CharacterData => _characterData;
        public CharacterClassData ClassData => _classData;
        public SkinnedMeshRenderer MeshRenderer => _meshRenderer;
        public bool IsFirstTimeEquipped => _isFirstTimeEquipped;
        public int BattlesCompleted => _battlesCompleted;
        public int LevelWhenEquipped => _levelWhenEquipped;

        #endregion

        #region Initialization
        public CharacterClassDataInstance(
            CharacterData characterData,
            CharacterClassData classData,
            SkinnedMeshRenderer meshRenderer = null,
            bool isFirstTimeEquipped = true
        )
        {
            _characterData = characterData;
            _classData = classData;
            _meshRenderer = meshRenderer;
            _isFirstTimeEquipped = isFirstTimeEquipped;
            _battlesCompleted = 0;
            _levelWhenEquipped = characterData?.Level ?? 1;
            _masteredSkills = new List<Skill>();
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

            var mat = _meshRenderer.material;

            var identity = _classData.Identity;
            mat.SetTexture("_Base", identity.Base);
            mat.SetTexture("_MSE", identity.MSE);
            mat.SetTexture("_Tint_Mask", identity.TintMask);

            return true;
        }

        public void OnAfterDeserialize()
        {
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
            return OperationResult.Successful();
        }

        #endregion

        #region Stat Application

        /// <summary>
        /// Apply class stat bonuses to a character instance.
        /// These are persistent bonuses while the class is equipped.
        /// </summary>
        public OperationResult ApplyClassBonuses(CharacterInstance character)
        {
            var _res_applyBonuses = StatApplicationHelper.ValidateReferences(
                character,
                _classData,
                "CharacterClassDataInstance.ApplyClassBonuses"
            );
            if (!_res_applyBonuses.Success)
            {
                return _res_applyBonuses;
            }

            StatApplicationHelper.ApplyBoundedBonuses(_classData.Stats.StatBonuses, character);
            StatApplicationHelper.ApplyUnboundedBonuses(
                _classData.Stats.UnboundedStatBonuses,
                character
            );
            return OperationResult.Successful();
        }

        /// <summary>
        /// Remove class stat bonuses from a character instance.
        /// Call when changing classes to remove old class bonuses.
        /// </summary>
        public OperationResult RemoveClassBonuses(CharacterInstance character)
        {
            var _res_removeBonuses = StatApplicationHelper.ValidateReferences(
                character,
                _classData,
                "CharacterClassDataInstance.RemoveClassBonuses"
            );
            if (!_res_removeBonuses.Success)
            {
                return _res_removeBonuses;
            }

            StatApplicationHelper.RemoveBoundedBonuses(_classData.Stats.StatBonuses, character);
            StatApplicationHelper.RemoveUnboundedBonuses(
                _classData.Stats.UnboundedStatBonuses,
                character
            );
            return OperationResult.Successful();
        }

        /// <summary>
        /// Apply one-time class change bonuses (permanent stat increases).
        /// Only applied the first time a character equips this class.
        /// </summary>
        public OperationResult ApplyClassChangeBonuses(CharacterInstance character)
        {
            if (!_isFirstTimeEquipped)
            {
                return OperationResult.Successful();
            }

            var _res_applyChange = StatApplicationHelper.ValidateReferences(
                character,
                _classData,
                "CharacterClassDataInstance.ApplyClassChangeBonuses"
            );
            if (!_res_applyChange.Success)
            {
                return _res_applyChange;
            }

            StatApplicationHelper.ApplyBoundedPermanentBonuses(
                _classData.Stats.ClassChangeBonuses,
                character,
                logChanges: true
            );
            StatApplicationHelper.ApplyUnboundedPermanentBonuses(
                _classData.Stats.UnboundedClassChangeBonuses,
                character,
                logChanges: true
            );

            _isFirstTimeEquipped = false;
            return OperationResult.Successful();
        }

        public void EnforceStatMinimums(CharacterInstance character)
        {
            var _res_enforce = StatApplicationHelper.ValidateReferences(
                character,
                _classData,
                "CharacterClassDataInstance.EnforceStatMinimums"
            );
            if (!_res_enforce.Success)
            {
                return;
            }

            StatApplicationHelper.EnforceBoundedMinimums(
                _classData.Stats.StatMinimums,
                character,
                logChanges: true
            );
            StatApplicationHelper.EnforceUnboundedMinimums(
                _classData.Stats.UnboundedStatMinimums,
                character,
                logChanges: true
            );
        }

        public void ApplyStatCaps(CharacterInstance character)
        {
            var _res_caps = StatApplicationHelper.ValidateReferences(
                character,
                _classData,
                "CharacterClassDataInstance.ApplyStatCaps"
            );
            if (!_res_caps.Success)
            {
                return;
            }

            StatApplicationHelper.ApplyBoundedCaps(_classData.Stats.StatCaps, character);
        }

        public bool IsAboveCaps(CharacterInstance character)
        {
            var _res_isAbove = StatApplicationHelper.ValidateReferences(character, _classData, "");
            return _res_isAbove.Success
                && StatApplicationHelper.IsAboveUnboundedCaps(
                    _classData.Stats.UnboundedStatCaps,
                    character
                );
        }

        #endregion

        #region Mastery Tracking
        public void IncrementBattleCount() => _battlesCompleted++;

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
