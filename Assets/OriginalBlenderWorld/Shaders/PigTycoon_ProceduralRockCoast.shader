Shader "PigTycoon/URP/ProceduralRockCoast"
{
    Properties
    {
        [Header(Ramp Colors)]
        _RampColor0 ("Crevice / Basal Strata Color", Color) = (0.09, 0.052, 0.028, 1.0)
        _RampColor1 ("Weathered Midtone Strata", Color) = (0.36, 0.225, 0.118, 1.0)
        _RampColor2 ("Exposed Crest / Sunlit Rim", Color) = (0.58, 0.395, 0.205, 1.0)
        _RampPos0 ("Ramp Stop 0", Range(0, 1)) = 0.18
        _RampPos1 ("Ramp Stop 1", Range(0, 1)) = 0.52
        _RampPos2 ("Ramp Stop 2", Range(0, 1)) = 0.84

        [Header(Strata and Noise Parameters)]
        _StrataFrequency ("Sedimentary Band Frequency", Float) = 4.5
        _NoiseFrequency ("Weathering Stipple Frequency", Float) = 2.8
        _StrataDistortion ("Band Warp Amount", Float) = 0.35
        _BumpStrength ("Procedural Normal Depth", Range(0, 2)) = 0.85

        [Header(Surface Properties)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma target 3.5
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RampColor0;
                float4 _RampColor1;
                float4 _RampColor2;
                float _RampPos0;
                float _RampPos1;
                float _RampPos2;
                float _StrataFrequency;
                float _NoiseFrequency;
                float _StrataDistortion;
                float _BumpStrength;
                float _Smoothness;
                float _Metallic;
            CBUFFER_END

            float4 _PigTycoonSunDir;
            float4 _PigTycoonSunColor;

            float Hash31(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(lerp(Hash31(i + float3(0,0,0)), Hash31(i + float3(1,0,0)), u.x),
                                 lerp(Hash31(i + float3(0,1,0)), Hash31(i + float3(1,1,0)), u.x), u.y),
                            lerp(lerp(Hash31(i + float3(0,0,1)), Hash31(i + float3(1,0,1)), u.x),
                                 lerp(Hash31(i + float3(0,1,1)), Hash31(i + float3(1,1,1)), u.x), u.y), u.z);
            }

            float EvaluateHeightField(float3 posWS)
            {
                // Multi-octave 3D weathering noise without high-frequency sine wrapping (eliminates zebra bullseye rings)
                float nMacro = ValueNoise3D(posWS * 0.14 + float3(3.7, 1.2, 8.5));
                float nMed   = ValueNoise3D(posWS * 0.55 + float3(11.3, 5.4, 2.1));
                float nFine  = ValueNoise3D(posWS * 1.85);
                float fbm = nMacro * 0.55 + nMed * 0.30 + nFine * 0.15;

                // Gentle horizontal sedimentary bedding (clamped frequency & warp so strata never fold into concentric rings)
                float strataFreq = clamp(_StrataFrequency * 0.22, 0.25, 1.15);
                float warp = clamp(_StrataDistortion * 0.18, 0.12, 0.75);
                float strata = sin(posWS.y * strataFreq + (nMacro - 0.5) * warp * 2.4) * 0.5 + 0.5;

                return saturate(fbm * 0.68 + strata * 0.32);
            }

            float3 EvaluateColorRamp(float fac)
            {
                fac = saturate(fac);
                // Ensure warm weathered limestone/sandstone floor (preventing harsh black crevices)
                float3 c0 = max(_RampColor0.rgb, float3(0.36, 0.29, 0.21));
                float3 c1 = max(_RampColor1.rgb, float3(0.58, 0.49, 0.37));
                float3 c2 = max(_RampColor2.rgb, float3(0.76, 0.67, 0.53));

                if (fac <= _RampPos0) return c0;
                if (fac >= _RampPos2) return c2;
                if (fac < _RampPos1)
                {
                    float t = smoothstep(_RampPos0, max(_RampPos0 + 0.05, _RampPos1), fac);
                    return lerp(c0, c1, t);
                }
                else
                {
                    float t = smoothstep(_RampPos1, max(_RampPos1 + 0.05, _RampPos2), fac);
                    return lerp(c1, c2, t);
                }
            }

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 normalWS = normalize(input.normalWS);

                float eps = 0.02;
                float hC = EvaluateHeightField(input.positionWS);
                float hX = EvaluateHeightField(input.positionWS + float3(eps, 0, 0));
                float hY = EvaluateHeightField(input.positionWS + float3(0, eps, 0));
                float hZ = EvaluateHeightField(input.positionWS + float3(0, 0, eps));

                float3 grad = float3(hX - hC, hY - hC, hZ - hC) / eps;
                float3 perturbedNormalWS = normalize(normalWS - grad * _BumpStrength * 0.15);

                float slope = saturate(dot(normalWS, float3(0, 1, 0)));
                float rampFac = saturate(hC * 0.65 + slope * 0.35);
                float3 albedo = EvaluateColorRamp(rampFac);

                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;
                float3 lightCol = mainLight.color;
                if (dot(_PigTycoonSunColor.rgb, _PigTycoonSunColor.rgb) > 0.001)
                {
                    L = normalize(_PigTycoonSunDir.xyz);
                    lightCol = _PigTycoonSunColor.rgb;
                }
                else if (dot(L, L) < 0.001 || dot(lightCol, lightCol) < 0.001)
                {
                    L = normalize(float3(0.528, 0.616, -0.585));
                    lightCol = float3(1.02, 0.96, 0.88);
                }
                else
                {
                    L = normalize(L);
                }

                float dayBrightness = saturate(dot(lightCol, float3(0.33, 0.33, 0.33)));
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float NdotL = saturate((dot(perturbedNormalWS, L) + 0.18) / 1.18);
                float3 directDiffuse = albedo * NdotL * lightCol * 0.84;

                float3 halfDir = normalize(L + viewDir);
                float NdotH = saturate(dot(perturbedNormalWS, halfDir));
                float specPower = exp2(10.0 * _Smoothness + 1.0);
                float3 specColor = lerp(float3(0.04, 0.04, 0.04), albedo, _Metallic);
                float3 directSpecular = pow(NdotH, specPower) * specColor * lightCol * NdotL * 0.32;

                float3 sh = max(SampleSH(perturbedNormalWS), float3(0.32, 0.30, 0.28)) * lerp(0.42, 1.0, dayBrightness);
                float3 ambient = sh * albedo;
                float3 finalColor = directDiffuse + directSpecular + ambient;

                float dist = length(_WorldSpaceCameraPos - input.positionWS);
                float fogFactor = saturate((dist - 140.0) / 300.0);
                float3 fogCol = lerp(float3(0.12, 0.16, 0.28), float3(0.76, 0.83, 0.90), dayBrightness);
                finalColor = lerp(finalColor, fogCol, fogFactor * 0.65);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
