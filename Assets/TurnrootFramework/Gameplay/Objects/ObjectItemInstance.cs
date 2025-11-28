using System;
using Turnroot.Serialization;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    [Serializable]
    public class ObjectItemInstance : IPostDeserialize
    {
        [SerializeField]
        private string _id;

        [SerializeField]
        private ObjectItem _template;

        public ObjectItem Template => _template;

        // Make constructor non-public to encourage using ObjectItemInstance.Create
        internal ObjectItemInstance(ObjectItem template)
        {
            _template = template;
            _id = Guid.NewGuid().ToString();
        }

        public static ObjectItemInstance Create(ObjectItem template)
        {
            return new ObjectItemInstance(template);
        }

        public void OnAfterDeserialize()
        {
            // there is nothing here yet
        }
    }
}
