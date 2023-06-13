namespace UnityEngine.Rendering.Universal.Internal
{
	public class FinalBlitPass : ScriptableRenderPass
	{
		private RenderTargetHandle m_Source;

		private Material m_BlitMaterial;

		public FinalBlitPass(RenderPassEvent evt, Material blitMaterial)
		{
			base.profilingSampler = new ProfilingSampler("FinalBlitPass");
			m_BlitMaterial = blitMaterial;
			base.renderPassEvent = evt;
		}

		public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle)
		{
			m_Source = colorHandle;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (m_BlitMaterial == null)
			{
				Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_BlitMaterial, GetType().Name);
				return;
			}
			ref CameraData cameraData = ref renderingData.cameraData;
			RenderTargetIdentifier renderTargetIdentifier = ((cameraData.targetTexture != null) ? new RenderTargetIdentifier(cameraData.targetTexture) : ((RenderTargetIdentifier)BuiltinRenderTextureType.CameraTarget));
			bool isSceneViewCamera = cameraData.isSceneViewCamera;
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(URPProfileId.FinalBlit)))
			{
				CoreUtils.SetKeyword(commandBuffer, ShaderKeywordStrings.LinearToSRGBConversion, cameraData.requireSrgbConversion);
				commandBuffer.SetGlobalTexture(ShaderPropertyId.sourceTex, m_Source.Identifier());
				if (cameraData.xr.enabled)
				{
					int depthSlice = (cameraData.xr.singlePassEnabled ? (-1) : cameraData.xr.GetTextureArraySlice());
					renderTargetIdentifier = new RenderTargetIdentifier(cameraData.xr.renderTarget, 0, CubemapFace.Unknown, depthSlice);
					CoreUtils.SetRenderTarget(commandBuffer, renderTargetIdentifier, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, ClearFlag.None, Color.black);
					commandBuffer.SetViewport(cameraData.pixelRect);
					Vector4 value = ((!cameraData.xr.renderTargetIsRenderTexture && SystemInfo.graphicsUVStartsAtTop) ? new Vector4(1f, -1f, 0f, 1f) : new Vector4(1f, 1f, 0f, 0f));
					commandBuffer.SetGlobalVector(ShaderPropertyId.scaleBias, value);
					commandBuffer.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, 0, MeshTopology.Quads, 4);
				}
				else if (isSceneViewCamera || cameraData.isDefaultViewport)
				{
					commandBuffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
					commandBuffer.Blit(m_Source.Identifier(), renderTargetIdentifier, m_BlitMaterial);
				}
				else
				{
					CoreUtils.SetRenderTarget(commandBuffer, renderTargetIdentifier, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, ClearFlag.None, Color.black);
					Camera camera = cameraData.camera;
					commandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
					commandBuffer.SetViewport(cameraData.pixelRect);
					commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_BlitMaterial);
					commandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
				}
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}
	}
}
