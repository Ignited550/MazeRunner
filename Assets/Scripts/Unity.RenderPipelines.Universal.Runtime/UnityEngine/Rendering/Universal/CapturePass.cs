using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
	internal class CapturePass : ScriptableRenderPass
	{
		private RenderTargetHandle m_CameraColorHandle;

		private const string m_ProfilerTag = "Capture Pass";

		private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Capture Pass");

		public CapturePass(RenderPassEvent evt)
		{
			base.profilingSampler = new ProfilingSampler("CapturePass");
			base.renderPassEvent = evt;
		}

		public void Setup(RenderTargetHandle colorHandle)
		{
			m_CameraColorHandle = colorHandle;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
			{
				RenderTargetIdentifier arg = m_CameraColorHandle.Identifier();
				IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> captureActions = renderingData.cameraData.captureActions;
				captureActions.Reset();
				while (captureActions.MoveNext())
				{
					captureActions.Current(arg, commandBuffer);
				}
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}
	}
}
