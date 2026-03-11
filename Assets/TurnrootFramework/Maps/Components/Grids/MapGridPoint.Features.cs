using System.Collections.Generic;
using Turnroot.Gameplay.Objects;
using UnityEngine;

namespace Turnroot.Gameplay.Maps
{
    public partial class MapGridPoint : MonoBehaviour
    {
        // ---------------------------------------------------------------------
        // Feature-specific state
        // ---------------------------------------------------------------------
        [SerializeField]
        [Tooltip("Feature display name (optional).")]
        private string _featureName = string.Empty;

        [SerializeField]
        private bool _featureLocked = false;

        [SerializeField]
        private ObjectItem _unlockItem = null;

        [SerializeField]
        private ObjectItem _featureCommonItem = null;

        [SerializeField]
        private ObjectItem _featureRareItem = null;

        [SerializeField]
        private List<Vector2Int> _warpDestinations = new();

        [SerializeField]
        private int _activeWarpIndex = 0;

        // shelter targeting restrictions
        [SerializeField]
        private bool _shelterNoFly = false;

        [SerializeField]
        private bool _shelterNoRide = false;

        [SerializeField]
        private bool _shelterNoInfantry = false;

        // breakable-specific state
        [SerializeField]
        private int _breakableHealth = 0;

        // healing-specific state
        [SerializeField]
        private float _healingPercentPerTurn = 0f;

        // ranged-specific state
        [SerializeField]
        private int _rangedRange = 0;

        [SerializeField]
        private int _rangedDamage = 0;

        [SerializeField]
        private float _rangedHit = 0f;

        [SerializeField]
        private bool _rangedAllowsRiding = false;

        [SerializeField]
        private bool _rangedAllowsFlying = false;

        [SerializeField]
        private bool _rangedMagicOnly = false;

