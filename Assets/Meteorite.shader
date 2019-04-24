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
                float4x4 Mat;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
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
                float3 normal: NORMAL;
            };
            
            v2f vert(appdata_custom v)
            {
                float4 wpos = mul(MeteoritesState[v.instanceID].Mat, v.vertex);
                
                v2f o;
                o.pos = UnityObjectToClipPos(float4(wpos.xyz, 1));
                o.uv = v.texcoord.xy;
                o.normal = v.normal;
                return o;
            }
            
            fixed4 frag(v2f i): SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                color *= dot(i.normal, _WorldSpaceLightPos0.xyz);
                return color;
            }
            ENDCG
            
        }
    }
}