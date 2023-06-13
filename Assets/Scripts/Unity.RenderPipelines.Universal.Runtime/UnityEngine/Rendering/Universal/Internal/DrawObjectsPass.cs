using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal.Internal
{
	public class DrawObjectsPass : ScriptableRenderPass
	{
		private FilteringSettings m_FilteringSettings;

		private RenderStateBlock m_RenderStateBlock;

		private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

		private string m_ProfilerTag;

		private ProfilingSampler m_ProfilingSampler;

		private bool m_IsOpaque;

		private static readonly int s_DrawObjectPassDataPropID = Shader.PropertyToID("_DrawObjectPassData");

		public DrawObjectsPass(string profilerTag, ShaderTagId[] shaderTagIds, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
		{
			base.profilingSampler = new ProfilingSampler("DrawObjectsPass");
			m_ProfilerTag = profilerTag;
			m_ProfilingSampler = new ProfilingSampler(profilerTag);
			foreach (ShaderTagId item in shaderTagIds)
			{
				m_ShaderTagIdList.Add(item);
			}
			base.renderPassEvent = evt;
			m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
			m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
			m_IsOpaque = opaque;
			if (stencilState.enabled)
			{
				m_RenderStateBlock.stencilReference = stencilReference;
				m_RenderStateBlock.mask = RenderStateMask.Stencil;
				m_RenderStateBlock.stencilState = stencilState;
			}
		}

		public DrawObjectsPass(string profilerTag, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
			: this(profilerTag, new ShaderTagId[4]
			{
				new ShaderTagId("SRPDefaultUnlit"),
				new ShaderTagId("UniversalForward"),
				new ShaderTagId("UniversalForwardOnly"),
				new ShaderTagId("LightweightForward")
			}, opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference)
		{
		}

		internal DrawObjectsPass(URPProfileId profileId, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
			: this(profileId.GetType().Name, opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference)
		{
			m_ProfilingSampler = ProfilingSampler.Get(profileId);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
			{
				Vector4 value = new Vector4(0f, 0f, 0f, m_IsOpaque ? 1f : 0f);
				commandBuffer.SetGlobalVector(s_DrawObjectPassDataPropID, value);
				float num = (renderingData.cameraData.IsCameraProjectionMatrixFlipped() ? (-1f) : 1f);
				Vector4 value2 = ((num < 0f) ? new Vector4(num, 1f, -1f, 1f) : new Vector4(num, 0f, 1f, 1f));
				commandBuffer.SetGlobalVector(ShaderPropertyId.scaleBiasRt, value2);
				context.ExecuteCommandBuffer(commandBuffer);
				commandBuffer.Clear();
				SortingCriteria sortingCriteria = (m_IsOpaque ? renderingData.cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent);
				DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortingCriteria);
				FilteringSettings filteringSettings = m_FilteringSettings;
				context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref m_RenderStateBlock);
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}
	}
}
