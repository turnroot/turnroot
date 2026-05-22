using Turnroot.UI;
using Turnroot.Utilities;
using UnityEngine;

namespace Turnroot.Gameplay.NonCombatScenes.Hub
{
    public partial class HubManager
    {
        #region Helpers

        private void CacheSpawnPointHeights()
        {
            _spawnPointHeights.Clear();
            if (SpawnGroundCollider == null)
            {
                return;
            }
            if (subLocations == null)
            {
                return;
            }

            foreach (var sub in subLocations)
            {
                if (sub == null || sub.UnitSpawnPoints == null)
                {
                    continue;
                }

                foreach (var entry in sub.UnitSpawnPoints)
                {
                    var spawnPoint = entry.UnitSpawnPoint;
                    if (spawnPoint == null)
                    {
                        continue;
                    }

                    var origin = spawnPoint.position + Vector3.up * SpawnPointRaycastDistance;
                    var ray = new Ray(origin, Vector3.down);
                    if (
                        SpawnGroundCollider.Raycast(
                            ray,
                            out var hit,
                            SpawnPointRaycastDistance * 2f
                        )
                    )
                    {
                        _spawnPointHeights[spawnPoint] = hit.point.y;
                    }
                }
            }
        }

        public float GetSpawnPointHeight(Transform spawnPoint, float defaultHeight) =>
            spawnPoint == null ? defaultHeight
            : _spawnPointHeights.TryGetValue(spawnPoint, out var h) ? h
            : defaultHeight;

        public void UpdateDateText()
        {
            if (dateText != null)
            {
                Month month = (Month)Mathf.Clamp(gameDate.month - 1, 0, 11);
                string daySuffix = GameDate.GetDaySuffix(gameDate.day);
                string monthName = month.ToString();
                dateText.text = $"{monthName} {gameDate.day}{daySuffix}";
            }
        }

        private void BuildNavigableChoices()
        {
            var list = new System.Collections.Generic.List<UiChoice>();

            if (LocationChoices != null)
            {
                list.AddRange(LocationChoices);
            }

            // Only append ExploreChoice separately if it is NOT already embedded inside LocationChoices.
            bool exploreEmbedded =
                ExploreChoice != null
                && LocationChoices != null
                && System.Array.IndexOf(LocationChoices, ExploreChoice) >= 0;

            if (ExploreChoice != null && !exploreEmbedded)
            {
                list.Add(ExploreChoice);
            }

            // Only append BattlefieldsChoice separately if it is NOT already embedded inside LocationChoices.
            bool battlefieldsEmbedded =
                BattlefieldsChoice != null
                && LocationChoices != null
                && System.Array.IndexOf(LocationChoices, BattlefieldsChoice) >= 0;

            if (BattlefieldsChoice != null && !battlefieldsEmbedded)
            {
                list.Add(BattlefieldsChoice);
            }

            if (EndDay != null)
            {
                list.Add(EndDay);
            }

            if (Settings != null)
            {
                list.Add(Settings);
            }

            _navigableChoices = list.ToArray();
        }

        private void UpdateChoiceSelection()
        {
            if (_navigableChoices == null || _navigableChoices.Length == 0)
            {
                return;
            }

            for (int i = 0; i < _navigableChoices.Length; i++)
            {
                if (_navigableChoices[i] == null)
                {
                    continue;
                }

                if (i == currentIndex)
                {
                    _navigableChoices[i].Select();
                }
                else
                {
                    _navigableChoices[i].Deselect();
                }
            }
        }

        #endregion
    }
}
