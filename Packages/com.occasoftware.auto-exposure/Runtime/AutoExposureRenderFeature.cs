using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace OccaSoftware.AutoExposure.Runtime
{
    public class AutoExposureRenderFeature : ScriptableRendererFeature
    {
        // in feature
        [System.Serializable]
        public class Settings
        {
            public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        public Settings settings;

        private AutoExposureOverride autoExposure;
        private AutoExposureComputeRenderPass computeRenderPass = null;
        private AutoExposureFragmentRenderPass fragmentRenderPass = null;

        private bool DeviceSupportsComputeShaders()
        {
            const int _COMPUTE_SHADER_LEVEL = 45;
            if (SystemInfo.graphicsShaderLevel >= _COMPUTE_SHADER_LEVEL)
                return true;

            return false;
        }

        private bool DeviceHasXRSinglePassInstancedRenderingEnabled()
        {
            if (XRSettings.enabled && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
                return true;

            return false;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged += Recreate;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += Recreate;
#endif
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += Recreate;
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= Recreate;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= Recreate;
#endif
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= Recreate;
        }

        private void Recreate(UnityEngine.SceneManagement.Scene current, UnityEngine.SceneManagement.Scene next)
        {
            Create();
        }

        public override void Create()
        {
            Setup();
        }

        /// <summary>
        /// Clears the two render passes.
        /// Initializing the pass is handled later, during AddRenderPasses.
        /// </summary>
        internal void Setup()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            computeRenderPass?.Dispose();
            computeRenderPass = null;
            fragmentRenderPass?.Dispose();
            fragmentRenderPass = null;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Get the Auto Exposure component from the Volume Manager stack.
        /// </summary>
        /// <returns>If Auto Exposure component is null or inactive, returns false.</returns>
        internal bool RegisterAutoExposureStackComponent()
        {
            autoExposure = VolumeManager.instance.stack.GetComponent<AutoExposureOverride>();
            if (autoExposure == null)
                return false;

            return autoExposure.IsActive();
        }

        private void InitializeCompute()
        {
            if (computeRenderPass == null)
            {
                computeRenderPass = new AutoExposureComputeRenderPass();
                computeRenderPass.renderPassEvent = settings.renderPassEvent;
            }

            if (fragmentRenderPass != null)
            {
                fragmentRenderPass = null;
            }
        }

        private void InitializeFragment()
        {
            if (fragmentRenderPass == null)
            {
                fragmentRenderPass = new AutoExposureFragmentRenderPass();
                fragmentRenderPass.renderPassEvent = settings.renderPassEvent;
            }

            if (computeRenderPass != null)
            {
                computeRenderPass = null;
            }
        }

        private bool warningReported = false;

        private enum PassType
        {
            Compute,
            Fragment,
            None
        }

        private PassType passType = PassType.None;

        /// <summary>
        /// Validates the auto exposure stack component.
        /// Validates the relevant render pass.
        /// Sets up the relevant render pass and enqueues it.
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="renderingData"></param>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (IsExcludedCameraType(renderingData.cameraData.camera.cameraType))
                return;

            bool isActive = RegisterAutoExposureStackComponent();
            if (!isActive)
                return;

            if (autoExposure.renderingMode.value == AutoExposureRenderingMode.Compute && DeviceSupportsComputeShaders())
            {
                if (DeviceHasXRSinglePassInstancedRenderingEnabled())
                {
                    if (!warningReported)
                    {
                        Debug.LogWarning(
                          "Rendering mode is set to compute, but Auto Exposure's compute mode is incompatible with XR SPI. Switch to Fragment mode for Auto Exposure to work."
                        );
                        warningReported = true;
                    }

                    return;
                }

                InitializeCompute();

                bool hasValidComputeShader = computeRenderPass.LoadComputeShader();
                if (!hasValidComputeShader)
                    return;

                passType = PassType.Compute;
                computeRenderPass?.Setup(autoExposure);
                renderer.EnqueuePass(computeRenderPass);
            }
            else
            {
                passType = PassType.Fragment;

                InitializeFragment();
                fragmentRenderPass?.Setup(autoExposure);
                renderer.EnqueuePass(fragmentRenderPass);
            }

            bool IsExcludedCameraType(CameraType type)
            {
                switch (type)
                {
                    case CameraType.Preview:
                    case CameraType.Reflection:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public override void SetupRenderPasses(
          ScriptableRenderer renderer,
          in RenderingData renderingData)
        {
            // Maybe do this differently?
            if (passType == PassType.Compute)
            {
                computeRenderPass?.ConfigureInput(ScriptableRenderPassInput.Color);
            }
            if (passType == PassType.Fragment)
            {
                fragmentRenderPass?.ConfigureInput(ScriptableRenderPassInput.Color);
            }
        }
    }
}
