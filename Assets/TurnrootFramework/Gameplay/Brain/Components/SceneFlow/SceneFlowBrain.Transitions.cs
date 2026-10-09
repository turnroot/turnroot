using System.Collections.Generic;
using Turnroot.Gameplay.Combat;

namespace Turnroot.Utilities.SceneFlows
{
    public partial class SceneFlowBrain
    {
        #region Navigation (UnityEvent Compatible)

        /// <summary>
        /// Advances the game loop from the current scene:
        /// game start -> hub, hub -> end of hub day, end of hub day -> hub, battle -> hub,
        /// credits -> game start.
        /// </summary>
        public void AdvanceToNextScene()
        {
            switch (CurrentSceneKind)
            {
                case GameSceneKind.Hub:
                    EndHubDay();
                    break;
                case GameSceneKind.Credits:
                    GoToGameStart();
                    break;
                case GameSceneKind.GameStart:
                case GameSceneKind.EndOfHubDay:
                case GameSceneKind.Battle:
                    GoToHub();
                    break;
                default:
                    $"SceneFlowBrain: Cannot advance from '{_currentSceneName}' — it is not part of the game flow. Returning to hub.".LogWarning();
                    GoToHub();
                    break;
            }
        }

        public void GoToGameStart() => TransitionTo(Registry?.GameStartSceneName, "Game Start");

        public void GoToHub() => TransitionTo(Registry?.HubSceneName, "Hub");

        public void GoToCredits() => TransitionTo(Registry?.CreditsSceneName, "Credits");

        /// <summary>
        /// Transitions from the hub to the End Of Hub Day scene.
        /// </summary>
        public void EndHubDay() => TransitionTo(Registry?.EndOfHubDaySceneName, "End Of Hub Day");

        /// <summary>
        /// Transitions to the battle with the given scene name. The battle must exist in the
        /// <see cref="GameFlowRegistry"/> and currently be available.
        /// </summary>
        public void GoToBattle(string battleSceneName)
        {
            if (!IsBattleAvailable(battleSceneName))
            {
                $"SceneFlowBrain: Battle '{battleSceneName}' is not available.".LogWarning();
                Brain.PublishSceneTransitionBlocked(battleSceneName, "Battle not available");
                return;
            }

            TransitionTo(battleSceneName, GetDisplayName(battleSceneName));
        }

        private void TransitionTo(string sceneName, string displayName)
        {
            if (_isTransitioning)
            {
                $"SceneFlowBrain: Transition to '{sceneName}' ignored — a transition is already in progress.".LogWarning();
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                $"SceneFlowBrain: Cannot transition to '{displayName}' — scene is not assigned in the GameFlowRegistry.".LogError();
                return;
            }

            _isTransitioning = true;
            _lastSideEffectsSceneName = null;
            StartCoroutine(LoadSceneAsync(sceneName, displayName));
        }

        #endregion

        #region Battle Registry State

        private const string BattleUnlockedFlagPrefix = "battle_unlocked_";
        private const string BattleCompletedFlagPrefix = "battle_completed_";

        public void SetBattleUnlocked(string battleSceneName, bool unlocked) =>
            SetCustomFlag(BattleUnlockedFlagPrefix + battleSceneName, unlocked);

        public void SetBattleCompleted(string battleSceneName, bool completed) =>
            SetCustomFlag(BattleCompletedFlagPrefix + battleSceneName, completed);

        public bool IsBattleCompleted(string battleSceneName) =>
            GetCustomFlag(BattleCompletedFlagPrefix + battleSceneName);

        public bool IsBattleUnlocked(string battleSceneName) =>
            Registry != null
            && Registry.TryGetBattle(battleSceneName, out var battle)
            && (
                battle.UnlockedByDefault
                || GetCustomFlag(BattleUnlockedFlagPrefix + battleSceneName)
            );

        /// <summary>
        /// A battle is available when it is in the registry, unlocked, and either repeatable
        /// or not yet completed.
        /// </summary>
        public bool IsBattleAvailable(string battleSceneName) =>
            IsBattleUnlocked(battleSceneName)
            && Registry.TryGetBattle(battleSceneName, out var battle)
            && (battle.Repeateable || !IsBattleCompleted(battleSceneName));

        /// <summary>
        /// All currently available battles, in registry order.
        /// </summary>
        public List<GameFlowRegistry.BattleEntry> GetAvailableBattles()
        {
            var result = new List<GameFlowRegistry.BattleEntry>();
            if (Registry?.Battles == null)
            {
                return result;
            }

            foreach (var battle in Registry.Battles)
            {
                if (battle.BattleScene == null || battle.BattleScene.IsEmpty)
                {
                    continue;
                }

                if (IsBattleAvailable(battle.BattleScene.SceneName))
                {
                    result.Add(battle);
                }
            }

            return result;
        }

        private void HandleBattleCompleted(BattleExitType exitType)
        {
            if (exitType != BattleExitType.Victory || CurrentSceneKind != GameSceneKind.Battle)
            {
                return;
            }

            SetBattleCompleted(_currentSceneName, true);
            RefreshChapter(force: false);
        }

        #endregion

        #region Chapter

        private int _publishedChapterNumber = int.MinValue;
        private string _publishedChapterName;

        /// <summary>
        /// The current chapter is that of the first Required Story Battle (in registry order)
        /// that has not been completed. When all are completed, the last one's chapter is used.
        /// Returns false if the registry has no Required Story Battles.
        /// </summary>
        public bool TryGetCurrentChapter(out int chapterNumber, out string chapterName)
        {
            chapterNumber = 0;
            chapterName = string.Empty;

            if (Registry?.Battles == null)
            {
                return false;
            }

            bool found = false;
            foreach (var battle in Registry.Battles)
            {
                if (!battle.RequiredStoryBattle)
                {
                    continue;
                }

                found = true;
                chapterNumber = battle.ChapterNumber;
                chapterName = battle.ChapterName ?? string.Empty;

                bool completed =
                    battle.BattleScene != null
                    && !battle.BattleScene.IsEmpty
                    && IsBattleCompleted(battle.BattleScene.SceneName);
                if (!completed)
                {
                    break;
                }
            }

            return found;
        }

        public int CurrentChapterNumber => TryGetCurrentChapter(out var n, out _) ? n : 0;

        /// <summary>
        /// Publishes the current chapter to the save file when it changed (or when
        /// <paramref name="force"/> is true, e.g. a save file just became active).
        /// </summary>
        private void RefreshChapter(bool force)
        {
            if (!TryGetCurrentChapter(out var number, out var name))
            {
                return;
            }

            if (!force && number == _publishedChapterNumber && name == _publishedChapterName)
            {
                return;
            }

            _publishedChapterNumber = number;
            _publishedChapterName = name;
            EnsureBrainReference()?.PublishSetSaveFileChapter(name, number);
        }

        #endregion
    }
}
