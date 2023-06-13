using System;

namespace UnityEngine.Rendering.Universal.Internal
{
	public class CopyColorPass : ScriptableRenderPass
	{
		private int m_SampleOffsetShaderHandle;

		private Material m_SamplingMaterial;

		private Downsampling m_DownsamplingMethod;

		private Material m_CopyColorMaterial;

		private RenderTargetIdentifier source { get; set; }

		private RenderTargetHandle destination { get; set; }

		public CopyColorPass(RenderPassEvent evt, Material samplingMaterial, Material copyColorMaterial = null)
		{
			base.profilingSampler = new ProfilingSampler("CopyColorPass");
			m_SamplingMaterial = samplingMaterial;
			m_CopyColorMaterial = copyColorMaterial;
			m_SampleOffsetShaderHandle = Shader.PropertyToID("_SampleOffset");
			base.renderPassEvent = evt;
			m_DownsamplingMethod = Downsampling.None;
		}

		public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination, Downsampling downsampling)
		{
			this.source = source;
			this.destination = destination;
			m_DownsamplingMethod = downsampling;
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
			cameraTargetDescriptor.msaaSamples = 1;
			cameraTargetDescriptor.depthBufferBits = 0;
			if (m_DownsamplingMethod == Downsampling._2xBilinear)
			{
				cameraTargetDescriptor.width /= 2;
				cameraTargetDescriptor.height /= 2;
			}
			else if (m_DownsamplingMethod == Downsampling._4xBox || m_DownsamplingMethod == Downsampling._4xBilinear)
			{
				cameraTargetDescriptor.width /= 4;
				cameraTargetDescriptor.height /= 4;
			}
			cmd.GetTemporaryRT(destination.id, cameraTargetDescriptor, (m_DownsamplingMethod != 0) ? FilterMode.Bilinear : FilterMode.Point);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (m_SamplingMaterial == null)
			{
				Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_SamplingMaterial, GetType().Name);
				return;
			}
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(URPProfileId.CopyColor)))
			{
				RenderTargetIdentifier renderTargetIdentifier = destination.Identifier();
				ScriptableRenderer.SetRenderTarget(commandBuffer, renderTargetIdentifier, BuiltinRenderTextureType.CameraTarget, base.clearFlag, base.clearColor);
				bool enabled = renderingData.cameraData.xr.enabled;
				switch (m_DownsamplingMethod)
				{
				case Downsampling.None:
					RenderingUtils.Blit(commandBuffer, source, renderTargetIdentifier, m_CopyColorMaterial, 0, enabled);
					break;
				case Downsampling._2xBilinear:
					RenderingUtils.Blit(commandBuffer, source, renderTargetIdentifier, m_CopyColorMaterial, 0, enabled);
					break;
				case Downsampling._4xBox:
					m_SamplingMaterial.SetFloat(m_SampleOffsetShaderHandle, 2f);
					RenderingUtils.Blit(commandBuffer, source, renderTargetIdentifier, m_SamplingMaterial, 0, enabled);
					break;
				case Downsampling._4xBilinear:
					RenderingUtils.Blit(commandBuffer, source, renderTargetIdentifier, m_CopyColorMaterial, 0, enabled);
					break;
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
			if (destination != RenderTargetHandle.CameraTarget)
			{
				cmd.ReleaseTemporaryRT(destination.id);
				destination = RenderTargetHandle.CameraTarget;
			}
		}
	}
}
