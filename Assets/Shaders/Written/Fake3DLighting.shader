// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/Fake3DLighting"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "white" {}
        _PixelResolution("Pixel Resolution", float) = 100
        _InstancePixelDimensions("Instance Dimensions In Pixel", Vector) = (0,0,0,1)
        _CenterUvCoord("Center coordinates in UV", Vector) = (0,0,0,1)
        _NumOfInstances("Num Of Instances", Vector) = (0,0,0,1)
        _FallOf("Fall Of", float) = 0.68
        _Fullfilness("Fulfilness", float) = 0.15
        [Toggle] _Normals("Enable Normals", float) = 0
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
            #pragma multi_compile_instancing
            #pragma multi_compile DUMMY PIXELSNAP_ON

            #include "UnityCG.cginc"
  
            sampler2D _MainTex;
            float4  _MainTex_ST;
            sampler2D _NormalMap;
            sampler2D _HeightMap;
            float _FallOf;
            float _Fullfilness;
            float _PixelResolution;
            float4 _InstancePixelDimensions;
            float4 _CenterUvCoord;
            float4 _sunColor;
            float4 _NumOfInstances;
            float2 _Screen;

            bool _Normals;

            float4 _MovingLightArray[200];
            float4 _MovingLightColors[200];
            int _NumOfMovingLights;
 
            struct Vertex
            {
                float4 vertex : POSITION;
                float2 uv_MainTex : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float4 color : COLOR;
            };
     
            struct Fragment
            {
                float4 vertex : POSITION;
                float2 uv_MainTex : TEXCOORD0;
                float2 value : TEXCOORD1;
                float4 color : COLOR;
            };
  
            Fragment vert(Vertex v)
            {
                Fragment o;
     
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv_MainTex = v.uv_MainTex;
                o.color = v.color;

                fixed valueX = v.uv_MainTex.x * _NumOfInstances.x - floor(v.uv_MainTex.x * _NumOfInstances.x);
                fixed valueY = v.uv_MainTex.y * _NumOfInstances.y - floor(v.uv_MainTex.y * _NumOfInstances.y);

                fixed4 worldPos = mul(unity_ObjectToWorld, v.vertex);

                fixed4 worldSpace = fixed4(
                    (_CenterUvCoord.x - valueX) * _InstancePixelDimensions.x / _PixelResolution + worldPos.x, 
                    (_CenterUvCoord.y - valueY) * _InstancePixelDimensions.y / _PixelResolution + worldPos.y, 
                    0, 
                    1
                );

                fixed4 clipSpace = mul(UNITY_MATRIX_VP, worldSpace);
                o.value = fixed2((1 + clipSpace.x)/ 2, 1-(1+clipSpace.y)/ 2);
     
                return o;
            }

            fixed4 lights(float3 normal, Fragment o, half height, bool is3D)
            {
                half realHeight = (height - 0.50588);

                fixed ratio = _Screen.x / _Screen.y;
                fixed2 value = o.value;
                fixed3 screenPos = o.vertex / _Screen.y - half3(0,realHeight * 16 * 4 / 3 / 100,0);
                fixed2 screenPos2D = fixed2(screenPos.x, screenPos.y * 2);

                fixed4 totalResult = fixed4(0,0,0,1);
                fixed totalDistance = 0;
                fixed4 color;

                for(int i = 0; i < 200; i++)
                {
                    if(i >= _NumOfMovingLights) break;

                    fixed4 _light = _MovingLightArray[i];
                    half2 distanceVector = half2(_light.x * ratio, _light.y * 2) - screenPos2D;

                    if(abs(distanceVector.x) + abs(distanceVector.y) >= _light.z * _FallOf * 1.5) continue;

                    float factor = 1;
                    if(_light.y >= value.y && is3D)
                    {
                        fixed2 jsp = normalize(fixed2(_light.x, _light.y) - fixed2(value.x, value.y));
                        fixed angle = acos(dot(jsp, fixed2(0,1)));
                        fixed pi = 3.14159265;

                        factor = (angle - pi/6) / (pi/3);
                        factor = clamp(factor,0,1);
                    }

                    fixed2 worldPosLight = fixed2(_light.x * ratio, _light.y * 2);

                    half realDistance = length(distanceVector);
                    half distance = clamp(1 - pow(realDistance /_light.z / _FallOf, 3),0,1);
                    half result; 
                    if(_Normals) 
                    {
                        result = dot(normalize(fixed3(worldPosLight, 0.1) - half3(screenPos2D, screenPos.z)) * distance, normal) * factor * _light.w;
                    }
                    else
                    {
                        result = distance * _light.w * factor;
                    }

                    if(result > totalDistance){
                        totalDistance = result;
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
                if(IN.color.a > 0.01)
                {
                    o.a = c.a * IN.color.a;
                }
                else
                {
                    o.a = c.a;
                }

                if(o.a <= 0.01) return o;

                float height = tex2D(_HeightMap, IN.uv_MainTex);

                float3 normal = UnpackNormalmapRGorAG(tex2D(_NormalMap, IN.uv_MainTex));

                o.rgb = c.rgb * IN.color.rgb * globalLight();
                if(IN.color.a > 0.01)
                {
                    o.rgb += c.rgb * IN.color.rgb * lights(normal, IN, height, true) * (sqrt(3) - length(o.rgb)) / sqrt(3);
                }
                else
                {
                    o.rgb += c.rgb * IN.color.rgb * lights(normal, IN, height, false) * (sqrt(3) - length(o.rgb)) / sqrt(3);
                }
                     
                return o;
            }
 
            ENDCG
        }
    }
}
