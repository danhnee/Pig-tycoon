Shader "PigTycoon/URP/ElasticMeadowGrass"
{
    Properties
    {
        _BaseColor("Tip Gold Color", Color) = (0.85, 0.70, 0.30, 1)
        _RootEarthColor("Root Earth Blend Color", Color) = (0.46, 0.33, 0.20, 1)
        _MidOliveColor("Mid Blade Warm Ochre", Color) = (0.70, 0.56, 0.25, 1)
        _WindSpeed("Wind Wave Speed", Float) = 1.65
        _WindStrength("Wind Sway Strength", Float) = 0.085
        _ElasticStiffness("Elastic Stocking Curve", Float) = 1.65
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Cull Off

        Pass
        {
            Name "SRPDefaultUnlit"
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RootEarthColor;
                half4 _MidOliveColor;
                float _WindSpeed;
                float _WindStrength;
                float _ElasticStiffness;
            CBUFFER_END

            float4 _PigTycoonSunDir;
            float4 _PigTycoonSunColor;
            int _GrassInteractorCount;
            float4 _GrassInteractorPos[16];  // xyz = posWS, w = radius
            float4 _GrassInteractorBend[16]; // xyz = spring-damper velocity bend vector

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
                half   bladeH     : TEXCOORD2;
                half   bendAmount : TEXCOORD3;
                half   patchVar   : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);

                // Normalized height along the grass blade (LD_GrassClump height is 0..0.174m)
                float h = saturate(input.positionOS.y / 0.165);
                // Elastic stocking flex profile: base stays anchored in soil, upper blade bends in a silky curve
                float flex = pow(h, max(1.1, _ElasticStiffness));

                // 1. Smooth Traveling Meadow Wind Wave
                float windPhase = dot(posWS.xz, float2(0.32, 0.21)) + _Time.y * _WindSpeed;
                float gust = sin(windPhase) * 0.7 + cos(windPhase * 1.73 + posWS.x * 0.5) * 0.3;
                float2 windDispXZ = float2(gust, gust * 0.45) * (_WindStrength * flex);

                // 2. Elastic Spring-Damper Actor Interaction (Khoa, Man, Pigs, Raiders)
                float2 totalActorBendXZ = float2(0.0, 0.0);
                float totalSquash = 0.0;

                [unroll(12)]
                for (int i = 0; i < 12; i++)
                {
                    if (i >= _GrassInteractorCount) break;
                    float4 actorPosRad = _GrassInteractorPos[i];
                    float radius = max(0.25, actorPosRad.w);
                    float2 toBlade = posWS.xz - actorPosRad.xz;
                    float dist = length(toBlade);
                    if (dist < radius)
                    {
                        // Smooth cubic Hermite falloff (elastic stocking tension envelope)
                        float u = 1.0 - saturate(dist / radius);
                        float envelope = u * u * (3.0 - 2.0 * u);
                        float2 radialDir = dist > 0.001 ? (toBlade / dist) : float2(1.0, 0.0);

                        // Combine radial parting with the C# Hooke spring-damper oscillation vector
                        float2 springVec = _GrassInteractorBend[i].xz;
                        float2 pushXZ = (radialDir * 0.24 + springVec * 0.88) * envelope;
                        totalActorBendXZ += pushXZ;
                        totalSquash = max(totalSquash, envelope);
                    }
                }

                float2 netHorizDisp = windDispXZ + totalActorBendXZ * flex;
                float dispMag = length(netHorizDisp);

                // Arc-length preservation: bending horizontally pulls the blade tip down smoothly
                float maxLen = max(0.02, input.positionOS.y);
                float verticalDrop = dispMag * 0.55 + totalSquash * flex * 0.06;
                posWS.xz += netHorizDisp;
                posWS.y  -= min(verticalDrop, maxLen * 0.78);

                // 3. Reconstruct Upward-Hemisphere Normal (fixes broken negative normals in 004526..4529.pmb)
                float3 rawN = TransformObjectToWorldNormal(input.normalOS);
                float3 fixedN = normalize(float3(rawN.x * 0.30 + netHorizDisp.x * 2.2, abs(rawN.y) + 0.75, rawN.z * 0.30 + netHorizDisp.y * 2.2));

                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.normalWS   = fixedN;
                o.bladeH     = (half)h;
                o.bendAmount = (half)saturate(totalSquash);
                o.patchVar   = (half)frac(sin(dot(floor(posWS.xz * 0.5), float2(12.9898, 78.233))) * 43758.5453);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half h = input.bladeH;

                // Root-to-Tip Color Gradient:
                // Bottom melts into lush golden-olive meadow sward so tufts look soft, plush, and sunlit (matching Image 3)
                half3 rootOlive = max(_RootEarthColor.rgb, half3(0.54, 0.49, 0.22));
                half3 midGold   = max(_MidOliveColor.rgb,  half3(0.76, 0.65, 0.28));
                half3 tipSun    = max(_BaseColor.rgb,      half3(0.90, 0.78, 0.36));
                half3 tipCol    = lerp(tipSun, midGold, input.patchVar * 0.38h);
                half3 bladeCol  = lerp(rootOlive, midGold, smoothstep(0.0h, 0.45h, h));
                bladeCol        = lerp(bladeCol, tipCol, smoothstep(0.38h, 0.92h, h));

                // Subtle golden highlight when bent by elastic interaction
                bladeCol = lerp(bladeCol, tipCol * 1.16h, input.bendAmount * h * 0.40h);

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
                half3 N = normalize(input.normalWS);
                half ndotl = (half)saturate(dot(N, L) * 0.65 + 0.35);

                // Root ambient occlusion (darker at root, brighter at sunlit tip)
                half rootAO = lerp(0.62h, 1.08h, h);
                half3 ambient = half3(0.40, 0.35, 0.26) * lerp(0.42h, 1.0h, dayBrightness);
                half3 lit = bladeCol * rootAO * (ambient + (half3)lightCol * (ndotl * 0.85h));

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
