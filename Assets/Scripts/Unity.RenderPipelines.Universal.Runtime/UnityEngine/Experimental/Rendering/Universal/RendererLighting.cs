using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
	internal static class RendererLighting
	{
		private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Clear Normals");

		private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");

		private static readonly Color k_NormalClearColor = new Color(0.5f, 0.5f, 1f, 1f);

		private static readonly string k_SpriteLightKeyword = "SPRITE_LIGHT";

		private static readonly string k_UsePointLightCookiesKeyword = "USE_POINT_LIGHT_COOKIES";

		private static readonly string k_LightQualityFastKeyword = "LIGHT_QUALITY_FAST";

		private static readonly string k_UseNormalMap = "USE_NORMAL_MAP";

		private static readonly string k_UseAdditiveBlendingKeyword = "USE_ADDITIVE_BLENDING";

		private static readonly string[] k_UseBlendStyleKeywords = new string[4] { "USE_SHAPE_LIGHT_TYPE_0", "USE_SHAPE_LIGHT_TYPE_1", "USE_SHAPE_LIGHT_TYPE_2", "USE_SHAPE_LIGHT_TYPE_3" };

		private static readonly int[] k_BlendFactorsPropIDs = new int[4]
		{
			Shader.PropertyToID("_ShapeLightBlendFactors0"),
			Shader.PropertyToID("_ShapeLightBlendFactors1"),
			Shader.PropertyToID("_ShapeLightBlendFactors2"),
			Shader.PropertyToID("_ShapeLightBlendFactors3")
		};

		private static readonly int[] k_MaskFilterPropIDs = new int[4]
		{
			Shader.PropertyToID("_ShapeLightMaskFilter0"),
			Shader.PropertyToID("_ShapeLightMaskFilter1"),
			Shader.PropertyToID("_ShapeLightMaskFilter2"),
			Shader.PropertyToID("_ShapeLightMaskFilter3")
		};

		private static readonly int[] k_InvertedFilterPropIDs = new int[4]
		{
			Shader.PropertyToID("_ShapeLightInvertedFilter0"),
			Shader.PropertyToID("_ShapeLightInvertedFilter1"),
			Shader.PropertyToID("_ShapeLightInvertedFilter2"),
			Shader.PropertyToID("_ShapeLightInvertedFilter3")
		};

		private static GraphicsFormat s_RenderTextureFormatToUse = GraphicsFormat.R8G8B8A8_UNorm;

		private static bool s_HasSetupRenderTextureFormatToUse;

		private static readonly int k_SrcBlendID = Shader.PropertyToID("_SrcBlend");

		private static readonly int k_DstBlendID = Shader.PropertyToID("_DstBlend");

		private static readonly int k_FalloffIntensityID = Shader.PropertyToID("_FalloffIntensity");

		private static readonly int k_FalloffDistanceID = Shader.PropertyToID("_FalloffDistance");

		private static readonly int k_FalloffOffsetID = Shader.PropertyToID("_FalloffOffset");

		private static readonly int k_LightColorID = Shader.PropertyToID("_LightColor");

		private static readonly int k_VolumeOpacityID = Shader.PropertyToID("_VolumeOpacity");

		private static readonly int k_CookieTexID = Shader.PropertyToID("_CookieTex");

		private static readonly int k_FalloffLookupID = Shader.PropertyToID("_FalloffLookup");

		private static readonly int k_LightPositionID = Shader.PropertyToID("_LightPosition");

		private static readonly int k_LightInvMatrixID = Shader.PropertyToID("_LightInvMatrix");

		private static readonly int k_LightNoRotInvMatrixID = Shader.PropertyToID("_LightNoRotInvMatrix");

		private static readonly int k_InnerRadiusMultID = Shader.PropertyToID("_InnerRadiusMult");

		private static readonly int k_OuterAngleID = Shader.PropertyToID("_OuterAngle");

		private static readonly int k_InnerAngleMultID = Shader.PropertyToID("_InnerAngleMult");

		private static readonly int k_LightLookupID = Shader.PropertyToID("_LightLookup");

		private static readonly int k_IsFullSpotlightID = Shader.PropertyToID("_IsFullSpotlight");

		private static readonly int k_LightZDistanceID = Shader.PropertyToID("_LightZDistance");

		private static readonly int k_PointLightCookieTexID = Shader.PropertyToID("_PointLightCookieTex");

		private static GraphicsFormat GetRenderTextureFormat()
		{
			if (!s_HasSetupRenderTextureFormatToUse)
			{
				if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Blend))
				{
					s_RenderTextureFormatToUse = GraphicsFormat.B10G11R11_UFloatPack32;
				}
				else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Blend))
				{
					s_RenderTextureFormatToUse = GraphicsFormat.R16G16B16A16_SFloat;
				}
				s_HasSetupRenderTextureFormatToUse = true;
			}
			return s_RenderTextureFormatToUse;
		}

		public static void CreateNormalMapRenderTexture(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd)
		{
			RenderTextureDescriptor desc = new RenderTextureDescriptor(renderingData.cameraData.cameraTargetDescriptor.width, renderingData.cameraData.cameraTargetDescriptor.height);
			desc.graphicsFormat = GetRenderTextureFormat();
			desc.useMipMap = false;
			desc.autoGenerateMips = false;
			desc.depthBufferBits = 0;
			desc.msaaSamples = renderingData.cameraData.cameraTargetDescriptor.msaaSamples;
			desc.dimension = TextureDimension.Tex2D;
			cmd.GetTemporaryRT(pass.rendererData.normalsRenderTarget.id, desc, FilterMode.Bilinear);
		}

		public static void CreateBlendStyleRenderTexture(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int blendStyleIndex)
		{
			float num = Mathf.Clamp(pass.rendererData.lightBlendStyles[blendStyleIndex].renderTextureScale, 0.01f, 1f);
			int width = (int)((float)renderingData.cameraData.cameraTargetDescriptor.width * num);
			int height = (int)((float)renderingData.cameraData.cameraTargetDescriptor.height * num);
			RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height);
			desc.graphicsFormat = GetRenderTextureFormat();
			desc.useMipMap = false;
			desc.autoGenerateMips = false;
			desc.depthBufferBits = 0;
			desc.msaaSamples = 1;
			desc.dimension = TextureDimension.Tex2D;
			ref Light2DBlendStyle reference = ref pass.rendererData.lightBlendStyles[blendStyleIndex];
			cmd.GetTemporaryRT(reference.renderTargetHandle.id, desc, FilterMode.Bilinear);
			reference.hasRenderTarget = true;
			reference.isDirty = true;
		}

		public static void EnableBlendStyle(CommandBuffer cmd, int blendStyleIndex, bool enabled)
		{
			string keyword = k_UseBlendStyleKeywords[blendStyleIndex];
			if (enabled)
			{
				cmd.EnableShaderKeyword(keyword);
			}
			else
			{
				cmd.DisableShaderKeyword(keyword);
			}
		}

		public static void ReleaseRenderTextures(this IRenderPass2D pass, CommandBuffer cmd)
		{
			for (int i = 0; i < pass.rendererData.lightBlendStyles.Length; i++)
			{
				if (pass.rendererData.lightBlendStyles[i].hasRenderTarget)
				{
					pass.rendererData.lightBlendStyles[i].hasRenderTarget = false;
					cmd.ReleaseTemporaryRT(pass.rendererData.lightBlendStyles[i].renderTargetHandle.id);
				}
			}
			cmd.ReleaseTemporaryRT(pass.rendererData.normalsRenderTarget.id);
			cmd.ReleaseTemporaryRT(pass.rendererData.shadowsRenderTarget.id);
		}

		private static bool RenderLightSet(IRenderPass2D pass, RenderingData renderingData, int blendStyleIndex, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTexture, bool rtNeedsClear, Color clearColor, List<Light2D> lights)
		{
			bool flag = false;
			foreach (Light2D light in lights)
			{
				if (!(light != null) || light.lightType == Light2D.LightType.Global || light.blendStyleIndex != blendStyleIndex || !light.IsLitLayer(layerToRender))
				{
					continue;
				}
				Material lightMaterial = pass.rendererData.GetLightMaterial(light, isVolume: false);
				if (lightMaterial == null)
				{
					continue;
				}
				Mesh lightMesh = light.lightMesh;
				if (!(lightMesh == null))
				{
					ShadowRendering.RenderShadows(pass, renderingData, cmd, layerToRender, light, light.shadowIntensity, renderTexture, renderTexture);
					if (!flag && rtNeedsClear)
					{
						cmd.ClearRenderTarget(clearDepth: false, clearColor: true, clearColor);
					}
					flag = true;
					if (light.lightType == Light2D.LightType.Sprite && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
					{
						cmd.SetGlobalTexture(k_CookieTexID, light.lightCookieSprite.texture);
					}
					cmd.SetGlobalFloat(k_FalloffIntensityID, light.falloffIntensity);
					cmd.SetGlobalFloat(k_FalloffDistanceID, light.shapeLightFalloffSize);
					cmd.SetGlobalVector(k_FalloffOffsetID, light.shapeLightFalloffOffset);
					cmd.SetGlobalColor(k_LightColorID, light.intensity * light.color);
					cmd.SetGlobalFloat(k_VolumeOpacityID, light.volumeOpacity);
					if (light.useNormalMap || light.lightType == Light2D.LightType.Point)
					{
						SetPointLightShaderGlobals(cmd, light);
					}
					if (light.lightType == Light2D.LightType.Parametric || light.lightType == Light2D.LightType.Freeform || light.lightType == Light2D.LightType.Sprite)
					{
						cmd.DrawMesh(lightMesh, light.transform.localToWorldMatrix, lightMaterial);
					}
					else if (light.lightType == Light2D.LightType.Point)
					{
						Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(light.pointLightOuterRadius, light.pointLightOuterRadius, light.pointLightOuterRadius), pos: light.transform.position, q: Quaternion.identity);
						cmd.DrawMesh(lightMesh, matrix, lightMaterial);
					}
				}
			}
			if (!flag && rtNeedsClear)
			{
				cmd.ClearRenderTarget(clearDepth: false, clearColor: true, clearColor);
			}
			return flag;
		}

		private static void RenderLightVolumeSet(IRenderPass2D pass, RenderingData renderingData, int blendStyleIndex, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTexture, RenderTargetIdentifier depthTexture, List<Light2D> lights)
		{
			if (lights.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < lights.Count; i++)
			{
				Light2D light2D = lights[i];
				int topMostLitLayer = light2D.GetTopMostLitLayer();
				if (layerToRender != topMostLitLayer || !(light2D != null) || light2D.lightType == Light2D.LightType.Global || !(light2D.volumeOpacity > 0f) || light2D.blendStyleIndex != blendStyleIndex || !light2D.IsLitLayer(layerToRender))
				{
					continue;
				}
				Material lightMaterial = pass.rendererData.GetLightMaterial(light2D, isVolume: true);
				if (!(lightMaterial != null))
				{
					continue;
				}
				Mesh lightMesh = light2D.lightMesh;
				if (lightMesh != null)
				{
					ShadowRendering.RenderShadows(pass, renderingData, cmd, layerToRender, light2D, light2D.shadowVolumeIntensity, renderTexture, depthTexture);
					if (light2D.lightType == Light2D.LightType.Sprite && light2D.lightCookieSprite != null && light2D.lightCookieSprite.texture != null)
					{
						cmd.SetGlobalTexture(k_CookieTexID, light2D.lightCookieSprite.texture);
					}
					cmd.SetGlobalFloat(k_FalloffIntensityID, light2D.falloffIntensity);
					cmd.SetGlobalFloat(k_FalloffDistanceID, light2D.shapeLightFalloffSize);
					cmd.SetGlobalVector(k_FalloffOffsetID, light2D.shapeLightFalloffOffset);
					cmd.SetGlobalColor(k_LightColorID, light2D.intensity * light2D.color);
					cmd.SetGlobalFloat(k_VolumeOpacityID, light2D.volumeOpacity);
					if (light2D.useNormalMap || light2D.lightType == Light2D.LightType.Point)
					{
						SetPointLightShaderGlobals(cmd, light2D);
					}
					if (light2D.lightType == Light2D.LightType.Parametric || light2D.lightType == Light2D.LightType.Freeform || light2D.lightType == Light2D.LightType.Sprite)
					{
						cmd.DrawMesh(lightMesh, light2D.transform.localToWorldMatrix, lightMaterial);
					}
					else if (light2D.lightType == Light2D.LightType.Point)
					{
						Matrix4x4 matrix = Matrix4x4.TRS(s: new Vector3(light2D.pointLightOuterRadius, light2D.pointLightOuterRadius, light2D.pointLightOuterRadius), pos: light2D.transform.position, q: Quaternion.identity);
						cmd.DrawMesh(lightMesh, matrix, lightMaterial);
					}
				}
			}
		}

		public static void SetShapeLightShaderGlobals(this IRenderPass2D pass, CommandBuffer cmd)
		{
			for (int i = 0; i < pass.rendererData.lightBlendStyles.Length; i++)
			{
				Light2DBlendStyle light2DBlendStyle = pass.rendererData.lightBlendStyles[i];
				if (i >= k_BlendFactorsPropIDs.Length)
				{
					break;
				}
				cmd.SetGlobalVector(k_BlendFactorsPropIDs[i], light2DBlendStyle.blendFactors);
				cmd.SetGlobalVector(k_MaskFilterPropIDs[i], light2DBlendStyle.maskTextureChannelFilter.mask);
				cmd.SetGlobalVector(k_InvertedFilterPropIDs[i], light2DBlendStyle.maskTextureChannelFilter.inverted);
			}
			cmd.SetGlobalTexture(k_FalloffLookupID, Light2DLookupTexture.GetFalloffLookupTexture());
		}

		private static float GetNormalizedInnerRadius(Light2D light)
		{
			return light.pointLightInnerRadius / light.pointLightOuterRadius;
		}

		private static float GetNormalizedAngle(float angle)
		{
			return angle / 360f;
		}

		private static void GetScaledLightInvMatrix(Light2D light, out Matrix4x4 retMatrix, bool includeRotation)
		{
			float pointLightOuterRadius = light.pointLightOuterRadius;
			Vector3 one = Vector3.one;
			Vector3 s = new Vector3(one.x * pointLightOuterRadius, one.y * pointLightOuterRadius, one.z * pointLightOuterRadius);
			Transform transform = light.transform;
			Quaternion q = (includeRotation ? transform.rotation : Quaternion.identity);
			Matrix4x4 m = Matrix4x4.TRS(transform.position, q, s);
			retMatrix = Matrix4x4.Inverse(m);
		}

		private static void SetPointLightShaderGlobals(CommandBuffer cmd, Light2D light)
		{
			GetScaledLightInvMatrix(light, out var retMatrix, includeRotation: true);
			GetScaledLightInvMatrix(light, out var retMatrix2, includeRotation: false);
			float normalizedInnerRadius = GetNormalizedInnerRadius(light);
			float normalizedAngle = GetNormalizedAngle(light.pointLightInnerAngle);
			float normalizedAngle2 = GetNormalizedAngle(light.pointLightOuterAngle);
			float value = 1f / (1f - normalizedInnerRadius);
			cmd.SetGlobalVector(k_LightPositionID, light.transform.position);
			cmd.SetGlobalMatrix(k_LightInvMatrixID, retMatrix);
			cmd.SetGlobalMatrix(k_LightNoRotInvMatrixID, retMatrix2);
			cmd.SetGlobalFloat(k_InnerRadiusMultID, value);
			cmd.SetGlobalFloat(k_OuterAngleID, normalizedAngle2);
			cmd.SetGlobalFloat(k_InnerAngleMultID, 1f / (normalizedAngle2 - normalizedAngle));
			cmd.SetGlobalTexture(k_LightLookupID, Light2DLookupTexture.GetLightLookupTexture());
			cmd.SetGlobalTexture(k_FalloffLookupID, Light2DLookupTexture.GetFalloffLookupTexture());
			cmd.SetGlobalFloat(k_FalloffIntensityID, light.falloffIntensity);
			cmd.SetGlobalFloat(k_IsFullSpotlightID, (normalizedAngle == 1f) ? 1f : 0f);
			cmd.SetGlobalFloat(k_LightZDistanceID, light.pointLightDistance);
			if (light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
			{
				cmd.SetGlobalTexture(k_PointLightCookieTexID, light.lightCookieSprite.texture);
			}
		}

		public static void ClearDirtyLighting(this IRenderPass2D pass, CommandBuffer cmd, uint blendStylesUsed)
		{
			for (int i = 0; i < pass.rendererData.lightBlendStyles.Length; i++)
			{
				if ((blendStylesUsed & (uint)(1 << i)) != 0 && pass.rendererData.lightBlendStyles[i].isDirty)
				{
					cmd.SetRenderTarget(pass.rendererData.lightBlendStyles[i].renderTargetHandle.Identifier());
					cmd.ClearRenderTarget(clearDepth: false, clearColor: true, Color.black);
					pass.rendererData.lightBlendStyles[i].isDirty = false;
				}
			}
		}

		public static void RenderNormals(this IRenderPass2D pass, ScriptableRenderContext context, CullingResults cullResults, DrawingSettings drawSettings, FilteringSettings filterSettings, RenderTargetIdentifier depthTarget)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
			{
				commandBuffer.SetRenderTarget(pass.rendererData.normalsRenderTarget.Identifier(), depthTarget);
				commandBuffer.ClearRenderTarget(clearDepth: true, clearColor: true, k_NormalClearColor);
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
			drawSettings.SetShaderPassName(0, k_NormalsRenderingPassName);
			context.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
		}

		public static void RenderLights(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int layerToRender, uint blendStylesUsed)
		{
			Light2DBlendStyle[] lightBlendStyles = pass.rendererData.lightBlendStyles;
			for (int i = 0; i < lightBlendStyles.Length; i++)
			{
				if ((blendStylesUsed & (uint)(1 << i)) != 0)
				{
					string name = lightBlendStyles[i].name;
					cmd.BeginSample(name);
					RenderTargetIdentifier renderTargetIdentifier = pass.rendererData.lightBlendStyles[i].renderTargetHandle.Identifier();
					cmd.SetRenderTarget(renderTargetIdentifier);
					bool flag = false;
					if (!Light2DManager.GetGlobalColor(layerToRender, i, out var color))
					{
						color = Color.black;
					}
					else
					{
						flag = true;
					}
					flag |= RenderLightSet(pass, renderingData, i, cmd, layerToRender, renderTargetIdentifier, pass.rendererData.lightBlendStyles[i].isDirty || flag, color, pass.rendererData.lightCullResult.visibleLights);
					pass.rendererData.lightBlendStyles[i].isDirty = flag;
					cmd.EndSample(name);
				}
			}
		}

		public static void RenderLightVolumes(this IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int layerToRender, RenderTargetIdentifier renderTarget, RenderTargetIdentifier depthTarget, uint blendStylesUsed)
		{
			Light2DBlendStyle[] lightBlendStyles = pass.rendererData.lightBlendStyles;
			for (int i = 0; i < lightBlendStyles.Length; i++)
			{
				if ((blendStylesUsed & (uint)(1 << i)) != 0)
				{
					string name = lightBlendStyles[i].name;
					cmd.BeginSample(name);
					RenderLightVolumeSet(pass, renderingData, i, cmd, layerToRender, renderTarget, depthTarget, pass.rendererData.lightCullResult.visibleLights);
					cmd.EndSample(name);
				}
			}
		}

		private static void SetBlendModes(Material material, BlendMode src, BlendMode dst)
		{
			material.SetFloat(k_SrcBlendID, (float)src);
			material.SetFloat(k_DstBlendID, (float)dst);
		}

		private static uint GetLightMaterialIndex(Light2D light, bool isVolume)
		{
			bool isPointLight = light.isPointLight;
			int num = 0;
			uint num2 = (isVolume ? ((uint)(1 << num)) : 0u);
			num++;
			uint num3 = ((!isPointLight) ? ((uint)(1 << num)) : 0u);
			num++;
			uint num4 = ((!light.alphaBlendOnOverlap) ? ((uint)(1 << num)) : 0u);
			num++;
			uint num5 = ((light.lightType == Light2D.LightType.Sprite) ? ((uint)(1 << num)) : 0u);
			num++;
			uint num6 = ((isPointLight && light.lightCookieSprite != null && light.lightCookieSprite.texture != null) ? ((uint)(1 << num)) : 0u);
			num++;
			int num7 = ((isPointLight && light.pointLightQuality == Light2D.PointLightQuality.Fast) ? (1 << num) : 0);
			num++;
			uint num8 = (light.useNormalMap ? ((uint)(1 << num)) : 0u);
			return (uint)num7 | num6 | num5 | num4 | num3 | num2 | num8;
		}

		private static Material CreateLightMaterial(Renderer2DData rendererData, Light2D light, bool isVolume)
		{
			bool isPointLight = light.isPointLight;
			Material material;
			if (isVolume)
			{
				material = CoreUtils.CreateEngineMaterial(isPointLight ? rendererData.pointLightVolumeShader : rendererData.shapeLightVolumeShader);
			}
			else
			{
				material = CoreUtils.CreateEngineMaterial(isPointLight ? rendererData.pointLightShader : rendererData.shapeLightShader);
				if (!light.alphaBlendOnOverlap)
				{
					SetBlendModes(material, BlendMode.One, BlendMode.One);
					material.EnableKeyword(k_UseAdditiveBlendingKeyword);
				}
				else
				{
					SetBlendModes(material, BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
				}
			}
			if (light.lightType == Light2D.LightType.Sprite)
			{
				material.EnableKeyword(k_SpriteLightKeyword);
			}
			if (isPointLight && light.lightCookieSprite != null && light.lightCookieSprite.texture != null)
			{
				material.EnableKeyword(k_UsePointLightCookiesKeyword);
			}
			if (isPointLight && light.pointLightQuality == Light2D.PointLightQuality.Fast)
			{
				material.EnableKeyword(k_LightQualityFastKeyword);
			}
			if (light.useNormalMap)
			{
				material.EnableKeyword(k_UseNormalMap);
			}
			return material;
		}

		private static Material GetLightMaterial(this Renderer2DData rendererData, Light2D light, bool isVolume)
		{
			uint lightMaterialIndex = GetLightMaterialIndex(light, isVolume);
			if (!rendererData.lightMaterials.TryGetValue(lightMaterialIndex, out var value))
			{
				value = CreateLightMaterial(rendererData, light, isVolume);
				rendererData.lightMaterials[lightMaterialIndex] = value;
			}
			return value;
		}
	}
}
