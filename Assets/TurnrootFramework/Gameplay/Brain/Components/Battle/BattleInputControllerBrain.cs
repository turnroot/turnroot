using Turnroot.Characters;
using Turnroot.Gameplay.Brain.Events;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public class BattleInputControllerBrain : BrainComponent
    {
        [HideInInspector]
        public MapGridPoint CursorPosition;

        [HideInInspector]
        public MapGridPoint PotentialCursorPosition;

        [HideInInspector]
        public CharacterInstance SelectedUnit =>
            _brain.battleBrain.BattleObject.Context.Unit.UnitInstance;

        [HideInInspector]
        public BattleContext BattleContext => _brain.battleBrain.BattleObject.Context;

        protected override EventPriority GetSubscriptionPriority() => EventPriority.High;

        protected override void SubscribeToBrainEvents() { }

        protected override void UnsubscribeFromBrainEvents() { }

        protected override void Awake()
        {
            base.Awake();
        }

        public void MoveCursorToPoint(MapGridPoint point) { }

        public void ConfirmTileSelection() { }

        public void ChangeSelectedUnit(CharacterInstance unit) { }

        public void OpenActionMenu() { }

        public void RequestUndo() { }

        public void OpenMenu() { }

        public void HandleNavigateInput(Vector2 direction) { }

        public void HandleConfirmInput() { }

        public void HandleCancelInput() { }
    }
}
