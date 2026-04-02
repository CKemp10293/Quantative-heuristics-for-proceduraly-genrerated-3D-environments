Shader "MyGame/SimpleTree_Cutout" {
    Properties {
        _Color ("Main Color", Color) = (1, 1, 1, 1)
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }
    SubShader {
        // "TransparentCutout" allows us to see through the gaps in leaves
        Tags { "Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            fixed _Cutoff;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; 
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                UNITY_SETUP_INSTANCE_ID(i);
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 tint = UNITY_ACCESS_INSTANCED_PROP(Props, _Color);
                
                // CRITICAL: Discard pixels that are transparent (leaves)
                clip(col.a - _Cutoff);
                
                return col * tint;
            }
            ENDCG
        }
    }
}