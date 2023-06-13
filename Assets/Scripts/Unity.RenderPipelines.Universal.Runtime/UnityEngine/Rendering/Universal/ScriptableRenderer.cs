using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
	[MovedFrom("UnityEngine.Rendering.LWRP")]
	public abstract class ScriptableRenderer : IDisposable
	{
		private static class Profiling
		{
			public static class RenderBlock
			{
				private const string k_Name = "RenderPassBlock";

				public static readonly ProfilingSampler beforeRendering = new ProfilingSampler("RenderPassBlock.BeforeRendering");

				public static readonly ProfilingSampler mainRenderingOpaque = new ProfilingSampler("RenderPassBlock.MainRenderingOpaque");

				public static readonly ProfilingSampler mainRenderingTransparent = new ProfilingSampler("RenderPassBlock.MainRenderingTransparent");

				public static readonly ProfilingSampler afterRendering = new ProfilingSampler("RenderPassBlock.AfterRendering");
			}

			public static class RenderPass
			{
				private const string k_Name = "ScriptableRenderPass";

				public static readonly ProfilingSampler configure = new ProfilingSampler("ScriptableRenderPass.Configure");
			}

			private const string k_Name = "ScriptableRenderer";

			public static readonly ProfilingSampler setPerCameraShaderVariables = new ProfilingSampler("ScriptableRenderer.SetPerCameraShaderVariables");

			public static readonly ProfilingSampler sortRenderPasses = new ProfilingSampler("Sort Render Passes");

			public static readonly ProfilingSampler setupLights = new ProfilingSampler("ScriptableRenderer.SetupLights");

			public static readonly ProfilingSampler setupCamera = new ProfilingSampler("Setup Camera Parameters");

			public static readonly ProfilingSampler addRenderPasses = new ProfilingSampler("ScriptableRenderer.AddRenderPasses");

			public static readonly ProfilingSampler clearRenderingState = new ProfilingSampler("ScriptableRenderer.ClearRenderingState");

			public static readonly ProfilingSampler internalStartRendering = new ProfilingSampler("ScriptableRenderer.InternalStartRendering");

			public static readonly ProfilingSampler internalFinishRendering = new ProfilingSampler("ScriptableRenderer.InternalFinishRendering");
		}

		public class RenderingFeatures
		{
			public bool cameraStacking { get; set; }

			public bool msaa { get; set; } = true;

		}

		private static class RenderPassBlock
		{
			public static readonly int BeforeRendering = 0;

			public static readonly int MainRenderingOpaque = 1;

			public static readonly int MainRenderingTransparent = 2;

			public static readonly int AfterRendering = 3;
		}

		internal struct RenderBlocks : IDisposable
		{
			public struct BlockRange : IDisposable
			{
				private int m_Current;

				private int m_End;

				public int Current => m_Current;

				public BlockRange(int begin, int end)
				{
					m_Current = ((begin < end) ? begin : end);
					m_End = ((end >= begin) ? end : begin);
					m_Current--;
				}

				public BlockRange GetEnumerator()
				{
					return this;
				}

				public bool MoveNext()
				{
					return ++m_Current < m_End;
				}

				public void Dispose()
				{
				}
			}

			private NativeArray<RenderPassEvent> m_BlockEventLimits;

			private NativeArray<int> m_BlockRanges;

			private NativeArray<int> m_BlockRangeLengths;

			public RenderBlocks(List<ScriptableRenderPass> activeRenderPassQueue)
			{
				m_BlockEventLimits = new NativeArray<RenderPassEvent>(4, Allocator.Temp);
				m_BlockRanges = new NativeArray<int>(m_BlockEventLimits.Length + 1, Allocator.Temp);
				m_BlockRangeLengths = new NativeArray<int>(m_BlockRanges.Length, Allocator.Temp);
				m_BlockEventLimits[RenderPassBlock.BeforeRendering] = RenderPassEvent.BeforeRenderingPrepasses;
				m_BlockEventLimits[RenderPassBlock.MainRenderingOpaque] = RenderPassEvent.AfterRenderingOpaques;
				m_BlockEventLimits[RenderPassBlock.MainRenderingTransparent] = RenderPassEvent.AfterRenderingPostProcessing;
				m_BlockEventLimits[RenderPassBlock.AfterRendering] = (RenderPassEvent)2147483647;
				FillBlockRanges(activeRenderPassQueue);
				m_BlockEventLimits.Dispose();
				for (int i = 0; i < m_BlockRanges.Length - 1; i++)
				{
					m_BlockRangeLengths[i] = m_BlockRanges[i + 1] - m_BlockRanges[i];
				}
			}

			public void Dispose()
			{
				m_BlockRangeLengths.Dispose();
				m_BlockRanges.Dispose();
			}

			private void FillBlockRanges(List<ScriptableRenderPass> activeRenderPassQueue)
			{
				int index = 0;
				int i = 0;
				m_BlockRanges[index++] = 0;
				for (int j = 0; j < m_BlockEventLimits.Length - 1; j++)
				{
					for (; i < activeRenderPassQueue.Count && activeRenderPassQueue[i].renderPassEvent < m_BlockEventLimits[j]; i++)
					{
					}
					m_BlockRanges[index++] = i;
				}
				m_BlockRanges[index] = activeRenderPassQueue.Count;
			}

			public int GetLength(int index)
			{
				return m_BlockRangeLengths[index];
			}

			public BlockRange GetRange(int index)
			{
				return new BlockRange(m_BlockRanges[index], m_BlockRanges[index + 1]);
			}
		}

		internal static ScriptableRenderer current = null;

		private const int k_RenderPassBlockCount = 4;

		private List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>(32);

		private List<ScriptableRendererFeature> m_RendererFeatures = new List<ScriptableRendererFeature>(10);

		private RenderTargetIdentifier m_CameraColorTarget;

		private RenderTargetIdentifier m_CameraDepthTarget;

		private bool m_FirstTimeCameraColorTargetIsBound = true;

		private bool m_FirstTimeCameraDepthTargetIsBound = true;

		private bool m_IsPipelineExecuting;

		internal bool isCameraColorTargetValid;

		private static RenderTargetIdentifier[] m_ActiveColorAttachments = new RenderTargetIdentifier[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

		private static RenderTargetIdentifier m_ActiveDepthAttachment;

		private static RenderTargetIdentifier[][] m_TrimmedColorAttachmentCopies = new RenderTargetIdentifier[9][]
		{
			new RenderTargetIdentifier[0],
			new RenderTargetIdentifier[1] { 0 },
			new RenderTargetIdentifier[2] { 0, 0 },
			new RenderTargetIdentifier[3] { 0, 0, 0 },
			new RenderTargetIdentifier[4] { 0, 0, 0, 0 },
			new RenderTargetIdentifier[5] { 0, 0, 0, 0, 0 },
			new RenderTargetIdentifier[6] { 0, 0, 0, 0, 0, 0 },
			new RenderTargetIdentifier[7] { 0, 0, 0, 0, 0, 0, 0 },
			new RenderTargetIdentifier[8] { 0, 0, 0, 0, 0, 0, 0, 0 }
		};

		[Obsolete("cameraDepth has been renamed to cameraDepthTarget. (UnityUpgradable) -> cameraDepthTarget")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public RenderTargetIdentifier cameraDepth => m_CameraDepthTarget;

		protected ProfilingSampler profilingExecute { get; set; }

		public RenderTargetIdentifier cameraColorTarget
		{
			get
			{
				if (!m_IsPipelineExecuting && !isCameraColorTargetValid)
				{
					Debug.LogWarning("You can only call cameraColorTarget inside the scope of a ScriptableRenderPass. Otherwise the pipeline camera target texture might have not been created or might have already been disposed.");
				}
				return m_CameraColorTarget;
			}
		}

		public RenderTargetIdentifier cameraDepthTarget
		{
			get
			{
				if (!m_IsPipelineExecuting)
				{
					Debug.LogWarning("You can only call cameraDepthTarget inside the scope of a ScriptableRenderPass. Otherwise the pipeline camera target texture might have not been created or might have already been disposed.");
				}
				return m_CameraDepthTarget;
			}
		}

		protected List<ScriptableRendererFeature> rendererFeatures => m_RendererFeatures;

		protected List<ScriptableRenderPass> activeRenderPassQueue => m_ActiveRenderPassQueue;

		public RenderingFeatures supportedRenderingFeatures { get; set; } = new RenderingFeatures();


		public GraphicsDeviceType[] unsupportedGraphicsDeviceTypes { get; set; } = new GraphicsDeviceType[0];


		public static void SetCameraMatrices(CommandBuffer cmd, ref CameraData cameraData, bool setInverseMatrices)
		{
			if (cameraData.xr.enabled)
			{
				cameraData.xr.UpdateGPUViewAndProjectionMatrices(cmd, ref cameraData, cameraData.xr.renderTargetIsRenderTexture);
				return;
			}
			Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
			Matrix4x4 projectionMatrix = cameraData.GetProjectionMatrix();
			cmd.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
			if (setInverseMatrices)
			{
				Matrix4x4 gPUProjectionMatrix = cameraData.GetGPUProjectionMatrix();
				_ = gPUProjectionMatrix * viewMatrix;
				Matrix4x4 matrix4x = Matrix4x4.Inverse(viewMatrix);
				Matrix4x4 matrix4x2 = Matrix4x4.Inverse(gPUProjectionMatrix);
				Matrix4x4 value = matrix4x * matrix4x2;
				Matrix4x4 value2 = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * viewMatrix;
				Matrix4x4 inverse = value2.inverse;
				cmd.SetGlobalMatrix(ShaderPropertyId.worldToCameraMatrix, value2);
				cmd.SetGlobalMatrix(ShaderPropertyId.cameraToWorldMatrix, inverse);
				cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewMatrix, matrix4x);
				cmd.SetGlobalMatrix(ShaderPropertyId.inverseProjectionMatrix, matrix4x2);
				cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewAndProjectionMatrix, value);
			}
		}

		private void SetPerCameraShaderVariables(CommandBuffer cmd, ref CameraData cameraData)
		{
			using (new ProfilingScope(cmd, Profiling.setPerCameraShaderVariables))
			{
				Camera camera = cameraData.camera;
				Rect pixelRect = cameraData.pixelRect;
				float num = (cameraData.isSceneViewCamera ? 1f : cameraData.renderScale);
				float num2 = pixelRect.width * num;
				float num3 = pixelRect.height * num;
				float num4 = pixelRect.width;
				float num5 = pixelRect.height;
				if (cameraData.xr.enabled)
				{
					num2 = cameraData.cameraTargetDescriptor.width;
					num3 = cameraData.cameraTargetDescriptor.height;
					num4 = cameraData.cameraTargetDescriptor.width;
					num5 = cameraData.cameraTargetDescriptor.height;
				}
				if (camera.allowDynamicResolution)
				{
					num2 *= ScalableBufferManager.widthScaleFactor;
					num3 *= ScalableBufferManager.heightScaleFactor;
				}
				float nearClipPlane = camera.nearClipPlane;
				float farClipPlane = camera.farClipPlane;
				float num6 = (Mathf.Approximately(nearClipPlane, 0f) ? 0f : (1f / nearClipPlane));
				float num7 = (Mathf.Approximately(farClipPlane, 0f) ? 0f : (1f / farClipPlane));
				float w = (camera.orthographic ? 1f : 0f);
				float num8 = 1f - farClipPlane * num6;
				float num9 = farClipPlane * num6;
				Vector4 value = new Vector4(num8, num9, num8 * num7, num9 * num7);
				if (SystemInfo.usesReversedZBuffer)
				{
					value.y += value.x;
					value.x = 0f - value.x;
					value.w += value.z;
					value.z = 0f - value.z;
				}
				Vector4 value2 = new Vector4(camera.orthographicSize * cameraData.aspectRatio, camera.orthographicSize, 0f, w);
				cmd.SetGlobalVector(ShaderPropertyId.worldSpaceCameraPos, camera.transform.position);
				cmd.SetGlobalVector(ShaderPropertyId.screenParams, new Vector4(num4, num5, 1f + 1f / num4, 1f + 1f / num5));
				cmd.SetGlobalVector(ShaderPropertyId.scaledScreenParams, new Vector4(num2, num3, 1f + 1f / num2, 1f + 1f / num3));
				cmd.SetGlobalVector(ShaderPropertyId.zBufferParams, value);
				cmd.SetGlobalVector(ShaderPropertyId.orthoParams, value2);
			}
		}

		private void SetShaderTimeValues(CommandBuffer cmd, float time, float deltaTime, float smoothDeltaTime)
		{
			float f = time / 8f;
			float f2 = time / 4f;
			float f3 = time / 2f;
			Vector4 value = time * new Vector4(0.05f, 1f, 2f, 3f);
			Vector4 value2 = new Vector4(Mathf.Sin(f), Mathf.Sin(f2), Mathf.Sin(f3), Mathf.Sin(time));
			Vector4 value3 = new Vector4(Mathf.Cos(f), Mathf.Cos(f2), Mathf.Cos(f3), Mathf.Cos(time));
			Vector4 value4 = new Vector4(deltaTime, 1f / deltaTime, smoothDeltaTime, 1f / smoothDeltaTime);
			Vector4 value5 = new Vector4(time, Mathf.Sin(time), Mathf.Cos(time), 0f);
			cmd.SetGlobalVector(ShaderPropertyId.time, value);
			cmd.SetGlobalVector(ShaderPropertyId.sinTime, value2);
			cmd.SetGlobalVector(ShaderPropertyId.cosTime, value3);
			cmd.SetGlobalVector(ShaderPropertyId.deltaTime, value4);
			cmd.SetGlobalVector(ShaderPropertyId.timeParameters, value5);
		}

		internal static void ConfigureActiveTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment)
		{
			m_ActiveColorAttachments[0] = colorAttachment;
			for (int i = 1; i < m_ActiveColorAttachments.Length; i++)
			{
				m_ActiveColorAttachments[i] = 0;
			}
			m_ActiveDepthAttachment = depthAttachment;
		}

		public ScriptableRenderer(ScriptableRendererData data)
		{
			profilingExecute = new ProfilingSampler("ScriptableRenderer.Execute: " + data.name);
			foreach (ScriptableRendererFeature rendererFeature in data.rendererFeatures)
			{
				if (!(rendererFeature == null))
				{
					rendererFeature.Create();
					m_RendererFeatures.Add(rendererFeature);
				}
			}
			Clear(CameraRenderType.Base);
			m_ActiveRenderPassQueue.Clear();
		}

		public void Dispose()
		{
			for (int i = 0; i < m_RendererFeatures.Count; i++)
			{
				if (!(rendererFeatures[i] == null))
				{
					rendererFeatures[i].Dispose();
				}
			}
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		}

		public void ConfigureCameraTarget(RenderTargetIdentifier colorTarget, RenderTargetIdentifier depthTarget)
		{
			m_CameraColorTarget = colorTarget;
			m_CameraDepthTarget = depthTarget;
		}

		internal void ConfigureCameraColorTarget(RenderTargetIdentifier colorTarget)
		{
			m_CameraColorTarget = colorTarget;
		}

		public abstract void Setup(ScriptableRenderContext context, ref RenderingData renderingData);

		public virtual void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
		{
		}

		public virtual void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
		{
		}

		public virtual void FinishRendering(CommandBuffer cmd)
		{
		}

		public void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			m_IsPipelineExecuting = true;
			ref CameraData cameraData = ref renderingData.cameraData;
			Camera camera = cameraData.camera;
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			CommandBuffer cmd = (renderingData.cameraData.xr.enabled ? null : commandBuffer);
			using (new ProfilingScope(cmd, profilingExecute))
			{
				InternalStartRendering(context, ref renderingData);
				float time = Time.time;
				float deltaTime = Time.deltaTime;
				float smoothDeltaTime = Time.smoothDeltaTime;
				ClearRenderingState(commandBuffer);
				SetPerCameraShaderVariables(commandBuffer, ref cameraData);
				SetShaderTimeValues(commandBuffer, time, deltaTime, smoothDeltaTime);
				context.ExecuteCommandBuffer(commandBuffer);
				commandBuffer.Clear();
				using (new ProfilingScope(commandBuffer, Profiling.sortRenderPasses))
				{
					SortStable(m_ActiveRenderPassQueue);
				}
				RenderBlocks renderBlocks = new RenderBlocks(m_ActiveRenderPassQueue);
				try
				{
					using (new ProfilingScope(commandBuffer, Profiling.setupLights))
					{
						SetupLights(context, ref renderingData);
					}
					using (new ProfilingScope(commandBuffer, Profiling.RenderBlock.beforeRendering))
					{
						ExecuteBlock(RenderPassBlock.BeforeRendering, in renderBlocks, context, ref renderingData);
					}
					using (new ProfilingScope(commandBuffer, Profiling.setupCamera))
					{
						context.SetupCameraProperties(camera);
						SetCameraMatrices(commandBuffer, ref cameraData, setInverseMatrices: true);
						SetShaderTimeValues(commandBuffer, time, deltaTime, smoothDeltaTime);
					}
					context.ExecuteCommandBuffer(commandBuffer);
					commandBuffer.Clear();
					BeginXRRendering(commandBuffer, context, ref renderingData.cameraData);
					if (renderBlocks.GetLength(RenderPassBlock.MainRenderingOpaque) > 0)
					{
						using (new ProfilingScope(commandBuffer, Profiling.RenderBlock.mainRenderingOpaque))
						{
							ExecuteBlock(RenderPassBlock.MainRenderingOpaque, in renderBlocks, context, ref renderingData);
						}
					}
					if (renderBlocks.GetLength(RenderPassBlock.MainRenderingTransparent) > 0)
					{
						using (new ProfilingScope(commandBuffer, Profiling.RenderBlock.mainRenderingTransparent))
						{
							ExecuteBlock(RenderPassBlock.MainRenderingTransparent, in renderBlocks, context, ref renderingData);
						}
					}
					if (renderBlocks.GetLength(RenderPassBlock.AfterRendering) > 0)
					{
						using (new ProfilingScope(commandBuffer, Profiling.RenderBlock.afterRendering))
						{
							ExecuteBlock(RenderPassBlock.AfterRendering, in renderBlocks, context, ref renderingData);
						}
					}
					EndXRRendering(commandBuffer, context, ref renderingData.cameraData);
					InternalFinishRendering(context, cameraData.resolveFinalTarget);
				}
				finally
				{
					((IDisposable)renderBlocks).Dispose();
				}
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		public void EnqueuePass(ScriptableRenderPass pass)
		{
			m_ActiveRenderPassQueue.Add(pass);
		}

		protected static ClearFlag GetCameraClearFlag(ref CameraData cameraData)
		{
			CameraClearFlags clearFlags = cameraData.camera.clearFlags;
			if (cameraData.renderType == CameraRenderType.Overlay)
			{
				if (!cameraData.clearDepth)
				{
					return ClearFlag.None;
				}
				return ClearFlag.Depth;
			}
			if (Application.isMobilePlatform)
			{
				return ClearFlag.All;
			}
			if ((clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) || clearFlags == CameraClearFlags.Nothing)
			{
				return ClearFlag.Depth;
			}
			return ClearFlag.All;
		}

		protected void AddRenderPasses(ref RenderingData renderingData)
		{
			using (new ProfilingScope(null, Profiling.addRenderPasses))
			{
				for (int i = 0; i < rendererFeatures.Count; i++)
				{
					if (rendererFeatures[i].isActive)
					{
						rendererFeatures[i].AddRenderPasses(this, ref renderingData);
					}
				}
				for (int num = activeRenderPassQueue.Count - 1; num >= 0; num--)
				{
					if (activeRenderPassQueue[num] == null)
					{
						activeRenderPassQueue.RemoveAt(num);
					}
				}
			}
		}

		private void ClearRenderingState(CommandBuffer cmd)
		{
			using (new ProfilingScope(cmd, Profiling.clearRenderingState))
			{
				cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadows);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.MainLightShadowCascades);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsVertex);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightsPixel);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.AdditionalLightShadows);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadows);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.MixedLightingSubtractive);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.LightmapShadowMixing);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.ShadowsShadowMask);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
			}
		}

		internal void Clear(CameraRenderType cameraType)
		{
			m_ActiveColorAttachments[0] = BuiltinRenderTextureType.CameraTarget;
			for (int i = 1; i < m_ActiveColorAttachments.Length; i++)
			{
				m_ActiveColorAttachments[i] = 0;
			}
			m_ActiveDepthAttachment = BuiltinRenderTextureType.CameraTarget;
			m_FirstTimeCameraColorTargetIsBound = cameraType == CameraRenderType.Base;
			m_FirstTimeCameraDepthTargetIsBound = true;
			m_CameraColorTarget = BuiltinRenderTextureType.CameraTarget;
			m_CameraDepthTarget = BuiltinRenderTextureType.CameraTarget;
		}

		private void ExecuteBlock(int blockIndex, in RenderBlocks renderBlocks, ScriptableRenderContext context, ref RenderingData renderingData, bool submit = false)
		{
			foreach (int item in renderBlocks.GetRange(blockIndex))
			{
				ScriptableRenderPass renderPass = m_ActiveRenderPassQueue[item];
				ExecuteRenderPass(context, renderPass, ref renderingData);
			}
			if (submit)
			{
				context.Submit();
			}
		}

		private void ExecuteRenderPass(ScriptableRenderContext context, ScriptableRenderPass renderPass, ref RenderingData renderingData)
		{
			using (new ProfilingScope(null, renderPass.profilingSampler))
			{
				ref CameraData cameraData = ref renderingData.cameraData;
				CommandBuffer commandBuffer = CommandBufferPool.Get();
				using (new ProfilingScope(commandBuffer, Profiling.RenderPass.configure))
				{
					renderPass.Configure(commandBuffer, cameraData.cameraTargetDescriptor);
					SetRenderPassAttachments(commandBuffer, renderPass, ref cameraData);
				}
				context.ExecuteCommandBuffer(commandBuffer);
				CommandBufferPool.Release(commandBuffer);
				renderPass.Execute(context, ref renderingData);
			}
		}

		private void SetRenderPassAttachments(CommandBuffer cmd, ScriptableRenderPass renderPass, ref CameraData cameraData)
		{
			Camera camera = cameraData.camera;
			ClearFlag cameraClearFlag = GetCameraClearFlag(ref cameraData);
			if (RenderingUtils.GetValidColorBufferCount(renderPass.colorAttachments) == 0)
			{
				return;
			}
			if (RenderingUtils.IsMRT(renderPass.colorAttachments))
			{
				bool flag = false;
				bool flag2 = false;
				int num = RenderingUtils.IndexOf(renderPass.colorAttachments, m_CameraColorTarget);
				if (num != -1 && m_FirstTimeCameraColorTargetIsBound)
				{
					m_FirstTimeCameraColorTargetIsBound = false;
					flag = (cameraClearFlag & ClearFlag.Color) != (renderPass.clearFlag & ClearFlag.Color) || CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor) != renderPass.clearColor;
				}
				if (renderPass.depthAttachment == m_CameraDepthTarget && m_FirstTimeCameraDepthTargetIsBound)
				{
					m_FirstTimeCameraDepthTargetIsBound = false;
					flag2 = (cameraClearFlag & ClearFlag.Depth) != (renderPass.clearFlag & ClearFlag.Depth);
				}
				if (flag)
				{
					if ((cameraClearFlag & ClearFlag.Color) != 0)
					{
						SetRenderTarget(cmd, renderPass.colorAttachments[num], renderPass.depthAttachment, ClearFlag.Color, CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor));
					}
					if ((renderPass.clearFlag & ClearFlag.Color) != 0)
					{
						uint num2 = RenderingUtils.CountDistinct(renderPass.colorAttachments, m_CameraColorTarget);
						RenderTargetIdentifier[] array = m_TrimmedColorAttachmentCopies[num2];
						int num3 = 0;
						for (int i = 0; i < renderPass.colorAttachments.Length; i++)
						{
							if (renderPass.colorAttachments[i] != m_CameraColorTarget && renderPass.colorAttachments[i] != 0)
							{
								array[num3] = renderPass.colorAttachments[i];
								num3++;
							}
						}
						if (num3 != num2)
						{
							Debug.LogError("writeIndex and otherTargetsCount values differed. writeIndex:" + num3 + " otherTargetsCount:" + num2);
						}
						SetRenderTarget(cmd, array, m_CameraDepthTarget, ClearFlag.Color, renderPass.clearColor);
					}
				}
				ClearFlag clearFlag = ClearFlag.None;
				clearFlag |= (flag2 ? (cameraClearFlag & ClearFlag.Depth) : (renderPass.clearFlag & ClearFlag.Depth));
				clearFlag |= ((!flag) ? (renderPass.clearFlag & ClearFlag.Color) : ClearFlag.None);
				if (RenderingUtils.SequenceEqual(renderPass.colorAttachments, m_ActiveColorAttachments) && !(renderPass.depthAttachment != m_ActiveDepthAttachment) && clearFlag == ClearFlag.None)
				{
					return;
				}
				int num4 = RenderingUtils.LastValid(renderPass.colorAttachments);
				if (num4 >= 0)
				{
					int num5 = num4 + 1;
					RenderTargetIdentifier[] array2 = m_TrimmedColorAttachmentCopies[num5];
					for (int j = 0; j < num5; j++)
					{
						array2[j] = renderPass.colorAttachments[j];
					}
					SetRenderTarget(cmd, array2, renderPass.depthAttachment, clearFlag, renderPass.clearColor);
					if (cameraData.xr.enabled)
					{
						bool flag3 = RenderingUtils.IndexOf(renderPass.colorAttachments, cameraData.xr.renderTarget) != -1 && !cameraData.xr.renderTargetIsRenderTexture;
						cameraData.xr.UpdateGPUViewAndProjectionMatrices(cmd, ref cameraData, !flag3);
					}
				}
				return;
			}
			RenderTargetIdentifier colorAttachment = renderPass.colorAttachment;
			RenderTargetIdentifier depthAttachment = renderPass.depthAttachment;
			if (!renderPass.overrideCameraTarget)
			{
				if (renderPass.renderPassEvent < RenderPassEvent.BeforeRenderingOpaques)
				{
					return;
				}
				colorAttachment = m_CameraColorTarget;
				depthAttachment = m_CameraDepthTarget;
			}
			ClearFlag clearFlag2 = ClearFlag.None;
			Color clearColor;
			if (colorAttachment == m_CameraColorTarget && m_FirstTimeCameraColorTargetIsBound)
			{
				m_FirstTimeCameraColorTargetIsBound = false;
				clearFlag2 |= cameraClearFlag & ClearFlag.Color;
				clearColor = CoreUtils.ConvertSRGBToActiveColorSpace(camera.backgroundColor);
				if (m_FirstTimeCameraDepthTargetIsBound)
				{
					m_FirstTimeCameraDepthTargetIsBound = false;
					clearFlag2 |= cameraClearFlag & ClearFlag.Depth;
				}
			}
			else
			{
				clearFlag2 |= renderPass.clearFlag & ClearFlag.Color;
				clearColor = renderPass.clearColor;
			}
			if (m_CameraDepthTarget != BuiltinRenderTextureType.CameraTarget && (depthAttachment == m_CameraDepthTarget || colorAttachment == m_CameraDepthTarget) && m_FirstTimeCameraDepthTargetIsBound)
			{
				m_FirstTimeCameraDepthTargetIsBound = false;
				clearFlag2 |= cameraClearFlag & ClearFlag.Depth;
			}
			else
			{
				clearFlag2 |= renderPass.clearFlag & ClearFlag.Depth;
			}
			if (colorAttachment != m_ActiveColorAttachments[0] || depthAttachment != m_ActiveDepthAttachment || clearFlag2 != 0)
			{
				SetRenderTarget(cmd, colorAttachment, depthAttachment, clearFlag2, clearColor);
				if (cameraData.xr.enabled)
				{
					bool flag4 = colorAttachment == cameraData.xr.renderTarget && !cameraData.xr.renderTargetIsRenderTexture;
					cameraData.xr.UpdateGPUViewAndProjectionMatrices(cmd, ref cameraData, !flag4);
				}
			}
		}

		private void BeginXRRendering(CommandBuffer cmd, ScriptableRenderContext context, ref CameraData cameraData)
		{
			if (cameraData.xr.enabled)
			{
				cameraData.xr.StartSinglePass(cmd);
				cmd.EnableShaderKeyword(ShaderKeywordStrings.UseDrawProcedural);
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
			}
		}

		private void EndXRRendering(CommandBuffer cmd, ScriptableRenderContext context, ref CameraData cameraData)
		{
			if (cameraData.xr.enabled)
			{
				cameraData.xr.StopSinglePass(cmd);
				cmd.DisableShaderKeyword(ShaderKeywordStrings.UseDrawProcedural);
				context.ExecuteCommandBuffer(cmd);
				cmd.Clear();
			}
		}

		internal static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment, ClearFlag clearFlag, Color clearColor)
		{
			m_ActiveColorAttachments[0] = colorAttachment;
			for (int i = 1; i < m_ActiveColorAttachments.Length; i++)
			{
				m_ActiveColorAttachments[i] = 0;
			}
			m_ActiveDepthAttachment = depthAttachment;
			RenderBufferLoadAction colorLoadAction = (((clearFlag & ClearFlag.Color) != 0) ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load);
			RenderBufferLoadAction depthLoadAction = (((clearFlag & ClearFlag.Depth) != 0) ? RenderBufferLoadAction.DontCare : RenderBufferLoadAction.Load);
			SetRenderTarget(cmd, colorAttachment, colorLoadAction, RenderBufferStoreAction.Store, depthAttachment, depthLoadAction, RenderBufferStoreAction.Store, clearFlag, clearColor);
		}

		private static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorAttachment, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction, ClearFlag clearFlags, Color clearColor)
		{
			CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlags, clearColor);
		}

		private static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorAttachment, RenderBufferLoadAction colorLoadAction, RenderBufferStoreAction colorStoreAction, RenderTargetIdentifier depthAttachment, RenderBufferLoadAction depthLoadAction, RenderBufferStoreAction depthStoreAction, ClearFlag clearFlags, Color clearColor)
		{
			if (depthAttachment == BuiltinRenderTextureType.CameraTarget)
			{
				SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlags, clearColor);
			}
			else
			{
				CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, depthAttachment, depthLoadAction, depthStoreAction, clearFlags, clearColor);
			}
		}

		private static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment, ClearFlag clearFlag, Color clearColor)
		{
			m_ActiveColorAttachments = colorAttachments;
			m_ActiveDepthAttachment = depthAttachment;
			CoreUtils.SetRenderTarget(cmd, colorAttachments, depthAttachment, clearFlag, clearColor);
		}

		[Conditional("UNITY_EDITOR")]
		private void DrawGizmos(ScriptableRenderContext context, Camera camera, GizmoSubset gizmoSubset)
		{
		}

		[Conditional("UNITY_EDITOR")]
		private void DrawWireOverlay(ScriptableRenderContext context, Camera camera)
		{
			context.DrawWireOverlay(camera);
		}

		private void InternalStartRendering(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, Profiling.internalStartRendering))
			{
				for (int i = 0; i < m_ActiveRenderPassQueue.Count; i++)
				{
					m_ActiveRenderPassQueue[i].OnCameraSetup(commandBuffer, ref renderingData);
				}
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		private void InternalFinishRendering(ScriptableRenderContext context, bool resolveFinalTarget)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, Profiling.internalFinishRendering))
			{
				for (int i = 0; i < m_ActiveRenderPassQueue.Count; i++)
				{
					m_ActiveRenderPassQueue[i].FrameCleanup(commandBuffer);
				}
				if (resolveFinalTarget)
				{
					for (int j = 0; j < m_ActiveRenderPassQueue.Count; j++)
					{
						m_ActiveRenderPassQueue[j].OnFinishCameraStackRendering(commandBuffer);
					}
					FinishRendering(commandBuffer);
					m_IsPipelineExecuting = false;
				}
				m_ActiveRenderPassQueue.Clear();
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		internal static void SortStable(List<ScriptableRenderPass> list)
		{
			for (int i = 1; i < list.Count; i++)
			{
				ScriptableRenderPass scriptableRenderPass = list[i];
				int num = i - 1;
				while (num >= 0 && scriptableRenderPass < list[num])
				{
					list[num + 1] = list[num];
					num--;
				}
				list[num + 1] = scriptableRenderPass;
			}
		}
	}
}
