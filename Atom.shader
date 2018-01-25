Shader "Unlit/MetaBall2"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_RadialModifier("Radial Modifier", Range(1.0, 120.0)) = 10

		[Toggle(RANDOM_FORE_COLOR)]
		_RandomForegroundColor("Randomize Foreground", Float) = 1

		[Toggle(RANDOM_BACK_COLOR)]
		_RandomBackgroundColor("Randomize Background", Float) = 1

		_ForegroundColor("ForegroundColor", Color) = (1, 0, 0, 1)
		_BackgroundColor("BackgroundColor", Color) = (0, 0, 0, 0)

		_NucleusAttraction("Nucleus Attraction", Range(0.0, 10)) = 2.48
		_NucleusRepulsion("Nucleus Repulsion", Range(0.0, 0.5)) = 0.171
		_NucleusSize("Nucleus Size", Range(1.0, 45.0)) = 8.9 
		_ElectronCount("Electron Count", Range(1.0, 58000.0)) = 41.0
		_ElectronSize("Electron Size", Range(1.0, 8.0)) = 3.62
		_ElectronSpeed("Electron Speed", Range(0.1, 17.0)) = 8.1
	}
	SubShader
	{
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature RANDOM_FORE_COLOR
			#pragma shader_feature RANDOM_BACK_COLOR
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			fixed4 _ForegroundColor;
			fixed4 _BackgroundColor;
			float _RadialModifier;
			float _NucleusAttraction;
			float _NucleusRepulsion;
			float _NucleusSize;
			float _ElectronCount;
			float _ElectronSize;
			float _ElectronSpeed;

			float _RandomForegroundColor;
			float _RandomBackgroundColor;

			float dstElectron(float2 pos, float2 center, float radius)
			{
				float2 diff = pos - center;
				return radius / dot(diff, diff);
			}
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// Set constants for atom calculations
				const float NUCLEUS_OFFSET = 0.5;
				const float SPAWN_OFFSET = 0.15;
				const float COLOR_THRESHOLD = 0.001;

				// Setup positions
				float aspect = _ScreenParams.x / _ScreenParams.y;
				float2 tex = i.vertex.xy / _ScreenParams.xy;
				tex.x *= aspect;
				tex -= clamp(float2(aspect, 1.0 / aspect) - 1.0, 0.0, 1.0)  * 0.5;

				// Keep track of accumulated distance between electrons
				float dst = 0.0;

				// Add the nucleus in the middle
				dst += dstElectron(tex, float2(NUCLEUS_OFFSET, NUCLEUS_OFFSET), _ElectronSize) * _NucleusSize;

				// Initialize electrons
				float2 electronPos = float2(SPAWN_OFFSET, 0.0);
				float angle = radians(_RadialModifier + _Time.y * _ElectronSpeed);

				// Setup rotation matrix (float2x2)
				float4 matRotation = float4(cos(angle), -sin(angle), sin(angle), cos(angle));

				// Iterate over the electrons, sum up the distance and apply movement
				for (int i = 0; i < _ElectronCount; ++i) 
				{
					// Apply rotation to the electrons
					float2 origin = electronPos;
					electronPos.x = (matRotation[0] * origin.x) + (matRotation[1] * origin.y);
					electronPos.y = (matRotation[2] * origin.x) + (matRotation[3] * origin.y);

					// Apply electron pull-push relationship with nucleus
					float2 dir = float2(sin(i), cos(i));
					electronPos += sign(float(i) - _NucleusAttraction) * sin(_Time.y * _ElectronSpeed * 0.1) * 0.5 * dir * _NucleusRepulsion;

					// Accumulate distance
					dst += dstElectron(tex, electronPos + NUCLEUS_OFFSET, _ElectronSize);
				}

				// Normalize the distance
				dst /= _ElectronCount + 1.0;

				// Modify the intensity and color of the atom
				float3 atomColor = float3(sin(_Time.y * _ElectronSpeed * 0.1),
					sin(_Time.y * _ElectronSpeed * 0.09),
					sin(_Time.y * _ElectronSpeed * 0.08));

				atomColor = normalize(atomColor);

				if (_RandomForegroundColor > 0.5) 
				{
					atomColor = atomColor * 0.5 + 0.5;
				}
				else 
				{
					atomColor = atomColor * _ForegroundColor + _ForegroundColor;
				}

				float3 backColor = _BackgroundColor;

				if (_RandomBackgroundColor > 0.5)
				{
					backColor = float3(sin(_Time.y * _ElectronSpeed / 2 * 0.1),
						sin(_Time.y * _ElectronSpeed / 2 * 0.09),
						sin(_Time.y * _ElectronSpeed / 2 * 0.08));
				}

				// Interpolate between colors
				float3 color = lerp(backColor, atomColor, dst * COLOR_THRESHOLD);
				return fixed4(color, 1.0);
			}

			ENDCG
		}
	}
}
