using Turnroot.Characters;
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

    [SerializeField, HideInInspector]
    private Turnroot.Gameplay.Brain.Brain _brain;

    [HideInInspector]
    public Turnroot.Gameplay.Brain.Brain Brain
    {
        get => _brain;
        set => _brain = value;
    }

    private TurnOrder _currentTurnOrder = TurnOrder.PlayerStart;

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

    public bool Progress()
    {
        TurnOrder previousOrder = _currentTurnOrder;
        _currentTurnOrder = GetNextTurnOrder();
        if (_brain != null)
        {
            // When cycling back to PlayerStart, a new battle round begins
            bool newRoundStarted =
                _currentTurnOrder == TurnOrder.PlayerStart
                && previousOrder != TurnOrder.PlayerStart;

            // If completing a round, publish turn ended for the previous round
            if (newRoundStarted)
            {
                _brain.PublishTurnEnded();
            }

            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                    // Publish general turn start (increments turn number)
                    if (newRoundStarted)
                    {
                        _brain.PublishTurnBegin();
                    }
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
            ChangeBattleContextData();
            return true;
        }
        else
        {
            Debug.LogError("TurnRotisserie Progress failed: Brain reference is null.");
            return false;
        }
    }

    public OperationResult ChangeBattleContextData()
    {
        var context = Brain.battleBrain.BattleObject.Context;
        try
        {
            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                    CharacterInstance activeCharacter = null; // TODO: Get this from roster
                    ChangeToPlayerContext(activeCharacter);
                    break;
                // TODO: Fill in the rest
            }
            return OperationResult.SuccessResult();
        }
        catch (System.Exception ex)
        {
            return OperationResult.Failure("ChangeBattleContextData failed: " + ex.Message);
        }
    }

    public void ChangeToPlayerContext(CharacterInstance activeCharacter)
    {
        var context = Brain.battleBrain.BattleObject.Context;
        context.UnitInstance = activeCharacter;
        Brain.PublishPlayerControlledUnitActivated(activeCharacter);
        // TODO: Fill targets, allies, third party from rosters
        // TODO: Do something with context.AdjacentUnits.GetAdjacentAlliesNonAlloc and GetAdjacentEnemiesNonAlloc
    }
}
