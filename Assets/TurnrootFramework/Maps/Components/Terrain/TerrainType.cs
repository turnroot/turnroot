using System;
using UnityEngine;

[Serializable]
public class TerrainType
{
    [SerializeField]
    private string _id = System.Guid.NewGuid().ToString();

    [SerializeField]
    private string _name = "New Terrain";

    [SerializeField]
    private float _costWalk = 1f;

    [SerializeField]
    private float _costFly = 1f;

    [SerializeField]
    private float _costRide = 1f;

    [SerializeField]
    private float _costMagic = 1f;

    [SerializeField]
    private float _costArmor = 1f;

    [SerializeField, Range(-20, 20)]
    private int _healthChangePerTurnWalk = 0;

    [SerializeField, Range(-20, 20)]
    private int _healthChangePerTurnRiding = 0;

    [SerializeField, Range(-20, 20)]
    private int _healthChangePerTurnFlying = 0;

    [SerializeField, Range(-40, 40)]
    private int _defenseBonusWalk = 0;

    [SerializeField, Range(-40, 40)]
    private int _defenseBonusRiding = 0;

    [SerializeField, Range(-40, 40)]
    private int _defenseBonusFlying = 0;

    [SerializeField, Range(-40, 40)]
    private int _avoidBonusWalk = 0;

    [SerializeField, Range(-40, 40)]
    private int _avoidBonusRiding = 0;

    [SerializeField, Range(-40, 40)]
    private int _avoidBonusFlying = 0;

    [SerializeField]
    private Color _editorColor = Color.white;

    public string Name => string.IsNullOrEmpty(_name) ? "(unnamed)" : _name;
    public float CostWalk => _costWalk;
    public float CostFly => _costFly;
    public float CostRide => _costRide;
    public float CostMagic => _costMagic;
    public float CostArmor => _costArmor;

    public int HealthChangePerTurnWalk => _healthChangePerTurnWalk;
    public int HealthChangePerTurnRiding => _healthChangePerTurnRiding;
    public int HealthChangePerTurnFlying => _healthChangePerTurnFlying;
    public int DefenseBonusWalk => _defenseBonusWalk;
    public int DefenseBonusRiding => _defenseBonusRiding;
    public int DefenseBonusFlying => _defenseBonusFlying;
    public int AvoidBonusWalk => _avoidBonusWalk;
    public int AvoidBonusRiding => _avoidBonusRiding;
    public int AvoidBonusFlying => _avoidBonusFlying;
    public Color EditorColor => _editorColor;

    // Stable identifier used to reference this type from other objects
    public string Id => _id;

    public TerrainType() { }

    public TerrainType(
        string name,
        float costWalk,
        float costFly,
        float costRide,
        float costMagic,
        float costArmor,
        int healthChangePerTurnWalk,
        int healthChangePerTurnRiding,
        int healthChangePerTurnFlying,
        int defenseBonusWalk,
        int defenseBonusRiding,
        int defenseBonusFlying,
        int avoidBonusWalk,
        int avoidBonusRiding,
        int avoidBonusFlying,
        Color editorColor
    )
    {
        _name = name;
        _costWalk = costWalk;
        _costFly = costFly;
        _costRide = costRide;
        _costMagic = costMagic;
        _costArmor = costArmor;
        _healthChangePerTurnWalk = healthChangePerTurnWalk;
        _healthChangePerTurnRiding = healthChangePerTurnRiding;
        _healthChangePerTurnFlying = healthChangePerTurnFlying;
        _defenseBonusWalk = defenseBonusWalk;
        _defenseBonusRiding = defenseBonusRiding;
        _defenseBonusFlying = defenseBonusFlying;
        _avoidBonusWalk = avoidBonusWalk;
        _avoidBonusRiding = avoidBonusRiding;
        _avoidBonusFlying = avoidBonusFlying;
        _editorColor = editorColor;
    }
}
