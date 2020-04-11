using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace Gameheads.RenderPipelines.BoilerPlate.Runtime
{
    internal class GameheadsRenderPipeline : UnityEngine.Rendering.RenderPipeline
    {
        readonly GameheadsRenderPipelineAsset m_Asset;
        internal GameheadsRenderPipelineAsset asset { get { return m_Asset; }}

        internal const PerObjectData k_RendererConfigurationBakedLighting = PerObjectData.LightProbe | PerObjectData.Lightmaps | PerObjectData.LightProbeProxyVolume;
        internal const PerObjectData k_RendererConfigurationBakedLightingWithShadowMask = k_RendererConfigurationBakedLighting | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume | PerObjectData.ShadowMask;

        Material postProcessingMaterial;
        
        internal GameheadsRenderPipeline(GameheadsRenderPipelineAsset asset)
        {
            m_Asset = asset;
            Build();
            Allocate();
        }

        internal protected void Build()
        {
            ConfigureGlobalRenderPipelineTag();
            ConfigureSRPBatcherFromAsset(m_Asset);
        }

        static void ConfigureGlobalRenderPipelineTag()
        {
            // https://docs.unity3d.com/ScriptReference/Shader-globalRenderPipeline.html
            // Set globalRenderPipeline so that only subshaders with Tags{ "RenderPipeline" = "GameheadsRenderPipeline" } will be rendered.
            Shader.globalRenderPipeline = GameheadsStringConstants.s_GlobalRenderPipelineStr;
        }

        static void ConfigureSRPBatcherFromAsset(GameheadsRenderPipelineAsset asset)
        {
            if (asset.isSRPBatcherEnabled)
            {
                GraphicsSettings.useScriptableRenderPipelineBatching = true;
            }
        }

        internal protected void Allocate()
        {
            this.postProcessingMaterial = CoreUtils.CreateEngineMaterial(m_Asset.renderPipelineResources.shaders.postProcessingPS);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            CoreUtils.Destroy(postProcessingMaterial);
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            if (cameras.Length == 0) { return; }

            UnityEngine.Rendering.RenderPipeline.BeginFrameRendering(context, cameras);

            // Loop over all active cameras in the scene and render them.
            foreach (var camera in cameras)
            {
                if (camera == null) { continue; }

                // S E T U P
                UnityEngine.Rendering.RenderPipeline.BeginCameraRendering(context, camera);

                // TODO: Should we move this after we set the rasterization render target so that scene view UI is also pixelated?
                DrawSceneViewUI(camera);

                ScriptableCullingParameters cullingParameters;
                if (!camera.TryGetCullingParameters(camera.stereoEnabled, out cullingParameters)) { continue; }

                // Need to update the volume manager for the current camera before querying any volume parameter results.
                // This triggers the volume manager to blend volume parameters spatially, based on the camera position. 
                VolumeManager.instance.Update(camera.transform, camera.cullingMask);

                // Compute the list of visible render meshes and light sources that are inside the camera's view.
                CullingResults cullingResults = context.Cull(ref cullingParameters);

                // Setup camera for rendering (sets render target, view/projection matrices and other per-camera built-in shader variables).
                context.SetupCameraProperties(camera);

                
                // R E N D E R   S C E N E
                var cmd = CommandBufferPool.Get(GameheadsStringConstants.s_CommandBufferRenderForwardStr);
                int renderTargetWidth = camera.pixelWidth;
                int renderTargetHeight = camera.pixelHeight;
                RenderTexture rasterizationRT = RenderTexture.GetTemporary(renderTargetWidth, renderTargetHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 1, RenderTextureMemoryless.None, VRTextureUsage.None, false); 
                cmd.SetRenderTarget(rasterizationRT);
                {
                    PushGlobalRasterizationParameters(camera, cmd, renderTargetWidth, renderTargetHeight);
                    PushFogParameters(camera, cmd);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Release();
                    
                    DrawOpaque(context, camera, ref cullingResults);
                    // DrawTransparent(context, camera, ref cullingResults);
                    // TODO: DrawSkybox(context, camera);
                    DrawLegacyCanvasUI(context, camera, ref cullingResults);
                    DrawGizmos(context, camera, GizmoSubset.PreImageEffects);
                    DrawGizmos(context, camera, GizmoSubset.PostImageEffects);
                }


                // R E N D E R   P O S T   P R O C E S S I N G
                cmd = CommandBufferPool.Get(GameheadsStringConstants.s_CommandBufferRenderPostProcessStr);
                cmd.SetRenderTarget(camera.targetTexture);
                {
                    PushGlobalPostProcessingParameters(camera, cmd, m_Asset, rasterizationRT, renderTargetWidth, renderTargetHeight);
                    PushTonemapperParameters(camera, cmd);
                    GameheadsRenderPipeline.DrawFullScreenQuad(cmd, postProcessingMaterial);
                }


                // C L E A N   U P
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
                context.Submit();
                RenderTexture.ReleaseTemporary(rasterizationRT);
                UnityEngine.Rendering.RenderPipeline.EndCameraRendering(context, camera);
            }

            UnityEngine.Rendering.RenderPipeline.EndFrameRendering(context, cameras);
        }

        static bool IsMainGameView(Camera camera)
        {
            return camera.cameraType == CameraType.Game && camera.targetTexture == null; 
        }

        static Color GetFogColorFromFogVolume()
        {
            var volumeSettings = VolumeManager.instance.stack.GetComponent<FogVolume>();
            if (!volumeSettings) volumeSettings = FogVolume.@default;
            return volumeSettings.color.value;
        }

        static void PushTonemapperParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, GameheadsProfilingSamplers.s_PushTonemapperParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<TonemapperVolume>();
                if (!volumeSettings) volumeSettings = TonemapperVolume.@default;

                cmd.SetGlobalFloat(GameheadsShaderIDs._TonemapperSaturation, volumeSettings.saturation.value);
            }
        }

        static void PushFogParameters(Camera camera, CommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, GameheadsProfilingSamplers.s_PushFogParameters))
            {
                var volumeSettings = VolumeManager.instance.stack.GetComponent<FogVolume>();
                if (!volumeSettings) volumeSettings = FogVolume.@default;

                Vector2 fogDistanceScaleBias = new Vector2(
                    1.0f / (volumeSettings.distanceMax.value - volumeSettings.distanceMin.value), 
                    -volumeSettings.distanceMin.value / (volumeSettings.distanceMax.value - volumeSettings.distanceMin.value)
                );

                cmd.SetGlobalVector(GameheadsShaderIDs._FogColor, new Vector4(volumeSettings.color.value.r, volumeSettings.color.value.g, volumeSettings.color.value.b, volumeSettings.color.value.a));
                cmd.SetGlobalVector(GameheadsShaderIDs._FogDistanceScaleBias, fogDistanceScaleBias);
            }
        }

        static void PushGlobalRasterizationParameters(Camera camera, CommandBuffer cmd, int rasterizationWidth, int rasterizationHeight)
        {
            using (new ProfilingScope(cmd, GameheadsProfilingSamplers.s_PushGlobalRasterizationParameters))
            {
                // Clear background to fog color to create seamless blend between forward-rendered fog, and "sky" / infinity.
                cmd.ClearRenderTarget(clearDepth: true, clearColor: true, backgroundColor: GetFogColorFromFogVolume());
                cmd.SetGlobalVector(GameheadsShaderIDs._ScreenSize, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalVector(GameheadsShaderIDs._Time, new Vector4(Time.timeSinceLevelLoad / 20.0f, Time.timeSinceLevelLoad, Time.timeSinceLevelLoad * 2.0f, Time.timeSinceLevelLoad * 3.0f));
            }  
        }

        static void PushGlobalPostProcessingParameters(Camera camera, CommandBuffer cmd, GameheadsRenderPipelineAsset asset, RenderTexture rasterizationRT, int rasterizationWidth, int rasterizationHeight)
        {
            using (new ProfilingScope(cmd, GameheadsProfilingSamplers.s_PushGlobalPostProcessingParameters))
            {
                bool flipY = IsMainGameView(camera);
                cmd.SetGlobalInt(GameheadsShaderIDs._FlipY, flipY ? 1 : 0);
                cmd.SetGlobalVector(GameheadsShaderIDs._UVTransform, flipY ? new Vector4(1.0f, -1.0f, 0.0f, 1.0f) : new Vector4(1.0f,  1.0f, 0.0f, 0.0f));
                cmd.SetGlobalVector(GameheadsShaderIDs._ScreenSize, new Vector4(camera.pixelWidth, camera.pixelHeight, 1.0f / (float)camera.pixelWidth, 1.0f / (float)camera.pixelHeight));
                cmd.SetGlobalVector(GameheadsShaderIDs._FrameBufferScreenSize, new Vector4(rasterizationWidth, rasterizationHeight, 1.0f / (float)rasterizationWidth, 1.0f / (float)rasterizationHeight));
                cmd.SetGlobalTexture(GameheadsShaderIDs._FrameBufferTexture, rasterizationRT);

                cmd.SetGlobalVector(GameheadsShaderIDs._Time, new Vector4(Time.timeSinceLevelLoad / 20.0f, Time.timeSinceLevelLoad, Time.timeSinceLevelLoad * 2.0f, Time.timeSinceLevelLoad * 3.0f));
            }
        }

        static Texture2D GetFramebufferDitherTexFromAsset(GameheadsRenderPipelineAsset asset)
        {

            if (asset.renderPipelineResources.textures.framebufferDitherTex == null)
            {
                return Texture2D.grayTexture;
            }

            return asset.renderPipelineResources.textures.framebufferDitherTex;
        }

        static void DrawSceneViewUI(Camera camera)
        {
        #if UNITY_EDITOR
            // Emit scene view UI
            if (camera.cameraType == CameraType.SceneView)
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        #endif
        }

        static void DrawOpaque(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            // Draw opaque objects

            // Controls what order the objects are rendered in.
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };
            var drawingSettings = new DrawingSettings(GameheadsShaderPassNames.s_Opaque, sortingSettings);
            
            // Controls what objects will be rendered at this call.
            // This filter specifies that we only want to render opaque objects.
            var filteringSettings = new FilteringSettings()
            {
                renderQueueRange = RenderQueueRange.opaque,
                layerMask = camera.cullingMask, // Respect the culling mask specified on the camera so that users can selectively omit specific layers from rendering to this camera.
                renderingLayerMask = UInt32.MaxValue, // Everything
                excludeMotionVectorObjects = false
            };

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        static void DrawTransparent(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            // Draw transparent objects.

            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };

            var drawingSettings = new DrawingSettings(GameheadsShaderPassNames.s_Transparent, sortingSettings);
            
            var filteringSettings = new FilteringSettings()
            {
                renderQueueRange = RenderQueueRange.transparent,
                layerMask = camera.cullingMask, // Respect the culling mask specified on the camera so that users can selectively omit specific layers from rendering to this camera.
                renderingLayerMask = UInt32.MaxValue, // Everything
                excludeMotionVectorObjects = false
            };

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        }

        static void DrawSkybox(ScriptableRenderContext context, Camera camera)
        {
            context.DrawSkybox(camera);
        }

        static void DrawLegacyCanvasUI(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
        {
            // Draw legacy Canvas UI meshes.
            var sortingSettings = new SortingSettings(camera);
            var drawSettings = new DrawingSettings(GameheadsShaderPassNames.s_SRPDefaultUnlit, sortingSettings);
            var filterSettings = FilteringSettings.defaultValue;
            context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
        }

        // In order to render a fullscreen post process, we simply draw a plane, facing the camera, that covers the entire viewport with the post process shader.
        // This utility function creates this [-1, 1] sized mesh in code for us.
        static Mesh s_FullscreenMesh = null;
        static Mesh fullscreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 0.0f;
                float bottomV = 1.0f;

                s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_FullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                s_FullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_FullscreenMesh.UploadMeshData(true);
                return s_FullscreenMesh;
            }
        }

        // Draws a fullscreen quad (to maintain webgl build support).
        static void DrawFullScreenQuad(CommandBuffer cmd, Material material,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            cmd.DrawMesh(GameheadsRenderPipeline.fullscreenMesh, Matrix4x4.identity, material);
        }

        static void DrawGizmos(ScriptableRenderContext context, Camera camera, GizmoSubset gizmoSubset)
        {
    #if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos())
                context.DrawGizmos(camera, gizmoSubset);
        #endif
        }
    }
}