using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
	internal class GBufferPass : ScriptableRenderPass
	{
		private static ShaderTagId s_ShaderTagLit = new ShaderTagId("Lit");

		private static ShaderTagId s_ShaderTagSimpleLit = new ShaderTagId("SimpleLit");

		private static ShaderTagId s_ShaderTagUnlit = new ShaderTagId("Unlit");

		private static ShaderTagId s_ShaderTagUniversalGBuffer = new ShaderTagId("UniversalGBuffer");

		private static ShaderTagId s_ShaderTagUniversalMaterialType = new ShaderTagId("UniversalMaterialType");

		private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render GBuffer");

		private DeferredLights m_DeferredLights;

		private ShaderTagId[] m_ShaderTagValues;

		private RenderStateBlock[] m_RenderStateBlocks;

		private FilteringSettings m_FilteringSettings;

		private RenderStateBlock m_RenderStateBlock;

		public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference, DeferredLights deferredLights)
		{
			base.profilingSampler = new ProfilingSampler("GBufferPass");
			base.renderPassEvent = evt;
			m_DeferredLights = deferredLights;
			m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
			m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
			m_RenderStateBlock.stencilState = stencilState;
			m_RenderStateBlock.stencilReference = stencilReference;
			m_RenderStateBlock.mask = RenderStateMask.Stencil;
			m_ShaderTagValues = new ShaderTagId[4];
			m_ShaderTagValues[0] = s_ShaderTagLit;
			m_ShaderTagValues[1] = s_ShaderTagSimpleLit;
			m_ShaderTagValues[2] = s_ShaderTagUnlit;
			m_ShaderTagValues[3] = default(ShaderTagId);
			m_RenderStateBlocks = new RenderStateBlock[4];
			m_RenderStateBlocks[0] = DeferredLights.OverwriteStencil(m_RenderStateBlock, 96, 32);
			m_RenderStateBlocks[1] = DeferredLights.OverwriteStencil(m_RenderStateBlock, 96, 64);
			m_RenderStateBlocks[2] = DeferredLights.OverwriteStencil(m_RenderStateBlock, 96, 0);
			m_RenderStateBlocks[3] = m_RenderStateBlocks[0];
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			RenderTargetHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;
			for (int i = 0; i < gbufferAttachments.Length; i++)
			{
				if (i != m_DeferredLights.GBufferLightingIndex)
				{
					RenderTextureDescriptor desc = cameraTextureDescriptor;
					desc.depthBufferBits = 0;
					desc.stencilFormat = GraphicsFormat.None;
					desc.graphicsFormat = m_DeferredLights.GetGBufferFormat(i);
					cmd.GetTemporaryRT(m_DeferredLights.GbufferAttachments[i].id, desc);
				}
			}
			ConfigureTarget(m_DeferredLights.GbufferAttachmentIdentifiers, m_DeferredLights.DepthAttachmentIdentifier);
			ConfigureClear(ClearFlag.None, Color.black);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
			{
				if (m_DeferredLights.IsOverlay)
				{
					m_DeferredLights.ClearStencilPartial(commandBuffer);
				}
				context.ExecuteCommandBuffer(commandBuffer);
				commandBuffer.Clear();
				_ = ref renderingData.cameraData;
				ShaderTagId shaderTagId = s_ShaderTagUniversalGBuffer;
				DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagId, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
				ShaderTagId tagName = s_ShaderTagUniversalMaterialType;
				NativeArray<ShaderTagId> tagValues = new NativeArray<ShaderTagId>(m_ShaderTagValues, Allocator.Temp);
				NativeArray<RenderStateBlock> stateBlocks = new NativeArray<RenderStateBlock>(m_RenderStateBlocks, Allocator.Temp);
				context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings, tagName, isPassTagName: false, tagValues, stateBlocks);
				tagValues.Dispose();
				stateBlocks.Dispose();
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			RenderTargetHandle[] gbufferAttachments = m_DeferredLights.GbufferAttachments;
			for (int i = 0; i < gbufferAttachments.Length; i++)
			{
				if (i != m_DeferredLights.GBufferLightingIndex)
				{
					cmd.ReleaseTemporaryRT(gbufferAttachments[i].id);
				}
			}
		}
	}
}
