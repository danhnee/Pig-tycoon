Shader "PigTycoon/URP/WaterSurface"
{
    Properties
    {
        [Header(Water Colors)]
        _ShallowColor ("Shallow Aqua Tone", Color) = (0.20, 0.47, 0.39, 0.85)
        _DeepColor ("Abyssal Deep Ocean Blue", Color) = (0.04, 0.22, 0.32, 0.94)
        _DepthDistance ("Depth Gradient Distance", Float) = 3.5

        [Header(Wave Animation)]
        _WaveSpeed ("Wave Propagation Speed", Float) = 0.45
        _WaveFrequency ("Wave Spatial Frequency", Float) = 1.8
        _RippleScale ("Fine Ripple Scale", Float) = 6.0
        _NormalPerturbation ("Surface Normal Distortion", Range(0, 1)) = 0.28

        [Header(Shore Foam and Specular)]
        _FoamColor ("Foam Edge Color", Color) = (0.85, 0.95, 0.95, 1.0)
        _FoamDistance ("Foam Contact Width", Float) = 0.25
        _FresnelPower ("Fresnel Exponent", Range(1, 10)) = 4.0
        _Smoothness ("Water Surface Smoothness", Range(0, 1)) = 0.92
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Geometry+60"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 300
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite On
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "ForwardWater"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex WaterPassVertex
            #pragma fragment WaterPassFragment
            #pragma target 3.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float  fogFactor    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float _DepthDistance;
                float _WaveSpeed;
                float _WaveFrequency;
                float _RippleScale;
                float _NormalPerturbation;
                float _FoamDistance;
                float _FresnelPower;
                float _Smoothness;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash21(i + float2(0.0, 0.0)), Hash21(i + float2(1.0, 0.0)), u.x),
                    lerp(Hash21(i + float2(0.0, 1.0)), Hash21(i + float2(1.0, 1.0)), u.x),
                    u.y);
            }

            Varyings WaterPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.fogFactor = ComputeFogFactor(vertexInputs.positionCS.z);
                return output;
            }

            half4 WaterPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 posWS = input.positionWS;
                float time = _Time.y * max(0.35, _WaveSpeed);
                float freq = max(0.6, _WaveFrequency * 0.45);

                // Multi-directional organic wave field (eliminates grid regularity)
                float2 uv1 = posWS.xz * freq + float2(time * 0.75, time * 0.42);
                float2 uv2 = mul(float2x2(0.80, -0.60, 0.60, 0.80), posWS.xz * (freq * 1.65)) + float2(-time * 0.62, time * 0.88);
                float2 uv3 = mul(float2x2(0.60, 0.80, -0.80, 0.60), posWS.xz * max(1.8, _RippleScale * 0.42)) + float2(time * 1.15, -time * 0.95);

                float n1 = ValueNoise(uv1);
                float n2 = ValueNoise(uv2);
                float n3 = ValueNoise(uv3);

                float waveHeight = n1 * 0.50 + n2 * 0.32 + n3 * 0.18;
                float dHx = (ValueNoise(uv1 + float2(0.18, 0.0)) - n1) * 1.6 + (ValueNoise(uv2 + float2(0.18, 0.0)) - n2) * 1.2;
                float dHz = (ValueNoise(uv1 + float2(0.0, 0.18)) - n1) * 1.6 + (ValueNoise(uv2 + float2(0.0, 0.18)) - n2) * 1.2;

                float3 geomNormalWS = normalize(input.normalWS);
                float perturb = clamp(_NormalPerturbation, 0.18, 0.65);
                float3 normalWS = normalize(geomNormalWS + float3(-dHx * perturb, 0.0, -dHz * perturb));

                // Pond & shoreline shallow-ring enhancement (Image 2 & Image 3 turquoise shore to sapphire center)
                float dPond1 = length(posWS.xz - float2(-22.0, -15.0));
                float dPond2 = length(posWS.xz - float2(-14.5, -10.5));
                float dPond3 = length(posWS.xz - float2(-27.5, -19.2));
                float minPondDist = min(dPond1, min(dPond2, dPond3));
                float pondShoreFactor = saturate((minPondDist - 2.2) / 4.2);

                // Coastal shallow turquoise shelf near Harbor Town & river banks
                float coastalBand = ValueNoise(posWS.xz * 0.035 + float2(3.1, 7.4));
                float shallowMix = saturate(waveHeight * 0.58 + coastalBand * 0.35 + (minPondDist < 12.0 ? pondShoreFactor * 0.45 : 0.15));

                float3 shallowRgb = max(_ShallowColor.rgb, float3(0.16, 0.62, 0.74));
                float3 deepRgb = max(_DeepColor.rgb, float3(0.06, 0.32, 0.54));
                float3 waterRgb = lerp(deepRgb, shallowRgb, shallowMix);

                // Waterfall cascade detection (steep normal or high shallow brightness)
                float isCascade = saturate((1.0 - abs(geomNormalWS.y)) * 2.2);
                float foamNoise = ValueNoise(posWS.xz * 2.4 + float2(0.0, time * 2.6));
                float crestFoam = smoothstep(0.74, 0.93, waveHeight) * 0.35 + isCascade * smoothstep(0.35, 0.75, foamNoise) * 0.68;

                // Soft shoreline foam ring inside the 3 meadow ponds
                if (minPondDist < 9.5)
                {
                    float ringFoam = smoothstep(4.2, 5.8, minPondDist) * (1.0 - smoothstep(5.8, 6.8, minPondDist));
                    crestFoam = max(crestFoam, ringFoam * (0.45 + 0.35 * n3));
                }

                waterRgb = lerp(waterRgb, _FoamColor.rgb, saturate(crestFoam));

                float3 viewDir = normalize(GetCameraPositionWS() - posWS);
                float NdotV = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - NdotV, clamp(_FresnelPower, 2.2, 5.0));

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 halfDir = normalize(lightDir + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));

                float specSharp = pow(NdotH, 96.0) * _Smoothness * 0.85;
                float specBroad = pow(NdotH, 16.0) * _Smoothness * 0.22;

                // Sky reflection gradient (azure sky + golden horizon warmth)
                float3 skyReflect = lerp(float3(0.38, 0.68, 0.88), float3(0.82, 0.88, 0.94), fresnel);
                float3 finalRgb = lerp(waterRgb, skyReflect, fresnel * 0.48)
                                + (specSharp + specBroad) * mainLight.color * float3(1.05, 0.98, 0.88);

                finalRgb = MixFog(finalRgb, input.fogFactor);
                float finalAlpha = saturate(max(0.92, _DeepColor.a) + fresnel * 0.06);
                return half4(finalRgb, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
