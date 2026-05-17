using UnityEngine;

namespace Turnroot.Utilities.Weather
{
    public partial class SceneSkyboxSetter : MonoBehaviour
    {
        #region Material Instancing

        private void InstantiateCelMaterialsForRenderers()
        {
            if (CelMaterials == null || CelMaterials.Length == 0)
            {
                return;
            }

            var celSet = new HashSet<Material>(CelMaterials);

            InstantiateMaterials(celSet);
            ProcessRenderers(celSet);
        }

        private void InstantiateMaterials(HashSet<Material> celSet)
        {
            foreach (var mat in CelMaterials)
            {
                if (mat == null)
                {
                    continue;
                }

                if (!_celMaterialInstances.ContainsKey(mat))
                {
                    var inst = Instantiate(mat);
                    inst.hideFlags = HideFlags.DontSave;
                    _celMaterialInstances[mat] = inst;
                }
            }
        }

        private void ProcessRenderers(HashSet<Material> celSet)
        {
            foreach (
                var renderer in FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                )
            )
            {
                if (renderer == null)
                {
                    continue;
                }

                var shared = renderer.sharedMaterials;
                bool hasCelMaterial = false;
                for (int i = 0; i < shared.Length; i++)
                {
                    if (shared[i] != null && celSet.Contains(shared[i]))
                    {
                        hasCelMaterial = true;
                        break;
                    }
                }

                if (!hasCelMaterial || _celRendererOriginalMaterials.ContainsKey(renderer))
                {
                    continue;
                }

                _celRendererOriginalMaterials[renderer] = shared;

                var instanced = (Material[])shared.Clone();
                for (int i = 0; i < instanced.Length; i++)
                {
                    if (instanced[i] == null || !celSet.Contains(shared[i]))
                    {
                        continue;
                    }

                    if (!_celMaterialInstances.TryGetValue(shared[i], out var runtimeMat))
                    {
                        runtimeMat = Instantiate(shared[i]);
                        runtimeMat.hideFlags = HideFlags.DontSave;
                        _celMaterialInstances[shared[i]] = runtimeMat;
                    }

                    instanced[i] = runtimeMat;
                }

                renderer.materials = instanced;
            }
        }

        private void RestoreCelMaterials()
        {
            foreach (var kvp in _celRendererOriginalMaterials)
            {
                var renderer = kvp.Key;
                if (renderer == null)
                {
                    continue;
                }

                renderer.sharedMaterials = kvp.Value;
            }

            _celRendererOriginalMaterials.Clear();

            foreach (var runtimeMat in _celMaterialInstances.Values)
            {
                if (runtimeMat != null)
                {
                    Destroy(runtimeMat);
                }
            }
            _celMaterialInstances.Clear();
        }

        #endregion
    }
}
