using Turnroot.Gameplay.Player;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.Brain
{
    public class PlayerInputBrain : BrainComponent
    {
        public PlayerController ScenePlayerController { get; private set; }

        protected override void Awake()
        {
            base.Awake(); // Calls parent Awake
            Debug.Log("PlayerInputBrain Awake called.");
        }

        private readonly SingleValueCache<PlayerController> _playerControllerCache = new();

        public void PopulateScenePlayerController(PlayerController controller)
        {
            ScenePlayerController = controller;
            controller.Brain = this;
            _playerControllerCache.Invalidate(); // Invalidate cache when manually set
            Debug.Log("Brain populated scene PlayerController.");
        }

        public void TryLinkPlayerController()
        {
            var controller = _playerControllerCache.GetOrCompute(() =>
            {
                var controllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                return (controllers != null && controllers.Length > 0) ? controllers[0] : null;
            });

            if (controller != null)
            {
                ScenePlayerController = controller;
                Debug.Log("Brain linked to scene PlayerController.");
                controller.Brain = this;
                controller.Initialize();
            }
            else
            {
                Debug.Log("Brain could not find a PlayerController in the scene.");
            }
        }

        protected override void SubscribeToBrainEvents()
        {
            // Subscribe to events if needed
        }

        protected override void UnsubscribeFromBrainEvents()
        {
            // No subscriptions to clean up yet
        }
    }
}
