Shader "Unlit/HzbInstance"
{
	Properties
	{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Cutoff("Cutoff", float) = 0.5
	}
	SubShader
	{
		Tags
		{
			"Queue" = "Geometry+200"
			"IgnoreProjector" = "True"
			"DisableBatching" = "True"
		}
		Cull Off
		LOD 200
		//ColorMask RGB
		pass
		{
			HLSLPROGRAM
			
			#pragma vertex vert
			#pragma fragment frag
			#pragma instancing_options procedural:setup
			#pragma multi_compile_instancing
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			TEXTURE2D(_MainTex);	SAMPLER(sampler_MainTex);
			half _Cutoff;
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
					StructuredBuffer<float3> posVisibleBuffer;
			#endif

			struct Attribute
			{
				float4 vertex : POSITION;
 				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyfying {
				float2 uv_MainTex : TEXCOORD0;
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			UNITY_INSTANCING_BUFFER_START(Props)

			UNITY_INSTANCING_BUFFER_END(Props)

			void setup()
			{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				float3 position =  posVisibleBuffer[unity_InstanceID] ;
 
				float rot = frac(sin(position.x) * 100) * 3.14 * 2;
				float crot, srot;
				sincos(rot, srot, crot);
				unity_ObjectToWorld._11_21_31_41 = float4(crot, 0, srot, 0);
				unity_ObjectToWorld._12_22_32_42 = float4(0, 1, 0, 0);
				unity_ObjectToWorld._13_23_33_43 = float4(-srot, 0, crot, 0);
				unity_ObjectToWorld._14_24_34_44 = float4(position.xyz,1);


				unity_WorldToObject = unity_ObjectToWorld;
				unity_WorldToObject._14_24_34 *= -1;
				unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
			#endif
			}

			Varyfying vert(Attribute input)
			{
				Varyfying output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				
				output.vertex = TransformObjectToHClip(input.vertex);
				output.uv_MainTex = input.texcoord;
				return output;
			}

			half4 frag(Varyfying input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv_MainTex);
				clip(col.a - _Cutoff);
				return half4(col.rgb, 1);
			}
			ENDHLSL
		}
	}
}