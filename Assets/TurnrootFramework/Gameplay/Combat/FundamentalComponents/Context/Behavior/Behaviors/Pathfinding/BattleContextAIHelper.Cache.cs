namespace Turnroot.Gameplay.Combat.FundamentalComponents.Battles
{
    public partial class BattleContextAIHelper
    {
        #region Cache Management
        public void ClearReusableTileDictionaries()
        {
            _reusableMoveTiles.Clear();
            _reusableAttackTiles.Clear();
        }

        public void InvalidateAllCaches()
        {
            ClearReusableTileDictionaries();
            _context?.InvalidateAllPathfindingParameters();
        }
        #endregion
    }
}