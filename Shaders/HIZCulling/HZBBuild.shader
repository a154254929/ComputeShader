Shader "HZB/HZBBuild"
{
    Properties
    {
        [HideInInspector] _MainTex("Depth Texture", 2D) = "black" {}
        [HideInInspector] _InvSize("Inverse Mipmap Size", Vector) = (0, 0, 0, 0) //x,y = (1/MipMapSize.x, 1/MipMapSize.y), zw = (0, 0)
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always
		Pass
        {
			Name "HZBBuild"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex HZBVert
            #pragma fragment HZBBuildFrag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
			TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);
 			float4 _InvSize;

            struct HZBAttributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct HZBVaryings
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;

            };

            HZBVaryings HZBVert(HZBAttributes v)
            {
                HZBVaryings o;
                o.vertex = TransformWorldToHClip(v.vertex.xyz);
                o.uv = v.uv;

                return o;
            }

			float4 HZBBuildFrag(HZBVaryings input) : Color
			{	   
				float2 invSize = _InvSize.xy;
				float2 inUV = input.uv;
                float4 samplerDepth;
                float2 uv0 = inUV + float2(-0.25f, -0.25f) * _InvSize;
                float2 uv1 = inUV + float2(0.25f, -0.25f) * _InvSize;
                float2 uv2 = inUV + float2(-0.25f, 0.25f) * _InvSize;
                float2 uv3 = inUV + float2(0.25f, 0.25f) * _InvSize;
                //float2 uv0 = inUV + float2(-0.5f, -0.5f) * _InvSize;
                //float2 uv1 = inUV + float2(0.5f, -0.5f) * _InvSize;
                //float2 uv2 = inUV + float2(-0.5f, 0.5f) * _InvSize;
                //float2 uv3 = inUV + float2(0.5f, 0.5f) * _InvSize;

                samplerDepth.x = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv0).x;
                samplerDepth.y = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv1).x;
                samplerDepth.z = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv2).x;
                samplerDepth.w = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv3).x;
#if defined(UNITY_REVERSED_Z)
                float depth = min(min(samplerDepth.x, samplerDepth.y), min(samplerDepth.z, samplerDepth.w));
#else
                float depth = max(max(samplerDepth.x, samplerDepth.y), max(samplerDepth.z, samplerDepth.w));
#endif

				return float4(depth, 0, 0, 1.0f);
			}
			ENDHLSL
		}
    }
}