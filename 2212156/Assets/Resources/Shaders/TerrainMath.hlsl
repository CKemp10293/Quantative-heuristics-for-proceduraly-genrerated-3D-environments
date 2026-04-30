// These arrays are sent directly from your C# script!
float layerCount;
float4 baseColours[8];
float baseStartHeights[8];
float baseBlends[8];
float baseTextureScales[8];

float minHeight;
float maxHeight;

void GetTerrainColor_float(float3 WorldPos, float3 WorldNormal, UnityTexture2DArray BaseTextures, out float3 OutColor) {
    
    // Calculate height (0.0 to 1.0)
    float heightPercent = saturate((WorldPos.y - minHeight) / (maxHeight - minHeight));
    float3 finalColor = float3(0,0,0);

    // Calculate Tri-planar blend weights.
    // We take the absolute value of the normal so negative facing walls map the same as positive.
    float3 blendAxes = abs(WorldNormal);
    
    // Divide by the sum to ensure all three axes perfectly equal 1.0
    blendAxes /= (blendAxes.x + blendAxes.y + blendAxes.z);

    for (int i = 0; i < 8; i++) {
        if (i >= layerCount) break;
        
        // Calculate blend
        float blendVal = baseBlends[i] / 2.0;
        float drawStrength = saturate(((heightPercent - baseStartHeights[i]) - (-blendVal - 0.0001)) / (blendVal - (-blendVal - 0.0001)));
        
        float scale = baseTextureScales[i];
        
        // Calculate the 3 different UV projections using the scaled world position
        float2 uvX = WorldPos.zy / scale; // Side projection
        float2 uvY = WorldPos.xz / scale; // Top-down projection
        float2 uvZ = WorldPos.xy / scale; // Front projection

        // Sample the texture array 3 times (once from each axis)
        float3 texX = BaseTextures.tex.Sample(BaseTextures.samplerstate, float3(uvX, i)).rgb;
        float3 texY = BaseTextures.tex.Sample(BaseTextures.samplerstate, float3(uvY, i)).rgb;
        float3 texZ = BaseTextures.tex.Sample(BaseTextures.samplerstate, float3(uvZ, i)).rgb;

        // Multiply each sample by its normal weight and add them together
        float3 texColour = (texX * blendAxes.x) + (texY * blendAxes.y) + (texZ * blendAxes.z);
        
        // Add it to the final color
        finalColor = finalColor * (1.0 - drawStrength) + (texColour * baseColours[i].rgb) * drawStrength;
    }
    
    OutColor = finalColor;
}