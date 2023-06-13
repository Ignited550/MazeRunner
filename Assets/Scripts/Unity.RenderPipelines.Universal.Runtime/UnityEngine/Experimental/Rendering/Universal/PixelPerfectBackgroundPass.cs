using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering.Universal
{
	internal class PixelPerfectBackgroundPass : ScriptableRenderPass
	{
		public PixelPerfectBackgroundPass(RenderPassEvent evt)
		{
			base.renderPassEvent = evt;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			CoreUtils.SetRenderTarget(commandBuffer, BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Color, Color.black);
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}
	}
}
