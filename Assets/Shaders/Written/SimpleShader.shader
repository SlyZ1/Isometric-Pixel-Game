// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/SimpleShader"
{
    Properties
    {
        _MainTex ("Texture2D", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "white" {}
        _Emission("Emission", float) = 0
        _FallOf("Fall Of", float) = 0.68
        _Fullfilness("Fulfilness", float) = 0.15
        [Toggle] _IsUI("Is UI", float) = 0
    }
    SubShader
    {
        Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}
 
        Pass
        { 
            Cull Off
		    Lighting Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha 
  
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile DUMMY PIXELSNAP_ON

            #include "UnityCG.cginc"
  
            sampler2D _MainTex;
            sampler2D _NormalMap;
            sampler2D _HeightMap;
            float _FallOf;
            float _Fullfilness;
            float _Emission;
            fixed4 _sunColor;
            bool _IsUI;

            float4 _MovingLightArray[200];
            float4 _MovingLightColors[200];
            int _NumOfMovingLights;
            float2 _Screen;
 
            struct Vertex
            {
                float4 vertex : POSITION;
                float2 uv_MainTex : TEXCOORD0;
                half4 color : COLOR;
                float2 uv2 : TEXCOORD1;
            };
     
            struct Fragment
            {
                float4 vertex : POSITION;
                float2 uv_MainTex : TEXCOORD0;
                half4 color : COLOR;
                float2 uv2 : TEXCOORD1;
            };
  
            Fragment vert(Vertex v)
            {
                Fragment o;
     
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv_MainTex = v.uv_MainTex;
                fixed4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv2 = float2(worldPos.x, worldPos.y);

                o.color = v.color;
     
                return o;
            }

            fixed4 lights(float3 normal, Fragment o, float height)
            {
                half realHeight = (height - 0.50588);

                fixed ratio = _Screen.x / _Screen.y;

                fixed4 worldSpace = fixed4(o.uv2, 0, 1);

                fixed4 clipSpace = mul(UNITY_MATRIX_VP, worldSpace);
                fixed2 value = fixed2((1 + clipSpace.x)/ 2, 1-(1+clipSpace.y)/ 2);

                fixed3 screenPos = o.vertex / _Screen.y - half3(0,realHeight * 16 * 4 / 3 / 100,0);
                fixed2 screenPos2D = fixed2(screenPos.x, screenPos.y * 2);

                fixed totalDistance = 0;
                fixed4 color;

                for(int i = 0; i < 200; i++)
                {
                    if(i >= _NumOfMovingLights) break;

                    float4 _light = _MovingLightArray[i];
                    half2 distanceVector = half2(_light.x * ratio, _light.y * 2) - screenPos2D;

                    if(abs(distanceVector.x) + abs(distanceVector.y) >= _light.z * _FallOf * 1.5) continue;
                    
                    half realDistance = length(distanceVector);
                    half distance = clamp(1 - pow(realDistance /_light.z / _FallOf, 3),0,1);

                    if(distance * _light.w > totalDistance){
                        totalDistance = distance * _light.w;
                        color = _MovingLightColors[i];
                    }
                    
                }

                return lerp(0,totalDistance, 1 - length(_sunColor) / 3) * color;
            }


            fixed4 globalLight()
            {
                return _sunColor;
            }
                                                     
            float4 frag(Fragment IN) : COLOR
            {
                half4 o = half4(0,0,0,1);
 
                half4 c = tex2D (_MainTex, IN.uv_MainTex);
                o.rgb = fixed4(0,0,0,1); 
                o.a = c.a * IN.color.a;

                if(o.a <= 0.01) return o;

                half height = tex2D (_HeightMap, IN.uv_MainTex);

                float3 normal = UnpackNormalmapRGorAG(tex2D(_NormalMap, IN.uv_MainTex));
                if(_IsUI)
                {
                    o.rgb = IN.color * globalLight();
                    float3 result = IN.color * lights(normal, IN, height) * (sqrt(3) - length(o.rgb)) / sqrt(3);
                    o.rgb += result;
                }
                else
                {
                    o.rgb = IN.color * c.rgb * globalLight();
                    float3 result = c.rgb * IN.color * lights(normal, IN, height) * (sqrt(3) - length(o.rgb)) / sqrt(3);
                    o.rgb += result;
                    o.rgb += IN.color * c.rgb * _Emission;
                }
                
                     
                return o;
            }
 
            ENDCG
        }
    }
}
