Shader "Custom/Meteorite"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" { }
    }
    SubShader
    {
        Pass
        {
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5
            
            struct MeteoriteState
            {
                float3 Position;
                float3 Rotation;
                float3 Scale;
                float4x4 MatM;
                float4x4 MatMVP;
            };
            
            sampler2D _MainTex;
            StructuredBuffer<MeteoriteState> MeteoritesState;
            
            struct appdata_custom
            {
                uint instanceID: SV_InstanceID;
                float4 vertex: POSITION;
                float4 texcoord: TEXCOORD0;
                float3 normal: NORMAL;
            };
            
            struct v2f
            {
                float4 pos: SV_POSITION;
                float2 uv: TEXCOORD0;
                float lightStrength: TEXCOORD1;
            };
            
            v2f vert(appdata_custom v)
            {
                v2f o;
                o.pos = mul(MeteoritesState[v.instanceID].MatMVP, v.vertex);
                o.uv = v.texcoord.xy;
                o.lightStrength = dot(mul(MeteoritesState[v.instanceID].MatM, v.normal), _WorldSpaceLightPos0.xyz);
                return o;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                color *= i.lightStrength;
                return color;
            }
            ENDCG
            
        }
    }
}