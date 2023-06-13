using System;

namespace UnityEngine.Rendering.Universal.Internal
{
	public class CopyDepthPass : ScriptableRenderPass
	{
		private Material m_CopyDepthMaterial;

		private RenderTargetHandle source { get; set; }

		private RenderTargetHandle destination { get; set; }

		internal bool AllocateRT { get; set; }

		public CopyDepthPass(RenderPassEvent evt, Material copyDepthMaterial)
		{
			base.profilingSampler = new ProfilingSampler("CopyDepthPass");
			AllocateRT = true;
			m_CopyDepthMaterial = copyDepthMaterial;
			base.renderPassEvent = evt;
		}

		public void Setup(RenderTargetHandle source, RenderTargetHandle destination)
		{
			this.source = source;
			this.destination = destination;
			AllocateRT = AllocateRT && !destination.HasInternalRenderTargetId();
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			cameraTargetDescriptor.colorFormat = RenderTextureFormat.Depth;
			cameraTargetDescriptor.depthBufferBits = 32;
			cameraTargetDescriptor.msaaSamples = 1;
			if (AllocateRT)
			{
				cmd.GetTemporaryRT(destination.id, cameraTargetDescriptor, FilterMode.Point);
			}
			ConfigureTarget(new RenderTargetIdentifier(destination.Identifier(), 0, CubemapFace.Unknown, -1));
			ConfigureClear(ClearFlag.None, Color.black);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (m_CopyDepthMaterial == null)
			{
				Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_CopyDepthMaterial, GetType().Name);
				return;
			}
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(URPProfileId.CopyDepth)))
			{
				RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
				int msaaSamples = cameraTargetDescriptor.msaaSamples;
				CameraData cameraData = renderingData.cameraData;
				switch (msaaSamples)
				{
				case 8:
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
					commandBuffer.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
					break;
				case 4:
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
					commandBuffer.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
					break;
				case 2:
					commandBuffer.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
					break;
				default:
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
					break;
				}
				commandBuffer.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());
				if (renderingData.cameraData.xr.enabled)
				{
					float num = ((destination.Identifier() == cameraData.xr.renderTarget && !cameraData.xr.renderTargetIsRenderTexture && SystemInfo.graphicsUVStartsAtTop) ? (-1f) : 1f);
					Vector4 value = ((num < 0f) ? new Vector4(num, 1f, -1f, 1f) : new Vector4(num, 0f, 1f, 1f));
					commandBuffer.SetGlobalVector(ShaderPropertyId.scaleBiasRt, value);
					commandBuffer.DrawProcedural(Matrix4x4.identity, m_CopyDepthMaterial, 0, MeshTopology.Quads, 4);
				}
				else
				{
					float num2 = (cameraData.IsCameraProjectionMatrixFlipped() ? (-1f) : 1f);
					Vector4 value2 = ((num2 < 0f) ? new Vector4(num2, 1f, -1f, 1f) : new Vector4(num2, 0f, 1f, 1f));
					commandBuffer.SetGlobalVector(ShaderPropertyId.scaleBiasRt, value2);
					commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyDepthMaterial);
				}
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			if (cmd == null)
			{
				throw new ArgumentNullException("cmd");
			}
			if (AllocateRT)
			{
				cmd.ReleaseTemporaryRT(destination.id);
			}
			destination = RenderTargetHandle.CameraTarget;
		}
	}
}
