using System;
using Newtonsoft.Json;
using Turnroot.GameSettings;
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

        public ExperienceRankInstance()
        {
            _experienceTypeId = string.Empty;
            _rank = new CommonAncestors.LeveledLetteredField();
            _experiencePoints = 0;
        }

        /// <summary>
        /// Deserialization constructor: Newtonsoft.Json matches JSON property names
        /// (ExperienceTypeId, Rank, ExperiencePoints) to these parameter names case-insensitively.
        /// </summary>
        [JsonConstructor]
        public ExperienceRankInstance(
            string experienceTypeId,
            CommonAncestors.LeveledLetteredField rank,
            int experiencePoints
        )
        {
            _experienceTypeId = experienceTypeId ?? string.Empty;
            _rank = rank ?? new CommonAncestors.LeveledLetteredField();
            _experiencePoints = experiencePoints;
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

        public void AddExperience(int amount)
        {
            _experiencePoints += amount;

            var settings = GameplayGeneralSettings.Instance;
            int threshold = settings != null ? settings.ExperienceRankUpThreshold : 100;

            while (
                _experiencePoints >= threshold
                && _rank.Value != CommonAncestors.LeveledLetteredField.S
            )
            {
                _experiencePoints -= threshold;
                _rank.Increase();
            }
        }

        public void SetRank(string rankLetter) =>
            _rank = new CommonAncestors.LeveledLetteredField(rankLetter);
    }
}
