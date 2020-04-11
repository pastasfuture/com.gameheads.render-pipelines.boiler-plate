using UnityEngine;
using UnityEngine.Rendering;

namespace Gameheads.RenderPipelines.BoilerPlate.Runtime
{
    internal static class GameheadsStringConstants
    {
        public static readonly string s_PackagePath = "Packages/com.gameheads.render-pipelines.boiler-plate";
        public static readonly string s_GlobalRenderPipelineStr = "GameheadsRenderPipeline";
        public static readonly string s_CommandBufferRenderForwardStr = "GameheadsRenderPipeline.RenderForward";
        public static readonly string s_CommandBufferRenderPostProcessStr = "GameheadsRenderPipeline.PostProcessing";
    }

    internal static class GameheadsProfilingSamplers
    {
        public static readonly string s_PushGlobalRasterizationParametersStr = "Push Global Rasterization Parameters";
        public static readonly string s_PushGlobalPostProcessingParametersStr = "Push Global Post Processing Parameters";
        public static readonly string s_PushFogParametersStr = "Push Fog Parameters";
        public static readonly string s_PushTonemapperParametersStr = "Push Tonemapper Parameters";

        public static ProfilingSampler s_PushGlobalRasterizationParameters = new ProfilingSampler(s_PushGlobalRasterizationParametersStr);
        public static ProfilingSampler s_PushGlobalPostProcessingParameters = new ProfilingSampler(s_PushGlobalPostProcessingParametersStr);
        public static ProfilingSampler s_PushFogParameters = new ProfilingSampler(s_PushFogParametersStr);
        public static ProfilingSampler s_PushTonemapperParameters = new ProfilingSampler(s_PushTonemapperParametersStr);
    }

    internal static class GameheadsShaderPassNames
    {
        // ShaderPass string - use to have consistent naming through the codebase.
        public static readonly string s_OpaqueStr = "GameheadsOpaque";
        public static readonly string s_TransparentStr = "GameheadsTransparent";
        public static readonly string s_SRPDefaultUnlitStr = "SRPDefaultUnlit";

        // ShaderPass name
        public static readonly ShaderTagId s_Opaque = new ShaderTagId(s_OpaqueStr);
        public static readonly ShaderTagId s_Transparent = new ShaderTagId(s_TransparentStr);
        public static readonly ShaderTagId s_SRPDefaultUnlit = new ShaderTagId(s_SRPDefaultUnlitStr);
    }

    // Pre-hashed shader ids to avoid runtime hashing cost, runtime string manipulation, and to ensure we do not have naming conflicts across
    // all global shader uniforms.
    internal static class GameheadsShaderIDs
    {
        // Unity built in global uniforms:
        public static readonly int _ScreenSize = Shader.PropertyToID("_ScreenSize");
        public static readonly int _Time = Shader.PropertyToID("_Time");
        public static readonly int _FlipY = Shader.PropertyToID("_FlipY");
        public static readonly int _UVTransform = Shader.PropertyToID("_UVTransform");
        
        // Framebuffer (Render Target) specific uniforms.
        public static readonly int _FrameBufferTexture = Shader.PropertyToID("_FrameBufferTexture");
        public static readonly int _FrameBufferScreenSize = Shader.PropertyToID("_FrameBufferScreenSize");
        
        // Fog uniforms:
        public static readonly int _FogColor = Shader.PropertyToID("_FogColor");
        public static readonly int _FogDistanceScaleBias = Shader.PropertyToID("_FogDistanceScaleBias");

        // PostProcessing uniforms:
        public static readonly int _TonemapperSaturation = Shader.PropertyToID("_TonemapperSaturation");
        public static readonly int _WhiteNoiseTexture = Shader.PropertyToID("_WhiteNoiseTexture");
        public static readonly int _WhiteNoiseSize = Shader.PropertyToID("_WhiteNoiseSize");
    }
}
