Shader "PigTycoon/OriginalWorld/SurfaceLit3D"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Bump Scale", Float) = 1.0
        _HasNormalMap ("Has Normal Map", Float) = 0.0
        _RoughnessMap ("Roughness Map", 2D) = "white" {}
        _HasRoughnessMap ("Has Roughness Map", Float) = 0.0
        _SecondaryMap ("Secondary / Emission Map", 2D) = "white" {}
        _HasSecondaryMap ("Has Secondary Map", Float) = 0.0
        _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.25
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _SubsurfaceWrap ("Subsurface Wrap", Range(0, 0.6)) = 0.08
        _AlphaClip ("Alpha Clip", Float) = 0.0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.35
        _Surface ("Surface", Float) = 0.0
        _SrcBlend ("SrcBlend", Float) = 1.0
        _DstBlend ("DstBlend", Float) = 0.0
        _ZWrite ("ZWrite", Float) = 1.0
        _Cull ("Cull", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_SecondaryMap);
            SAMPLER(sampler_SecondaryMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _EmissionColor;
                float _BumpScale;
                float _HasNormalMap;
                float _HasRoughnessMap;
                float _HasSecondaryMap;
                float _Smoothness;
                float _Metallic;
                float _SubsurfaceWrap;
                float _AlphaClip;
                float _Cutoff;
            CBUFFER_END

            float4 _PigTycoonSunDir;
            float4 _PigTycoonSunColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = float4(normInputs.tangentWS, input.tangentOS.w);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input, half facing : VFACE) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 texSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                float4 baseCol = texSample * _BaseColor;

                if (_AlphaClip > 0.5)
                {
                    clip(baseCol.a - _Cutoff);
                }

                float3 N = normalize(input.normalWS) * (facing >= 0 ? 1.0 : -1.0);
                if (_HasNormalMap > 0.5)
                {
                    float3 T = normalize(input.tangentWS.xyz);
                    float3 B = cross(N, T) * input.tangentWS.w;
                    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                    N = normalize(normalTS.x * T + normalTS.y * B + normalTS.z * N);
                }

                float smoothness = _Smoothness;
                if (_HasRoughnessMap > 0.5)
                {
                    float roughSample = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r;
                    smoothness = saturate((1.0 - roughSample) * max(_Smoothness * 1.6, 0.25));
                }

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

                float ndotl = dot(N, L);
                float isChar = saturate((_SubsurfaceWrap - 0.09) * 6.0);
                float stdWrap = saturate((ndotl + _SubsurfaceWrap) / (1.0 + _SubsurfaceWrap));
                float animeRamp = lerp(0.56, 1.04, smoothstep(-0.22, 0.28, ndotl));
                float wrap = lerp(stdWrap, animeRamp, isChar);
                float terminator = smoothstep(-0.25, 0.10, ndotl) * (1.0 - smoothstep(0.05, 0.42, ndotl));
                float3 sssTint = lerp(float3(1.0, 1.0, 1.0), float3(1.04, 0.78, 0.68), isChar * terminator + saturate(_SubsurfaceWrap * 1.5) * saturate(1.0 - max(0.0, ndotl)) * 0.45);

                float dayBrightness = saturate(dot(lightCol, float3(0.33, 0.33, 0.33)));
                float ambientScale = lerp(0.42, 1.0, dayBrightness);
                float upW = saturate(N.y);
                float downW = saturate(-N.y);
                float eqW = 1.0 - upW - downW;
                float3 ambient = (upW * float3(0.48, 0.54, 0.62)
                               + eqW * float3(0.44, 0.41, 0.38)
                               + downW * float3(0.32, 0.28, 0.24)) * ambientScale;

                float3 V = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 H = normalize(L + V);
                float specPower = exp2(3.0 + smoothness * 6.0);
                float specNorm = (specPower + 2.0) * 0.008 * smoothness * smoothness;
                float spec = pow(saturate(dot(N, H)), specPower) * specNorm * saturate(ndotl * 2.0);
                float3 specColor = lerp(float3(1.0, 0.97, 0.92), baseCol.rgb, _Metallic);

                // ZZZ / Endfield / WuWa Stylized Fresnel Rim Light on characters, pigs & monsters
                float ndotv = saturate(dot(N, V));
                float rim = pow(1.0 - ndotv, 3.6) * saturate(0.35 + 0.65 * ndotl) * isChar * 0.32;
                float3 rimColor = lerp(lightCol, baseCol.rgb * 1.35 + float3(0.25, 0.22, 0.18), 0.45);

                float3 diffuse = baseCol.rgb * (1.0 - _Metallic * 0.75) * (ambient + lightCol * wrap * sssTint * 0.78);
                float3 emit = _EmissionColor.rgb;
                if (_HasSecondaryMap > 0.5)
                {
                    emit *= SAMPLE_TEXTURE2D(_SecondaryMap, sampler_SecondaryMap, input.uv).rgb;
                }
                float3 lit = diffuse + spec * specColor * lightCol + rim * rimColor + emit;

                float dist = length(_WorldSpaceCameraPos - input.positionWS);
                float fogFactor = saturate((dist - 140.0) / 300.0);
                float3 fogCol = lerp(float3(0.12, 0.16, 0.28), float3(0.76, 0.83, 0.90), dayBrightness);
                lit = lerp(lit, fogCol, fogFactor * 0.65);

                return half4(lit, baseCol.a);
            }
            ENDHLSL
        }
    }
}
