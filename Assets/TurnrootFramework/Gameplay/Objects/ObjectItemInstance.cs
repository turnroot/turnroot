using System;
using Turnroot.Serialization;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    [Serializable]
    public class ObjectItemInstance : IPostDeserialize
    {
        [SerializeField]
        private ObjectItem _template;

        public ObjectItem Template => _template;

        public ObjectItemInstance(ObjectItem template)
        {
            _template = template;
        }

        // Parameterless constructor for JSON deserialization / serialization
        public ObjectItemInstance() { }

        public void OnAfterDeserialize()
        {
            // No special initialization needed currently; placeholder for future needs.
        }
    }
}
