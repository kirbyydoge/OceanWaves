Shader "Unlit/OceanWater" {
    Properties {
        _SunColor ("Sun Color", Color) = (1,1,1,1)
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularPower ("Specular Power", Float) = 32
        _FresnelPower ("Fresnel Power", Float) = 4.0
        _ShallowColor ("Shallow Water Color", Color) = (0.2, 0.5, 0.7, 1)
        _DeepColor ("Deep Water Color", Color) = (0.0, 0.1, 0.3, 1)
        _DepthFadeDistance ("Fade Distance", Float) = 100
        _SkyboxTex ("Skybox Cubemap", CUBE) = "" {}
    }

    SubShader {
        Tags { "RenderType"="Opaque" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"


            #define PI 3.14159265359

            struct appdata {
                float4 pos : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            struct Wave {
                float2 direction;
                float amplitude;
                float waveLength;
                float speed;
                float phaseOffset;
            };

            StructuredBuffer<Wave> _Waves;
            int _NumWaves;

            float3 _SunDir;
            float4 _SunColor;
            float4 _SpecularColor;
            float _SpecularPower;
            float _FresnelPower;

            float4 _ShallowColor;
            float4 _DeepColor;
            float _DepthFadeDistance;

            float _FoamSharpness;
            float _FoamIntensity;

            samplerCUBE _SkyboxTex;

            v2f vert (appdata v) {
                v2f o;

                float4 displace = float4(0.0, 0.0, 0.0, 0.0f);
                float3 tangentX = float3(1.0, 0.0, 0.0);
                float3 tangentZ = float3(0.0, 0.0, 1.0);

                for (int i = 0; i < _NumWaves; i++) {
                    Wave wave = _Waves[i];
                    float2 dir = normalize(wave.direction);
                    float amplitude = wave.amplitude;
                    float waveLength = wave.waveLength;
                    float speed = wave.speed;

                    float k = 2.0 * PI / waveLength;
                    float phase = k * dot(dir, v.pos.xz) + _Time * speed;
                    float sinP = sin(phase);
                    float cosP = cos(phase);

                    displace.x += amplitude * (dir.x / k) * cosP;
                    displace.y += amplitude * sinP;
                    displace.z += amplitude * (dir.y / k) * cosP;

                    tangentX += float3(
                        -amplitude * sinP * dir.x * dir.x,
                        amplitude * dir.x * cosP * k,
                        -amplitude * sinP * dir.y * dir.x
                    );

                    tangentZ += float3(
                        -amplitude * sinP * dir.x * dir.y,
                        amplitude * dir.y * cosP * k,
                        -amplitude * sinP * dir.y * dir.y
                    );
                }

                float4 newPos = v.pos + displace;
                o.pos = UnityObjectToClipPos(newPos);
                o.worldPos = mul(unity_ObjectToWorld, newPos);
                o.normal = cross(tangentX, tangentZ);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                float3 normal = normalize(i.normal);
                float3 lightDir = normalize(_SunDir.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

                float diff = max(dot(normal, lightDir), 0.0);
                float3 diffuse = diff * _SunColor.rgb;
                
                float3 reflectDir = reflect(lightDir, normal);
                float spec = pow(max(dot(viewDir, reflectDir), 0.0), _SpecularPower);
                float3 specular = spec * _SpecularColor.rgb;
                
                float3 ambient = 0.1 * _SunColor.rgb;

                float3 reflViewDir = reflect(-viewDir, normal);
                float3 skyColor = texCUBE(_SkyboxTex, reflViewDir).rgb;

                float viewDot = saturate(dot(viewDir, normal));
                float fresnel = saturate(_FresnelPower * pow(1.0 - viewDot, 5.0));

                float depthFade = saturate(length(viewDir) / _DepthFadeDistance);
                float3 baseColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, depthFade);

                float3 reflection = lerp(baseColor, skyColor, fresnel);

                float lightingAmount = saturate(depthFade * 1.2);
                float3 litSurface = reflection + diffuse + specular;
                float3 finalColor = lerp(baseColor, litSurface, lightingAmount);

                float slope = 1.0 - saturate(dot(normal, float3(0, 1, 0)));
                float foam = pow(slope, _FoamSharpness);
                finalColor = lerp(finalColor, float3(1, 1, 1), foam * _FoamIntensity);
                return float4(finalColor, 1.0);
            }
            ENDCG
        }
    }
}
