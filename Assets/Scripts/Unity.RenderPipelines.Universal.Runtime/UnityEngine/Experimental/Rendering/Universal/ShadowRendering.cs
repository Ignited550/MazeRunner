using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
	internal static class ShadowRendering
	{
		private static readonly int k_LightPosID = Shader.PropertyToID("_LightPos");

		private static readonly int k_ShadowStencilGroupID = Shader.PropertyToID("_ShadowStencilGroup");

		private static readonly int k_ShadowIntensityID = Shader.PropertyToID("_ShadowIntensity");

		private static readonly int k_ShadowVolumeIntensityID = Shader.PropertyToID("_ShadowVolumeIntensity");

		private static readonly int k_ShadowRadiusID = Shader.PropertyToID("_ShadowRadius");

		private static Material GetShadowMaterial(this Renderer2DData rendererData, int index)
		{
			int num = index % 255;
			if (rendererData.shadowMaterials[num] == null)
			{
				rendererData.shadowMaterials[num] = CoreUtils.CreateEngineMaterial(rendererData.shadowGroupShader);
				rendererData.shadowMaterials[num].SetFloat(k_ShadowStencilGroupID, index);
			}
			return rendererData.shadowMaterials[num];
		}

		private static Material GetRemoveSelfShadowMaterial(this Renderer2DData rendererData, int index)
		{
			int num = index % 255;
			if (rendererData.removeSelfShadowMaterials[num] == null)
			{
				rendererData.removeSelfShadowMaterials[num] = CoreUtils.CreateEngineMaterial(rendererData.removeSelfShadowShader);
				rendererData.removeSelfShadowMaterials[num].SetFloat(k_ShadowStencilGroupID, index);
			}
			return rendererData.removeSelfShadowMaterials[num];
		}

		private static void CreateShadowRenderTexture(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmd, int blendStyleIndex)
		{
			float num = Mathf.Clamp(pass.rendererData.lightBlendStyles[blendStyleIndex].renderTextureScale, 0.01f, 1f);
			int width = (int)((float)renderingData.cameraData.cameraTargetDescriptor.width * num);
			int height = (int)((float)renderingData.cameraData.cameraTargetDescriptor.height * num);
			RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height);
			desc.useMipMap = false;
			desc.autoGenerateMips = false;
			desc.depthBufferBits = 24;
			desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
			desc.msaaSamples = 1;
			desc.dimension = TextureDimension.Tex2D;
			cmd.GetTemporaryRT(pass.rendererData.shadowsRenderTarget.id, desc, FilterMode.Bilinear);
		}

		public static void RenderShadows(IRenderPass2D pass, RenderingData renderingData, CommandBuffer cmdBuffer, int layerToRender, Light2D light, float shadowIntensity, RenderTargetIdentifier renderTexture, RenderTargetIdentifier depthTexture)
		{
			cmdBuffer.SetGlobalFloat(k_ShadowIntensityID, 1f - light.shadowIntensity);
			cmdBuffer.SetGlobalFloat(k_ShadowVolumeIntensityID, 1f - light.shadowVolumeIntensity);
			if (!(shadowIntensity > 0f))
			{
				return;
			}
			CreateShadowRenderTexture(pass, renderingData, cmdBuffer, light.blendStyleIndex);
			cmdBuffer.SetRenderTarget(pass.rendererData.shadowsRenderTarget.Identifier(), RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
			cmdBuffer.ClearRenderTarget(clearDepth: true, clearColor: true, Color.black);
			float value = 1.42f * light.boundingSphere.radius;
			cmdBuffer.SetGlobalVector(k_LightPosID, light.transform.position);
			cmdBuffer.SetGlobalFloat(k_ShadowRadiusID, value);
			Material shadowMaterial = pass.rendererData.GetShadowMaterial(1);
			Material removeSelfShadowMaterial = pass.rendererData.GetRemoveSelfShadowMaterial(1);
			List<ShadowCasterGroup2D> shadowCasterGroups = ShadowCasterGroup2DManager.shadowCasterGroups;
			if (shadowCasterGroups != null && shadowCasterGroups.Count > 0)
			{
				int b = -1;
				int num = 0;
				for (int i = 0; i < shadowCasterGroups.Count; i++)
				{
					ShadowCasterGroup2D shadowCasterGroup2D = shadowCasterGroups[i];
					List<ShadowCaster2D> shadowCasters = shadowCasterGroup2D.GetShadowCasters();
					int shadowGroup = shadowCasterGroup2D.GetShadowGroup();
					if (LightUtility.CheckForChange(shadowGroup, ref b) || shadowGroup == 0)
					{
						num++;
						shadowMaterial = pass.rendererData.GetShadowMaterial(num);
						removeSelfShadowMaterial = pass.rendererData.GetRemoveSelfShadowMaterial(num);
					}
					if (shadowCasters == null)
					{
						continue;
					}
					for (int j = 0; j < shadowCasters.Count; j++)
					{
						ShadowCaster2D shadowCaster2D = shadowCasters[j];
						if (shadowCaster2D != null && shadowMaterial != null && shadowCaster2D.IsShadowedLayer(layerToRender) && shadowCaster2D.castsShadows)
						{
							cmdBuffer.DrawMesh(shadowCaster2D.mesh, shadowCaster2D.transform.localToWorldMatrix, shadowMaterial);
						}
					}
					for (int k = 0; k < shadowCasters.Count; k++)
					{
						ShadowCaster2D shadowCaster2D2 = shadowCasters[k];
						if (!(shadowCaster2D2 != null) || !(shadowMaterial != null) || !shadowCaster2D2.IsShadowedLayer(layerToRender))
						{
							continue;
						}
						if (shadowCaster2D2.useRendererSilhouette)
						{
							Renderer component = shadowCaster2D2.GetComponent<Renderer>();
							if (component != null)
							{
								if (!shadowCaster2D2.selfShadows)
								{
									cmdBuffer.DrawRenderer(component, removeSelfShadowMaterial);
								}
								else
								{
									cmdBuffer.DrawRenderer(component, shadowMaterial, 0, 1);
								}
							}
						}
						else if (!shadowCaster2D2.selfShadows)
						{
							Matrix4x4 localToWorldMatrix = shadowCaster2D2.transform.localToWorldMatrix;
							cmdBuffer.DrawMesh(shadowCaster2D2.mesh, localToWorldMatrix, removeSelfShadowMaterial);
						}
					}
				}
			}
			cmdBuffer.ReleaseTemporaryRT(pass.rendererData.shadowsRenderTarget.id);
			cmdBuffer.SetRenderTarget(renderTexture, depthTexture);
		}
	}
}
