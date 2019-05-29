Shader "Custom/RandomDisperseMesh/Meteorite_Pro"
{
	Properties
	{
		_Main("Main", 2D) = "white" { }
		_Main_intensity("Main_intensity", Float) = 1
		_Specular_fanwei("Specular_fanwei", Float) = 1
		_Specular_intensity("Specular_intensity", Float) = 1
		_Main_color("Main_color", Color) = (0.5, 0.5, 0.5, 1)
	}

		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			Pass
			{
				Name "FORWARD"
				Tags { "LightMode" = "ForwardBase" }

				CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#define UNITY_PASS_FORWARDBASE
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"
				#include "Lighting.cginc"
				#pragma multi_compile_fwdbase_fullshadows
				#pragma multi_compile_fog
				#pragma only_renderers d3d9 d3d11 glcore gles
				#pragma target 3.0

				struct MeshState
				{
			// 相对于父节点的坐标
			float3 LocalPosition;
			// 相对于父节点的旋转
			float3 LocalRotation;
			// 相对于父节点的缩放
			float3 LocalScale;
			// 没有缩放的M矩阵，用于转换Normal
			float4x4 MatM;
			// MVP矩阵，用于转换Vertex
			float4x4 MatMVP;
			// 是否显示
			int Display;
		};

		uniform sampler2D _Main;
		uniform float4 _Main_ST;
		uniform float _Main_intensity;
		uniform float _Specular_fanwei;
		uniform float _Specular_intensity;
		uniform float4 _Main_color;
		StructuredBuffer<MeshState> _MeshStates;


		struct VertexInput
		{
			uint instanceID: SV_InstanceID;
			float4 vertex: POSITION;
			float3 normal: NORMAL;
			float2 texcoord0: TEXCOORD0;
		};
		struct VertexOutput
		{
			uint instanceID: SV_InstanceID;
			float4 pos: SV_POSITION;
			float3 normal: NORMAL;
			float2 uv0: TEXCOORD0;
			float4 vertex: TEXCOORD1;
			LIGHTING_COORDS(3, 4)
			UNITY_FOG_COORDS(5)
		};


		VertexOutput vert(VertexInput v)
		{
			VertexOutput o = (VertexOutput)0;
			o.instanceID = v.instanceID;
			o.uv0 = v.texcoord0.xy;
			o.pos = _MeshStates[v.instanceID].Display * float4(99999, 99999, 99999, 0) + mul(_MeshStates[v.instanceID].MatMVP, v.vertex);
			o.vertex = v.vertex;
			o.normal = v.normal;
			UNITY_TRANSFER_FOG(o, o.pos);
			TRANSFER_VERTEX_TO_FRAGMENT(o)
			return o;
		}

		float4 frag(VertexOutput i) : COLOR
		{
			float3 normalDirection = mul(_MeshStates[i.instanceID].MatM, i.normal);
			float3 vertexWorld = mul(_MeshStates[i.instanceID].MatM, i.vertex);
			float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - vertexWorld);
			float3 halfDirection = normalize(viewDirection + _WorldSpaceLightPos0.xyz);

			// Lighting
			float attenuation = LIGHT_ATTENUATION(i);
			float4 _Main_var = tex2D(_Main, TRANSFORM_TEX(i.uv0, _Main));
			float node_7598 = (0.5 * (0.5 + max(0, dot(normalDirection, _WorldSpaceLightPos0.xyz))));
			float3 finalColor = (((_Main_color.rgb * ((_Main_var.rgb * (node_7598 * node_7598)) * _Main_intensity)) + (pow(max(0, dot(normalDirection, halfDirection)), exp2(_Specular_fanwei)) * _Specular_intensity)) * attenuation * _LightColor0.rgb);
			fixed4 finalRGBA = fixed4(finalColor, 1);
			UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
			return finalRGBA;
		}
		ENDCG

	}

	Pass
	{
		Name "FORWARD_DELTA"
		Tags { "LightMode" = "ForwardAdd" }
		Blend One One

		CGPROGRAM

		#pragma vertex vert
		#pragma fragment frag
		#define UNITY_PASS_FORWARDADD
		#include "UnityCG.cginc"
		#include "AutoLight.cginc"
		#include "Lighting.cginc"
		#pragma multi_compile_fwdadd_fullshadows
		#pragma multi_compile_fog
		#pragma only_renderers d3d9 d3d11 glcore gles
		#pragma target 3.0

		uniform sampler2D _Main;
		uniform float4 _Main_ST;
		uniform float _Main_intensity;
		uniform float _Specular_fanwei;
		uniform float _Specular_intensity;
		uniform float4 _Main_color;

		struct VertexInput
		{
			float4 vertex: POSITION;
			float3 normal: NORMAL;
			float2 texcoord0: TEXCOORD0;
		};
		struct VertexOutput
		{
			float4 pos: SV_POSITION;
			float2 uv0: TEXCOORD0;
			float4 posWorld: TEXCOORD1;
			float3 normalDir: TEXCOORD2;
			LIGHTING_COORDS(3, 4)
			UNITY_FOG_COORDS(5)
		};
		VertexOutput vert(VertexInput v)
		{
			VertexOutput o = (VertexOutput)0;
			o.uv0 = v.texcoord0;
			o.normalDir = UnityObjectToWorldNormal(v.normal);
			o.posWorld = mul(unity_ObjectToWorld, v.vertex);
			float3 lightColor = _LightColor0.rgb;
			o.pos = UnityObjectToClipPos(v.vertex);
			UNITY_TRANSFER_FOG(o, o.pos);
			TRANSFER_VERTEX_TO_FRAGMENT(o)
			return o;
		}
		float4 frag(VertexOutput i) : COLOR
		{
			i.normalDir = normalize(i.normalDir);
			float3 viewDirection = normalize(_WorldSpaceCameraPos.xyz - i.posWorld.xyz);
			float3 normalDirection = i.normalDir;
			float3 lightDirection = normalize(lerp(_WorldSpaceLightPos0.xyz, _WorldSpaceLightPos0.xyz - i.posWorld.xyz, _WorldSpaceLightPos0.w));
			float3 lightColor = _LightColor0.rgb;
			float3 halfDirection = normalize(viewDirection + lightDirection);
			////// Lighting:
			float attenuation = LIGHT_ATTENUATION(i);
			float4 _Main_var = tex2D(_Main, TRANSFORM_TEX(i.uv0, _Main));
			float node_6922 = 0.5;
			float node_7598 = (node_6922 * (node_6922 + max(0, dot(i.normalDir, lightDirection))));
			float3 finalColor = (((_Main_color.rgb * ((_Main_var.rgb * (node_7598 * node_7598)) * _Main_intensity)) + (pow(max(0, dot(i.normalDir, halfDirection)), exp2(_Specular_fanwei)) * _Specular_intensity)) * attenuation * _LightColor0.rgb);
			fixed4 finalRGBA = fixed4(finalColor * 1, 0);
			UNITY_APPLY_FOG(i.fogCoord, finalRGBA);
			return finalRGBA;
		}
		ENDCG

	}
		}
}