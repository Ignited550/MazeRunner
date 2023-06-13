using System;

namespace UnityEngine.Rendering.Universal.Internal
{
	public class DepthOnlyPass : ScriptableRenderPass
	{
		private int kDepthBufferBits = 32;

		private FilteringSettings m_FilteringSettings;

		private ShaderTagId m_ShaderTagId = new ShaderTagId("DepthOnly");

		private RenderTargetHandle depthAttachmentHandle { get; set; }

		internal RenderTextureDescriptor descriptor { get; private set; }

		public DepthOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
		{
			base.profilingSampler = new ProfilingSampler("DepthOnlyPass");
			m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
			base.renderPassEvent = evt;
		}

		public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthAttachmentHandle)
		{
			this.depthAttachmentHandle = depthAttachmentHandle;
			baseDescriptor.colorFormat = RenderTextureFormat.Depth;
			baseDescriptor.depthBufferBits = kDepthBufferBits;
			baseDescriptor.msaaSamples = 1;
			descriptor = baseDescriptor;
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			cmd.GetTemporaryRT(depthAttachmentHandle.id, descriptor, FilterMode.Point);
			ConfigureTarget(new RenderTargetIdentifier(depthAttachmentHandle.Identifier(), 0, CubemapFace.Unknown, -1));
			ConfigureClear(ClearFlag.All, Color.black);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(URPProfileId.DepthPrepass)))
			{
				context.ExecuteCommandBuffer(commandBuffer);
				commandBuffer.Clear();
				SortingCriteria defaultOpaqueSortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
				DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, defaultOpaqueSortFlags);
				drawingSettings.perObjectData = PerObjectData.None;
				context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings);
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
			if (depthAttachmentHandle != RenderTargetHandle.CameraTarget)
			{
				cmd.ReleaseTemporaryRT(depthAttachmentHandle.id);
				depthAttachmentHandle = RenderTargetHandle.CameraTarget;
			}
		}
	}
}
