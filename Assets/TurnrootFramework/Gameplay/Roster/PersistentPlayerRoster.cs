using Turnroot.Characters;
using UnityEngine;

namespace Turnroot.Gameplay.Roster
{
    [CreateAssetMenu(
        fileName = "PersistentPlayerRoster",
        menuName = "Turnroot/Gameplay/Roster/Game Player Team Roster"
    )]
    public class PersistentPlayerRoster : SingletonScriptableObject<PersistentPlayerRoster>
    {
        public PlayerTeamRoster PlayerRoster;
    }
}
