using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
	public sealed class ForwardRenderer : ScriptableRenderer
	{
		private static class Profiling
		{
			private const string k_Name = "ForwardRenderer";

			public static readonly ProfilingSampler createCameraRenderTarget = new ProfilingSampler("ForwardRenderer.CreateCameraRenderTarget");
		}

		private struct RenderPassInputSummary
		{
			internal bool requiresDepthTexture;

			internal bool requiresDepthPrepass;

			internal bool requiresNormalsTexture;

			internal bool requiresColorTexture;
		}

		private const int k_DepthStencilBufferBits = 32;

		private ColorGradingLutPass m_ColorGradingLutPass;

		private DepthOnlyPass m_DepthPrepass;

		private DepthNormalOnlyPass m_DepthNormalPrepass;

		private MainLightShadowCasterPass m_MainLightShadowCasterPass;

		private AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;

		private GBufferPass m_GBufferPass;

		private CopyDepthPass m_GBufferCopyDepthPass;

		private TileDepthRangePass m_TileDepthRangePass;

		private TileDepthRangePass m_TileDepthRangeExtraPass;

		private DeferredPass m_DeferredPass;

		private DrawObjectsPass m_RenderOpaqueForwardOnlyPass;

		private DrawObjectsPass m_RenderOpaqueForwardPass;

		private DrawSkyboxPass m_DrawSkyboxPass;

		private CopyDepthPass m_CopyDepthPass;

		private CopyColorPass m_CopyColorPass;

		private TransparentSettingsPass m_TransparentSettingsPass;

		private DrawObjectsPass m_RenderTransparentForwardPass;

		private InvokeOnRenderObjectCallbackPass m_OnRenderObjectCallbackPass;

		private PostProcessPass m_PostProcessPass;

		private PostProcessPass m_FinalPostProcessPass;

		private FinalBlitPass m_FinalBlitPass;

		private CapturePass m_CapturePass;

		private XROcclusionMeshPass m_XROcclusionMeshPass;

		private CopyDepthPass m_XRCopyDepthPass;

		private RenderTargetHandle m_ActiveCameraColorAttachment;

		private RenderTargetHandle m_ActiveCameraDepthAttachment;

		private RenderTargetHandle m_CameraColorAttachment;

		private RenderTargetHandle m_CameraDepthAttachment;

		private RenderTargetHandle m_DepthTexture;

		private RenderTargetHandle m_NormalsTexture;

		private RenderTargetHandle[] m_GBufferHandles;

		private RenderTargetHandle m_OpaqueColor;

		private RenderTargetHandle m_AfterPostProcessColor;

		private RenderTargetHandle m_ColorGradingLut;

		private RenderTargetHandle m_DepthInfoTexture;

		private RenderTargetHandle m_TileDepthInfoTexture;

		private ForwardLights m_ForwardLights;

		private DeferredLights m_DeferredLights;

		private RenderingMode m_RenderingMode;

		private StencilState m_DefaultStencilState;

		private Material m_BlitMaterial;

		private Material m_CopyDepthMaterial;

		private Material m_SamplingMaterial;

		private Material m_ScreenspaceShadowsMaterial;

		private Material m_TileDepthInfoMaterial;

		private Material m_TileDeferredMaterial;

		private Material m_StencilDeferredMaterial;

		internal RenderingMode renderingMode => RenderingMode.Forward;

		internal RenderingMode actualRenderingMode
		{
			get
			{
				if (!GL.wireframe && m_DeferredLights != null && m_DeferredLights.IsRuntimeSupportedThisFrame())
				{
					return renderingMode;
				}
				return RenderingMode.Forward;
			}
		}

		internal bool accurateGbufferNormals
		{
			get
			{
				if (m_DeferredLights == null)
				{
					return false;
				}
				return m_DeferredLights.AccurateGbufferNormals;
			}
		}

		public ForwardRenderer(ForwardRendererData data)
			: base(data)
		{
			UniversalRenderPipeline.m_XRSystem.InitializeXRSystemData(data.xrSystemData);
			m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitPS);
			m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(data.shaders.copyDepthPS);
			m_SamplingMaterial = CoreUtils.CreateEngineMaterial(data.shaders.samplingPS);
			m_ScreenspaceShadowsMaterial = CoreUtils.CreateEngineMaterial(data.shaders.screenSpaceShadowPS);
			m_StencilDeferredMaterial = CoreUtils.CreateEngineMaterial(data.shaders.stencilDeferredPS);
			StencilStateData defaultStencilState = data.defaultStencilState;
			m_DefaultStencilState = StencilState.defaultValue;
			m_DefaultStencilState.enabled = defaultStencilState.overrideStencilState;
			m_DefaultStencilState.SetCompareFunction(defaultStencilState.stencilCompareFunction);
			m_DefaultStencilState.SetPassOperation(defaultStencilState.passOperation);
			m_DefaultStencilState.SetFailOperation(defaultStencilState.failOperation);
			m_DefaultStencilState.SetZFailOperation(defaultStencilState.zFailOperation);
			m_ForwardLights = new ForwardLights();
			m_RenderingMode = RenderingMode.Forward;
			m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
			m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
			m_XROcclusionMeshPass = new XROcclusionMeshPass(RenderPassEvent.BeforeRenderingOpaques);
			m_XRCopyDepthPass = new CopyDepthPass((RenderPassEvent)1002, m_CopyDepthMaterial);
			m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrepasses, RenderQueueRange.opaque, data.opaqueLayerMask);
			m_DepthNormalPrepass = new DepthNormalOnlyPass(RenderPassEvent.BeforeRenderingPrepasses, RenderQueueRange.opaque, data.opaqueLayerMask);
			m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrepasses, data.postProcessData);
			if (renderingMode == RenderingMode.Deferred)
			{
				m_DeferredLights = new DeferredLights(m_TileDepthInfoMaterial, m_TileDeferredMaterial, m_StencilDeferredMaterial);
				m_DeferredLights.AccurateGbufferNormals = data.accurateGbufferNormals;
				m_DeferredLights.TiledDeferredShading = false;
				_ = GraphicsSettings.renderPipelineAsset;
				m_GBufferPass = new GBufferPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, defaultStencilState.stencilReference, m_DeferredLights);
				StencilState stencilState = DeferredLights.OverwriteStencil(m_DefaultStencilState, 96);
				ShaderTagId[] shaderTagIds = new ShaderTagId[3]
				{
					new ShaderTagId("UniversalForwardOnly"),
					new ShaderTagId("SRPDefaultUnlit"),
					new ShaderTagId("LightweightForward")
				};
				int stencilReference = defaultStencilState.stencilReference | 0;
				m_RenderOpaqueForwardOnlyPass = new DrawObjectsPass("Render Opaques Forward Only", shaderTagIds, opaque: true, (RenderPassEvent)251, RenderQueueRange.opaque, data.opaqueLayerMask, stencilState, stencilReference);
				m_GBufferCopyDepthPass = new CopyDepthPass((RenderPassEvent)252, m_CopyDepthMaterial);
				m_TileDepthRangePass = new TileDepthRangePass((RenderPassEvent)253, m_DeferredLights, 0);
				m_TileDepthRangeExtraPass = new TileDepthRangePass((RenderPassEvent)254, m_DeferredLights, 1);
				m_DeferredPass = new DeferredPass((RenderPassEvent)255, m_DeferredLights);
			}
			m_RenderOpaqueForwardPass = new DrawObjectsPass(URPProfileId.DrawOpaqueObjects, opaque: true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, defaultStencilState.stencilReference);
			m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingSkybox, m_CopyDepthMaterial);
			m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
			m_CopyColorPass = new CopyColorPass(RenderPassEvent.AfterRenderingSkybox, m_SamplingMaterial, m_BlitMaterial);
			m_TransparentSettingsPass = new TransparentSettingsPass(RenderPassEvent.BeforeRenderingTransparents, data.shadowTransparentReceive);
			m_RenderTransparentForwardPass = new DrawObjectsPass(URPProfileId.DrawTransparentObjects, opaque: false, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, data.transparentLayerMask, m_DefaultStencilState, defaultStencilState.stencilReference);
			m_OnRenderObjectCallbackPass = new InvokeOnRenderObjectCallbackPass(RenderPassEvent.BeforeRenderingPostProcessing);
			m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data.postProcessData, m_BlitMaterial);
			m_FinalPostProcessPass = new PostProcessPass((RenderPassEvent)1001, data.postProcessData, m_BlitMaterial);
			m_CapturePass = new CapturePass(RenderPassEvent.AfterRendering);
			m_FinalBlitPass = new FinalBlitPass((RenderPassEvent)1001, m_BlitMaterial);
			m_CameraColorAttachment.Init("_CameraColorTexture");
			m_CameraDepthAttachment.Init("_CameraDepthAttachment");
			m_DepthTexture.Init("_CameraDepthTexture");
			m_NormalsTexture.Init("_CameraNormalsTexture");
			if (renderingMode == RenderingMode.Deferred)
			{
				m_GBufferHandles = new RenderTargetHandle[6];
				m_GBufferHandles[0].Init("_GBufferDepthAsColor");
				m_GBufferHandles[1].Init("_GBuffer0");
				m_GBufferHandles[2].Init("_GBuffer1");
				m_GBufferHandles[3].Init("_GBuffer2");
				m_GBufferHandles[4] = default(RenderTargetHandle);
				m_GBufferHandles[5].Init("_GBuffer4");
			}
			m_OpaqueColor.Init("_CameraOpaqueTexture");
			m_AfterPostProcessColor.Init("_AfterPostProcessTexture");
			m_ColorGradingLut.Init("_InternalGradingLut");
			m_DepthInfoTexture.Init("_DepthInfoTexture");
			m_TileDepthInfoTexture.Init("_TileDepthInfoTexture");
			base.supportedRenderingFeatures = new RenderingFeatures
			{
				cameraStacking = true
			};
			if (renderingMode == RenderingMode.Deferred)
			{
				base.unsupportedGraphicsDeviceTypes = new GraphicsDeviceType[3]
				{
					GraphicsDeviceType.OpenGLCore,
					GraphicsDeviceType.OpenGLES2,
					GraphicsDeviceType.OpenGLES3
				};
			}
		}

		protected override void Dispose(bool disposing)
		{
			m_PostProcessPass.Cleanup();
			m_FinalPostProcessPass.Cleanup();
			m_ColorGradingLutPass.Cleanup();
			CoreUtils.Destroy(m_BlitMaterial);
			CoreUtils.Destroy(m_CopyDepthMaterial);
			CoreUtils.Destroy(m_SamplingMaterial);
			CoreUtils.Destroy(m_ScreenspaceShadowsMaterial);
			CoreUtils.Destroy(m_TileDepthInfoMaterial);
			CoreUtils.Destroy(m_TileDeferredMaterial);
			CoreUtils.Destroy(m_StencilDeferredMaterial);
		}

		public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			Camera camera = renderingData.cameraData.camera;
			ref CameraData cameraData = ref renderingData.cameraData;
			RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
			if (cameraData.targetTexture != null && cameraData.targetTexture.format == RenderTextureFormat.Depth)
			{
				ConfigureCameraTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
				AddRenderPasses(ref renderingData);
				EnqueuePass(m_RenderOpaqueForwardPass);
				EnqueuePass(m_DrawSkyboxPass);
				EnqueuePass(m_RenderTransparentForwardPass);
				return;
			}
			if (m_DeferredLights != null)
			{
				m_DeferredLights.ResolveMixedLightingMode(ref renderingData);
			}
			bool isPreviewCamera = cameraData.isPreviewCamera;
			bool flag = false;
			flag = UniversalRenderPipeline.IsRunningHololens(cameraData);
			bool flag2 = base.rendererFeatures.Count != 0 && !flag && !isPreviewCamera;
			if (flag2)
			{
				m_ActiveCameraColorAttachment = m_CameraColorAttachment;
				RenderTargetIdentifier renderTargetIdentifier = m_ActiveCameraColorAttachment.Identifier();
				if (cameraData.xr.enabled)
				{
					renderTargetIdentifier = new RenderTargetIdentifier(renderTargetIdentifier, 0, CubemapFace.Unknown, -1);
				}
				ConfigureCameraColorTarget(renderTargetIdentifier);
			}
			isCameraColorTargetValid = true;
			AddRenderPasses(ref renderingData);
			isCameraColorTargetValid = false;
			RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);
			bool postProcessEnabled = cameraData.postProcessEnabled;
			bool postProcessingEnabled = renderingData.postProcessingEnabled;
			bool postProcessEnabled2 = cameraData.postProcessEnabled;
			bool isSceneViewCamera = cameraData.isSceneViewCamera;
			bool flag3 = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || actualRenderingMode == RenderingMode.Deferred;
			bool flag4 = m_MainLightShadowCasterPass.Setup(ref renderingData);
			bool flag5 = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);
			bool flag6 = m_TransparentSettingsPass.Setup(ref renderingData);
			bool flag7 = flag3 && !CanCopyDepth(ref renderingData.cameraData);
			flag7 = flag7 || isSceneViewCamera;
			flag7 = flag7 || isPreviewCamera;
			flag7 |= renderPassInputs.requiresDepthPrepass;
			flag7 |= renderPassInputs.requiresNormalsTexture;
			m_CopyDepthPass.renderPassEvent = ((!flag3 && (postProcessEnabled || isSceneViewCamera)) ? RenderPassEvent.AfterRenderingTransparents : RenderPassEvent.AfterRenderingOpaques);
			flag2 |= RequiresIntermediateColorTexture(ref cameraData);
			flag2 |= renderPassInputs.requiresColorTexture;
			flag2 = flag2 && !isPreviewCamera;
			if (cameraData.xr.enabled && flag3)
			{
				flag7 = true;
			}
			bool flag8 = cameraData.requiresDepthTexture && !flag7;
			flag8 |= cameraData.renderType == CameraRenderType.Base && !cameraData.resolveFinalTarget;
			flag8 |= actualRenderingMode == RenderingMode.Deferred;
			if (cameraData.xr.enabled)
			{
				flag8 = flag8 || flag2;
				flag2 = flag8;
			}
			if (cameraData.renderType == CameraRenderType.Base)
			{
				RenderTargetHandle cameraTarget = RenderTargetHandle.GetCameraTarget(cameraData.xr);
				m_ActiveCameraColorAttachment = (flag2 ? m_CameraColorAttachment : cameraTarget);
				m_ActiveCameraDepthAttachment = (flag8 ? m_CameraDepthAttachment : cameraTarget);
				if (flag2 || flag8)
				{
					CreateCameraRenderTarget(context, ref descriptor, flag2, flag8);
				}
			}
			else
			{
				m_ActiveCameraColorAttachment = m_CameraColorAttachment;
				m_ActiveCameraDepthAttachment = m_CameraDepthAttachment;
			}
			RenderTargetIdentifier renderTargetIdentifier2 = m_ActiveCameraColorAttachment.Identifier();
			RenderTargetIdentifier renderTargetIdentifier3 = m_ActiveCameraDepthAttachment.Identifier();
			if (cameraData.xr.enabled)
			{
				renderTargetIdentifier2 = new RenderTargetIdentifier(renderTargetIdentifier2, 0, CubemapFace.Unknown, -1);
				renderTargetIdentifier3 = new RenderTargetIdentifier(renderTargetIdentifier3, 0, CubemapFace.Unknown, -1);
			}
			ConfigureCameraTarget(renderTargetIdentifier2, renderTargetIdentifier3);
			bool flag9 = base.activeRenderPassQueue.Find((ScriptableRenderPass x) => x.renderPassEvent == RenderPassEvent.AfterRendering) != null;
			if (flag4)
			{
				EnqueuePass(m_MainLightShadowCasterPass);
			}
			if (flag5)
			{
				EnqueuePass(m_AdditionalLightsShadowCasterPass);
			}
			if (flag7)
			{
				if (renderPassInputs.requiresNormalsTexture)
				{
					m_DepthNormalPrepass.Setup(descriptor, m_DepthTexture, m_NormalsTexture);
					EnqueuePass(m_DepthNormalPrepass);
				}
				else
				{
					m_DepthPrepass.Setup(descriptor, m_DepthTexture);
					EnqueuePass(m_DepthPrepass);
				}
			}
			if (postProcessEnabled2)
			{
				m_ColorGradingLutPass.Setup(in m_ColorGradingLut);
				EnqueuePass(m_ColorGradingLutPass);
			}
			if (cameraData.xr.hasValidOcclusionMesh)
			{
				EnqueuePass(m_XROcclusionMeshPass);
			}
			if (actualRenderingMode == RenderingMode.Deferred)
			{
				EnqueueDeferred(ref renderingData, flag7, flag4, flag5);
			}
			else
			{
				EnqueuePass(m_RenderOpaqueForwardPass);
			}
			cameraData.camera.TryGetComponent<Skybox>(out var component);
			bool flag10 = cameraData.renderType == CameraRenderType.Overlay;
			if (camera.clearFlags == CameraClearFlags.Skybox && (RenderSettings.skybox != null || component?.material != null) && !flag10)
			{
				EnqueuePass(m_DrawSkyboxPass);
			}
			bool flag11 = !flag7 && renderingData.cameraData.requiresDepthTexture && flag8 && actualRenderingMode != RenderingMode.Deferred;
			if (flag11)
			{
				m_CopyDepthPass.Setup(m_ActiveCameraDepthAttachment, m_DepthTexture);
				EnqueuePass(m_CopyDepthPass);
			}
			if (cameraData.renderType == CameraRenderType.Base && !flag7 && !flag11)
			{
				Shader.SetGlobalTexture(m_DepthTexture.id, SystemInfo.usesReversedZBuffer ? Texture2D.blackTexture : Texture2D.whiteTexture);
			}
			if (renderingData.cameraData.requiresOpaqueTexture || renderPassInputs.requiresColorTexture)
			{
				Downsampling opaqueDownsampling = UniversalRenderPipeline.asset.opaqueDownsampling;
				m_CopyColorPass.Setup(m_ActiveCameraColorAttachment.Identifier(), m_OpaqueColor, opaqueDownsampling);
				EnqueuePass(m_CopyColorPass);
			}
			if (flag6)
			{
				EnqueuePass(m_TransparentSettingsPass);
			}
			EnqueuePass(m_RenderTransparentForwardPass);
			EnqueuePass(m_OnRenderObjectCallbackPass);
			bool resolveFinalTarget = cameraData.resolveFinalTarget;
			bool num = renderingData.cameraData.captureActions != null && resolveFinalTarget;
			bool flag12 = postProcessingEnabled && resolveFinalTarget && renderingData.cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing;
			bool flag13 = !num && !flag9 && !flag12;
			if (resolveFinalTarget)
			{
				if (postProcessEnabled)
				{
					RenderTargetHandle destination = (flag13 ? RenderTargetHandle.CameraTarget : m_AfterPostProcessColor);
					bool enableSRGBConversion = flag13;
					m_PostProcessPass.Setup(in descriptor, in m_ActiveCameraColorAttachment, in destination, in m_ActiveCameraDepthAttachment, in m_ColorGradingLut, flag12, enableSRGBConversion);
					EnqueuePass(m_PostProcessPass);
				}
				RenderTargetHandle source = (postProcessEnabled ? m_AfterPostProcessColor : m_ActiveCameraColorAttachment);
				if (flag12)
				{
					m_FinalPostProcessPass.SetupFinalPass(in source);
					EnqueuePass(m_FinalPostProcessPass);
				}
				if (renderingData.cameraData.captureActions != null)
				{
					m_CapturePass.Setup(source);
					EnqueuePass(m_CapturePass);
				}
				if (!flag12 && (!postProcessEnabled || flag9) && !(m_ActiveCameraColorAttachment == RenderTargetHandle.GetCameraTarget(cameraData.xr)))
				{
					m_FinalBlitPass.Setup(descriptor, source);
					EnqueuePass(m_FinalBlitPass);
				}
				if (!(m_ActiveCameraDepthAttachment == RenderTargetHandle.GetCameraTarget(cameraData.xr)) && cameraData.xr.copyDepth)
				{
					m_XRCopyDepthPass.Setup(m_ActiveCameraDepthAttachment, RenderTargetHandle.GetCameraTarget(cameraData.xr));
					EnqueuePass(m_XRCopyDepthPass);
				}
			}
			else if (postProcessEnabled)
			{
				m_PostProcessPass.Setup(in descriptor, in m_ActiveCameraColorAttachment, in m_AfterPostProcessColor, in m_ActiveCameraDepthAttachment, in m_ColorGradingLut, hasFinalPass: false, enableSRGBConversion: false);
				EnqueuePass(m_PostProcessPass);
			}
		}

		public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			m_ForwardLights.Setup(context, ref renderingData);
			if (actualRenderingMode == RenderingMode.Deferred)
			{
				m_DeferredLights.SetupLights(context, ref renderingData);
			}
		}

		public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
		{
			bool num = !UniversalRenderPipeline.asset.supportsMainLightShadows && !UniversalRenderPipeline.asset.supportsAdditionalLightShadows;
			bool flag = Mathf.Approximately(cameraData.maxShadowDistance, 0f);
			if (num || flag)
			{
				cullingParameters.cullingOptions &= ~CullingOptions.ShadowCasters;
			}
			if (actualRenderingMode == RenderingMode.Deferred)
			{
				cullingParameters.maximumVisibleLights = 65535;
			}
			else
			{
				cullingParameters.maximumVisibleLights = UniversalRenderPipeline.maxVisibleAdditionalLights + 1;
			}
			cullingParameters.shadowDistance = cameraData.maxShadowDistance;
		}

		public override void FinishRendering(CommandBuffer cmd)
		{
			if (m_ActiveCameraColorAttachment != RenderTargetHandle.CameraTarget)
			{
				cmd.ReleaseTemporaryRT(m_ActiveCameraColorAttachment.id);
				m_ActiveCameraColorAttachment = RenderTargetHandle.CameraTarget;
			}
			if (m_ActiveCameraDepthAttachment != RenderTargetHandle.CameraTarget)
			{
				cmd.ReleaseTemporaryRT(m_ActiveCameraDepthAttachment.id);
				m_ActiveCameraDepthAttachment = RenderTargetHandle.CameraTarget;
			}
		}

		private void EnqueueDeferred(ref RenderingData renderingData, bool hasDepthPrepass, bool applyMainShadow, bool applyAdditionalShadow)
		{
			m_GBufferHandles[4] = m_ActiveCameraColorAttachment;
			m_DeferredLights.Setup(ref renderingData, applyAdditionalShadow ? m_AdditionalLightsShadowCasterPass : null, hasDepthPrepass, renderingData.cameraData.renderType == CameraRenderType.Overlay, m_DepthTexture, m_DepthInfoTexture, m_TileDepthInfoTexture, m_ActiveCameraDepthAttachment, m_GBufferHandles);
			EnqueuePass(m_GBufferPass);
			EnqueuePass(m_RenderOpaqueForwardOnlyPass);
			if (!hasDepthPrepass)
			{
				m_GBufferCopyDepthPass.Setup(m_CameraDepthAttachment, m_DepthTexture);
				EnqueuePass(m_GBufferCopyDepthPass);
			}
			if (m_DeferredLights.HasTileLights())
			{
				EnqueuePass(m_TileDepthRangePass);
				if (m_DeferredLights.HasTileDepthRangeExtraPass())
				{
					EnqueuePass(m_TileDepthRangeExtraPass);
				}
			}
			EnqueuePass(m_DeferredPass);
		}

		private RenderPassInputSummary GetRenderPassInputs(ref RenderingData renderingData)
		{
			RenderPassInputSummary result = default(RenderPassInputSummary);
			for (int i = 0; i < base.activeRenderPassQueue.Count; i++)
			{
				ScriptableRenderPass scriptableRenderPass = base.activeRenderPassQueue[i];
				bool flag = (scriptableRenderPass.input & ScriptableRenderPassInput.Depth) != 0;
				bool flag2 = (scriptableRenderPass.input & ScriptableRenderPassInput.Normal) != 0;
				bool flag3 = (scriptableRenderPass.input & ScriptableRenderPassInput.Color) != 0;
				bool flag4 = scriptableRenderPass.renderPassEvent <= RenderPassEvent.BeforeRenderingOpaques;
				result.requiresDepthTexture |= flag;
				result.requiresDepthPrepass |= flag2 || (flag && flag4);
				result.requiresNormalsTexture |= flag2;
				result.requiresColorTexture |= flag3;
			}
			return result;
		}

		private void CreateCameraRenderTarget(ScriptableRenderContext context, ref RenderTextureDescriptor descriptor, bool createColor, bool createDepth)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, Profiling.createCameraRenderTarget))
			{
				if (createColor)
				{
					bool flag = m_ActiveCameraDepthAttachment == RenderTargetHandle.CameraTarget;
					RenderTextureDescriptor desc = descriptor;
					desc.useMipMap = false;
					desc.autoGenerateMips = false;
					desc.depthBufferBits = (flag ? 32 : 0);
					commandBuffer.GetTemporaryRT(m_ActiveCameraColorAttachment.id, desc, FilterMode.Bilinear);
				}
				if (createDepth)
				{
					RenderTextureDescriptor desc2 = descriptor;
					desc2.useMipMap = false;
					desc2.autoGenerateMips = false;
					desc2.bindMS = desc2.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && SystemInfo.supportsMultisampledTextures != 0;
					desc2.colorFormat = RenderTextureFormat.Depth;
					desc2.depthBufferBits = 32;
					commandBuffer.GetTemporaryRT(m_ActiveCameraDepthAttachment.id, desc2, FilterMode.Point);
				}
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		private bool PlatformRequiresExplicitMsaaResolve()
		{
			if (!SystemInfo.supportsMultisampleAutoResolve)
			{
				if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
				{
					return !Application.isMobilePlatform;
				}
				return true;
			}
			return false;
		}

		private bool RequiresIntermediateColorTexture(ref CameraData cameraData)
		{
			if (cameraData.renderType == CameraRenderType.Base && !cameraData.resolveFinalTarget)
			{
				return true;
			}
			if (actualRenderingMode == RenderingMode.Deferred)
			{
				return true;
			}
			bool isSceneViewCamera = cameraData.isSceneViewCamera;
			RenderTextureDescriptor cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
			int msaaSamples = cameraTargetDescriptor.msaaSamples;
			bool flag = !Mathf.Approximately(cameraData.renderScale, 1f);
			bool flag2 = cameraTargetDescriptor.dimension == TextureDimension.Tex2D;
			bool flag3 = msaaSamples > 1 && PlatformRequiresExplicitMsaaResolve();
			bool num = cameraData.targetTexture != null && !isSceneViewCamera;
			bool flag4 = cameraData.captureActions != null;
			if (cameraData.xr.enabled)
			{
				flag2 = cameraData.xr.renderTargetDesc.dimension == cameraTargetDescriptor.dimension;
			}
			bool flag5 = cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || flag3 || !cameraData.isDefaultViewport;
			if (num)
			{
				return flag5;
			}
			if (!(flag5 || isSceneViewCamera || flag || cameraData.isHdrEnabled || !flag2 || flag4))
			{
				return cameraData.requireSrgbConversion;
			}
			return true;
		}

		private bool CanCopyDepth(ref CameraData cameraData)
		{
			bool num = cameraData.cameraTargetDescriptor.msaaSamples > 1;
			bool flag = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
			bool flag2 = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
			bool num2 = !num && (flag2 || flag);
			bool flag3 = false;
			return num2 || flag3;
		}
	}
}
