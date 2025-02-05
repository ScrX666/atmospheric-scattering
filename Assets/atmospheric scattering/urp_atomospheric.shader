Shader "Hidden/urp_atomospheric"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "urp_atomosphericMain.hlsl"
            
            ENDHLSL
        }
    }
}
