using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Environment;
using UnityEngine;

[RequireComponent(typeof(EnvironmentalConditions))]
public class BattleGameObject : MonoBehaviour
{
    [Header("Battle Components")]
    [SerializeField]
    private BattleContext _battleContext;

    [SerializeField, SerializeReference]
    private BattleCondition[] _battleConditions;

    [SerializeField]
    private MapGrid _mapGrid;
}
