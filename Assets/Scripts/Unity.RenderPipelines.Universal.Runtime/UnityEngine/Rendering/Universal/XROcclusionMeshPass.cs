namespace UnityEngine.Rendering.Universal
{
	public class XROcclusionMeshPass : ScriptableRenderPass
	{
		public XROcclusionMeshPass(RenderPassEvent evt)
		{
			base.profilingSampler = new ProfilingSampler("XROcclusionMeshPass");
			base.renderPassEvent = evt;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.xr.enabled)
			{
				CommandBuffer commandBuffer = CommandBufferPool.Get();
				renderingData.cameraData.xr.RenderOcclusionMesh(commandBuffer);
				context.ExecuteCommandBuffer(commandBuffer);
				CommandBufferPool.Release(commandBuffer);
			}
		}
	}
}
