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
        _currentTurnOrder = GetNextTurnOrder();
        if (_brain != null)
        {
            switch (_currentTurnOrder)
            {
                case TurnOrder.PlayerStart:
                    _brain.InvokePlayerTurnStarted();
                    break;
                case TurnOrder.PlayerEnd:
                    _brain.InvokePlayerTurnEnded();
                    break;
                case TurnOrder.EnemyStart:
                    _brain.InvokeEnemyTurnStarted();
                    break;
                case TurnOrder.EnemyEnd:
                    _brain.InvokeEnemyTurnEnded();
                    break;
                case TurnOrder.ThirdPartyStart:
                    _brain.InvokeThirdPartyTurnStarted();
                    break;
                case TurnOrder.ThirdPartyEnd:
                    _brain.InvokeThirdPartyTurnEnded();
                    break;
            }
            return true;
        }
        else
        {
            Debug.LogError("TurnRotisserie Progress failed: Brain reference is null.");
            return false;
        }
    }
}
