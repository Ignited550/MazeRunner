using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
	internal class Render2DLightingPass : ScriptableRenderPass, IRenderPass2D
	{
		private static readonly int k_HDREmulationScaleID = Shader.PropertyToID("_HDREmulationScale");

		private static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");

		private static readonly int k_UseSceneLightingID = Shader.PropertyToID("_UseSceneLighting");

		private static readonly int k_RendererColorID = Shader.PropertyToID("_RendererColor");

		private static readonly int k_ShapeLightTexture0ID = Shader.PropertyToID("_ShapeLightTexture0");

		private static readonly int k_ShapeLightTexture1ID = Shader.PropertyToID("_ShapeLightTexture1");

		private static readonly int k_ShapeLightTexture2ID = Shader.PropertyToID("_ShapeLightTexture2");

		private static readonly int k_ShapeLightTexture3ID = Shader.PropertyToID("_ShapeLightTexture3");

		private static readonly ShaderTagId k_CombinedRenderingPassNameOld = new ShaderTagId("Lightweight2D");

		private static readonly ShaderTagId k_CombinedRenderingPassName = new ShaderTagId("Universal2D");

		private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");

		private static readonly ShaderTagId k_LegacyPassName = new ShaderTagId("SRPDefaultUnlit");

		private static readonly List<ShaderTagId> k_ShaderTags = new List<ShaderTagId> { k_LegacyPassName, k_CombinedRenderingPassName, k_CombinedRenderingPassNameOld };

		private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render 2D Lighting");

		private static readonly ProfilingSampler m_ProfilingSamplerUnlit = new ProfilingSampler("Render Unlit");

		private readonly Renderer2DData m_Renderer2DData;

		Renderer2DData IRenderPass2D.rendererData => m_Renderer2DData;

		public Render2DLightingPass(Renderer2DData rendererData)
		{
			m_Renderer2DData = rendererData;
		}

		private void GetTransparencySortingMode(Camera camera, ref SortingSettings sortingSettings)
		{
			TransparencySortMode transparencySortMode = camera.transparencySortMode;
			if (transparencySortMode == TransparencySortMode.Default)
			{
				transparencySortMode = m_Renderer2DData.transparencySortMode;
				if (transparencySortMode == TransparencySortMode.Default)
				{
					transparencySortMode = ((!camera.orthographic) ? TransparencySortMode.Perspective : TransparencySortMode.Orthographic);
				}
			}
			switch (transparencySortMode)
			{
			case TransparencySortMode.Perspective:
				sortingSettings.distanceMetric = DistanceMetric.Perspective;
				break;
			case TransparencySortMode.Orthographic:
				sortingSettings.distanceMetric = DistanceMetric.Orthographic;
				break;
			default:
				sortingSettings.distanceMetric = DistanceMetric.CustomAxis;
				sortingSettings.customAxis = m_Renderer2DData.transparencySortAxis;
				break;
			}
		}

		private bool CompareLightsInLayer(int layerIndex1, int layerIndex2, SortingLayer[] sortingLayers)
		{
			int id = sortingLayers[layerIndex1].id;
			int id2 = sortingLayers[layerIndex2].id;
			foreach (Light2D visibleLight in m_Renderer2DData.lightCullResult.visibleLights)
			{
				if (visibleLight.IsLitLayer(id) != visibleLight.IsLitLayer(id2))
				{
					return false;
				}
			}
			return true;
		}

		private int FindUpperBoundInBatch(int startLayerIndex, SortingLayer[] sortingLayers)
		{
			for (int i = startLayerIndex + 1; i < sortingLayers.Length; i++)
			{
				if (!CompareLightsInLayer(startLayerIndex, i, sortingLayers))
				{
					return i - 1;
				}
			}
			return sortingLayers.Length - 1;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			bool flag = true;
			SortingLayer[] cachedSortingLayer = Light2DManager.GetCachedSortingLayer();
			Camera camera = renderingData.cameraData.camera;
			FilteringSettings filteringSettings = default(FilteringSettings);
			filteringSettings.renderQueueRange = RenderQueueRange.all;
			filteringSettings.layerMask = -1;
			filteringSettings.renderingLayerMask = uint.MaxValue;
			filteringSettings.sortingLayerRange = SortingLayerRange.all;
			if (m_Renderer2DData.lightCullResult.IsSceneLit())
			{
				CommandBuffer commandBuffer = CommandBufferPool.Get();
				commandBuffer.Clear();
				using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
				{
					this.CreateNormalMapRenderTexture(renderingData, commandBuffer);
					commandBuffer.SetGlobalFloat(k_HDREmulationScaleID, m_Renderer2DData.hdrEmulationScale);
					commandBuffer.SetGlobalFloat(k_InverseHDREmulationScaleID, 1f / m_Renderer2DData.hdrEmulationScale);
					commandBuffer.SetGlobalFloat(k_UseSceneLightingID, flag ? 1f : 0f);
					commandBuffer.SetGlobalColor(k_RendererColorID, Color.white);
					this.SetShapeLightShaderGlobals(commandBuffer);
					context.ExecuteCommandBuffer(commandBuffer);
					DrawingSettings drawingSettings = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
					DrawingSettings drawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);
					SortingSettings sortingSettings = drawingSettings.sortingSettings;
					GetTransparencySortingMode(camera, ref sortingSettings);
					drawingSettings.sortingSettings = sortingSettings;
					drawSettings.sortingSettings = sortingSettings;
					int num = m_Renderer2DData.lightBlendStyles.Length;
					int num2 = 0;
					while (num2 < cachedSortingLayer.Length)
					{
						int id = cachedSortingLayer[num2].id;
						LightStats lightStatsByLayer = m_Renderer2DData.lightCullResult.GetLightStatsByLayer(id);
						commandBuffer.Clear();
						for (int i = 0; i < num; i++)
						{
							uint num3 = (uint)(1 << i);
							bool flag2 = (lightStatsByLayer.blendStylesUsed & num3) != 0;
							if (flag2 && !m_Renderer2DData.lightBlendStyles[i].hasRenderTarget)
							{
								this.CreateBlendStyleRenderTexture(renderingData, commandBuffer, i);
							}
							RendererLighting.EnableBlendStyle(commandBuffer, i, flag2);
						}
						context.ExecuteCommandBuffer(commandBuffer);
						int num4 = FindUpperBoundInBatch(num2, cachedSortingLayer);
						short num5 = (short)cachedSortingLayer[num2].value;
						short lowerBound = ((num2 == 0) ? short.MinValue : num5);
						short num6 = (short)cachedSortingLayer[num4].value;
						short upperBound = ((num4 == cachedSortingLayer.Length - 1) ? short.MaxValue : num6);
						filteringSettings.sortingLayerRange = new SortingLayerRange(lowerBound, upperBound);
						if (lightStatsByLayer.totalNormalMapUsage > 0)
						{
							this.RenderNormals(context, renderingData.cullResults, drawSettings, filteringSettings, base.depthAttachment);
						}
						commandBuffer.Clear();
						if (lightStatsByLayer.totalLights > 0)
						{
							this.RenderLights(renderingData, commandBuffer, id, lightStatsByLayer.blendStylesUsed);
						}
						else
						{
							this.ClearDirtyLighting(commandBuffer, lightStatsByLayer.blendStylesUsed);
						}
						CoreUtils.SetRenderTarget(commandBuffer, base.colorAttachment, base.depthAttachment, ClearFlag.None, Color.white);
						context.ExecuteCommandBuffer(commandBuffer);
						context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
						if (lightStatsByLayer.totalVolumetricUsage > 0)
						{
							commandBuffer.Clear();
							this.RenderLightVolumes(renderingData, commandBuffer, id, base.colorAttachment, base.depthAttachment, lightStatsByLayer.blendStylesUsed);
							context.ExecuteCommandBuffer(commandBuffer);
							commandBuffer.Clear();
						}
						num2 = num4 + 1;
					}
					commandBuffer.Clear();
					this.ReleaseRenderTextures(commandBuffer);
				}
				context.ExecuteCommandBuffer(commandBuffer);
				CommandBufferPool.Release(commandBuffer);
				filteringSettings.sortingLayerRange = SortingLayerRange.all;
			}
			else
			{
				DrawingSettings drawingSettings2 = CreateDrawingSettings(k_ShaderTags, ref renderingData, SortingCriteria.CommonTransparent);
				CommandBuffer commandBuffer2 = CommandBufferPool.Get();
				using (new ProfilingScope(commandBuffer2, m_ProfilingSamplerUnlit))
				{
					CoreUtils.SetRenderTarget(commandBuffer2, base.colorAttachment, base.depthAttachment, ClearFlag.None, Color.white);
					commandBuffer2.SetGlobalTexture(k_ShapeLightTexture0ID, Texture2D.blackTexture);
					commandBuffer2.SetGlobalTexture(k_ShapeLightTexture1ID, Texture2D.blackTexture);
					commandBuffer2.SetGlobalTexture(k_ShapeLightTexture2ID, Texture2D.blackTexture);
					commandBuffer2.SetGlobalTexture(k_ShapeLightTexture3ID, Texture2D.blackTexture);
					commandBuffer2.SetGlobalFloat(k_UseSceneLightingID, flag ? 1f : 0f);
					commandBuffer2.SetGlobalColor(k_RendererColorID, Color.white);
					commandBuffer2.EnableShaderKeyword("USE_SHAPE_LIGHT_TYPE_0");
				}
				context.ExecuteCommandBuffer(commandBuffer2);
				CommandBufferPool.Release(commandBuffer2);
				context.DrawRenderers(renderingData.cullResults, ref drawingSettings2, ref filteringSettings);
			}
		}
	}
}
