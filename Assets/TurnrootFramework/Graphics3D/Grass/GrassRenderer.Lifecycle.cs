namespace Turnroot.Graphics3D
{
    public partial class GrassRenderer
    {
        // ── Lifecycle ─────────────────────────────────────────────────────────────
        private void OnEnable() => Init();

        private void OnDisable() => ReleaseBuffers();

        private void OnDestroy()
        {
            ReleaseBuffers();
            DestroyMesh(ref _bladeMesh);
            DestroyMesh(ref _planeMesh);
        }

        private void LateUpdate()
        {
            if (
                !grassEnabled
                || _allBladesBuffer == null
                || computeShader == null
                || grassMaterial == null
                || _totalBladeCount == 0
            )
            {
                return;
            }

            UnityEngine.Camera cam = GetRenderCamera();
            if (cam == null)
            {
                return;
            }

            DrawGroup(
                _allBladesBuffer,
                _visibleBladesBuffer,
                _indirectArgsBuffer,
                _groundUVsBuffer,
                _totalBladeCount,
                _bladeMesh,
                grassMaterial,
                cam
            );

            if (_extraGroups != null)
            {
                foreach (var g in _extraGroups)
                {
                    DrawGroup(
                        g.all,
                        g.visible,
                        g.args,
                        g.groundUVs,
                        g.totalCount,
                        _planeMesh,
                        g.material,
                        cam
                    );
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────
        public void SetGrassEnabled(bool enabled) => grassEnabled = enabled;

        // ── Camera resolution ─────────────────────────────────────────────────────
        private UnityEngine.Camera GetRenderCamera()
        {
            if (targetCamera != null && targetCamera.isActiveAndEnabled)
            {
                _resolvedCamera = targetCamera;
                return targetCamera;
            }

            if (_resolvedCamera != null && _resolvedCamera.isActiveAndEnabled)
            {
                return _resolvedCamera;
            }

            if (UnityEngine.Camera.main != null)
            {
                _resolvedCamera = UnityEngine.Camera.main;
                return _resolvedCamera;
            }

            foreach (var c in UnityEngine.Camera.allCameras)
            {
                if (c.isActiveAndEnabled)
                {
                    _resolvedCamera = c;
                    return _resolvedCamera;
                }
            }

            _resolvedCamera = null;

            return null;
        }
    }
}
