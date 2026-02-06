using Turnroot.Characters;
using Turnroot.Utilities.AbstractScripts;
using UnityEngine;

namespace Turnroot.Gameplay.Roster
{
    /// <summary>
    /// Singleton ScriptableObject holding a reference to the persistent player team roster across scenes.
    /// </summary>
    [CreateAssetMenu(
        fileName = "PersistentPlayerRoster",
        menuName = "Turnroot/Gameplay/Roster/Game Player Team Roster"
    )]
    public class PersistentPlayerRoster : SingletonScriptableObject<PersistentPlayerRoster>
    {
        public PlayerTeamRoster PlayerRoster;
    }
}
