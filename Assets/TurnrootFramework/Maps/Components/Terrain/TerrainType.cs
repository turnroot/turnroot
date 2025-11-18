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
    private int _healthChangePerTurn = 0;

    [SerializeField, Range(-40, 40)]
    private int _defenseBonus = 0;

    [SerializeField, Range(-40, 40)]
    private int _avoidBonus = 0;

    [SerializeField]
    private Color _editorColor = Color.white;

    public string Name => string.IsNullOrEmpty(_name) ? "(unnamed)" : _name;
    public float CostWalk => _costWalk;
    public float CostFly => _costFly;
    public float CostRide => _costRide;
    public float CostMagic => _costMagic;
    public float CostArmor => _costArmor;

    public int HealthChangePerTurn => _healthChangePerTurn;
    public int DefenseBonus => _defenseBonus;
    public int AvoidBonus => _avoidBonus;
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
        int healthChangePerTurn,
        int defenseBonus,
        int avoidBonus,
        Color editorColor
    )
    {
        _name = name;
        _costWalk = costWalk;
        _costFly = costFly;
        _costRide = costRide;
        _costMagic = costMagic;
        _costArmor = costArmor;
        _healthChangePerTurn = healthChangePerTurn;
        _defenseBonus = defenseBonus;
        _avoidBonus = avoidBonus;
        _editorColor = editorColor;
    }
}
