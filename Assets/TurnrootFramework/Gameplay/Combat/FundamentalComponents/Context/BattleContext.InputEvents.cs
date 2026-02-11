namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContext
    {
        public class BattleInputNavigateEvent
        {
            public UnityEngine.Vector2 Direction { get; set; }
        }

        public class BattleInputConfirmEvent { }

        public class BattleInputCancelEvent { }

        public class BattleInputMenuEvent { }
    }
}
