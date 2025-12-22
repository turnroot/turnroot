using System.Collections.Generic;
using System.Linq;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Utilities;
using UnityEngine;

public enum TurnOrder
{
    PlayerStart = 0,
    PlayerEnd = 1,
    EnemyStart = 2,
    EnemyEnd = 3,
    ThirdPartyStart = 4,
    ThirdPartyEnd = 5,
}

[RequireComponent(typeof(BattleBrain))]
public class TurnRotisserie : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private bool _hasThirdParty;

    [HideInInspector]
    public bool HasThirdParty
    {
        get => _hasThirdParty;
        set => _hasThirdParty = value;
    }

    // TODO: Determine who views third-party as an enemy

    private BattleBrain BattleBrain => GetComponent<BattleBrain>();
    private Brain _brain => BattleBrain.Brain;

    private TurnOrder _currentTurnOrder = TurnOrder.PlayerStart;
    private int _currentRosterIndex = 0;
    private bool _unitTookAnotherTurn = false;

    public TurnOrder GetNextTurnOrder()
    {
        switch (_currentTurnOrder)
        {
            case TurnOrder.PlayerStart:
                return TurnOrder.PlayerEnd;
            case TurnOrder.PlayerEnd:
                return TurnOrder.EnemyStart;
            case TurnOrder.EnemyStart:
                return TurnOrder.EnemyEnd;
            case TurnOrder.EnemyEnd:
                return _hasThirdParty ? TurnOrder.ThirdPartyStart : TurnOrder.PlayerStart;
            case TurnOrder.ThirdPartyStart:
                return TurnOrder.ThirdPartyEnd;
            case TurnOrder.ThirdPartyEnd:
                return TurnOrder.PlayerStart;
            default:
                Debug.LogError("Invalid TurnOrder state.");
                return TurnOrder.PlayerStart;
        }
    }

    /// <summary>
    /// Gets the active units from the current roster, sorted by Order.
    /// </summary>
    private List<CharacterInstance> GetCurrentRosterUnits()
    {
        IReadOnlyList<CharacterInstance> instances = null;

        switch (_currentTurnOrder)
        {
            case TurnOrder.PlayerStart:
            case TurnOrder.PlayerEnd:
                instances = BattleBrain.PlayerTeamRoster?.Instances;
                break;
            case TurnOrder.EnemyStart:
            case TurnOrder.EnemyEnd:
                instances = BattleBrain.EnemyTeamRoster?.Instances;
                break;
            case TurnOrder.ThirdPartyStart:
            case TurnOrder.ThirdPartyEnd:
                instances = BattleBrain.ThirdPartyTeamRoster?.Instances;
                break;
        }

        if (instances == null || instances.Count == 0)
        {
            return new List<CharacterInstance>();
        }

        // Get roster to access Order field
        Roster roster = null;
        switch (_currentTurnOrder)
        {
            case TurnOrder.PlayerStart:
            case TurnOrder.PlayerEnd:
                roster = BattleBrain.PlayerTeamRoster?.roster;
                break;
            case TurnOrder.EnemyStart:
            case TurnOrder.EnemyEnd:
                roster = BattleBrain.EnemyTeamRoster?.roster;
                break;
            case TurnOrder.ThirdPartyStart:
            case TurnOrder.ThirdPartyEnd:
                roster = BattleBrain.ThirdPartyTeamRoster?.roster;
                break;
        }

        if (roster == null)
        {
            return new List<CharacterInstance>(instances);
        }

        // Sort by Order field
        return instances
            .OrderBy(unit =>
            {
                var placement = roster.characters.FirstOrDefault(p =>
                    p.CharacterData == unit.CharacterTemplate
                );
                return placement?.Order ?? int.MaxValue;
            })
            .ToList();
    }

    /// <summary>
    /// Progress to the next unit in the current roster, or the next turn phase if all units have acted.
    /// </summary>
    public bool Progress()
    {
        if (_brain == null)
        {
            Debug.LogError("TurnRotisserie Progress failed: Brain reference is null.");
            return false;
        }

        // Check if current unit gets another turn
        if (_unitTookAnotherTurn)
        {
            _unitTookAnotherTurn = false;
            // Same unit goes again, don't increment roster index
            ActivateCurrentUnit();
            return true;
        }

        // Get current roster
        var units = GetCurrentRosterUnits();

        // Try to find next non-defeated unit
        _currentRosterIndex++;

        while (_currentRosterIndex < units.Count)
        {
            var unit = units[_currentRosterIndex];

            if (!unit.IsDefeatedInCurrentBattle)
            {
                // Found next active unit
                ActivateCurrentUnit();
                return true;
            }

            // Skip defeated unit
            _currentRosterIndex++;
        }

        // All units in this roster have acted, progress to next phase
        return ProgressToNextPhase();
    }

    /// <summary>
    /// Activates the current unit in the current roster.
    /// </summary>
    private void ActivateCurrentUnit()
    {
        var units = GetCurrentRosterUnits();

        if (_currentRosterIndex < 0 || _currentRosterIndex >= units.Count)
        {
            Debug.LogError($"TurnRotisserie: Invalid roster index {_currentRosterIndex}");
            return;
        }

        var activeUnit = units[_currentRosterIndex];

        if (activeUnit == null)
        {
            Debug.LogError($"TurnRotisserie: Active unit at index {_currentRosterIndex} is null");
            return;
        }

        // Update battle context
        ChangeBattleContextData(activeUnit);
    }

    /// <summary>
    /// Progress to the next turn phase and reset roster index.
    /// </summary>
    private bool ProgressToNextPhase()
    {
        TurnOrder previousOrder = _currentTurnOrder;
        _currentTurnOrder = GetNextTurnOrder();
        _currentRosterIndex = -1; // Will be incremented to 0 on next Progress()

        // Publish phase transition events
        bool newRoundStarted =
            _currentTurnOrder == TurnOrder.PlayerStart && previousOrder != TurnOrder.PlayerStart;

        if (newRoundStarted)
        {
            _brain.PublishTurnEnded();
            _brain.PublishTurnBegin();
        }

        // Publish phase-specific events
        switch (_currentTurnOrder)
        {
            case TurnOrder.PlayerStart:
                _brain.PublishPlayerTurnStarted();
                break;
            case TurnOrder.PlayerEnd:
                _brain.PublishPlayerTurnEnded();
                break;
            case TurnOrder.EnemyStart:
                _brain.PublishEnemyTurnStarted();
                break;
            case TurnOrder.EnemyEnd:
                _brain.PublishEnemyTurnEnded();
                break;
            case TurnOrder.ThirdPartyStart:
                _brain.PublishThirdPartyTurnStarted();
                break;
            case TurnOrder.ThirdPartyEnd:
                _brain.PublishThirdPartyTurnEnded();
                break;
        }

        // Activate first unit of new phase
        return Progress();
    }

    /// <summary>
    /// Updates BattleContext with the active unit and its targets/allies.
    /// </summary>
    public OperationResult ChangeBattleContextData(CharacterInstance activeUnit)
    {
        var context = BattleBrain.BattleObject.Context;

        try
        {
            context.UnitInstance = activeUnit;

            // Clear previous context
            context.Targets.Clear();
            context.Allies.Clear();
            context.ThirdParty.Clear();

            // Populate based on current phase
            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                case TurnOrder.PlayerEnd:
                    PopulatePlayerContext(context);
                    break;
                case TurnOrder.EnemyStart:
                case TurnOrder.EnemyEnd:
                    PopulateEnemyContext(context);
                    break;
                case TurnOrder.ThirdPartyStart:
                case TurnOrder.ThirdPartyEnd:
                    PopulateThirdPartyContext(context);
                    break;
            }

            // Update adjacency
            context.AdjacentUnits =
                new Turnroot.Gameplay.Combat.FundamentalComponents.Battles.Locations.Adjacency(
                    activeUnit
                );

            // Publish activation event
            if (
                _currentTurnOrder == TurnOrder.PlayerStart
                || _currentTurnOrder == TurnOrder.PlayerEnd
            )
            {
                _brain.PublishPlayerControlledUnitActivated(activeUnit);
            }

            return OperationResult.SuccessResult();
        }
        catch (System.Exception ex)
        {
            return OperationResult.Failure($"ChangeBattleContextData failed: {ex.Message}");
        }
    }

    private void PopulatePlayerContext(
        Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext context
    )
    {
        // Targets = Enemies
        if (BattleBrain.EnemyTeamRoster?.Instances != null)
        {
            foreach (var enemy in BattleBrain.EnemyTeamRoster.Instances)
            {
                if (!enemy.IsDefeatedInCurrentBattle)
                {
                    context.Targets.Add(enemy);
                }
            }
        }

        // Allies = Other player units
        if (BattleBrain.PlayerTeamRoster?.Instances != null)
        {
            foreach (var ally in BattleBrain.PlayerTeamRoster.Instances)
            {
                if (!ally.IsDefeatedInCurrentBattle && ally != context.UnitInstance)
                {
                    context.Allies.Add(ally);
                }
            }
        }

        // ThirdParty = NPCs
        if (BattleBrain.ThirdPartyTeamRoster?.Instances != null)
        {
            foreach (var npc in BattleBrain.ThirdPartyTeamRoster.Instances)
            {
                if (!npc.IsDefeatedInCurrentBattle)
                {
                    context.ThirdParty.Add(npc);
                }
            }
        }
    }

    private void PopulateEnemyContext(
        Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext context
    )
    {
        // Targets = Player units
        if (BattleBrain.PlayerTeamRoster?.Instances != null)
        {
            foreach (var player in BattleBrain.PlayerTeamRoster.Instances)
            {
                if (!player.IsDefeatedInCurrentBattle)
                {
                    context.Targets.Add(player);
                }
            }
        }

        // Allies = Other enemy units
        if (BattleBrain.EnemyTeamRoster?.Instances != null)
        {
            foreach (var ally in BattleBrain.EnemyTeamRoster.Instances)
            {
                if (!ally.IsDefeatedInCurrentBattle && ally != context.UnitInstance)
                {
                    context.Allies.Add(ally);
                }
            }
        }

        // ThirdParty = NPCs
        if (BattleBrain.ThirdPartyTeamRoster?.Instances != null)
        {
            foreach (var npc in BattleBrain.ThirdPartyTeamRoster.Instances)
            {
                if (!npc.IsDefeatedInCurrentBattle)
                {
                    context.ThirdParty.Add(npc);
                }
            }
        }
    }

    private void PopulateThirdPartyContext(
        Turnroot.Gameplay.Combat.FundamentalComponents.Battles.BattleContext context
    )
    {
        // Targets = Both player and enemy units
        if (BattleBrain.PlayerTeamRoster?.Instances != null)
        {
            foreach (var player in BattleBrain.PlayerTeamRoster.Instances)
            {
                if (!player.IsDefeatedInCurrentBattle)
                {
                    context.Targets.Add(player);
                }
            }
        }

        if (BattleBrain.EnemyTeamRoster?.Instances != null)
        {
            foreach (var enemy in BattleBrain.EnemyTeamRoster.Instances)
            {
                if (!enemy.IsDefeatedInCurrentBattle)
                {
                    context.Targets.Add(enemy);
                }
            }
        }

        // Allies = Other third party units
        if (BattleBrain.ThirdPartyTeamRoster?.Instances != null)
        {
            foreach (var ally in BattleBrain.ThirdPartyTeamRoster.Instances)
            {
                if (!ally.IsDefeatedInCurrentBattle && ally != context.UnitInstance)
                {
                    context.Allies.Add(ally);
                }
            }
        }
    }

    /// <summary>
    /// Call this when a unit takes another turn.
    /// </summary>
    public void GrantAnotherTurn(CharacterInstance unit)
    {
        if (unit == null)
        {
            Debug.LogError("TurnRotisserie: Cannot grant another turn to null unit");
            return;
        }

        var currentUnits = GetCurrentRosterUnits();
        if (_currentRosterIndex >= 0 && _currentRosterIndex < currentUnits.Count)
        {
            if (currentUnits[_currentRosterIndex] == unit)
            {
                _unitTookAnotherTurn = true;
                Debug.Log(
                    $"TurnRotisserie: {unit.CharacterTemplate.DisplayName} will take another turn"
                );
            }
        }
    }
}
