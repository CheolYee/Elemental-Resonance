#pragma once

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "NiloOutlineUtil.hlsl"
#include "NiloZOffset.hlsl"
#include "NiloInvLerpRemap.hlsl"

struct Attributes
{
    float3 positionOS   : POSITION;
    half3 normalOS      : NORMAL;
    half4 tangentOS     : TANGENT;
    float2 uv           : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    float4 positionWSAndFogFactor   : TEXCOORD1;
    half3 normalWS                  : TEXCOORD2;
    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

// samplers outside CBUFFER
sampler2D _MouthMap;
sampler2D _BaseMap;
sampler2D _EmissionMap;
sampler2D _OcclusionMap;
sampler2D _OutlineZOffsetMaskTex;
sampler2D _DissolveNoiseTex;

CBUFFER_START(UnityPerMaterial)
    float   _IsFace;
    float   _UseMouthMap;
    float   _MouthIndex;
    float4  _BaseMap_ST;
    half4   _BaseColor;
    half    _Cutoff;
    float   _UseEmission;
    half3   _EmissionColor;
    half    _EmissionMulByBaseColor;
    half3   _EmissionMapChannelMask;
    float   _UseOcclusion;
    half    _OcclusionStrength;
    half4   _OcclusionMapChannelMask;
    half    _OcclusionRemapStart;
    half    _OcclusionRemapEnd;
    half3   _IndirectLightMinColor;
    half    _CelShadeMidPoint;
    half    _CelShadeSoftness;
    half    _ReceiveShadowMappingAmount;
    float   _ReceiveShadowMappingPosOffset;
    half3   _ShadowMapColor;
    float   _OutlineWidth;
    half3   _OutlineColor;
    float   _OutlinePureColor;
    float   _OutlineZOffset;
    float   _OutlineZOffsetMaskRemapStart;
    float   _OutlineZOffsetMaskRemapEnd;
    // dissolve
    float   _DissolveAmount;
    float   _EdgeWidth;
    half4   _EdgeColor;
    half    _EdgeIntensity;
    float   _DissolveYMin;
    float   _DissolveYMax;
    float   _HeightBlend;
    float   _DustScale;
    float   _DustSpread;
    float   _DustDensity;
CBUFFER_END

float3 _LightDirection;

struct ToonSurfaceData
{
    half3   albedo;
    half    alpha;
    half3   emission;
    half    occlusion;
};
struct ToonLightingData
{
    half3   normalWS;
    float3  positionWS;
    half3   viewDirectionWS;
    float4  shadowCoord;
};

float3 TransformPositionWSToOutlinePositionWS(float3 positionWS, float positionVS_Z, float3 normalWS)
{
    float outlineExpandAmount = _OutlineWidth * GetOutlineCameraFovAndDistanceFixMultiplier(positionVS_Z);
    #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED) || defined(UNITY_STEREO_DOUBLE_WIDE_ENABLED)
    outlineExpandAmount *= 0.5;
    #endif
    return positionWS + normalWS * outlineExpandAmount;
}

Varyings VertexShaderWork(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
    VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    float3 positionWS = vertexInput.positionWS;

#ifdef ToonShaderIsOutline
    positionWS = TransformPositionWSToOutlinePositionWS(vertexInput.positionWS, vertexInput.positionVS.z, vertexNormalInput.normalWS);
#endif

    float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.positionWSAndFogFactor = float4(positionWS, fogFactor);
    output.normalWS = vertexNormalInput.normalWS;
    output.positionCS = TransformWorldToHClip(positionWS);

#ifdef ToonShaderIsOutline
    float outlineZOffsetMaskTexExplictMipLevel = 0;
    float outlineZOffsetMask = tex2Dlod(_OutlineZOffsetMaskTex, float4(input.uv, 0, outlineZOffsetMaskTexExplictMipLevel)).r;
    outlineZOffsetMask = 1 - outlineZOffsetMask;
    outlineZOffsetMask = invLerpClamp(_OutlineZOffsetMaskRemapStart, _OutlineZOffsetMaskRemapEnd, outlineZOffsetMask);
    output.positionCS = NiloGetNewClipPosWithZOffset(output.positionCS, _OutlineZOffset * outlineZOffsetMask + 0.03 * _IsFace);
#endif

#ifdef ToonShaderApplyShadowBiasFix
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, output.normalWS, _LightDirection));
    #if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif
    output.positionCS = positionCS;
#endif

    return output;
}

half4 GetFinalBaseColor(Varyings input)
{
    half4 color = tex2D(_BaseMap, input.uv);
    if (_UseMouthMap != 0)
    {
        const int mouthX = int(_MouthIndex % 8);
        const int mouthY = int(7 - _MouthIndex / 8);
        const half2 mouthUV = half2(mouthX / 8., mouthY / 8.) + input.uv * 4 / 8;
        half4 mouthColor = tex2D(_MouthMap, mouthUV);
        if (input.uv.x < 0.25 & input.uv.y < 0.25)
            color = half4(lerp(color.xyz, mouthColor.xyz, mouthColor.a), color.a);
    }
    return color * _BaseColor;
}
half3 GetFinalEmissionColor(Varyings input)
{
    half3 result = 0;
    if (_UseEmission)
        result = tex2D(_EmissionMap, input.uv).rgb * _EmissionMapChannelMask * _EmissionColor.rgb;
    return result;
}
half GetFinalOcculsion(Varyings input)
{
    half result = 1;
    if (_UseOcclusion)
    {
        half4 texValue = tex2D(_OcclusionMap, input.uv);
        half occlusionValue = dot(texValue, _OcclusionMapChannelMask);
        occlusionValue = lerp(1, occlusionValue, _OcclusionStrength);
        occlusionValue = invLerpClamp(_OcclusionRemapStart, _OcclusionRemapEnd, occlusionValue);
        result = occlusionValue;
    }
    return result;
}
void DoClipTestToTargetAlphaValue(half alpha)
{
#if _UseAlphaClipping
    clip(alpha - _Cutoff);
#endif
}
ToonSurfaceData InitializeSurfaceData(Varyings input)
{
    ToonSurfaceData output;
    float4 baseColorFinal = GetFinalBaseColor(input);
    output.albedo = baseColorFinal.rgb;
    output.alpha = baseColorFinal.a;
    DoClipTestToTargetAlphaValue(output.alpha);
    output.emission = GetFinalEmissionColor(input);
    output.occlusion = GetFinalOcculsion(input);
    return output;
}
ToonLightingData InitializeLightingData(Varyings input)
{
    ToonLightingData lightingData;
    lightingData.positionWS = input.positionWSAndFogFactor.xyz;
    lightingData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - lightingData.positionWS);
    lightingData.normalWS = normalize(input.normalWS);
    return lightingData;
}

