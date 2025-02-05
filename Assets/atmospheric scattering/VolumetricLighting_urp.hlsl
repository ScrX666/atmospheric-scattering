#pragma once
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half ShadowAtten(float3 worldPosition)
{
    return MainLightRealtimeShadow(TransformWorldToShadowCoord(worldPosition));
}