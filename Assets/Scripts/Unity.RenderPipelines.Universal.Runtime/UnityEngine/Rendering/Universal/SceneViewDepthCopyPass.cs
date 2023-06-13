namespace UnityEngine.Rendering.Universal
{
	internal class SceneViewDepthCopyPass : ScriptableRenderPass
	{
		private Material m_CopyDepthMaterial;

		private const string m_ProfilerTag = "Copy Depth for Scene View";

		private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Copy Depth for Scene View");

		private RenderTargetHandle source { get; set; }

		public SceneViewDepthCopyPass(RenderPassEvent evt, Material copyDepthMaterial)
		{
			base.profilingSampler = new ProfilingSampler("SceneViewDepthCopyPass");
			m_CopyDepthMaterial = copyDepthMaterial;
			base.renderPassEvent = evt;
		}

		public void Setup(RenderTargetHandle source)
		{
			this.source = source;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (m_CopyDepthMaterial == null)
			{
				Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_CopyDepthMaterial, GetType().Name);
				return;
			}
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
			{
				CoreUtils.SetRenderTarget(commandBuffer, BuiltinRenderTextureType.CameraTarget);
				commandBuffer.SetGlobalTexture("_CameraDepthAttachment", source.Identifier());
				commandBuffer.EnableShaderKeyword(ShaderKeywordStrings.DepthNoMsaa);
				commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
				commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
				commandBuffer.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
				float num = (renderingData.cameraData.IsCameraProjectionMatrixFlipped() ? (-1f) : 1f);
				Vector4 value = ((num < 0f) ? new Vector4(num, 1f, -1f, 1f) : new Vector4(num, 0f, 1f, 1f));
				commandBuffer.SetGlobalVector(ShaderPropertyId.scaleBiasRt, value);
				commandBuffer.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyDepthMaterial);
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}
	}
}
