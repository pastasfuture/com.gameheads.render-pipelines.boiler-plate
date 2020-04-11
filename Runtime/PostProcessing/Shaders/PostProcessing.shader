Shader "Hidden/Gameheads/PostProcessing"
{
    HLSLINCLUDE

    // #pragma target 4.5
    // #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #pragma prefer_hlslcc gles
    #pragma exclude_renderers d3d11_9x
    #pragma target 2.0

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

    #include "Packages/com.gameheads.render-pipelines.boiler-plate/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.gameheads.render-pipelines.boiler-plate/Runtime/ShaderLibrary/ShaderFunctions.hlsl"

    TEXTURE2D(_FrameBufferTexture);
    float4 _FrameBufferScreenSize;
    float4 _UVTransform;

    // Post Processing settings:
    float _TonemapperSaturation;

    struct Attributes
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vertex(Attributes input)
    {
        Varyings output;
        output.positionCS = input.vertex;
        output.texcoord = input.uv;
        return output;
    }

    float4 Fragment(Varyings input) : SV_Target0
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        float2 positionNDC = input.texcoord;
        uint2 positionSS = input.texcoord * _ScreenSize.xy;

        #if UNITY_SINGLE_PASS_STEREO
        positionNDC.x = (positionNDC.x + unity_StereoEyeIndex) * 0.5;
        #endif

        // Flip logic
        positionSS = positionSS * _UVTransform.xy + _UVTransform.zw * (_ScreenSize.xy - 1.0);
        positionNDC = positionNDC * _UVTransform.xy + _UVTransform.zw;

        float2 uv = positionNDC.xy;
        float4 color = SAMPLE_TEXTURE2D_LOD(_FrameBufferTexture, s_point_clamp_sampler, uv, 0);

        float colorAverage = (color.r + color.g + color.b) / 3.0;
        color.rgb = lerp(colorAverage, color.rgb, _TonemapperSaturation);

        return color;
    }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "GameheadsRenderPipeline" }

        Pass
        {
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x gles
            #pragma target 4.5

            #pragma vertex Vertex
            #pragma fragment Fragment

            ENDHLSL
        }
    }
}
