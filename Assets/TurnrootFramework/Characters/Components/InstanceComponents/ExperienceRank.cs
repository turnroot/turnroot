using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Turnroot.Characters
{
    /// <summary>
    /// Tracks a character's experience and rank progression for a specific experience type (e.g., weapon proficiency).
    /// </summary>
    [Serializable]
    public class ExperienceRankInstance
    {
        [SerializeField]
        private string _experienceTypeId;

        [SerializeField]
        private CommonAncestors.LeveledLetteredField _rank;

        [SerializeField]
        private int _experiencePoints = 0;

        public string ExperienceTypeId => _experienceTypeId;
        public CommonAncestors.LeveledLetteredField Rank => _rank;
        public int ExperiencePoints => _experiencePoints;

        [JsonConstructor]
        public ExperienceRankInstance()
        {
            // Parameterless constructor used by Newtonsoft.Json when deserializing objects
            // Use the parameterless LeveledLetteredField ctor which defaults to 'E'
            _experienceTypeId = string.Empty;
            _rank = new CommonAncestors.LeveledLetteredField();
            _experiencePoints = 0;
        }

        public ExperienceRankInstance(string experienceTypeId, string rankLetter)
        {
            _experienceTypeId = experienceTypeId;
            _rank = new CommonAncestors.LeveledLetteredField(rankLetter);
            _experiencePoints = 0;
        }

        public ExperienceRankInstance(ExperienceRank template)
        {
            _experienceTypeId = template.ExperienceTypeId;
            _rank = new CommonAncestors.LeveledLetteredField(template.Rank.Value);
            _experiencePoints = 0;
        }

        public void AddExperience(int amount) => _experiencePoints += amount; // TODO: Implement rank progression based on experience thresholds

        public void SetRank(string rankLetter) =>
            _rank = new CommonAncestors.LeveledLetteredField(rankLetter);
    }
}
