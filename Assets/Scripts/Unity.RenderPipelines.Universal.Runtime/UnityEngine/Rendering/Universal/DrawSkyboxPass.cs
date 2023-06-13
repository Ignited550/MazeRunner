namespace UnityEngine.Rendering.Universal
{
	public class DrawSkyboxPass : ScriptableRenderPass
	{
		public DrawSkyboxPass(RenderPassEvent evt)
		{
			base.profilingSampler = new ProfilingSampler("DrawSkyboxPass");
			base.renderPassEvent = evt;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.xr.enabled)
			{
				if (renderingData.cameraData.xr.singlePassEnabled)
				{
					renderingData.cameraData.camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, renderingData.cameraData.GetProjectionMatrix());
					renderingData.cameraData.camera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, renderingData.cameraData.GetViewMatrix());
					renderingData.cameraData.camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, renderingData.cameraData.GetProjectionMatrix(1));
					renderingData.cameraData.camera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, renderingData.cameraData.GetViewMatrix(1));
					CommandBuffer commandBuffer = CommandBufferPool.Get();
					commandBuffer.SetSinglePassStereo(SystemInfo.supportsMultiview ? SinglePassStereoMode.Multiview : SinglePassStereoMode.Instancing);
					context.ExecuteCommandBuffer(commandBuffer);
					commandBuffer.Clear();
					context.DrawSkybox(renderingData.cameraData.camera);
					commandBuffer.SetSinglePassStereo(SinglePassStereoMode.None);
					context.ExecuteCommandBuffer(commandBuffer);
					CommandBufferPool.Release(commandBuffer);
					renderingData.cameraData.camera.ResetStereoProjectionMatrices();
					renderingData.cameraData.camera.ResetStereoViewMatrices();
				}
				else
				{
					renderingData.cameraData.camera.projectionMatrix = renderingData.cameraData.GetProjectionMatrix();
					renderingData.cameraData.camera.worldToCameraMatrix = renderingData.cameraData.GetViewMatrix();
					context.DrawSkybox(renderingData.cameraData.camera);
					context.Submit();
					renderingData.cameraData.camera.ResetProjectionMatrix();
					renderingData.cameraData.camera.ResetWorldToCameraMatrix();
				}
			}
			else
			{
				context.DrawSkybox(renderingData.cameraData.camera);
			}
		}
	}
}
