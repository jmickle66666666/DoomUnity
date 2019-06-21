float doomLight(float zdepth, float brightness) 
{

    float depth = zdepth * _ZBufferParams.z;
    // #if defined(UNITY_REVERSED_Z)
    //     depth = 1.0 - depth;
    // #endif
    depth *= 1.6;
    depth -= .1125;
    float li = (brightness * 2.0) - (224.0 / 256.0);
    // li = saturate(li);
    float maxlight = (brightness * 2.0) - (40.0 / 256.0);
    maxlight = saturate(maxlight);
    float dscale = depth * 0.4 * (1.0 - unity_OrthoParams.w);
    return saturate(li + dscale) + 0.01;
}