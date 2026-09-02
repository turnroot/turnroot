using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public interface IHairSimulation
    {
        /// <summary>
        /// Initialize the simulation for the given chain root transform.
        /// </summary>
        /// <param name="chainRoot">The root bone of the chain (e.g. a hair bone).</param>
        /// <param name="unitModel">The unit model this chain belongs to.</param>
        void Initialize(Transform chainRoot, GameObject unitModel);

        bool Enabled { get; set; }

        void UpdateSimulation();
    }
}