        public bool FeatureLocked
        {
            get => _featureLocked;
            set
            {
                _featureLocked = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public ObjectItem UnlockItem
        {
            get => _unlockItem;
            set
            {
                _unlockItem = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        /// <summary>
        /// For treasure/underground features, the common item reward.
        /// </summary>
        public ObjectItem FeatureCommonItem
        {
            get => _featureCommonItem;
            set
            {
                _featureCommonItem = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        /// <summary>
        /// For treasure/underground features, the rare item reward.
        /// </summary>
        public ObjectItem FeatureRareItem
        {
            get => _featureRareItem;
            set
            {
                _featureRareItem = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        /// <summary>
        /// Destination coordinates for warp features.
        /// </summary>
        public List<Vector2Int> WarpDestinations => _warpDestinations;

        /// <summary>
        /// Index into <see cref="WarpDestinations"/> identifying the active exit.
        /// </summary>
        public int ActiveWarpIndex
        {
            get => _activeWarpIndex;
            set
            {
                _activeWarpIndex = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        // Miscellaneous feature state properties
        public int BreakableHealth
        {
            get => _breakableHealth;
            set
            {
                _breakableHealth = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public float HealingPercentPerTurn
        {
            get => _healingPercentPerTurn;
            set
            {
                _healingPercentPerTurn = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public int RangedRange
        {
            get => _rangedRange;
            set
            {
                _rangedRange = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public int RangedDamage
        {
            get => _rangedDamage;
            set
            {
                _rangedDamage = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public float RangedHit
        {
            get => _rangedHit;
            set
            {
                _rangedHit = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool RangedAllowsRiding
        {
            get => _rangedAllowsRiding;
            set
            {
                _rangedAllowsRiding = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool RangedAllowsFlying
        {
            get => _rangedAllowsFlying;
            set
            {
                _rangedAllowsFlying = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool RangedMagicOnly
        {
            get => _rangedMagicOnly;
            set
            {
                _rangedMagicOnly = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool ShelterNoFly
        {
            get => _shelterNoFly;
            set
            {
                _shelterNoFly = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool ShelterNoRide
        {
            get => _shelterNoRide;
            set
            {
                _shelterNoRide = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        public bool ShelterNoInfantry
        {
            get => _shelterNoInfantry;
            set
            {
                _shelterNoInfantry = value;
                ParentGrid?.IncrementStateVersion();
            }
        }

        // Properties related to feature identification
        [SerializeField]
        [Tooltip("Feature type")]
        private string _featureTypeId = string.Empty;

        public string FeatureTypeId => _featureTypeId;

        public string FeatureName
        {
            get => _featureName;
            set => _featureName = value ?? string.Empty;
        }

        public MapGridPointFeature.FeatureType FeatureType
        {
            get => MapGridPointFeature.TypeFromId(FeatureTypeId);
            set => _featureTypeId = MapGridPointFeature.IdFromType(value) ?? string.Empty;
        }

        public void SetFeatureTypeId(string id)
        {
            _featureTypeId = id ?? string.Empty;
            // if the feature type changes we clear any door-locked state, item rewards,
            // any warp destinations, and specialized values
            _featureLocked = false;
            _unlockItem = null;
            _featureCommonItem = null;
            _featureRareItem = null;
            _warpDestinations.Clear();
            _activeWarpIndex = 0;
            _breakableHealth = 0;
            _healingPercentPerTurn = 0f;
            _rangedRange = 0;
            _rangedDamage = 0;
            _rangedHit = 0f;
            _rangedAllowsRiding = false;
            _rangedAllowsFlying = false;
            _rangedMagicOnly = false;
            _shelterNoFly = false;
            _shelterNoRide = false;
            _shelterNoInfantry = false;
            ParentGrid?.IncrementStateVersion();
        }

        public void ApplyFeature(string selId, string name, bool singleClickToggle)
        {
            if (string.IsNullOrEmpty(selId))
            {
                return;
            }

            if (selId == "eraser")
            {
                ClearFeature();
                return;
            }

            if (singleClickToggle && FeatureTypeId == selId)
            {
                ClearFeature();
                return;
            }

            _featureTypeId = selId;
            _featureName = name ?? string.Empty;
            // clear per-feature state from previous feature
            _featureLocked = false;
            _unlockItem = null;
            _featureCommonItem = null;
            _featureRareItem = null;
            _warpDestinations.Clear();
            _activeWarpIndex = 0;
            _breakableHealth = 0;
            _healingPercentPerTurn = 0f;
            _rangedRange = 0;
            _rangedDamage = 0;
            _rangedHit = 0f;
            _rangedAllowsRiding = false;
            _rangedAllowsFlying = false;
            _rangedMagicOnly = false;
            _shelterNoFly = false;
            _shelterNoRide = false;
            _shelterNoInfantry = false;
            ParentGrid?.IncrementStateVersion();

#if UNITY_EDITOR
            MarkDirtySafely();
#endif
        }

        public void ClearFeature()
        {
            _featureTypeId = string.Empty;
            _featureName = string.Empty;
            _featureLocked = false;
            _featureCommonItem = null;
            _featureRareItem = null;
            _warpDestinations.Clear();
            _activeWarpIndex = 0;
            _breakableHealth = 0;
            _healingPercentPerTurn = 0f;
            _rangedRange = 0;
            _rangedDamage = 0;
            _rangedHit = 0f;
            _rangedAllowsRiding = false;
            _rangedAllowsFlying = false;
            _rangedMagicOnly = false;
            ParentGrid?.IncrementStateVersion();

#if UNITY_EDITOR
            MarkDirtySafely();
#endif
        }

#if UNITY_EDITOR
        private void MarkDirtySafely()
        {
            if (
                !UnityEditor.EditorApplication.isCompiling
                && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                && !UnityEditor.EditorApplication.isUpdating
            )
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.EditorUtility.SetDirty(this.gameObject);
                UnityEditor.SceneView.RepaintAll();
            }
        }
#endif
    }
}
