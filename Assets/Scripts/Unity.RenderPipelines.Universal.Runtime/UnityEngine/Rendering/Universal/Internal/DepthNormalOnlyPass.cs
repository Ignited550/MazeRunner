using System;

namespace UnityEngine.Rendering.Universal.Internal
{
	public class DepthNormalOnlyPass : ScriptableRenderPass
	{
		private ShaderTagId m_ShaderTagId = new ShaderTagId("DepthNormals");

		private FilteringSettings m_FilteringSettings;

		private const int k_DepthBufferBits = 32;

		internal RenderTextureDescriptor normalDescriptor { get; private set; }

		internal RenderTextureDescriptor depthDescriptor { get; private set; }

		private RenderTargetHandle depthHandle { get; set; }

		private RenderTargetHandle normalHandle { get; set; }

		public DepthNormalOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
		{
			base.profilingSampler = new ProfilingSampler("DepthNormalOnlyPass");
			m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
			base.renderPassEvent = evt;
		}

		public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthHandle, RenderTargetHandle normalHandle)
		{
			this.depthHandle = depthHandle;
			baseDescriptor.colorFormat = RenderTextureFormat.Depth;
			baseDescriptor.depthBufferBits = 32;
			baseDescriptor.msaaSamples = 1;
			depthDescriptor = baseDescriptor;
			this.normalHandle = normalHandle;
			baseDescriptor.colorFormat = RenderTextureFormat.RGHalf;
			baseDescriptor.depthBufferBits = 0;
			baseDescriptor.msaaSamples = 1;
			normalDescriptor = baseDescriptor;
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			cmd.GetTemporaryRT(normalHandle.id, normalDescriptor, FilterMode.Point);
			cmd.GetTemporaryRT(depthHandle.id, depthDescriptor, FilterMode.Point);
			ConfigureTarget(new RenderTargetIdentifier(normalHandle.Identifier(), 0, CubemapFace.Unknown, -1), new RenderTargetIdentifier(depthHandle.Identifier(), 0, CubemapFace.Unknown, -1));
			ConfigureClear(ClearFlag.All, Color.black);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(URPProfileId.DepthNormalPrepass)))
			{
				context.ExecuteCommandBuffer(commandBuffer);
				commandBuffer.Clear();
				SortingCriteria defaultOpaqueSortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
				DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, defaultOpaqueSortFlags);
				drawingSettings.perObjectData = PerObjectData.None;
				_ = ref renderingData.cameraData;
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
			if (depthHandle != RenderTargetHandle.CameraTarget)
			{
				cmd.ReleaseTemporaryRT(normalHandle.id);
				cmd.ReleaseTemporaryRT(depthHandle.id);
				normalHandle = RenderTargetHandle.CameraTarget;
				depthHandle = RenderTargetHandle.CameraTarget;
			}
		}
	}
}
