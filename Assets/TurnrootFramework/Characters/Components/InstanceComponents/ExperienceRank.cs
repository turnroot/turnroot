using System;
using UnityEngine;

namespace Turnroot.Characters
{
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

        public ExperienceRankInstance(string experienceTypeId, string rankLetter)
        {
            _experienceTypeId = experienceTypeId;
            _rank = new CommonAncestors.LeveledLetteredField(rankLetter);
            _experiencePoints = 0;
        }

        public ExperienceRankInstance(CharacterData.ExperienceRank template)
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
