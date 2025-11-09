using System;
using UnityEngine;

namespace Turnroot.Gameplay.Objects
{
    [Serializable]
    public class ObjectItemInstance
    {
        [SerializeField]
        private ObjectItem _template;

        public ObjectItem Template => _template;

        public ObjectItemInstance(ObjectItem template)
        {
            _template = template;
        }
    }
}
