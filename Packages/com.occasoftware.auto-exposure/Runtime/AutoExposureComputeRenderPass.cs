using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.XR;
using System;



#if UNITY_2023_3_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace OccaSoftware.AutoExposure.Runtime
{
    /// <summary>
    /// Conducts Auto Exposure using the Compute Shader approach. Requires SM4.5+.
    /// </summary>
    internal sealed class AutoExposureComputeRenderPass : ScriptableRenderPass
    {
        private const string profilerTag = "[Auto Exposure] Compute Pass";
        private const string cmdBufferName = "AEPass";

        private const string shaderName = "os-AutoExposureCompute";

        private ComputeShader computeShader;
        private ComputeBuffer constDataBuffer;

        private int mainKernel;
        private int updateKernel;
        private int rtKernel;
        private bool isFirst = true;

        private uint threadGroupSizeX;
        private uint threadGroupSizeY;
        private RTHandle aeHandle;
        private const string handleId = "AutoExposureHandle";

        private Dictionary<Camera, ComputeBuffer> camBufferPairs = new Dictionary<Camera, ComputeBuffer>();
        private AutoExposureOverride autoExposure;


        public AutoExposureComputeRenderPass()
        {
            aeHandle = RTHandles.Alloc(Shader.PropertyToID(handleId), name: handleId);
            constDataBuffer = new ComputeBuffer(
              1,
              sizeof(int) * 3 + sizeof(float) * 7 + sizeof(uint) * 2
            );

#if UNITY_EDITOR
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnAssemblyReload;
#endif
        }

        public void Setup(AutoExposureOverride autoExposure)
        {
            this.autoExposure = autoExposure;
        }

        public bool LoadComputeShader()
        {
            computeShader = (ComputeShader)Resources.Load(shaderName);
            if (computeShader == null)
                return false;

            return true;
        }

        private void OnAssemblyReload()
        {
            Dispose();
        }

#if UNITY_2023_3_OR_NEWER
        private class PassData
        {
            internal TextureHandle source;
            internal RenderTextureDescriptor renderTextureDescriptor;
            internal Camera camera;
            internal bool isSceneViewType;
            internal bool isXRRendering;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Setting up the render pass in RenderGraph
            using (var builder = renderGraph.AddUnsafePass<PassData>(profilerTag, out var passData))
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                RenderTextureDescriptor renderTextureDescriptor = ConfigurePass(cameraData.cameraTargetDescriptor, cameraData.camera);

                passData.source = resourceData.cameraColor;
                passData.renderTextureDescriptor = renderTextureDescriptor;
                passData.camera = cameraData.camera;
                passData.isSceneViewType = cameraData.cameraType == CameraType.SceneView;
                passData.isXRRendering = cameraData.xrRendering;
                 
                builder.UseTexture(passData.source, AccessFlags.ReadWrite);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }

        private void ExecutePass(PassData data, UnsafeGraphContext context)
        {
            CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

            ExecutePass(cmd, data.source, data.renderTextureDescriptor, data.camera, data.isSceneViewType, data.isXRRendering);
        }
#endif

#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigurePass(renderingData.cameraData.cameraTargetDescriptor, renderingData.cameraData.camera);
        }

        public RenderTextureDescriptor ConfigurePass(RenderTextureDescriptor cameraTextureDescriptor, Camera camera)
        {
            RenderTextureDescriptor descriptor = cameraTextureDescriptor;

            SetupCameraBufferPair(camera);
            SetupRenderTexture(descriptor);
            SetupKernel();

            void SetupCameraBufferPair(Camera camera)
            {
                if (!camBufferPairs.TryGetValue(camera, out ComputeBuffer _))
                {
                    camBufferPairs.Add(camera, new ComputeBuffer(1, sizeof(uint) * 2 + sizeof(float) * 2));
                }
            }

            void SetupRenderTexture(RenderTextureDescriptor descriptor)
            {
                descriptor.enableRandomWrite = true;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;
                descriptor.width = Mathf.Max(descriptor.width, 1);
                descriptor.height = Mathf.Max(descriptor.height, 1);

                RenderingUtilsHelper.ReAllocateIfNeeded(
                  ref aeHandle,
                  descriptor,
                  FilterMode.Point,
                  TextureWrapMode.Clamp,
                  name: handleId
                );
            }

            void SetupKernel()
            {
                mainKernel = computeShader.FindKernel("AutoExposure");
                updateKernel = computeShader.FindKernel("UpdateTargetLum");
                rtKernel = computeShader.FindKernel("AdjustExposure");
                computeShader.GetKernelThreadGroupSizes(
                  mainKernel,
                  out threadGroupSizeX,
                  out threadGroupSizeY,
                  out _
                );
            }

            return descriptor;
        }

        private struct ConstantData
        {
            public ConstantData(
              float evMin,
              float evMax,
              float evCompensation,
              int adaptationMode,
              float darkToLightInterp,
              float lightToDarkInterp,
              float deltaTime,
              uint screenSizeX,
              uint screenSizeY,
              int isFirstFrame,
              int meteringMaskMode,
              float meteringProceduralFalloff
            )
            {
                this.evMin = evMin;
                this.evMax = evMax;
                this.evCompensation = evCompensation;
                this.adaptationMode = adaptationMode;
                this.darkToLightInterp = darkToLightInterp;
                this.lightToDarkInterp = lightToDarkInterp;
                this.deltaTime = deltaTime;
                this.screenSizeX = screenSizeX;
                this.screenSizeY = screenSizeY;
                this.isFirstFrame = isFirstFrame;
                this.meteringMaskMode = meteringMaskMode;
                this.meteringProceduralFalloff = meteringProceduralFalloff;
            }

            public float evMin;
            public float evMax;
            public float evCompensation;
            public int adaptationMode;
            public float darkToLightInterp;
            public float lightToDarkInterp;
            public float deltaTime;
            public uint screenSizeX;
            public uint screenSizeY;
            public int isFirstFrame;
            public int meteringMaskMode;
            public float meteringProceduralFalloff;
        };

        private static class ShaderParams
        {
            public static int MeteringMaskTexture = Shader.PropertyToID("MeteringMaskTexture");
            public static int Constants = Shader.PropertyToID("Constants");
            public static int Data = Shader.PropertyToID("Data");
            public static int _AutoExposureTarget = Shader.PropertyToID("_AutoExposureTarget");

            public static int ExposureCompensationCurve = Shader.PropertyToID(
              "ExposureCompensationCurve"
            );
        }

#if UNITY_2023_3_OR_NEWER
        [Obsolete]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            UnityEngine.Profiling.Profiler.BeginSample(profilerTag);
            CommandBuffer cmd = CommandBufferPool.Get(cmdBufferName);

            ExecutePass(cmd, renderingData.cameraData.renderer.cameraColorTargetHandle, renderingData.cameraData.cameraTargetDescriptor, renderingData.cameraData.camera, renderingData.cameraData.cameraType == CameraType.SceneView, renderingData.cameraData.xrRendering);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);

            UnityEngine.Profiling.Profiler.EndSample();
        }

        public void ExecutePass(CommandBuffer cmd, RTHandle source, RenderTextureDescriptor cameraTextureDescriptor, Camera camera, bool isSceneViewCamera, bool isXRRendering)
        {
            RenderTextureDescriptor desc = cameraTextureDescriptor;
            desc.width = (int)(ScalableBufferManager.widthScaleFactor * desc.width);
            desc.height = (int)(ScalableBufferManager.heightScaleFactor * desc.height);
            bool isSceneView = isSceneViewCamera;
            bool xrRendering = isXRRendering;

            ExecuteComputeShader(cmd);

            void ExecuteComputeShader(CommandBuffer cmd)
            {
                Blitter.BlitCameraTexture(cmd, source, aeHandle);
                SetupConstantData();

                int groupsX = GetGroupCount(desc.width, threadGroupSizeX);
                int groupsY = GetGroupCount(desc.height, threadGroupSizeY);

                RenderTargetIdentifier meteringMaskIdentifier = new RenderTargetIdentifier(
                  autoExposure.meteringMaskTexture.value
                );
                cmd.SetComputeTextureParam(
                  computeShader,
                  mainKernel,
                  ShaderParams.MeteringMaskTexture,
                  meteringMaskIdentifier
                );

                cmd.SetComputeBufferParam(computeShader, mainKernel, ShaderParams.Constants, constDataBuffer);
                cmd.SetComputeBufferParam(computeShader, mainKernel, ShaderParams.Data, camBufferPairs[camera]);
                cmd.SetComputeTextureParam(computeShader, mainKernel, ShaderParams._AutoExposureTarget, aeHandle.nameID);

                int computeShaderPassCount = 1;
                if (xrRendering && XRSettings.stereoRenderingMode == XRSettings.StereoRenderingMode.SinglePassInstanced)
                {
                    computeShaderPassCount = 2;
                }
                cmd.DispatchCompute(computeShader, mainKernel, groupsX, groupsY, computeShaderPassCount);

                RenderTargetIdentifier exposureCompensationCurveId = new RenderTargetIdentifier(
                  autoExposure.compensationCurveParameter.value.GetTexture()
                );
                cmd.SetComputeTextureParam(computeShader, updateKernel, ShaderParams.ExposureCompensationCurve, exposureCompensationCurveId);
                cmd.SetComputeBufferParam(computeShader, updateKernel, ShaderParams.Constants, constDataBuffer);
                cmd.SetComputeBufferParam(computeShader, updateKernel, ShaderParams.Data, camBufferPairs[camera]);
                cmd.DispatchCompute(computeShader, updateKernel, 1, 1, 1);

                cmd.SetComputeBufferParam(computeShader, rtKernel, ShaderParams.Constants, constDataBuffer);
                cmd.SetComputeBufferParam(computeShader, rtKernel, ShaderParams.Data, camBufferPairs[camera]);
                cmd.SetComputeTextureParam(computeShader, rtKernel, ShaderParams._AutoExposureTarget, aeHandle.nameID);
                cmd.DispatchCompute(computeShader, rtKernel, groupsX, groupsY, computeShaderPassCount);

                Blitter.BlitCameraTexture(cmd, aeHandle, source);

                int GetGroupCount(int textureDimension, uint groupSize)
                {
                    return Mathf.CeilToInt((textureDimension + groupSize - 1) / groupSize);
                }

                void SetupConstantData()
                {
                    AutoExposureAdaptationMode adaptationMode = autoExposure.adaptationMode.value;

                    // Progressive rendering doesn't perform accurately for scene view because of how deltatime works in editor.
                    // So, we force the adaptation mode to instant.
                    if (isSceneView)
                        adaptationMode = AutoExposureAdaptationMode.Instant;

                    // Progressive rendering doesn't work for game view when play mode is off.
                    // So, we force the adaptation mode to instant.
                    if (!isSceneView && !UnityEngine.Application.isPlaying)
                        adaptationMode = AutoExposureAdaptationMode.Instant;

                    ConstantData constants = new ConstantData(
                      autoExposure.evMin.value,
                      autoExposure.evMax.value,
                      autoExposure.evCompensation.value,
                      (int)adaptationMode,
                      autoExposure.darkToLightSpeed.value,
                      autoExposure.lightToDarkSpeed.value,
                      Time.deltaTime,
                      (uint)desc.width,
                      (uint)desc.height,
                      isFirst ? 1 : 0,
                      (int)autoExposure.meteringMaskMode.value,
                      autoExposure.meteringProceduralFalloff.value
                    );

                    cmd.SetBufferData(constDataBuffer, new ConstantData[] { constants });
                }
            }
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            isFirst = false;
        }

        internal void Dispose()
        {
            foreach (ComputeBuffer buffer in camBufferPairs.Values)
            {
                buffer?.Release();
            }

            camBufferPairs.Clear();

            aeHandle?.Release();
            aeHandle = null;

            constDataBuffer?.Release();
            constDataBuffer = null;
        }
    }
}
