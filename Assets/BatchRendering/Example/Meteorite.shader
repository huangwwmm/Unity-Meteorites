Shader "Custom/RandomDisperseMesh/Meteorite"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" { }
    }
    
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Fog
            {
                Mode Off
            }
            AlphaTest Off
            
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5
            
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
            
            sampler2D _MainTex;
            uniform float _StartFadeOutRange;
            uniform float _FadeOutEndOffset;
            StructuredBuffer<MeshState> _MeshStates;
            
            struct appdata_custom
            {
                uint instanceID: SV_InstanceID;
                float4 vertex: POSITION;
                float4 texcoord: TEXCOORD0;
                float3 normal: NORMAL;
            };
            
            struct v2f
            {
                uint instanceID: SV_InstanceID;
                float4 pos: SV_POSITION;
                float2 uv: TEXCOORD0;
                float3 normal: NORMAL; 
            };
            
            v2f vert(appdata_custom v)
            {
                v2f o;
                o.instanceID = v.instanceID;
                o.pos = _MeshStates[v.instanceID].Display * float4(99999, 99999, 99999, 0) + mul(_MeshStates[v.instanceID].MatMVP, v.vertex);
                o.uv = v.texcoord.xy;
                o.normal = v.normal;
                return o;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                color.xyz *= dot(mul(_MeshStates[i.instanceID].MatM, i.normal), _WorldSpaceLightPos0.xyz);
                return color;
            }
            ENDCG
            
        }
    }
}