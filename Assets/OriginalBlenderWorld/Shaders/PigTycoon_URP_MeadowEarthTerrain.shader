Shader "PigTycoon/URP/MeadowEarthTerrain"
{
    Properties
    {
        _SoilRichColor("Rich Loam Earth", Color) = (0.47, 0.33, 0.20, 1)
        _SoilDryColor("Sunbaked Ochre Earth", Color) = (0.60, 0.44, 0.26, 1)
        _TurfGoldColor("Meadow Turf Gold", Color) = (0.69, 0.56, 0.25, 1)
        _TurfOliveColor("Meadow Turf Olive", Color) = (0.51, 0.45, 0.21, 1)
        _PathDirtColor("Worn Dirt Path", Color) = (0.41, 0.28, 0.17, 1)
        _WetBankColor("Pond Wet Bank", Color) = (0.27, 0.20, 0.14, 1)
        _CliffSlopeColor("Steep Slope Limestone", Color) = (0.58, 0.48, 0.36, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SoilRichColor;
                half4 _SoilDryColor;
                half4 _TurfGoldColor;
                half4 _TurfOliveColor;
                half4 _PathDirtColor;
                half4 _WetBankColor;
                half4 _CliffSlopeColor;
            CBUFFER_END

            float4 _PigTycoonSunDir;
            float4 _PigTycoonSunColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

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
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FbmSoil(float2 p)
            {
                float v = 0.0;
                v += 0.500 * ValueNoise(p); p = p * 2.03 + float2(17.1, 31.7);
                v += 0.250 * ValueNoise(p); p = p * 2.01 + float2(-11.3, 23.9);
                v += 0.125 * ValueNoise(p); p = p * 2.02 + float2(7.7, -19.1);
                v += 0.0625 * ValueNoise(p);
                return v / 0.9375;
            }

            float DistSeg(float2 p, float2 a, float2 b)
            {
                float2 ab = b - a;
                float t = saturate(dot(p - a, ab) / max(0.0001, dot(ab, ab)));
                return length(p - (a + t * ab));
            }

            float MinPathDistance(float2 xz)
            {
                float d = 1e5;
                // Path A: West House -> Pasture -> Observatory
                d = min(d, DistSeg(xz, float2(-55.0, 20.0), float2(-42.0, 16.5)));
                d = min(d, DistSeg(xz, float2(-42.0, 16.5), float2(-29.0, 11.8)));
                d = min(d, DistSeg(xz, float2(-29.0, 11.8), float2(-17.5, 11.5)));
                d = min(d, DistSeg(xz, float2(-17.5, 11.5), float2(-6.0, 11.5)));
                d = min(d, DistSeg(xz, float2(-6.0, 11.5),  float2(6.5, 13.5)));
                d = min(d, DistSeg(xz, float2(6.5, 13.5),   float2(18.5, 17.5)));
                d = min(d, DistSeg(xz, float2(18.5, 17.5),  float2(28.0, 26.5)));
                d = min(d, DistSeg(xz, float2(28.0, 26.5),  float2(34.8, 34.0)));
                // Path B: Crossroads -> Lab -> North
                d = min(d, DistSeg(xz, float2(-17.5, 11.5), float2(-18.5, 23.5)));
                d = min(d, DistSeg(xz, float2(-18.5, 23.5), float2(-19.0, 36.0)));
                d = min(d, DistSeg(xz, float2(-19.0, 36.0), float2(-8.0, 48.0)));
                d = min(d, DistSeg(xz, float2(-8.0, 48.0),  float2(4.0, 62.0)));
                // Path C: South Pond Loop & Active Pasture Trail
                d = min(d, DistSeg(xz, float2(-29.0, 11.8), float2(-35.5, -10.5)));
                d = min(d, DistSeg(xz, float2(-35.5, -10.5), float2(-31.0, -24.0)));
                d = min(d, DistSeg(xz, float2(-31.0, -24.0), float2(-18.0, -34.0)));
                d = min(d, DistSeg(xz, float2(-18.0, -34.0), float2(-4.0, -28.0)));
                d = min(d, DistSeg(xz, float2(-4.0, -28.0),  float2(5.0, -14.0)));
                d = min(d, DistSeg(xz, float2(-6.0, -14.0),  float2(12.5, -12.0)));
                d = min(d, DistSeg(xz, float2(5.0, -14.0),   float2(6.5, 13.5)));
                // Path D: Southeast Spur
                d = min(d, DistSeg(xz, float2(6.5, 13.5),   float2(22.0, 5.0)));
                d = min(d, DistSeg(xz, float2(22.0, 5.0),   float2(52.5, -4.5)));
                return d;
            }

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = posInputs.positionCS;
                o.positionWS = posInputs.positionWS;
                o.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float2 xz = input.positionWS.xz;
                float3 N = normalize(input.normalWS);

                // 1. Base Earth & Warm Ochre-Loam (Image 3 warm golden-ochre & olive-gold meadow plateau)
                float soilMacro = FbmSoil(xz * 0.085);
                float soilMicro = FbmSoil(xz * 0.65);
                half3 warmLoam  = max(_SoilRichColor.rgb, half3(0.54, 0.43, 0.25));
                half3 warmOchre = max(_SoilDryColor.rgb,  half3(0.68, 0.55, 0.31));
                half3 earthCol  = lerp(warmLoam, warmOchre, smoothstep(0.25, 0.75, soilMacro));
                earthCol *= lerp(0.92h, 1.08h, (half)soilMicro);

                // 2. Lush Golden-Olive Meadow Turf Carpet (Image 3 rich golden pasture sward)
                float macroPatch = ValueNoise(xz * 0.07 + float2(13.7, -7.3));
                float microTuft  = ValueNoise(xz * 0.24 + float2(-31.1, 19.4));
                float clumpScore = macroPatch * 0.60 + microTuft * 0.40;

                half3 oliveSward = max(_TurfOliveColor.rgb, half3(0.56, 0.52, 0.24));
                half3 goldSward  = max(_TurfGoldColor.rgb,  half3(0.78, 0.66, 0.30));
                half3 sunlitGold = half3(0.85, 0.73, 0.36);
                half3 turfCol    = lerp(oliveSward, goldSward, smoothstep(0.22, 0.68, clumpScore));
                turfCol          = lerp(turfCol, sunlitGold, smoothstep(0.62, 0.90, microTuft) * 0.45h);

                // Keep 82-92% of the meadow covered in lush golden-olive turf (no bare brown mud desert in pasture)
                float turfMask = smoothstep(0.16, 0.42, clumpScore) * 0.88 + 0.10;
                half3 surfaceCol = lerp(earthCol, turfCol, (half)saturate(turfMask));

                // 3. Winding Golden-Ochre Footpaths (1.5m core + soft grassy shoulder matching Image 3)
                float pathDist = MinPathDistance(xz) + (soilMicro - 0.5) * 0.45;
                float pathMask = (1.0 - smoothstep(0.85, 2.05, pathDist)) * 0.78;
                half3 pathCol  = lerp(half3(0.58, 0.45, 0.28), warmOchre, smoothstep(0.2, 1.0, abs(pathDist - 0.65)));
                surfaceCol = lerp(surfaceCol, pathCol, (half)pathMask);

                // 4. Stepped Pond Warm Stone Rim (tight around the 3 meadow ponds, no dark mud swamp)
                float dP1 = length(xz - float2(-22.0, -15.0)) / 7.2;
                float dP2 = length(xz - float2(-14.5, -10.5)) / 5.4;
                float dP3 = length(xz - float2(-27.5, -19.2)) / 5.0;
                float pondNorm = min(min(dP1, dP2), dP3);
                float wetBankMask = 1.0 - smoothstep(0.78, 1.15, pondNorm);
                half3 pondStoneCol = max(_WetBankColor.rgb, half3(0.48, 0.42, 0.32));
                surfaceCol = lerp(surfaceCol, pondStoneCol, (half)(wetBankMask * 0.65));

                // 5. Steep Slope Warm Limestone transition
                float slopeMask = 1.0 - smoothstep(0.65, 0.90, saturate(N.y));
                half3 cliffCol  = max(_CliffSlopeColor.rgb, half3(0.68, 0.58, 0.44));
                surfaceCol = lerp(surfaceCol, cliffCol, (half)slopeMask);

                // 6. Perturb normal slightly with soil micro-grain for tactile earth richness
                float3 dN = float3((soilMicro - 0.5) * 0.14, 0.0, (soilMacro - 0.5) * 0.14);
                N = normalize(N + dN);

                // 7. Directional Light + Warm Sky/Ground Ambient
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

                half dayBrightness = (half)saturate(dot(lightCol, float3(0.33, 0.33, 0.33)));
                half ndotl = (half)saturate(dot(N, L));
                half3 ambient = lerp(half3(0.32, 0.25, 0.20), half3(0.48, 0.43, 0.35), saturate((half)N.y * 0.5h + 0.5h)) * lerp(0.42h, 1.0h, dayBrightness);
                half3 lit = surfaceCol * (ambient + (half3)lightCol * (ndotl * 0.88h));

                float dist = length(_WorldSpaceCameraPos - input.positionWS);
                half fogFactor = (half)saturate((dist - 140.0) / 300.0);
                half3 fogCol = lerp(half3(0.12, 0.16, 0.28), half3(0.76, 0.83, 0.90), dayBrightness);
                lit = lerp(lit, fogCol, fogFactor * 0.65h);

                return half4(lit, 1.0h);
            }
            ENDHLSL
        }
    }
}
