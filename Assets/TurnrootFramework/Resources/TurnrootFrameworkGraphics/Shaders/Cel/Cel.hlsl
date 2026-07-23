#ifndef TURNROOT_CEL_SHADING_INCLUDED
#define TURNROOT_CEL_SHADING_INCLUDED


#if SHADERPASS != SHADERPASS_FORWARD && SHADERPASS != SHADERPASS_GBUFFER
// Guarded so this doesn't emit "duplicate keyword" warnings if this file ends
// up included inside a Lit Shader Graph, which already declares these itself.
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#endif

void CelShading_float(
    in float3 Normal,
    in float ToonRampSmoothness,
    in float3 WorldPos,
    in float3 ToonRampTinting,
    in float ToonRampOffset,
    in float ToonRampOffsetPoint,
    in float Ambient,
    out float3 ToonRampOutput,
    out float3 Direction)
{
#ifdef SHADERGRAPH_PREVIEW
    ToonRampOutput = float3(0.5, 0.5, 0.5);
    Direction = float3(0.5, 0.5, 0);
#else
    // Shadow coord for the main light. _MAIN_LIGHT_SHADOWS_SCREEN is the real
    // URP keyword — the previous "SHADOWS_SCREEN" check never fires.
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
        float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(WorldPos));
    #else
        float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
    #endif

    #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        Light light = GetMainLight(shadowCoord);
    #else
        Light light = GetMainLight();
    #endif

    // Main light toon ramp: geometric term first, shadow attenuation applied
    // as a separate multiplier afterwards. This order matters — folding
    // shadowAttenuation into the dot-product remap instead (as the previous
    // version's counterpart in the main shader did) makes a fully-shadowed
    // pixel land on the midtone instead of the shadow band. This function's
    // own multiply-after-ramp approach was already correct.
    half dMain = saturate(dot(Normal, light.direction) * 0.5 + 0.5);
    half toonRampMain = smoothstep(ToonRampOffset, ToonRampOffset + ToonRampSmoothness, dMain);
    toonRampMain *= light.shadowAttenuation;

    // ── Additional lights (Forward+-correct) ──────────────────────────────
    float3 extraLights = float3(0, 0, 0);
    uint pixelLightCount = GetAdditionalLightsCount();

    // Forward+: additional *directional* lights aren't in the per-tile
    // cluster list and need this separate loop.
    #if USE_CLUSTER_LIGHT_LOOP
    UNITY_LOOP
    for (uint dirIndex = 0; dirIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); dirIndex++)
    {
        Light dirLight = GetAdditionalLight(dirIndex, WorldPos, half4(1, 1, 1, 1));
        float3 attenuatedLightColor = dirLight.color * (dirLight.distanceAttenuation * dirLight.shadowAttenuation);
        half dExtra = saturate(dot(Normal, dirLight.direction) * 0.5 + 0.5);
        half toonRampExtra = smoothstep(ToonRampOffsetPoint, ToonRampOffsetPoint + ToonRampSmoothness, dExtra);
        extraLights += attenuatedLightColor * toonRampExtra;
    }
    #endif

    // Point / spot lights (and, on the non-Forward+ path, all additional
    // lights) via URP's tile-aware light loop. LIGHT_LOOP_BEGIN needs
    // inputData.positionWS and inputData.normalizedScreenSpaceUV, which we
    // build by hand here — recomputed straight from WorldPos, same as the
    // shadow coord above, rather than trusting a passed-in clip position.
    InputData inputData = (InputData)0;
    inputData.positionWS = WorldPos;
    float4 screenPos = ComputeScreenPos(TransformWorldToHClip(WorldPos));
    inputData.normalizedScreenSpaceUV = screenPos.xy / screenPos.w;

    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light aLight = GetAdditionalLight(lightIndex, WorldPos, half4(1, 1, 1, 1));
        float3 attenuatedLightColor = aLight.color * (aLight.distanceAttenuation * aLight.shadowAttenuation);
        half dExtra = saturate(dot(Normal, aLight.direction) * 0.5 + 0.5);
        half toonRampExtra = smoothstep(ToonRampOffsetPoint, ToonRampOffsetPoint + ToonRampSmoothness, dExtra);
        extraLights += attenuatedLightColor * toonRampExtra;
    LIGHT_LOOP_END

    // ── Combine ─────────────────────────────────────────────────────────
    ToonRampOutput = light.color * (toonRampMain + ToonRampTinting) + Ambient;
    ToonRampOutput += extraLights;

    Direction = normalize(light.direction);
#endif
}

#endif // TURNROOT_CEL_SHADING_INCLUDED
