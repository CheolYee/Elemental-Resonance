// Simplified SDF shader:
// - No Shading Option (bevel / bump / env map)
// - No Glow Option
// - Softness is applied on both side of the outline

Shader "TMPEffect/Distance Field" {

Properties {
	[Header(Base)]
	_FaceColor			("Face Color", Color) = (1,1,1,1)
	_FaceDilate			("Face Dilate", Range(-1,1)) = 0

	[Header(Outline)]
	_OutlineColor		("Outline Color", Color) = (0,0,0,1)
	_OutlineWidth		("Outline Thickness", Range(0,1)) = 0
	_OutlineSoftness	("Outline Softness", Range(0,1)) = 0

	[Header(Outline2)]
	_OutlineColor2		("Outline Color2", Color) = (0,0,0,1)
	_OutlineWidth2		("Outline Thickness2", Range(0,1)) = 0

	[Header(Underlay)]
	_UnderlayColor		("Underlay Color", Color) = (0,0,0,.5)
	_UnderlayOffsetX 	("Underlay OffsetX", Range(-1,1)) = 0
	_UnderlayOffsetY 	("Underlay OffsetY", Range(-1,1)) = 0
	_UnderlayDilate		("Underlay Dilate", Range(-1,1)) = 0
	_UnderlaySoftness 	("Underlay Softness", Range(0,1)) = 0

	[Header(Glow)]
	_GlowColor("Glow Color", Color) = (0, 1, 0, 0.5)
	_GlowOffset("Glow Offset", Range(-1,1)) = 0
	_GlowInner("Glow Inner", Range(0,1)) = 0.05
	_GlowOuter("Glow Outer", Range(0,1)) = 0.05
	_GlowPower("Glow Power", Range(0,1)) = 0.75

	[HideInInspector]_WeightNormal		("Weight Normal", float) = 0
	[HideInInspector]_WeightBold			("Weight Bold", float) = .5

	[HideInInspector]_ShaderFlags		("Flags", float) = 0
	[HideInInspector]_ScaleRatioA		("Scale RatioA", float) = 1
	[HideInInspector]_ScaleRatioB		("Scale RatioB", float) = 1
	[HideInInspector]_ScaleRatioC		("Scale RatioC", float) = 1

	[HideInInspector]_MainTex			("Font Atlas", 2D) = "white" {}
	[HideInInspector]_TextureWidth		("Texture Width", float) = 512
	[HideInInspector]_TextureHeight		("Texture Height", float) = 512
	[HideInInspector]_GradientScale		("Gradient Scale", float) = 5
	[HideInInspector]_ScaleX				("Scale X", float) = 1
	[HideInInspector]_ScaleY				("Scale Y", float) = 1
	[HideInInspector]_PerspectiveFilter	("Perspective Correction", Range(0, 1)) = 0.875
	[HideInInspector]_Sharpness			("Sharpness", Range(-1,1)) = 0

	[HideInInspector]_VertexOffsetX		("Vertex OffsetX", float) = 0
	[HideInInspector]_VertexOffsetY		("Vertex OffsetY", float) = 0

	[HideInInspector]_ClipRect			("Clip Rect", vector) = (-32767, -32767, 32767, 32767)
	[HideInInspector]_MaskSoftnessX		("Mask SoftnessX", float) = 0
	[HideInInspector]_MaskSoftnessY		("Mask SoftnessY", float) = 0

	[HideInInspector]_StencilComp		("Stencil Comparison", Float) = 8
	[HideInInspector]_Stencil			("Stencil ID", Float) = 0
	[HideInInspector]_StencilOp			("Stencil Operation", Float) = 0
	[HideInInspector]_StencilWriteMask	("Stencil Write Mask", Float) = 255
	[HideInInspector]_StencilReadMask	("Stencil Read Mask", Float) = 255

	[HideInInspector]_CullMode			("Cull Mode", Float) = 0
	[HideInInspector]_ColorMask			("Color Mask", Float) = 15

	//alpha融合模式
	[HideInInspector]_SrcAlphaMode("__src_alpha",Float) = 1
	[HideInInspector]_DstAlphaMode("__dst_alpha",Float) = 10
}

SubShader {
	Tags
	{
		"Queue"="Transparent"
		"IgnoreProjector"="True"
		"RenderType"="Transparent"
	}


	Stencil
	{
		Ref [_Stencil]
		Comp [_StencilComp]
		Pass [_StencilOp]
		ReadMask [_StencilReadMask]
		WriteMask [_StencilWriteMask]
	}

	Cull [_CullMode]
	ZWrite Off
	Lighting Off
	Fog { Mode Off }
	ZTest [unity_GUIZTestMode]
	Blend [_SrcAlphaMode] [_DstAlphaMode]
	ColorMask [_ColorMask]

	Pass {
		CGPROGRAM
		#pragma vertex VertShader
		#pragma fragment PixShader
		#pragma multi_compile __ OUTLINE_ON
		#pragma multi_compile __ OUTLINE_ON_2
		#pragma multi_compile __ GLOW_ON
		#pragma multi_compile __ UNDERLAY_ON


		#pragma multi_compile __ UNITY_UI_CLIP_RECT
		#pragma multi_compile __ UNITY_UI_ALPHACLIP

		#pragma multi_compile __ USE_VERTEX_COLOR

		#include "UnityCG.cginc"
		#include "UnityUI.cginc"
		#include "TMPEffect_Properties.cginc"

		struct vertex_t {
			UNITY_VERTEX_INPUT_INSTANCE_ID
			float4	vertex			: POSITION;
			float3	normal			: NORMAL;
			fixed4	color			: COLOR;
			float2	texcoord0		: TEXCOORD0;
			float2	texcoord1		: TEXCOORD1;
		};

		struct pixel_t {
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
			float4	vertex			: SV_POSITION;
			fixed4	faceColor		: COLOR;
			#if OUTLINE_ON
			fixed4	outlineColor	: COLOR1;
			#if OUTLINE_ON_2
			fixed4 	outlineColor2   : COLOR2;
			#endif
			#endif
			float4	texcoord0		: TEXCOORD0;			// Texture UV, Mask UV
			half4	param			: TEXCOORD1;			// Scale(x), BiasIn(y), BiasOut(z), Bias(w)
			half4	mask			: TEXCOORD2;			// Position in clip space(xy), Softness(zw)

			#if GLOW_ON
			float2	glowParam		: TEXCOORD5;
			fixed4  glowColor       : COLOR3;
			#endif

			#if (UNDERLAY_ON)
			float4	texcoord1		: TEXCOORD3;			// Texture UV, alpha, reserved
			half2	underlayParam	: TEXCOORD4;			// Scale(x), Bias(y)
			#endif
		};

		half4 GetEffectColor(half4 originColor, half4 faceColor)
		{
			half4 resColor = originColor;
			#if USE_VERTEX_COLOR
			resColor = resColor * faceColor;
			#endif
			return resColor;
		}

		pixel_t VertShader(vertex_t input)
		{
			pixel_t output;

			UNITY_INITIALIZE_OUTPUT(pixel_t, output);
			UNITY_SETUP_INSTANCE_ID(input);
			UNITY_TRANSFER_INSTANCE_ID(input, output);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

			float bold = step(input.texcoord1.y, 0);

			float4 vert = input.vertex;
			vert.x += _VertexOffsetX;
			vert.y += _VertexOffsetY;

			float4 vPosition = UnityObjectToClipPos(vert);

			float2 pixelSize = vPosition.w;
			pixelSize /= float2(_ScaleX, _ScaleY) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

			float scale = rsqrt(dot(pixelSize, pixelSize));
			scale *= abs(input.texcoord1.y) * _GradientScale * (_Sharpness + 1);
			
			if (UNITY_MATRIX_P[3][3] == 0)
			{
				scale = lerp(abs(scale) * (1 - _PerspectiveFilter), scale, abs(dot(UnityObjectToWorldNormal(input.normal.xyz), normalize(WorldSpaceViewDir(vert)))));
			}

			float weight = lerp(_WeightNormal, _WeightBold, bold) / 4.0;
			
			weight = (weight + _FaceDilate) * _ScaleRatioA * 0.5;

			float layerScale = scale;

			scale /= 1 + (_OutlineSoftness * _ScaleRatioA * scale);
			float bias = (0.5 - weight) * scale - 0.5;


			#if GLOW_ON
			float glowBias = (0.5 - weight) + (.5 / scale);

			float alphaClip = (1.0 - max(_OutlineWidth,_OutlineWidth2) * _ScaleRatioA - _OutlineSoftness * _ScaleRatioA);

			alphaClip = min(alphaClip, 1.0 - _GlowOffset * _ScaleRatioB - _GlowOuter * _ScaleRatioB);

			alphaClip = alphaClip / 2.0 - (.5 / scale) - weight;
			#endif
			
			float outline = 0;
			float outline2 = 0;
			#if OUTLINE_ON
			outline = _OutlineWidth * _ScaleRatioA * 0.5 * scale;
			#if OUTLINE_ON_2
			outline2 = _OutlineWidth2 * _ScaleRatioA * 0.5 * scale;
			#endif
			#endif

			float opacity = input.color.a;

			#if (UNDERLAY_ON)
			opacity = 1.0;
			#endif

			fixed4 faceColor = fixed4(input.color.rgb, opacity) * _FaceColor;
			faceColor.rgb *= faceColor.a;

			#if OUTLINE_ON
			fixed4 outlineColor = _OutlineColor;
			outlineColor.a *= opacity;
			outlineColor.rgb *= outlineColor.a;
			outlineColor = lerp(faceColor, outlineColor, sqrt(min(1.0, (outline * 2))));
			#if OUTLINE_ON_2
			fixed4 outlineColor2 = _OutlineColor2;
			outlineColor2.a *= opacity;
			outlineColor2.rgb *= outlineColor2.a;
			outlineColor2 = lerp(faceColor, outlineColor2, sqrt(min(1.0, (outline2 * 2))));
			#endif
			#endif

			#if (UNDERLAY_ON)
			layerScale /= 1 + ((_UnderlaySoftness * _ScaleRatioC) * layerScale);
			float layerBias = (.5 - weight) * layerScale - .5 - ((_UnderlayDilate * _ScaleRatioC) * .5 * layerScale);

			float x = -(_UnderlayOffsetX * _ScaleRatioC) * _GradientScale / _TextureWidth;
			float y = -(_UnderlayOffsetY * _ScaleRatioC) * _GradientScale / _TextureHeight;
			float2 layerOffset = float2(x, y);
			#endif

			// Generate UV for the Masking Texture
			float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
			float2 maskUV = (vert.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);

			// Populate structure for pixel shader
			output.vertex = vPosition;
			output.faceColor = faceColor;
			#if OUTLINE_ON
			output.outlineColor = outlineColor;
			#if OUTLINE_ON_2
			output.outlineColor2 = outlineColor2;
			#endif
			#endif
			output.texcoord0 = float4(input.texcoord0.x, input.texcoord0.y, maskUV.x, maskUV.y);
			output.param = half4(scale, bias - outline, bias, bias - outline2);
			output.mask = half4(vert.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

			#if GLOW_ON
			output.glowParam = float2(alphaClip, glowBias);
			output.glowColor = _GlowColor;
			output.glowColor.a *= opacity;
			#endif

			#if (UNDERLAY_ON)
			output.texcoord1 = float4(input.texcoord0 + layerOffset, input.color.a, 0);
			output.underlayParam = half2(layerScale, layerBias);
			#endif

			return output;
		}


		
		fixed4 PixShader(pixel_t input) : SV_Target
		{
			UNITY_SETUP_INSTANCE_ID(input);
			

			half d = tex2D(_MainTex, input.texcoord0.xy).a * input.param.x;

			
			half4 c = input.faceColor * saturate(d - input.param.z);

			#if GLOW_ON
			float scale = input.param.x;
			float bias = input.glowParam.y;
			float sd = bias * scale - d;
			#endif 
	
			#ifdef OUTLINE_ON
			c = lerp(GetEffectColor(input.outlineColor, input.faceColor), input.faceColor, saturate(d - input.param.z));
			c *= saturate(d - input.param.y);
			#if OUTLINE_ON_2
			c = lerp(GetEffectColor(input.outlineColor2, input.faceColor), c, saturate(d - input.param.y));
			c *= saturate(d - input.param.w);
			#endif
			#endif

			#if UNDERLAY_ON
			d = tex2D(_MainTex, input.texcoord1.xy).a * input.underlayParam.x;
			half4 underlayColor = GetEffectColor(_UnderlayColor, input.faceColor);
			c += float4(underlayColor.rgb * underlayColor.a, underlayColor.a) * saturate(d - input.underlayParam.y) * (1 - c.a);
			#endif

			// Alternative implementation to UnityGet2DClipping with support for softness.
			#if UNITY_UI_CLIP_RECT
			half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(input.mask.xy)) * input.mask.zw);
			c *= m.x * m.y;
			#endif

			#if (UNDERLAY_ON)
			c *= input.texcoord1.z;
			#endif

			#if GLOW_ON
			float glow = sd - (_GlowOffset * _ScaleRatioB) * 0.5 * scale;
			float t = lerp(_GlowInner, (_GlowOuter * _ScaleRatioB), step(0.0, glow)) * 0.5 * scale;
			glow = saturate(abs(glow / (1.0 + t)));
			glow = 1.0 - pow(glow, _GlowPower);
			glow *= sqrt(min(1.0, t)); // Fade off glow thinner than 1 screen pixel
			half4 glowColor = GetEffectColor(input.glowColor, input.faceColor);
			glowColor = float4(glowColor.rgb, saturate(glowColor.a * glow * 2));
			c.rgb += glowColor.rgb * glowColor.a;
			#endif


			#if UNITY_UI_ALPHACLIP
			clip(c.a - 0.001);
			#endif

			return c;
		}
		ENDCG
	}
}
}
