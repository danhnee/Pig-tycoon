Shader "PigTycoon/URP/ObjectSpaceProcedural"
{
    Properties
    {
        [Header(Ramp Colors)]
        _RampColor0 ("Dark Grain / Crevice Tone", Color) = (0.4225, 0.3445, 0.221, 1.0)
        _RampColor1 ("Light Fiber / Surface Tone", Color) = (0.7475, 0.6095, 0.391, 1.0)
        _RampPos0 ("Ramp Stop 0", Range(0, 1)) = 0.0
        _RampPos1 ("Ramp Stop 1", Range(0, 1)) = 1.0

        [Header(Object Space Procedural Settings)]
        _ObjectScale ("Object Coordinate Frequency", Float) = 3.5
        _GrainStretch ("Directional Grain Stretch Y", Float) = 1.0
        _BumpStrength ("Bump Depth", Range(0, 2)) = 0.5

        [Header(Surface Properties)]
        _Smoothness ("Smoothness", Range(0, 1)) = 0.25
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
                float3 positionOS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RampColor0;
                float4 _RampColor1;
                float _RampPos0;
                float _RampPos1;
                float _ObjectScale;
                float _GrainStretch;
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

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = vertexInputs.positionCS;
                output.positionWS = vertexInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normalInputs.normalWS;
                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float3 scaledObjPos = input.positionOS * _ObjectScale;
                scaledObjPos.y /= max(0.1, _GrainStretch);

                float noiseVal = ValueNoise3D(scaledObjPos);
                float rampFac = saturate((noiseVal - _RampPos0) / max(0.001, _RampPos1 - _RampPos0));
                float3 albedo = lerp(_RampColor0.rgb, _RampColor1.rgb, rampFac);

                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);

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
                float NdotL = saturate((dot(normalWS, L) + 0.18) / 1.18);
                float3 directDiffuse = albedo * NdotL * lightCol * 0.84;

                float3 halfDir = normalize(L + viewDir);
                float NdotH = saturate(dot(normalWS, halfDir));
                float specPower = exp2(10.0 * _Smoothness + 1.0);
                float3 specColor = lerp(float3(0.04, 0.04, 0.04), albedo, _Metallic);
                float3 directSpecular = pow(NdotH, specPower) * specColor * lightCol * NdotL * 0.32;

                float3 sh = max(SampleSH(normalWS), float3(0.32, 0.30, 0.28)) * lerp(0.42, 1.0, dayBrightness);
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
