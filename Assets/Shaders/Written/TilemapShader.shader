// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/TilemapShader"
{
    Properties
    {
        _MainTex ("Texture2D", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "white" {}
        _HeightMap("HeightMap", 2D) = "white" {}
        _FallOf("Fall Of", float) = 0.68
        _Fullfilness("Fulfilness", float) = 0.15
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
		    Lighting Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha 
  
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile

            #include "UnityCG.cginc"
  
            sampler2D _MainTex;
            sampler2D _NormalMap;
            sampler2D _HeightMap;
            sampler2D _LightTex;

            half4 _sunColor;

            half4 _MovingLightArray[200];
            half4 _MovingLightColors[200];
            int _NumOfMovingLights;
            float _FallOf;
            float _Fullfilness;
            float2 _Screen;
 
            struct Vertex
            {
                half4 vertex : POSITION;
                half4 color : COLOR;
                half2 uv_MainTex : TEXCOORD0;
            };
     
            struct Fragment
            {
                half4 vertex : POSITION;
                half4 color : COLOR;
                half2 uv_MainTex : TEXCOORD0;
            };
  
            Fragment vert(Vertex v)
            {
                Fragment o;
     
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv_MainTex = v.uv_MainTex;
                o.color = v.color;
     
                return o;
            }

            half4 lights(float3 normal, Fragment o, float height)
            {
                half realHeight = (height - 0.50588); // 129/255 = 0.50588

                fixed ratio = _Screen.x / _Screen.y;
                half3 screenPos = (o.vertex) / _Screen.y - half3(0,realHeight * 16 * 4 / 3 / 100,0);
                half2 screenPos2D = half2(screenPos.x, screenPos.y * 2);

                half4 totalResult = half4(0,0,0,1);
                half totalDistance = 0;
                half4 color;

                for(int i = 0; i < 200; i++)
                {
                    if(i >= _NumOfMovingLights) break;

                    half4 _light = _MovingLightArray[i];
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


            half4 globalLight()
            {
                return _sunColor;
            }
                                                     
            half4 frag(Fragment IN) : COLOR
            {
                half4 o = half4(0,0,0,1);
 
                half4 c = tex2D (_MainTex, IN.uv_MainTex);
                o.a = c.a;

                if(o.a <= 0.01) return o;

                half height = tex2D (_HeightMap, IN.uv_MainTex);

                half3 normal = UnpackNormalmapRGorAG(tex2D(_NormalMap, IN.uv_MainTex));
                o.rgb = IN.color * c.rgb * globalLight();
                o.rgb += c.rgb * IN.color * lights(normal, IN, height) * (sqrt(3) - length(o.rgb)) / sqrt(3);

                return o;
            }
 
            ENDCG
        }
    }
}
