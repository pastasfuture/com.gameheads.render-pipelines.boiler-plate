Shader "Gameheads/Opaque"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EmissiveTexture("EmissiveTexture", 2D) = "black" {}
        _EmissiveIntensity("_EmissiveIntensity", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {

            Tags { "LightMode" = "GameheadsOpaque" }

            // TODO: Create variant for transparency:
            // Tags { "LightMode" = "GameheadsTransparent" }
            // Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x gles
            #pragma target 4.5

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex Vertex
            #pragma fragment Fragment
            
            #include "Packages/com.gameheads.render-pipelines.boiler-plate/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.gameheads.render-pipelines.boiler-plate/Runtime/ShaderLibrary/ShaderFunctions.hlsl"

            struct Attributes
            {
                float4 vertexOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };


            struct Varyings
            {
                float4 vertexCS : SV_POSITION;
                float3 uvw : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionVS : TEXCOORD2;
                float3 lighting : TEXCOORD3;
                float fogAlpha : TEXCOORD4;
            };
            
            Varyings Vertex(Attributes v)
            {
                Varyings o;

                // Common useful suffix conventions used in unity SRPs:
                //
                // Object-Space = "OS"
                // World-Space = "WS"
                // View-Space (aka Camera-Space) = "VS"
                // Clip-Space = "CS"
                float3 positionWS = TransformObjectToWorld(v.vertexOS.xyz);
                float3 positionVS = TransformWorldToView(positionWS);
                float4 positionCS = TransformWorldToHClip(positionWS);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.vertexCS = positionCS;
                o.positionVS = positionVS;
                o.normalWS = normalWS;

                float2 uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvw = float3(uv.x, uv.y, 1.0f);

                // Sample fake / painted lighting from the vertex color data.
                // Vertex colors can be painted inside of unity using the Polybrush package availible in the package manager.
                // Window->Package Manager->Polybrush
                o.lighting = v.color.rgb;

                // Calculate fog opacity based on our view space depth (distance from camera in world units). 
                o.fogAlpha = saturate(abs(positionVS.z) * _FogDistanceScaleBias.x + _FogDistanceScaleBias.y);

                return o;
            }
            
            float4 Fragment(Varyings i) : SV_Target
            {
                float2 uv = i.uvw.xy;
                float3 normalWS = normalize(i.normalWS);
                float4 color = SAMPLE_TEXTURE2D(_MainTex, s_point_clamp_sampler, uv);

                // Perform alpha cutoff transparency (i.e: discard pixels in the holes of a chain link fence texture, or in the negative space of a leaf texture).
                // Any alpha value < 0.5 will trigger the pixel to be discarded, any alpha value greater than or equal to 0.5 will trigger the pixel to be preserved.
                clip(color.a * 2.0f - 1.0f);

                // Compute lighting from a single, hardcoded directional light source.
                const float3 hardcodedLightColor = float3(0.9254902, 0.8175606, 0.4238745);
                const float3 hardcodedLightDirection = normalize(float3(1.0f, 1.0f, 1.0f));
                float3 pixelLighting = hardcodedLightColor * saturate(dot(normalWS, hardcodedLightDirection));
                
                // Blend per-pixel hardcoded light source with painted vertex color based lighting.
                float3 vertexLighting = i.lighting;
                float3 lighting = pixelLighting;
                color.rgb *= lighting;

                // Add lighting from emissive material.
                float3 emissive = SAMPLE_TEXTURE2D(_EmissiveTexture, s_point_clamp_sampler, uv).rgb;
                emissive *= _EmissiveIntensity;
                color.xyz += emissive;

                // Blend final color with fog.
                float4 fog = float4(_FogColor.rgb, i.fogAlpha);
                color.rgb = lerp(color.rgb, fog.rgb, fog.a);

                return color;
            }
            ENDHLSL
        }
    }
}
