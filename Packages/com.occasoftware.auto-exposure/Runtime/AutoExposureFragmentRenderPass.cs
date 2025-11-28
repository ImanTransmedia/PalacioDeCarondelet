using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System;
using UnityEngine.Experimental.Rendering;




#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace OccaSoftware.AutoExposure.Runtime
{
    /// <summary>
    /// Conducts Auto Exposure using the Fragment Shader approach.
    /// </summary>
    internal sealed class AutoExposureFragmentRenderPass : ScriptableRenderPass
    {
        private const string profilerTag = "[Auto Exposure] Fragment Pass";
        private const string cmdBufferName = "AEPass";

        //private RTHandle source;

        private RTHandle target;
        private RTHandle[] downscaledSource = new RTHandle[3];
        private RTHandle perFrameDataRt = null;

        private Material calculateExposure = null;
        private Material applyExposure = null;
        private Material blitData = null;
        private Material blitScreen = null;

        private const string calculateExposurePath =
          "OccaSoftware/AutoExposure/FragmentCalculateExposure";
        private const string applyExposurePath = "OccaSoftware/AutoExposure/FragmentApply";
        private const string blitDataPath = "OccaSoftware/AutoExposure/FragmentBlitData";
        private const string blitScreenPath = "OccaSoftware/AutoExposure/BlitScreen";
        private const string sourceID = "_Source";
        private Dictionary<Camera, RenderTexture> cameraTextureMapping = new Dictionary<Camera, RenderTexture>();

        private AutoExposureOverride autoExposure;

        private bool isFirst = true;

        private const string persistentDataId = "_AutoExposureDataPrevious";
        private const string outputId = "_AutoExposureResults";
        private const string perFrameDataId = "_AutoExposureData";
        private const string downscaleId = "_DownscaleResults";

        public AutoExposureFragmentRenderPass()
        {
            cameraTextureMapping = new Dictionary<Camera, RenderTexture>();
            target = RTHandles.Alloc(Shader.PropertyToID(outputId), name: outputId);
            for (int i = 0; i < downscaledSource.Length; i++)
            {
                downscaledSource[i] = RTHandles.Alloc(
                  Shader.PropertyToID(downscaleId + i),
                  name: downscaleId + i
                );
            }
        }

        public void Setup(AutoExposureOverride autoExposure)
        {
            this.autoExposure = autoExposure;
        }

        private void SetupMaterials()
        {
            GetShaderAndSetupMaterial(calculateExposurePath, ref calculateExposure);
            GetShaderAndSetupMaterial(applyExposurePath, ref applyExposure);
            GetShaderAndSetupMaterial(blitDataPath, ref blitData);
            GetShaderAndSetupMaterial(blitScreenPath, ref blitScreen);
        }

        /// <summary>
        /// Grabs the shader from path string and creates the material.
        /// If the material is already assigned, does nothing.
        /// If the path is null or invalid, does nothing.
        /// </summary>
        /// <param name="path">The shader path</param>
        /// <param name="material">The material to setup.</param>
        private void GetShaderAndSetupMaterial(string path, ref Material material)
        {
            if (material != null)
                return;

            Shader s = Shader.Find(path);
            if (s != null)
            {
                material = CoreUtils.CreateEngineMaterial(s);
            }
            else
            {
                Debug.Log("Missing shader reference at " + path);
            }
        }

        private void ClearRenderTexture(RenderTexture renderTexture)
        {
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = rt;
        }

#if UNITY_2023_3_OR_NEWER
        private class PassData
        {
            internal TextureHandle source;
            internal Camera camera;
            internal bool isSceneViewType;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Setting up the render pass in RenderGraph
            using (var builder = renderGraph.AddUnsafePass<PassData>(profilerTag, out var passData))
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                ConfigurePass(cameraData.cameraTargetDescriptor, cameraData.camera);

                passData.source = resourceData.cameraColor;
                passData.camera = cameraData.camera;
                passData.isSceneViewType = cameraData.cameraType == CameraType.SceneView;

                builder.UseTexture(passData.source, AccessFlags.ReadWrite);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

            ExecutePass(cmd, data.source, data.camera, data.isSceneViewType);
        }
#endif

#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
            ConfigurePass(renderingData.cameraData.cameraTargetDescriptor, renderingData.cameraData.camera);
        }

        public void ConfigurePass(RenderTextureDescriptor cameraTextureDescriptor, Camera camera)
        {
            SetupMaterials();

            RenderTextureDescriptor targetDescriptor = cameraTextureDescriptor;
            targetDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            targetDescriptor.msaaSamples = 1;
            targetDescriptor.depthBufferBits = 0;
            targetDescriptor.sRGB = false;

            RenderingUtilsHelper.ReAllocateIfNeeded(
              ref target,
              targetDescriptor,
              FilterMode.Point,
              TextureWrapMode.Clamp,
              name: outputId
            );

            // Luminance Setup
            RenderTextureDescriptor luminanceDescriptor = cameraTextureDescriptor;
            luminanceDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            luminanceDescriptor.sRGB = false;
            luminanceDescriptor.width = 1; 
            luminanceDescriptor.height = 1;
            luminanceDescriptor.depthBufferBits = 0;

            if (perFrameDataRt == null)
            {
                perFrameDataRt = RTHandles.Alloc(1, 1, 1, DepthBits.None, GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: false, name: camera + " Per Frame Luminance Data");
            }

            ClearRenderTexture(perFrameDataRt);

            if (!cameraTextureMapping.TryGetValue(camera, out RenderTexture _))
            {
                RTHandle rt = RTHandles.Alloc(1, 1, 1, DepthBits.None, GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: false, name: camera + " Per Frame Luminance Data Previous");

                ClearRenderTexture(rt);
                cameraTextureMapping.Add(camera, rt);
            }

            RenderTextureDescriptor tempDescriptor = cameraTextureDescriptor;
            tempDescriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            tempDescriptor.msaaSamples = 1;
            tempDescriptor.sRGB = false;
            tempDescriptor.depthBufferBits = 0;

            for (int i = 0; i < downscaledSource.Length; i++)
            {
                tempDescriptor.width /= 2;
                tempDescriptor.height /= 2;

                RenderingUtilsHelper.ReAllocateIfNeeded(
                  ref downscaledSource[i],
                  tempDescriptor,
                  FilterMode.Bilinear,
                  TextureWrapMode.Clamp,
                  name: downscaleId + i
                );
            }

        }

#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            UnityEngine.Profiling.Profiler.BeginSample(profilerTag);
            CommandBuffer cmd = CommandBufferPool.Get(cmdBufferName);

            ExecutePass(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.camera, renderingData.cameraData.cameraType == CameraType.SceneView);

            // Execute and Clear
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        public void ExecutePass(CommandBuffer cmd, RTHandle source, Camera camera, bool isSceneViewCamera)
        {
            //ConfigureTarget(source);
            cmd.SetRenderTarget(source);
            /*
            Steps:
            Get and write the Auto Exposure parameters.
            Load the previous data to the global texture defines.

            Write to the current data texture.

            Write to the output target using the data texture vars.
            Write to the screen.

            Write to the persistent data texture for next frame.
            */

            // Setup
            RenderTargetIdentifier persistentDataRTIdentifier = new RenderTargetIdentifier(
              cameraTextureMapping[camera]
            );

            // Load the previous Luminance
            cmd.SetGlobalTexture(persistentDataId, persistentDataRTIdentifier);
            WriteShaderParams(ref cmd, isSceneViewCamera);

            // Downscale
            cmd.SetGlobalTexture(sourceID, source);
            Blitter.BlitCameraTexture(cmd, source, downscaledSource[0], blitScreen, 0);

            cmd.SetGlobalTexture(sourceID, downscaledSource[0]);
            Blitter.BlitCameraTexture(cmd, source, downscaledSource[1], blitScreen, 0);

            cmd.SetGlobalTexture(sourceID, downscaledSource[1]);
            Blitter.BlitCameraTexture(cmd, source, downscaledSource[2], blitScreen, 0);

            // Calculate the auto exposure data (by rendering to the _AutoExposureData texture).
            RTHandle perFrameData = RTHandles.Alloc(perFrameDataRt);

            cmd.SetGlobalTexture(sourceID, downscaledSource[2]);
            Blitter.BlitCameraTexture(cmd, source, perFrameData, calculateExposure, 0);

            // Apply the Exposure
            cmd.SetGlobalTexture(sourceID, source);
            cmd.SetGlobalTexture(perFrameDataId, perFrameData);
            Blitter.BlitCameraTexture(cmd, source, target, applyExposure, 0);

            // Write to Screen
            cmd.SetGlobalTexture(sourceID, target);
            Blitter.BlitCameraTexture(cmd, target, source, blitScreen, 0);

            // Write to the previous luminance for next frame
            RTHandle persistentData = RTHandles.Alloc(
              cameraTextureMapping[camera]
            );
            Blitter.BlitCameraTexture(cmd, source, persistentData, blitData, 0);
        }

        /// <summary>
        /// Sets global shader variables corresponding to the property values in the interpolated auto exposure volume stack component.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="isSceneView"></param>
        private void WriteShaderParams(ref CommandBuffer cmd, bool isSceneView)
        {
            // Non-Compute-specific
            cmd.SetGlobalInteger(ShaderParams._SampleCount, autoExposure.sampleCount.value);
            cmd.SetGlobalFloat(ShaderParams._Response, autoExposure.response.value);
            cmd.SetGlobalInteger(
              ShaderParams._ClampingEnabled,
              (int)autoExposure.clampingEnabled.value
            );
            cmd.SetGlobalFloat(ShaderParams._ClampingBracket, autoExposure.clampingBracket.value);
            cmd.SetGlobalInteger(
              ShaderParams._AnimateSamplePositions,
              (int)autoExposure.animateSamplePositions.value
            );

            cmd.SetGlobalInteger(ShaderParams._IsFirstFrame, isFirst ? 1 : 0);

            AutoExposureAdaptationMode adaptationMode = autoExposure.adaptationMode.value;
            // Progressive rendering doesn't perform accurately for scene view because of how deltatime works in editor.
            // So, we force the adaptation mode to instant.
            if (isSceneView)
                adaptationMode = AutoExposureAdaptationMode.Instant;

            // Progressive rendering doesn't work for game view when play mode is off.
            // So, we force the adaptation mode to instant.
            if (!isSceneView && !Application.isPlaying)
                adaptationMode = AutoExposureAdaptationMode.Instant;

            // General

            cmd.SetGlobalInteger(
              ShaderParams._MeteringMaskMode,
              (int)autoExposure.meteringMaskMode.value
            );
            cmd.SetGlobalTexture(
              ShaderParams._MeteringMaskTexture,
              autoExposure.meteringMaskTexture.value
            );
            cmd.SetGlobalFloat(
              ShaderParams._MeteringProceduralFalloff,
              autoExposure.meteringProceduralFalloff.value
            );
            cmd.SetGlobalInteger(ShaderParams._AdaptationMode, (int)adaptationMode);
            cmd.SetGlobalFloat(ShaderParams._FixedCompensation, autoExposure.evCompensation.value);
            cmd.SetGlobalFloat(ShaderParams._DarkToLightSpeed, autoExposure.darkToLightSpeed.value);
            cmd.SetGlobalFloat(ShaderParams._LightToDarkSpeed, autoExposure.lightToDarkSpeed.value);
            cmd.SetGlobalFloat(ShaderParams._EvMin, autoExposure.evMin.value);
            cmd.SetGlobalFloat(ShaderParams._EvMax, autoExposure.evMax.value);
            cmd.SetGlobalTexture(
              ShaderParams._ExposureCompensationCurve,
              autoExposure.compensationCurveParameter.value.GetTexture()
            );
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            isFirst = false;
        }

        internal void Dispose()
        {
            target?.Release();
            target = null;

            //source?.Release();
            //source = null;

            downscaledSource[0]?.Release();
            downscaledSource[0] = null;

            downscaledSource[1]?.Release();
            downscaledSource[1] = null;

            perFrameDataRt?.Release();
            perFrameDataRt = null;

            cameraTextureMapping.Clear();
        }

        private static class ShaderParams
        {
            public static int _SampleCount = Shader.PropertyToID("_SampleCount");
            public static int _Response = Shader.PropertyToID("_Response");
            public static int _ClampingEnabled = Shader.PropertyToID("_ClampingEnabled");
            public static int _ClampingBracket = Shader.PropertyToID("_ClampingBracket");
            public static int _AnimateSamplePositions = Shader.PropertyToID("_AnimateSamplePositions");
            public static int _IsFirstFrame = Shader.PropertyToID("_IsFirstFrame");

            public static int _MeteringMaskMode = Shader.PropertyToID("_MeteringMaskMode");
            public static int _MeteringMaskTexture = Shader.PropertyToID("_MeteringMaskTexture");
            public static int _MeteringProceduralFalloff = Shader.PropertyToID(
              "_MeteringProceduralFalloff"
            );
            public static int _AdaptationMode = Shader.PropertyToID("_AdaptationMode");
            public static int _FixedCompensation = Shader.PropertyToID("_FixedCompensation");
            public static int _DarkToLightSpeed = Shader.PropertyToID("_DarkToLightSpeed");
            public static int _LightToDarkSpeed = Shader.PropertyToID("_LightToDarkSpeed");
            public static int _EvMin = Shader.PropertyToID("_EvMin");
            public static int _EvMax = Shader.PropertyToID("_EvMax");
            public static int _ExposureCompensationCurve = Shader.PropertyToID(
              "_ExposureCompensationCurve"
            );
        }
    }
}