#include "SimpleURPToonLitOutlineExample_LightingEquation.hlsl"

half3 ShadeAllLights(ToonSurfaceData surfaceData, ToonLightingData lightingData)
{
    half3 indirectResult = ShadeGI(surfaceData, lightingData);
    Light mainLight = GetMainLight();
    float3 shadowTestPosWS = lightingData.positionWS + mainLight.direction * (_ReceiveShadowMappingPosOffset + _IsFace);
#ifdef _MAIN_LIGHT_SHADOWS
    float4 shadowCoord = TransformWorldToShadowCoord(shadowTestPosWS);
    mainLight.shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
#endif
    half3 mainLightResult = ShadeSingleLight(surfaceData, lightingData, mainLight, false);
    half3 additionalLightSumResult = 0;
#ifdef _ADDITIONAL_LIGHTS
    int additionalLightsCount = GetAdditionalLightsCount();
    for (int i = 0; i < additionalLightsCount; ++i)
    {
        int perObjectLightIndex = GetPerObjectLightIndex(i);
        Light light = GetAdditionalPerObjectLight(perObjectLightIndex, lightingData.positionWS);
        light.shadowAttenuation = AdditionalLightRealtimeShadow(perObjectLightIndex, shadowTestPosWS);
        additionalLightSumResult += ShadeSingleLight(surfaceData, lightingData, light, true);
    }
#endif
    half3 emissionResult = ShadeEmission(surfaceData, lightingData);
    return CompositeAllLightResults(indirectResult, mainLightResult, additionalLightSumResult, emissionResult, surfaceData, lightingData);
}

half3 ConvertSurfaceColorToOutlineColor(half3 originalSurfaceColor)
{
    return lerp(originalSurfaceColor * _OutlineColor, _OutlineColor, _OutlinePureColor);
}
half3 ApplyFog(half3 color, Varyings input)
{
    half fogFactor = input.positionWSAndFogFactor.w;
    color = MixFog(color, fogFactor);
    return color;
}

half4 ShadeFinalColor(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // dissolve: bottom-to-top with floating dust particles
    float worldY = input.positionWSAndFogFactor.y;
    float normalizedY = saturate((worldY - _DissolveYMin) / max(_DissolveYMax - _DissolveYMin, 0.001));

    float coarseNoise = tex2D(_DissolveNoiseTex, input.uv).r;
    float fineNoise   = tex2D(_DissolveNoiseTex, input.uv * _DustScale).r;

    // solid body: height-driven, coarse noise adds irregular edge
    float mainEdge = normalizedY + (coarseNoise - 0.5) * (1.0 - _HeightBlend) * 0.3;
    float solidMask = step(_DissolveAmount, mainEdge);

    // dust particles: sparse fine-noise dots floating in the transparent band above the edge
    float inDustZone = step(_DissolveAmount, normalizedY) * step(normalizedY, _DissolveAmount + _DustSpread);
    float dustDotMask = step(1.0 - _DustDensity, fineNoise) * inDustZone * (1.0 - solidMask);

    clip(solidMask + dustDotMask - 0.5);

    ToonSurfaceData surfaceData = InitializeSurfaceData(input);
    ToonLightingData lightingData = InitializeLightingData(input);

    // edge glow: solid edge + dust dot glow
    if (_DissolveAmount > 0.001)
    {
        float edgeFactor = (1.0 - smoothstep(_DissolveAmount, _DissolveAmount + _EdgeWidth, mainEdge)) * solidMask;
        surfaceData.emission += _EdgeColor.rgb * _EdgeIntensity * max(edgeFactor, dustDotMask);
    }

    half3 color = ShadeAllLights(surfaceData, lightingData);

#ifdef ToonShaderIsOutline
    color = ConvertSurfaceColorToOutlineColor(color);
#endif

    color = ApplyFog(color, input);
    return half4(color, surfaceData.alpha);
}

void AlphaClipAndLODTest(Varyings input)
{
    DoClipTestToTargetAlphaValue(GetFinalBaseColor(input).a);
    #ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
    #endif
}

half DepthOnlyFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    AlphaClipAndLODTest(input);
    return input.positionCS.z;
}

void DepthNormalsFragment(
    Varyings input
    , out half4 outNormalWS : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    AlphaClipAndLODTest(input);

    #if defined(_GBUFFER_NORMALS_OCT)
        float3 normalWS = normalize(input.normalWS);
        float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
        float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
        half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
        outNormalWS = half4(packedNormalWS, 0.0);
    #else
        outNormalWS = half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
    #endif

    #ifdef _WRITE_RENDERING_LAYERS
        uint renderingLayers = GetMeshRenderingLayer();
        outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
    #endif
}
