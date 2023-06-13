using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
	internal class TileDepthRangePass : ScriptableRenderPass
	{
		private DeferredLights m_DeferredLights;

		private int m_PassIndex;

		public TileDepthRangePass(RenderPassEvent evt, DeferredLights deferredLights, int passIndex)
		{
			base.profilingSampler = new ProfilingSampler("TileDepthRangePass");
			base.renderPassEvent = evt;
			m_DeferredLights = deferredLights;
			m_PassIndex = passIndex;
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			RenderTargetHandle renderTargetHandle;
			RenderTextureDescriptor desc;
			if (m_PassIndex == 0 && m_DeferredLights.HasTileDepthRangeExtraPass())
			{
				int num = int.MinValue;
				int width = m_DeferredLights.RenderWidth + num - 1 >> 31;
				int height = m_DeferredLights.RenderHeight + num - 1 >> 31;
				renderTargetHandle = m_DeferredLights.DepthInfoTexture;
				desc = new RenderTextureDescriptor(width, height, GraphicsFormat.R32_UInt, 0);
			}
			else
			{
				int tileXCount = m_DeferredLights.GetTiler(0).TileXCount;
				int tileYCount = m_DeferredLights.GetTiler(0).TileYCount;
				renderTargetHandle = m_DeferredLights.TileDepthInfoTexture;
				desc = new RenderTextureDescriptor(tileXCount, tileYCount, GraphicsFormat.R32_UInt, 0);
			}
			cmd.GetTemporaryRT(renderTargetHandle.id, desc, FilterMode.Point);
			ConfigureTarget(renderTargetHandle.Identifier());
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (m_PassIndex == 0)
			{
				m_DeferredLights.ExecuteTileDepthInfoPass(context, ref renderingData);
			}
			else
			{
				m_DeferredLights.ExecuteDownsampleBitmaskPass(context, ref renderingData);
			}
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			if (cmd == null)
			{
				throw new ArgumentNullException("cmd");
			}
			cmd.ReleaseTemporaryRT(m_DeferredLights.TileDepthInfoTexture.id);
			m_DeferredLights.TileDepthInfoTexture = RenderTargetHandle.CameraTarget;
		}
	}
}
