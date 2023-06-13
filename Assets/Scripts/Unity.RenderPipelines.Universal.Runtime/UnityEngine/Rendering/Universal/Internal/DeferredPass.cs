namespace UnityEngine.Rendering.Universal.Internal
{
	internal class DeferredPass : ScriptableRenderPass
	{
		private DeferredLights m_DeferredLights;

		public DeferredPass(RenderPassEvent evt, DeferredLights deferredLights)
		{
			base.profilingSampler = new ProfilingSampler("DeferredPass");
			base.renderPassEvent = evt;
			m_DeferredLights = deferredLights;
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescripor)
		{
			RenderTargetIdentifier renderTargetIdentifier = m_DeferredLights.GbufferAttachmentIdentifiers[m_DeferredLights.GBufferLightingIndex];
			RenderTargetIdentifier depthAttachmentIdentifier = m_DeferredLights.DepthAttachmentIdentifier;
			ConfigureTarget(renderTargetIdentifier, depthAttachmentIdentifier);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			m_DeferredLights.ExecuteDeferredPass(context, ref renderingData);
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			m_DeferredLights.OnCameraCleanup(cmd);
		}
	}
}
