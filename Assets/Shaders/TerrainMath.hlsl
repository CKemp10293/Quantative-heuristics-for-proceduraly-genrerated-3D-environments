// These arrays are sent directly from your C# script!
float layerCount;
float4 baseColours[8];
float baseStartHeights[8];
float baseBlends[8];
float baseTextureScales[8];
float minHeight;
float maxHeight;

// This function processes the array and spits out the final pixel color
void GetTerrainColor_float(float3 WorldPos, UnityTexture2DArray BaseTextures, out float3 OutColor) {
    
    // Calculate height (0.0 to 1.0)
    float heightPercent = saturate((WorldPos.y - minHeight) / (maxHeight - minHeight));
    float3 finalColor = float3(0,0,0);

    for (int i = 0; i < 8; i++) {
        if (i >= layerCount) break;

        // Calculate blend
        float blendVal = baseBlends[i] / 2.0;
        float drawStrength = saturate(((heightPercent - baseStartHeights[i]) - (-blendVal - 0.0001)) / (blendVal - (-blendVal - 0.0001)));

        // Sample the texture flipbook!
        float2 uv = WorldPos.xz / baseTextureScales[i];
        float3 texColour = BaseTextures.tex.Sample(BaseTextures.samplerstate, float3(uv, i)).rgb;

        // Add it to the final color
        finalColor = finalColor * (1.0 - drawStrength) + (texColour * baseColours[i].rgb) * drawStrength;
    }
    OutColor = finalColor;
}
