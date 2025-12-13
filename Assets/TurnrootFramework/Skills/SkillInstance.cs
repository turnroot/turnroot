using System;
using Turnroot.Characters;
using Turnroot.Gameplay.Brain;
using Turnroot.Gameplay.Combat.FundamentalComponents.Battles;
using Turnroot.Serialization;
using Turnroot.Utilities;
using UnityEngine;

[Serializable]
public class SkillInstance : IPostDeserialize
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

    // Parameterless constructor for deserialization
    public SkillInstance() { }

    public void OnAfterDeserialize()
    {
        // No special initialization required currently. Provided for future-proofing.
    }

    /// <summary>
    /// Execute this skill instance with the given battle context.
    /// </summary>
    public void ExecuteSkill(BattleContext context)
    {
        if (_skillTemplate == null)
        {
            Debug.LogWarning("SkillInstance has no SkillTemplate assigned.");
            return;
        }

        if (_skillTemplate.BehaviorGraph == null)
        {
            Debug.LogWarning($"Skill {_skillTemplate.SkillName} has no BehaviorGraph assigned.");
            return;
        }

        // Set runtime context
        context.CurrentSkill = _skillTemplate;
        context.CurrentSkillGraph = _skillTemplate.BehaviorGraph;

        // Trigger template events
        _skillTemplate.TriggerSkillEvents();

        // Publish to Brain for centralized tracking
        var brain = GetBrain.Get();
        brain?.PublishSkillTriggered(context.UnitInstance, _skillTemplate);

        // Execute the behavior graph
        _skillTemplate.BehaviorGraph.Execute(context);

        // Reset ready state after execution
        _readyToFire = false;
    }

    public void SetReadyToFire(bool ready) => _readyToFire = ready;

    public void SetEquipped(bool equipped, CharacterInstance owner = null, Brain brain = null)
    {
        _equipped = equipped;
        if (equipped)
        {
            _skillTemplate.SkillEquipped?.Invoke();
            // Publish to Brain for centralized tracking
            brain?.PublishSkillEquipped(owner, _skillTemplate);
        }
        else
        {
            _skillTemplate.SkillUnequipped?.Invoke();
            // Publish to Brain for centralized tracking
            brain?.PublishSkillUnequipped(owner, _skillTemplate);
        }
    }
}
