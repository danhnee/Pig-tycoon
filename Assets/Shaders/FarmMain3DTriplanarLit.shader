Shader "PigTycoon/FarmMain3DTriplanarLit"
{
    Properties
    {
        _BaseMap ("Master 4K Albedo", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BumpMap ("Master 4K Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 0.72
        _DetailMap ("World Triplanar Micro-Detail Albedo (1K)", 2D) = "gray" {}
        _DetailNormalMap ("World Triplanar Micro-Detail Normal (1K)", 2D) = "bump" {}
        _DetailStrength ("Micro-Detail Strength", Range(0, 1)) = 0.52
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
        _EmissionStrength ("Warm Golden Emission", Range(0, 1)) = 0.36
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseColor;
            sampler2D _BumpMap;
            float _BumpScale;
            sampler2D _DetailMap;
            sampler2D _DetailNormalMap;
            float _DetailStrength;
            float _Smoothness;
            float _EmissionStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float3 worldTangent : TEXCOORD3;
                float3 worldBitangent : TEXCOORD4;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = wp;
                float3 wn = UnityObjectToWorldNormal(v.normal);
                float3 wt = UnityObjectToWorldDir(v.tangent.xyz);
                float3 wb = cross(wn, wt) * v.tangent.w * unity_WorldTransformParams.w;
                o.worldNormal = wn;
                o.worldTangent = wt;
                o.worldBitangent = wb;
                return o;
            }

            float3 SampleTriplanarAlbedo(float3 wp, float3 blendW, float scale)
            {
                float3 cx = tex2D(_DetailMap, wp.yz * scale).rgb;
                float3 cy = tex2D(_DetailMap, wp.xz * scale).rgb;
                float3 cz = tex2D(_DetailMap, wp.xy * scale).rgb;
                return cx * blendW.x + cy * blendW.y + cz * blendW.z;
            }

            float3 SampleTriplanarNormal(float3 wp, float3 blendW, float scale)
            {
                float3 nx = UnpackNormal(tex2D(_DetailNormalMap, wp.yz * scale));
                float3 ny = UnpackNormal(tex2D(_DetailNormalMap, wp.xz * scale));
                float3 nz = UnpackNormal(tex2D(_DetailNormalMap, wp.xy * scale));
                return normalize(nx * blendW.x + ny * blendW.y + nz * blendW.z);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Sample 4K Master Albedo (Tripo-free baked from 4K Reference)
                float3 baseCol = tex2D(_BaseMap, i.uv).rgb * _BaseColor.rgb;

                // 2. Compute camera distance so overview (122m) stays 1-to-1 with Reference,
                //    while zooming in (< 95m) reveals 2,150 px/m 3D World-Space Triplanar Micro-Detail!
                float camDist = distance(_WorldSpaceCameraPos.xyz, i.worldPos);
                float zoomDetailFade = saturate((110.0 - camDist) / 72.0);
                float activeDetailStr = _DetailStrength * (0.18 + 0.82 * zoomDetailFade);

                // 3. Multi-Octave 3D World-Space Triplanar Micro-Detail (0.45 cycles/m + 1.85 cycles/m)
                float3 wn = normalize(i.worldNormal);
                float3 blendW = pow(abs(wn), 4.0);
                blendW /= max(dot(blendW, float3(1.0, 1.0, 1.0)), 1e-4);

                float3 det1 = SampleTriplanarAlbedo(i.worldPos, blendW, 0.45);
                float3 det2 = SampleTriplanarAlbedo(i.worldPos, blendW, 1.85);
                float3 detCombined = (det1 * 0.60 + det2 * 0.40) - 0.5; // centered at 0

                // Soft-light / Overlay micro-detail modulation preserving exact hue & saturation
                float3 detailedCol = saturate(baseCol + baseCol * detCombined * (activeDetailStr * 1.65));

                // 4. Combine 4K Tangent Normal + World-Space Triplanar Micro-Normal
                float3 tNorm = UnpackNormal(tex2D(_BumpMap, i.uv));
                tNorm.xy *= _BumpScale;
                tNorm = normalize(tNorm);

                float3 mNorm = SampleTriplanarNormal(i.worldPos, blendW, 1.25);
                mNorm.xy *= (activeDetailStr * 0.85);
                float3 combinedTNorm = normalize(float3(tNorm.xy + mNorm.xy, tNorm.z));

                float3 wt = normalize(i.worldTangent);
                float3 wb = normalize(i.worldBitangent);
                float3 N = normalize(wt * combinedTNorm.x + wb * combinedTNorm.y + wn * combinedTNorm.z);

                // 5. Balanced Golden-Hour Directional + Hemispheric Lighting (matches Reference tones)
                float3 sunDir = normalize(float3(0.38, 0.74, -0.55));
                float3 fillDir = normalize(float3(-0.42, 0.65, 0.63));
                float ndlSun = saturate(dot(N, sunDir) * 0.5 + 0.5);
                float ndlFill = saturate(dot(N, fillDir) * 0.5 + 0.5);

                float3 lighting = float3(1.04, 0.96, 0.86) * (0.38 + 0.78 * ndlSun)
                                + float3(0.96, 0.90, 0.82) * (0.12 * ndlFill);

                // Subtle golden-hour water specular highlight on smooth surfaces (Zone03 ponds & river)
                float3 V = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                float3 H = normalize(sunDir + V);
                float spec = pow(saturate(dot(N, H)), 32.0) * _Smoothness * 0.35;

                // Blend between unlit-accurate reference color at far overview and rich 3D micro-lit relief when zoomed in
                float3 litCol = detailedCol * lerp(float3(1.0, 1.0, 1.0), lighting, 0.16 + 0.78 * zoomDetailFade)
                              + float3(1.0, 0.88, 0.65) * spec;

                return fixed4(saturate(litCol), 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
