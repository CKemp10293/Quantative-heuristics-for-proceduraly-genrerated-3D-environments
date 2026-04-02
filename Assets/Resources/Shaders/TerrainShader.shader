Shader "Custom/Terrain" {
    Properties {
        // We leave this blank because C# is feeding the properties dynamically!
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model
        #pragma surface surf Standard fullforwardshadows
        // Require Shader Model 3.5 to support Texture Arrays
        #pragma target 3.5

        const static int maxLayerCount = 8;
        const static float epsilon = 1E-4;

        // Variables receiving data from TextureData.cs
        // Variables receiving data from TextureData.cs
        int layerCount;
        float4 baseColours[maxLayerCount]; // FIX 1: Must be float4 to receive C# Color
        float baseStartHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseTextureScales[maxLayerCount];

        float minHeight;
        float maxHeight;

        // The Flipbook Array!
        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input {
            float3 worldPos;
            float3 worldNormal;
        };

        float inverseLerp(float a, float b, float value) {
            return saturate((value - a) / (b - a));
        }

        void surf (Input IN, inout SurfaceOutputStandard o) {
            float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);

            // FIX 2: Loop to the constant maxLayerCount so the GPU can unroll it
            for (int i = 0; i < maxLayerCount; i ++) {
                
                // Break out safely if we exceed our actual biomes
                if (i >= layerCount) {
                    break; 
                }

                float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);
                float2 uv = IN.worldPos.xz / baseTextureScales[i];
                float3 texColour = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(uv, i)).rgb;

                // FIX 1b: Explicitly ask for baseColours[i].rgb
                o.Albedo = o.Albedo * (1-drawStrength) + (texColour * baseColours[i].rgb) * drawStrength;
            }
        }
        ENDCG
    }
    FallBack "Diffuse"
}