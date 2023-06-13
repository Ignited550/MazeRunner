using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
	[MovedFrom("UnityEngine.Rendering.LWRP")]
	public abstract class ScriptableRenderPass
	{
		private RenderTargetIdentifier[] m_ColorAttachments = new RenderTargetIdentifier[1] { BuiltinRenderTextureType.CameraTarget };

		private RenderTargetIdentifier m_DepthAttachment = BuiltinRenderTextureType.CameraTarget;

		private ScriptableRenderPassInput m_Input;

		private ClearFlag m_ClearFlag;

		private Color m_ClearColor = Color.black;

		public RenderPassEvent renderPassEvent { get; set; }

		public RenderTargetIdentifier[] colorAttachments => m_ColorAttachments;

		public RenderTargetIdentifier colorAttachment => m_ColorAttachments[0];

		public RenderTargetIdentifier depthAttachment => m_DepthAttachment;

		public ScriptableRenderPassInput input => m_Input;

		public ClearFlag clearFlag => m_ClearFlag;

		public Color clearColor => m_ClearColor;

		protected internal ProfilingSampler profilingSampler { get; set; }

		internal bool overrideCameraTarget { get; set; }

		internal bool isBlitRenderPass { get; set; }

		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void FrameCleanup(CommandBuffer cmd)
		{
			OnCameraCleanup(cmd);
		}

		public ScriptableRenderPass()
		{
			renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
			m_ColorAttachments = new RenderTargetIdentifier[8]
			{
				BuiltinRenderTextureType.CameraTarget,
				0,
				0,
				0,
				0,
				0,
				0,
				0
			};
			m_DepthAttachment = BuiltinRenderTextureType.CameraTarget;
			m_ClearFlag = ClearFlag.None;
			m_ClearColor = Color.black;
			overrideCameraTarget = false;
			isBlitRenderPass = false;
			profilingSampler = new ProfilingSampler("ScriptableRenderPass");
		}

		public void ConfigureInput(ScriptableRenderPassInput passInput)
		{
			m_Input = passInput;
		}

		public void ConfigureTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment)
		{
			m_DepthAttachment = depthAttachment;
			ConfigureTarget(colorAttachment);
		}

		public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment)
		{
			overrideCameraTarget = true;
			uint validColorBufferCount = RenderingUtils.GetValidColorBufferCount(colorAttachments);
			if (validColorBufferCount > SystemInfo.supportedRenderTargetCount)
			{
				Debug.LogError("Trying to set " + validColorBufferCount + " renderTargets, which is more than the maximum supported:" + SystemInfo.supportedRenderTargetCount);
			}
			m_ColorAttachments = colorAttachments;
			m_DepthAttachment = depthAttachment;
		}

		public void ConfigureTarget(RenderTargetIdentifier colorAttachment)
		{
			overrideCameraTarget = true;
			m_ColorAttachments[0] = colorAttachment;
			for (int i = 1; i < m_ColorAttachments.Length; i++)
			{
				m_ColorAttachments[i] = 0;
			}
		}

		public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments)
		{
			ConfigureTarget(colorAttachments, BuiltinRenderTextureType.CameraTarget);
		}

		public void ConfigureClear(ClearFlag clearFlag, Color clearColor)
		{
			m_ClearFlag = clearFlag;
			m_ClearColor = clearColor;
		}

		public virtual void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
		}

		public virtual void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
		}

		public virtual void OnCameraCleanup(CommandBuffer cmd)
		{
		}

		public virtual void OnFinishCameraStackRendering(CommandBuffer cmd)
		{
		}

		public abstract void Execute(ScriptableRenderContext context, ref RenderingData renderingData);

		public void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material = null, int passIndex = 0)
		{
			ScriptableRenderer.SetRenderTarget(cmd, destination, BuiltinRenderTextureType.CameraTarget, clearFlag, clearColor);
			cmd.Blit(source, destination, material, passIndex);
		}

		public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, ref RenderingData renderingData, SortingCriteria sortingCriteria)
		{
			Camera camera = renderingData.cameraData.camera;
			SortingSettings sortingSettings = new SortingSettings(camera);
			sortingSettings.criteria = sortingCriteria;
			SortingSettings sortingSettings2 = sortingSettings;
			DrawingSettings result = new DrawingSettings(shaderTagId, sortingSettings2);
			result.perObjectData = renderingData.perObjectData;
			result.mainLightIndex = renderingData.lightData.mainLightIndex;
			result.enableDynamicBatching = renderingData.supportsDynamicBatching;
			result.enableInstancing = camera.cameraType != CameraType.Preview;
			return result;
		}

		public DrawingSettings CreateDrawingSettings(List<ShaderTagId> shaderTagIdList, ref RenderingData renderingData, SortingCriteria sortingCriteria)
		{
			if (shaderTagIdList == null || shaderTagIdList.Count == 0)
			{
				Debug.LogWarning("ShaderTagId list is invalid. DrawingSettings is created with default pipeline ShaderTagId");
				return CreateDrawingSettings(new ShaderTagId("UniversalPipeline"), ref renderingData, sortingCriteria);
			}
			DrawingSettings result = CreateDrawingSettings(shaderTagIdList[0], ref renderingData, sortingCriteria);
			for (int i = 1; i < shaderTagIdList.Count; i++)
			{
				result.SetShaderPassName(i, shaderTagIdList[i]);
			}
			return result;
		}

		public static bool operator <(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
		{
			return lhs.renderPassEvent < rhs.renderPassEvent;
		}

		public static bool operator >(ScriptableRenderPass lhs, ScriptableRenderPass rhs)
		{
			return lhs.renderPassEvent > rhs.renderPassEvent;
		}
	}
}
