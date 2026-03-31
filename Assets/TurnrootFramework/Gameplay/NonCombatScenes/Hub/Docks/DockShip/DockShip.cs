using NaughtyAttributes;
using Turnroot.Gameplay.NonCombatScenes.Hub.Abstract;
using Turnroot.Gameplay.NonCombatScenes.Hub.Shop;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub.Docks
{
    public partial class DockShip : HubVendor
    {
        #region Constants & Types
        public DockShipShopType CurrentDockShipShopType = DockShipShopType.Normal;

        private const string LtmKeyPrefix = "DockShipState_";

        [System.Serializable]
        private class DockShipState
        {
            public bool IsAtSea;
            public int CurrentAtSeaTime;
            public int CurrentDockedTime;
            public int DaysToStayAtSea;
            public float Trust;
            public GameDate LastVisitedDate;
        }

        #endregion

        #region Inspector Fields

        [Header("Basic Info")]
        public string ShipName;

        public enum DockSide
        {
            Left,
            Right,
        }

        public DockSide Side = DockSide.Left;

        [Header("Smuggling")]
        [InfoBox(
            "Smugglers use the trust system and will gamble for items. If you're not using smugglers, you can ignore this"
        )]
        public bool IsSmuggler;

        [
            Range(0f, 100f),
            InfoBox("The higher trust is, the better goods will be available on a smuggler ship")
        ]
        [ShowIf("IsSmuggler")]
        public float Trust;

        [InfoBox("Smuggled goods are rare, expensive, and can be gambled for")]
        [ShowIf("IsSmuggler")]
        public SmuggledItem[] SmuggledGoodsForSale;

        [InfoBox(
            "The 3D model of the ship that will appear in the dock. It will be disabled when the ship is at sea."
        )]
        public GameObject Ship;

        [InfoBox("If true, this ship will always be docked and never go to sea")]
        public bool AlwaysDocked = false;

        [HideInInspector]
        public bool IsDocked = true;

        [InfoBox(
            "The ship will be unavailable while at sea. The length of the sea voyage is random between the min and max."
        )]
        [HideIf("AlwaysDocked")]
        public int MinimumAtSeaTime = 8;

        [HideIf("AlwaysDocked")]
        public int MaximumAtSeaTime = 16;

        [InfoBox("When this ship is not at sea, how long will it be visitable?")]
        [HideIf("AlwaysDocked")]
        public int DaysDockedAtATime = 3;

        #endregion

        #region Runtime State

        private int _currentAtSeaTime = 0;
        private int _currentDockedTime = 0;
        private int _daysToStayAtSea = 0;
        private bool _isAtSea = false;
        private GameDate _lastVisitedDate = GameDate.Default;

        public int CurrentDockedTime => _currentDockedTime;
        public int CurrentAtSeaTime => _currentAtSeaTime;

        public ShopItem[] NormalGoodsForSale;

        private Brain.Brain _brain;

        #endregion

        #region Unity Callbacks

        private void Start()
        {
            _brain = FindFirstObjectByType<Brain.Brain>();
            LoadState();

            // Ensure ships marked as always docked never end up at sea due to stale save data
            if (AlwaysDocked)
            {
                EnforceAlwaysDockedState();
                SaveState();
            }
        }

        private void EnforceAlwaysDockedState()
        {
            _isAtSea = false;
            IsDocked = true;
            _currentAtSeaTime = 0;
            _currentDockedTime = 0;
            _daysToStayAtSea = 0;
        }

        protected override void OnDestroy()
        {
            // During application shutdown, other systems may already be destroyed.
            // Avoid noisy failure logs when brain/LTM is unavailable in late teardown.
            if (!Application.isPlaying)
            {
                return;
            }

            if (_brain == null)
            {
                _brain = FindFirstObjectByType<Brain.Brain>();
            }

            if (_brain?.ltm == null)
            {
                return;
            }

            SaveState();
        }

        #endregion

        #region Persistence

        private OperationResult LoadState()
        {
            if (_brain?.ltm == null || string.IsNullOrEmpty(ShipName))
            {
                return OperationResult.Failure(
                    $"Cannot load state for {ShipName}. Brain or LTM is null, or ShipName is empty.",
                    this.GetType().Name
                );
            }

            string key = LtmKeyPrefix + ShipName;
            string json = _brain.ltm.Recall(key);
            if (string.IsNullOrEmpty(json))
            {
                return OperationResult.Successful();
            }

            var state = JsonUtility.FromJson<DockShipState>(json);
            if (state == null)
            {
                $"{ShipName} has an invalid saved state in LTM with key {key}. Using default state.".LogWarning();
                return OperationResult.Successful();
            }

            _isAtSea = state.IsAtSea;
            _currentAtSeaTime = state.CurrentAtSeaTime;
            _currentDockedTime = state.CurrentDockedTime;
            _daysToStayAtSea = state.DaysToStayAtSea;
            Trust = state.Trust;
            _lastVisitedDate = state.LastVisitedDate;
            IsDocked = !_isAtSea;

            if (AlwaysDocked)
            {
                EnforceAlwaysDockedState();
            }

            if (Ship != null)
            {
                Ship.SetActive(IsDocked);
            }

            return OperationResult.Successful();
        }

        private OperationResult SaveState()
        {
            if (string.IsNullOrEmpty(ShipName))
            {
                return OperationResult.Failure(
                    $"Cannot save state for {ShipName}. ShipName is empty.",
                    this.GetType().Name
                );
            }

            if (AlwaysDocked)
            {
                EnforceAlwaysDockedState();
            }

            if (_brain == null)
            {
                _brain = FindFirstObjectByType<Brain.Brain>();
            }

            if (_brain?.ltm == null)
            {
                return OperationResult.Failure(
                    $"Cannot save state for {ShipName}. Brain or LTM is null.",
                    this.GetType().Name
                );
            }

            var state = new DockShipState
            {
                IsAtSea = _isAtSea,
                CurrentAtSeaTime = _currentAtSeaTime,
                CurrentDockedTime = _currentDockedTime,
                DaysToStayAtSea = _daysToStayAtSea,
                Trust = Trust,
                LastVisitedDate = _lastVisitedDate,
            };

            string key = LtmKeyPrefix + ShipName;
            _brain.ltm.Remember(key, JsonUtility.ToJson(state));
            return OperationResult.Successful();
        }

        #endregion

        #region Public API

        public void IncreaseTrust(float amount)
        {
            Trust = Mathf.Clamp(Trust + amount, 0f, 100f);
            SaveState();
        }

        public void DecreaseTrust(float amount)
        {
            Trust = Mathf.Clamp(Trust - amount, 0f, 100f);
            SaveState();
        }

        public void SetDockedState(bool docked)
        {
            // Keep internal state consistent (this may be invoked from Dock when managing ship docking round-robin)
            _isAtSea = !docked;
            IsDocked = docked;

            if (docked)
            {
                _currentDockedTime = 0;
                _currentAtSeaTime = 0;
            }

            if (Ship != null)
            {
                Ship.SetActive(docked);
            }

            SaveState();
        }

        public void ForceSendToSea()
        {
            if (!IsDocked)
            {
                return;
            }

            if (AlwaysDocked)
            {
                $"Dock: Tried to send '{ShipName}' to sea but it is marked AlwaysDocked. Skipping.".LogWarning();
                return;
            }

            _isAtSea = true;
            IsDocked = false;
            _currentAtSeaTime = 0;
            _daysToStayAtSea = HubDayRandom.Range(MinimumAtSeaTime, MaximumAtSeaTime + 1);

            $"Dock: '{ShipName}' sent to sea (at sea for {_daysToStayAtSea} days).".LogInfo();

            if (Ship != null)
            {
                Ship.SetActive(false);
            }

            SaveState();
        }

        public void CheckIsDockedAndUpdateVoyageStatusByOneDay()
        {
            if (AlwaysDocked)
            {
                IsDocked = true;
                return;
            }

            bool stateChanged = false;

            if (_isAtSea)
            {
                _currentAtSeaTime++;
                stateChanged = true;

                // Safety: if we somehow have no duration set, assign one now
                if (_daysToStayAtSea == 0)
                {
                    _daysToStayAtSea = HubDayRandom.Range(MinimumAtSeaTime, MaximumAtSeaTime + 1);
                    stateChanged = true;
                }

                if (_currentAtSeaTime >= _daysToStayAtSea)
                {
                    int days = _daysToStayAtSea;
                    _isAtSea = false;
                    IsDocked = true;
                    _currentAtSeaTime = 0;
                    _daysToStayAtSea = 0;
                    stateChanged = true;
                }
            }
            else
            {
                _currentDockedTime++;
                stateChanged = true;

                if (_currentDockedTime >= DaysDockedAtATime)
                {
                    _isAtSea = true;
                    IsDocked = false;
                    _currentDockedTime = 0;
                    _currentAtSeaTime = 0;
                    _daysToStayAtSea = HubDayRandom.Range(MinimumAtSeaTime, MaximumAtSeaTime + 1);
                    stateChanged = true;
                }
            }

            if (stateChanged)
            {
                if (Ship != null)
                {
                    Ship.SetActive(IsDocked);
                }
                SaveState();
            }
        }

        public void RefreshShipForNewDay(GameDate currentDay)
        {
            InitializeStockIfNeeded(currentDay);

            bool isDailyUpdateAlreadyProcessed = HubDayStateStore.HasProcessedDailyUpdates;

            if (NormalGoodsForSale != null)
            {
                for (int i = 0; i < NormalGoodsForSale.Length; i++)
                {
                    var item = NormalGoodsForSale[i];
                    if (!isDailyUpdateAlreadyProcessed)
                    {
                        item.Refresh(currentDay);
                    }

                    if (_brain != null && item.Item != null)
                    {
                        HubDayStateStore.SetShopItemQuantity(
                            _brain,
                            ShipName,
                            item.Item.name,
                            item.CurrentStatus.AvailableQuantity
                        );
                    }

                    NormalGoodsForSale[i] = item;
                }
            }

            if (SmuggledGoodsForSale != null)
            {
                for (int i = 0; i < SmuggledGoodsForSale.Length; i++)
                {
                    // Refresh via index so struct mutations write back to the array.
                    // Item.Refresh mutates Item.CurrentStatus on the struct copy;
                    // assigning back ensures the stored value stays up to date.
                    var smuggled = SmuggledGoodsForSale[i];
                    smuggled.Item.Refresh(currentDay);
                    SmuggledGoodsForSale[i] = smuggled;
                }
            }
        }

        /// <summary>
        /// Initializes <see cref="NormalGoodsForSale"/> quantities for the first time this ship
        /// is encountered (no persisted stock), or restores them from <see cref="HubDayStateStore"/>
        /// on subsequent visits. Mirrors the pattern used in <see cref="Shop.RefreshShopForNewDay"/>.
        /// </summary>
        private void InitializeStockIfNeeded(GameDate currentDay)
        {
            if (NormalGoodsForSale == null || NormalGoodsForSale.Length == 0)
            {
                return;
            }

            bool hasPersistedStock = HubDayStateStore.HasShopStock(ShipName);

            for (int i = 0; i < NormalGoodsForSale.Length; i++)
            {
                var item = NormalGoodsForSale[i];
                if (item.Item == null)
                {
                    continue;
                }

                if (!hasPersistedStock)
                {
                    // First time: start at max quantity (skip rare items — they start at 0).
                    if (!item.RareItem)
                    {
                        item.CurrentStatus.AvailableQuantity = item.MaxQuantity;
                        item.Initialize(currentDay);

                        if (_brain != null)
                        {
                            HubDayStateStore.SetShopItemQuantity(
                                _brain,
                                ShipName,
                                item.Item.name,
                                item.MaxQuantity
                            );
                        }
                    }
                }
                else
                {
                    // Restore persisted quantity so purchases survive cross-session.
                    int persisted = HubDayStateStore.GetShopItemQuantity(
                        ShipName,
                        item.Item.name,
                        item.MaxQuantity
                    );
                    item.CurrentStatus.AvailableQuantity = persisted;
                }

                NormalGoodsForSale[i] = item;
            }
        }

        #endregion
    }
}
