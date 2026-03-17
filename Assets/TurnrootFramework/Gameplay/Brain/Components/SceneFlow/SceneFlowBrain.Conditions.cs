namespace Turnroot.Utilities.SceneFlows
{
    /// <summary>
    /// Common condition keys used by the SceneFlowBrain condition evaluator.
    /// </summary>
    public static class SceneFlowConditionKeys
    {
        /// <summary>
        /// Flag indicating the next available scene transition should return to the hub.
        /// </summary>
        public const string ReturnToHub = "ReturnToHub";

        /// <summary>
        /// Flag used to trigger an end-of-day transition from the hub.
        /// </summary>
        public const string EndHubDay = "EndHubDay";
    }
}
