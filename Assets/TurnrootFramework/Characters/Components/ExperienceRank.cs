using System;
using Turnroot.CommonAncestors;
using UnityEngine;

namespace Turnroot.Characters
{
    [Serializable]
    public class ExperienceRank
    {
        [Tooltip("ID of the experience type (e.g., 'sword', 'riding', 'flying')")]
        [SerializeField]
        private string _experienceTypeId;

        [Tooltip("Current rank/level (E=0, D=1, C=2, B=3, A=4, S=5)")]
        [SerializeField]
        private LeveledLetteredField _rank = new(LeveledLetteredField.E);

        public string ExperienceTypeId
        {
            get => _experienceTypeId;
            set => _experienceTypeId = value;
        }

        public LeveledLetteredField Rank
        {
            get => _rank;
            set => _rank = value;
        }

        public ExperienceRank() { }

        public ExperienceRank(string experienceTypeId, string rankValue)
        {
            _experienceTypeId = experienceTypeId;
            _rank = new LeveledLetteredField(rankValue);
        }
    }
}
