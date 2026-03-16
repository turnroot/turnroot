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

                foreach (var spawnPoint in sub.UnitSpawnPoints)
                {
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

        public float GetSpawnPointHeight(Transform spawnPoint, float defaultHeight)
        {
            if (spawnPoint == null)
            {
                return defaultHeight;
            }
            return _spawnPointHeights.TryGetValue(spawnPoint, out var h) ? h : defaultHeight;
        }

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

        private void UpdateChoiceSelection()
        {
            if (LocationChoices == null || LocationChoices.Length == 0)
            {
                return;
            }

            for (int i = 0; i < LocationChoices.Length; i++)
            {
                if (LocationChoices[i] == null)
                {
                    continue;
                }

                if (i == currentIndex)
                {
                    LocationChoices[i].Select();
                }
                else
                {
                    LocationChoices[i].Deselect();
                }
            }
        }

        #endregion
    }
}
