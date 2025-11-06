using System;
using UnityEngine;

[Serializable]
public class SkillInstance
{
    [SerializeField]
    private Skill _skillTemplate;

    // Runtime state - unique per character/entity
    [SerializeField]
    private bool _readyToFire;

    [SerializeField]
    private bool _equipped;

    public Skill SkillTemplate => _skillTemplate;
    public bool ReadyToFire => _readyToFire;
    public bool Equipped => _equipped;

    public SkillInstance(Skill skillTemplate)
    {
        _skillTemplate = skillTemplate;
        _readyToFire = false;
        _equipped = false;
    }

    public void SetReadyToFire(bool ready)
    {
        _readyToFire = ready;
    }

    public void SetEquipped(bool equipped)
    {
        _equipped = equipped;
    }
}
