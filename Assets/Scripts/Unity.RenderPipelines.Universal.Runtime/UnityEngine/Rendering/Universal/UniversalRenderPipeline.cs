using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.XR;

namespace UnityEngine.Rendering.Universal
{
	public sealed class UniversalRenderPipeline : RenderPipeline
	{
		private static class Profiling
		{
			public static class Pipeline
			{
				public static class Renderer
				{
					private const string k_Name = "ScriptableRenderer";

					public static readonly ProfilingSampler setupCullingParameters = new ProfilingSampler("ScriptableRenderer.SetupCullingParameters");

					public static readonly ProfilingSampler setup = new ProfilingSampler("ScriptableRenderer.Setup");
				}

				public static class Context
				{
					private const string k_Name = "Context";

					public static readonly ProfilingSampler submit = new ProfilingSampler("Context.Submit");
				}

				public static class XR
				{
					public static readonly ProfilingSampler mirrorView = new ProfilingSampler("XR Mirror View");
				}

				public static readonly ProfilingSampler beginFrameRendering = new ProfilingSampler("RenderPipeline.BeginFrameRendering");

				public static readonly ProfilingSampler endFrameRendering = new ProfilingSampler("RenderPipeline.EndFrameRendering");

				public static readonly ProfilingSampler beginCameraRendering = new ProfilingSampler("RenderPipeline.BeginCameraRendering");

				public static readonly ProfilingSampler endCameraRendering = new ProfilingSampler("RenderPipeline.EndCameraRendering");

				private const string k_Name = "UniversalRenderPipeline";

				public static readonly ProfilingSampler initializeCameraData = new ProfilingSampler("UniversalRenderPipeline.InitializeCameraData");

				public static readonly ProfilingSampler initializeStackedCameraData = new ProfilingSampler("UniversalRenderPipeline.InitializeStackedCameraData");

				public static readonly ProfilingSampler initializeAdditionalCameraData = new ProfilingSampler("UniversalRenderPipeline.InitializeAdditionalCameraData");

				public static readonly ProfilingSampler initializeRenderingData = new ProfilingSampler("UniversalRenderPipeline.InitializeRenderingData");

				public static readonly ProfilingSampler initializeShadowData = new ProfilingSampler("UniversalRenderPipeline.InitializeShadowData");

				public static readonly ProfilingSampler initializeLightData = new ProfilingSampler("UniversalRenderPipeline.InitializeLightData");

				public static readonly ProfilingSampler getPerObjectLightFlags = new ProfilingSampler("UniversalRenderPipeline.GetPerObjectLightFlags");

				public static readonly ProfilingSampler getMainLightIndex = new ProfilingSampler("UniversalRenderPipeline.GetMainLightIndex");

				public static readonly ProfilingSampler setupPerFrameShaderConstants = new ProfilingSampler("UniversalRenderPipeline.SetupPerFrameShaderConstants");
			}

			private static Dictionary<int, ProfilingSampler> s_HashSamplerCache = new Dictionary<int, ProfilingSampler>();

			public static readonly ProfilingSampler unknownSampler = new ProfilingSampler("Unknown");

			public static ProfilingSampler TryGetOrAddCameraSampler(Camera camera)
			{
				ProfilingSampler value = null;
				int hashCode = camera.GetHashCode();
				if (!s_HashSamplerCache.TryGetValue(hashCode, out value))
				{
					value = new ProfilingSampler("UniversalRenderPipeline.RenderSingleCamera: " + camera.name);
					s_HashSamplerCache.Add(hashCode, value);
				}
				return value;
			}
		}

		public const string k_ShaderTagName = "UniversalPipeline";

		internal static XRSystem m_XRSystem = new XRSystem();

		private const int k_MaxVisibleAdditionalLightsMobileShaderLevelLessThan45 = 16;

		private const int k_MaxVisibleAdditionalLightsMobile = 32;

		private const int k_MaxVisibleAdditionalLightsNonMobile = 256;

		private static Vector4 k_DefaultLightPosition = new Vector4(0f, 0f, 1f, 0f);

		private static Vector4 k_DefaultLightColor = Color.black;

		private static Vector4 k_DefaultLightAttenuation = new Vector4(0f, 1f, 0f, 1f);

		private static Vector4 k_DefaultLightSpotDirection = new Vector4(0f, 0f, 1f, 0f);

		private static Vector4 k_DefaultLightsProbeChannel = new Vector4(0f, 0f, 0f, 0f);

		private static List<Vector4> m_ShadowBiasData = new List<Vector4>();

		private static List<XRDisplaySubsystem> displaySubsystemList = new List<XRDisplaySubsystem>();

		private Comparison<Camera> cameraComparison = (Camera camera1, Camera camera2) => (int)camera1.depth - (int)camera2.depth;

		private static Lightmapping.RequestLightsDelegate lightsDelegate = delegate(Light[] requests, NativeArray<LightDataGI> lightsOutput)
		{
			LightDataGI value = default(LightDataGI);
			for (int i = 0; i < requests.Length; i++)
			{
				Light light = requests[i];
				value.InitNoBake(light.GetInstanceID());
				lightsOutput[i] = value;
			}
		};

		public static float maxShadowBias => 10f;

		public static float minRenderScale => 0.1f;

		public static float maxRenderScale => 2f;

		public static int maxPerObjectLights
		{
			get
			{
				if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2)
				{
					return 8;
				}
				return 4;
			}
		}

		public static int maxVisibleAdditionalLights
		{
			get
			{
				bool isMobilePlatform = Application.isMobilePlatform;
				if (isMobilePlatform && (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && Graphics.minOpenGLESVersion <= OpenGLESVersion.OpenGLES30)))
				{
					return 16;
				}
				if (!isMobilePlatform && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3)
				{
					return 256;
				}
				return 32;
			}
		}

		public static UniversalRenderPipelineAsset asset => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

		public UniversalRenderPipeline(UniversalRenderPipelineAsset asset)
		{
			SetSupportedRenderingFeatures();
			if (((QualitySettings.antiAliasing <= 0) ? 1 : QualitySettings.antiAliasing) != asset.msaaSampleCount)
			{
				QualitySettings.antiAliasing = asset.msaaSampleCount;
				XRSystem.UpdateMSAALevel(asset.msaaSampleCount);
			}
			XRSystem.UpdateRenderScale(asset.renderScale);
			Shader.globalRenderPipeline = "UniversalPipeline,LightweightPipeline";
			Lightmapping.SetDelegate(lightsDelegate);
			CameraCaptureBridge.enabled = true;
			RenderingUtils.ClearSystemInfoCache();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			Shader.globalRenderPipeline = "";
			SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
			ShaderData.instance.Dispose();
			DeferredShaderData.instance.Dispose();
			m_XRSystem?.Dispose();
			Lightmapping.ResetDelegate();
			CameraCaptureBridge.enabled = false;
		}

		protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
		{
			using (new ProfilingScope(null, ProfilingSampler.Get(URPProfileId.UniversalRenderTotal)))
			{
				using (new ProfilingScope(null, Profiling.Pipeline.beginFrameRendering))
				{
					RenderPipeline.BeginFrameRendering(renderContext, cameras);
				}
				GraphicsSettings.lightsUseLinearIntensity = QualitySettings.activeColorSpace == ColorSpace.Linear;
				GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;
				SetupPerFrameShaderConstants();
				XRSystem.UpdateMSAALevel(asset.msaaSampleCount);
				SortCameras(cameras);
				foreach (Camera camera in cameras)
				{
					if (IsGameCamera(camera))
					{
						RenderCameraStack(renderContext, camera);
						continue;
					}
					using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
					{
						RenderPipeline.BeginCameraRendering(renderContext, camera);
					}
					UpdateVolumeFramework(camera, null);
					RenderSingleCamera(renderContext, camera);
					using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
					{
						RenderPipeline.EndCameraRendering(renderContext, camera);
					}
				}
				using (new ProfilingScope(null, Profiling.Pipeline.endFrameRendering))
				{
					RenderPipeline.EndFrameRendering(renderContext, cameras);
				}
			}
		}

		public static void RenderSingleCamera(ScriptableRenderContext context, Camera camera)
		{
			UniversalAdditionalCameraData component = null;
			if (IsGameCamera(camera))
			{
				camera.gameObject.TryGetComponent<UniversalAdditionalCameraData>(out component);
			}
			if (component != null && component.renderType != 0)
			{
				Debug.LogWarning("Only Base cameras can be rendered with standalone RenderSingleCamera. Camera will be skipped.");
				return;
			}
			InitializeCameraData(camera, component, resolveFinalTarget: true, out var cameraData);
			RenderSingleCamera(context, cameraData, cameraData.postProcessEnabled);
		}

		private static bool TryGetCullingParameters(CameraData cameraData, out ScriptableCullingParameters cullingParams)
		{
			if (cameraData.xr.enabled)
			{
				cullingParams = cameraData.xr.cullingParams;
				if (!cameraData.camera.usePhysicalProperties)
				{
					cameraData.camera.fieldOfView = 57.29578f * Mathf.Atan(1f / cullingParams.stereoProjectionMatrix.m11) * 2f;
				}
				return true;
			}
			return cameraData.camera.TryGetCullingParameters(stereoAware: false, out cullingParams);
		}

		private static void RenderSingleCamera(ScriptableRenderContext context, CameraData cameraData, bool anyPostProcessingEnabled)
		{
			Camera camera = cameraData.camera;
			ScriptableRenderer renderer = cameraData.renderer;
			if (renderer == null)
			{
				Debug.LogWarning($"Trying to render {camera.name} with an invalid renderer. Camera rendering will be skipped.");
			}
			else
			{
				if (!TryGetCullingParameters(cameraData, out var cullingParams))
				{
					return;
				}
				ScriptableRenderer.current = renderer;
				_ = cameraData.isSceneViewCamera;
				CommandBuffer commandBuffer = CommandBufferPool.Get();
				CommandBuffer cmd = (cameraData.xr.enabled ? null : commandBuffer);
				ProfilingSampler sampler = Profiling.TryGetOrAddCameraSampler(camera);
				using (new ProfilingScope(cmd, sampler))
				{
					renderer.Clear(cameraData.renderType);
					using (new ProfilingScope(commandBuffer, Profiling.Pipeline.Renderer.setupCullingParameters))
					{
						renderer.SetupCullingParameters(ref cullingParams, ref cameraData);
					}
					context.ExecuteCommandBuffer(commandBuffer);
					commandBuffer.Clear();
					CullingResults cullResults = context.Cull(ref cullingParams);
					InitializeRenderingData(asset, ref cameraData, ref cullResults, anyPostProcessingEnabled, out var renderingData);
					using (new ProfilingScope(commandBuffer, Profiling.Pipeline.Renderer.setup))
					{
						renderer.Setup(context, ref renderingData);
					}
					renderer.Execute(context, ref renderingData);
				}
				cameraData.xr.EndCamera(commandBuffer, cameraData);
				context.ExecuteCommandBuffer(commandBuffer);
				CommandBufferPool.Release(commandBuffer);
				using (new ProfilingScope(commandBuffer, Profiling.Pipeline.Context.submit))
				{
					context.Submit();
				}
				ScriptableRenderer.current = null;
			}
		}

		private static void RenderCameraStack(ScriptableRenderContext context, Camera baseCamera)
		{
			using (new ProfilingScope(null, ProfilingSampler.Get(URPProfileId.RenderCameraStack)))
			{
				baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var component);
				if (component != null && component.renderType == CameraRenderType.Overlay)
				{
					return;
				}
				ScriptableRenderer scriptableRenderer = component?.scriptableRenderer;
				List<Camera> list = ((scriptableRenderer == null || !scriptableRenderer.supportedRenderingFeatures.cameraStacking) ? null : component?.cameraStack);
				bool flag = component != null && component.renderPostProcessing;
				int num = -1;
				if (list != null)
				{
					Type type = component?.scriptableRenderer.GetType();
					bool flag2 = false;
					for (int i = 0; i < list.Count; i++)
					{
						Camera camera = list[i];
						if (camera == null)
						{
							flag2 = true;
						}
						else
						{
							if (!camera.isActiveAndEnabled)
							{
								continue;
							}
							camera.TryGetComponent<UniversalAdditionalCameraData>(out var component2);
							if (component2 == null || component2.renderType != CameraRenderType.Overlay)
							{
								Debug.LogWarning($"Stack can only contain Overlay cameras. {camera.name} will skip rendering.");
								continue;
							}
							Type type2 = component2?.scriptableRenderer.GetType();
							if (type2 != type)
							{
								Type typeFromHandle = typeof(Renderer2D);
								if (type2 != typeFromHandle && type != typeFromHandle)
								{
									Debug.LogWarning($"Only cameras with compatible renderer types can be stacked. {camera.name} will skip rendering");
									continue;
								}
							}
							flag |= component2.renderPostProcessing;
							num = i;
						}
					}
					if (flag2)
					{
						component.UpdateCameraStack();
					}
				}
				flag &= SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;
				bool flag3 = num != -1;
				UpdateVolumeFramework(baseCamera, component);
				InitializeCameraData(baseCamera, component, !flag3, out var cameraData);
				RenderTextureDescriptor cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
				bool flag4 = false;
				foreach (XRPass item in m_XRSystem.SetupFrame(cameraData))
				{
					cameraData.isStereoEnabled = (cameraData.xr = item).enabled;
					if (cameraData.xr.enabled)
					{
						flag4 = true;
						m_XRSystem.UpdateCameraData(ref cameraData, in cameraData.xr);
					}
					using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
					{
						RenderPipeline.BeginCameraRendering(context, baseCamera);
					}
					RenderSingleCamera(context, cameraData, flag);
					using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
					{
						RenderPipeline.EndCameraRendering(context, baseCamera);
					}
					if (flag3)
					{
						for (int j = 0; j < list.Count; j++)
						{
							Camera camera2 = list[j];
							if (!camera2.isActiveAndEnabled)
							{
								continue;
							}
							camera2.TryGetComponent<UniversalAdditionalCameraData>(out var component3);
							if (component3 != null)
							{
								CameraData cameraData2 = cameraData;
								bool resolveFinalTarget = j == num;
								using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
								{
									RenderPipeline.BeginCameraRendering(context, camera2);
								}
								UpdateVolumeFramework(camera2, component3);
								InitializeAdditionalCameraData(camera2, component3, resolveFinalTarget, ref cameraData2);
								if (cameraData.xr.enabled)
								{
									m_XRSystem.UpdateFromCamera(ref cameraData2.xr, cameraData2);
								}
								RenderSingleCamera(context, cameraData2, flag);
								using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
								{
									RenderPipeline.EndCameraRendering(context, camera2);
								}
							}
						}
					}
					if (cameraData.xr.enabled)
					{
						cameraData.cameraTargetDescriptor = cameraTargetDescriptor;
					}
				}
				if (flag4)
				{
					CommandBuffer commandBuffer = CommandBufferPool.Get();
					using (new ProfilingScope(commandBuffer, Profiling.Pipeline.XR.mirrorView))
					{
						m_XRSystem.RenderMirrorView(commandBuffer, baseCamera);
					}
					context.ExecuteCommandBuffer(commandBuffer);
					context.Submit();
					CommandBufferPool.Release(commandBuffer);
				}
				m_XRSystem.ReleaseFrame();
			}
		}

		private static void UpdateVolumeFramework(Camera camera, UniversalAdditionalCameraData additionalCameraData)
		{
			using (new ProfilingScope(null, ProfilingSampler.Get(URPProfileId.UpdateVolumeFramework)))
			{
				LayerMask layerMask = 1;
				Transform transform = camera.transform;
				if (additionalCameraData != null)
				{
					layerMask = additionalCameraData.volumeLayerMask;
					transform = ((additionalCameraData.volumeTrigger != null) ? additionalCameraData.volumeTrigger : transform);
				}
				else if (camera.cameraType == CameraType.SceneView)
				{
					Camera main = Camera.main;
					UniversalAdditionalCameraData component = null;
					if (main != null && main.TryGetComponent<UniversalAdditionalCameraData>(out component))
					{
						layerMask = component.volumeLayerMask;
					}
					transform = ((component != null && component.volumeTrigger != null) ? component.volumeTrigger : transform);
				}
				VolumeManager.instance.Update(transform, layerMask);
			}
		}

		private static bool CheckPostProcessForDepth(in CameraData cameraData)
		{
			if (!cameraData.postProcessEnabled)
			{
				return false;
			}
			if (cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
			{
				return true;
			}
			VolumeStack stack = VolumeManager.instance.stack;
			if (stack.GetComponent<DepthOfField>().IsActive())
			{
				return true;
			}
			if (stack.GetComponent<MotionBlur>().IsActive())
			{
				return true;
			}
			return false;
		}

		private static void SetSupportedRenderingFeatures()
		{
		}

		private static void InitializeCameraData(Camera camera, UniversalAdditionalCameraData additionalCameraData, bool resolveFinalTarget, out CameraData cameraData)
		{
			using (new ProfilingScope(null, Profiling.Pipeline.initializeCameraData))
			{
				cameraData = default(CameraData);
				InitializeStackedCameraData(camera, additionalCameraData, ref cameraData);
				InitializeAdditionalCameraData(camera, additionalCameraData, resolveFinalTarget, ref cameraData);
				bool flag = (additionalCameraData?.scriptableRenderer)?.supportedRenderingFeatures.msaa ?? false;
				int msaaSamples = 1;
				if (camera.allowMSAA && asset.msaaSampleCount > 1 && flag)
				{
					msaaSamples = ((camera.targetTexture != null) ? camera.targetTexture.antiAliasing : asset.msaaSampleCount);
				}
				if (cameraData.xrRendering)
				{
					msaaSamples = XRSystem.GetMSAALevel();
				}
				bool preserveFramebufferAlpha = Graphics.preserveFramebufferAlpha;
				cameraData.cameraTargetDescriptor = CreateRenderTextureDescriptor(camera, cameraData.renderScale, cameraData.isHdrEnabled, msaaSamples, preserveFramebufferAlpha, cameraData.requiresOpaqueTexture);
			}
		}

		private static void InitializeStackedCameraData(Camera baseCamera, UniversalAdditionalCameraData baseAdditionalCameraData, ref CameraData cameraData)
		{
			using (new ProfilingScope(null, Profiling.Pipeline.initializeStackedCameraData))
			{
				UniversalRenderPipelineAsset universalRenderPipelineAsset = asset;
				cameraData.targetTexture = baseCamera.targetTexture;
				cameraData.cameraType = baseCamera.cameraType;
				if (cameraData.isSceneViewCamera)
				{
					cameraData.volumeLayerMask = 1;
					cameraData.volumeTrigger = null;
					cameraData.isStopNaNEnabled = false;
					cameraData.isDitheringEnabled = false;
					cameraData.antialiasing = AntialiasingMode.None;
					cameraData.antialiasingQuality = AntialiasingQuality.High;
					cameraData.xrRendering = false;
				}
				else if (baseAdditionalCameraData != null)
				{
					cameraData.volumeLayerMask = baseAdditionalCameraData.volumeLayerMask;
					cameraData.volumeTrigger = ((baseAdditionalCameraData.volumeTrigger == null) ? baseCamera.transform : baseAdditionalCameraData.volumeTrigger);
					cameraData.isStopNaNEnabled = baseAdditionalCameraData.stopNaN && SystemInfo.graphicsShaderLevel >= 35;
					cameraData.isDitheringEnabled = baseAdditionalCameraData.dithering;
					cameraData.antialiasing = baseAdditionalCameraData.antialiasing;
					cameraData.antialiasingQuality = baseAdditionalCameraData.antialiasingQuality;
					cameraData.xrRendering = baseAdditionalCameraData.allowXRRendering && m_XRSystem.RefreshXrSdk();
				}
				else
				{
					cameraData.volumeLayerMask = 1;
					cameraData.volumeTrigger = null;
					cameraData.isStopNaNEnabled = false;
					cameraData.isDitheringEnabled = false;
					cameraData.antialiasing = AntialiasingMode.None;
					cameraData.antialiasingQuality = AntialiasingQuality.High;
					cameraData.xrRendering = m_XRSystem.RefreshXrSdk();
				}
				cameraData.isHdrEnabled = baseCamera.allowHDR && universalRenderPipelineAsset.supportsHDR;
				Rect rect = baseCamera.rect;
				cameraData.pixelRect = baseCamera.pixelRect;
				cameraData.pixelWidth = baseCamera.pixelWidth;
				cameraData.pixelHeight = baseCamera.pixelHeight;
				cameraData.aspectRatio = (float)cameraData.pixelWidth / (float)cameraData.pixelHeight;
				cameraData.isDefaultViewport = !(Math.Abs(rect.x) > 0f) && !(Math.Abs(rect.y) > 0f) && !(Math.Abs(rect.width) < 1f) && !(Math.Abs(rect.height) < 1f);
				cameraData.renderScale = ((Mathf.Abs(1f - universalRenderPipelineAsset.renderScale) < 0.05f) ? 1f : universalRenderPipelineAsset.renderScale);
				cameraData.xr = m_XRSystem.emptyPass;
				XRSystem.UpdateRenderScale(cameraData.renderScale);
				SortingCriteria sortingCriteria = SortingCriteria.CommonOpaque;
				SortingCriteria sortingCriteria2 = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;
				bool hasHiddenSurfaceRemovalOnGPU = SystemInfo.hasHiddenSurfaceRemovalOnGPU;
				bool flag = (baseCamera.opaqueSortMode == OpaqueSortMode.Default && hasHiddenSurfaceRemovalOnGPU) || baseCamera.opaqueSortMode == OpaqueSortMode.NoDistanceSort;
				cameraData.defaultOpaqueSortFlags = (flag ? sortingCriteria2 : sortingCriteria);
				cameraData.captureActions = CameraCaptureBridge.GetCaptureActions(baseCamera);
			}
		}

		private static void InitializeAdditionalCameraData(Camera camera, UniversalAdditionalCameraData additionalCameraData, bool resolveFinalTarget, ref CameraData cameraData)
		{
			using (new ProfilingScope(null, Profiling.Pipeline.initializeAdditionalCameraData))
			{
				UniversalRenderPipelineAsset universalRenderPipelineAsset = asset;
				cameraData.camera = camera;
				bool flag = universalRenderPipelineAsset.supportsMainLightShadows || universalRenderPipelineAsset.supportsAdditionalLightShadows;
				cameraData.maxShadowDistance = Mathf.Min(universalRenderPipelineAsset.shadowDistance, camera.farClipPlane);
				cameraData.maxShadowDistance = ((flag && cameraData.maxShadowDistance >= camera.nearClipPlane) ? cameraData.maxShadowDistance : 0f);
				bool isSceneViewCamera = cameraData.isSceneViewCamera;
				if (isSceneViewCamera)
				{
					cameraData.renderType = CameraRenderType.Base;
					cameraData.clearDepth = true;
					cameraData.postProcessEnabled = CoreUtils.ArePostProcessesEnabled(camera);
					cameraData.requiresDepthTexture = universalRenderPipelineAsset.supportsCameraDepthTexture;
					cameraData.requiresOpaqueTexture = universalRenderPipelineAsset.supportsCameraOpaqueTexture;
					cameraData.renderer = asset.scriptableRenderer;
				}
				else if (additionalCameraData != null)
				{
					cameraData.renderType = additionalCameraData.renderType;
					cameraData.clearDepth = additionalCameraData.renderType == CameraRenderType.Base || additionalCameraData.clearDepth;
					cameraData.postProcessEnabled = additionalCameraData.renderPostProcessing;
					cameraData.maxShadowDistance = (additionalCameraData.renderShadows ? cameraData.maxShadowDistance : 0f);
					cameraData.requiresDepthTexture = additionalCameraData.requiresDepthTexture;
					cameraData.requiresOpaqueTexture = additionalCameraData.requiresColorTexture;
					cameraData.renderer = additionalCameraData.scriptableRenderer;
				}
				else
				{
					cameraData.renderType = CameraRenderType.Base;
					cameraData.clearDepth = true;
					cameraData.postProcessEnabled = false;
					cameraData.requiresDepthTexture = universalRenderPipelineAsset.supportsCameraDepthTexture;
					cameraData.requiresOpaqueTexture = universalRenderPipelineAsset.supportsCameraOpaqueTexture;
					cameraData.renderer = asset.scriptableRenderer;
				}
				cameraData.postProcessEnabled &= SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;
				cameraData.requiresDepthTexture |= isSceneViewCamera || CheckPostProcessForDepth(in cameraData);
				cameraData.resolveFinalTarget = resolveFinalTarget;
				bool num = cameraData.renderType == CameraRenderType.Overlay;
				if (num)
				{
					cameraData.requiresDepthTexture = false;
					cameraData.requiresOpaqueTexture = false;
				}
				Matrix4x4 projectionMatrix = camera.projectionMatrix;
				if (num && !camera.orthographic && cameraData.pixelRect != camera.pixelRect)
				{
					float m = camera.projectionMatrix.m00 * camera.aspect / cameraData.aspectRatio;
					projectionMatrix.m00 = m;
				}
				cameraData.SetViewAndProjectionMatrix(camera.worldToCameraMatrix, projectionMatrix);
			}
		}

		private static void InitializeRenderingData(UniversalRenderPipelineAsset settings, ref CameraData cameraData, ref CullingResults cullResults, bool anyPostProcessingEnabled, out RenderingData renderingData)
		{
			using (new ProfilingScope(null, Profiling.Pipeline.initializeRenderingData))
			{
				NativeArray<VisibleLight> visibleLights = cullResults.visibleLights;
				int mainLightIndex = GetMainLightIndex(settings, visibleLights);
				bool mainLightCastShadows = false;
				bool flag = false;
				if (cameraData.maxShadowDistance > 0f)
				{
					mainLightCastShadows = mainLightIndex != -1 && visibleLights[mainLightIndex].light != null && visibleLights[mainLightIndex].light.shadows != LightShadows.None;
					if (settings.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
					{
						for (int i = 0; i < visibleLights.Length; i++)
						{
							if (i != mainLightIndex)
							{
								Light light = visibleLights[i].light;
								if (visibleLights[i].lightType == LightType.Spot && light != null && light.shadows != 0)
								{
									flag = true;
									break;
								}
							}
						}
					}
				}
				renderingData.cullResults = cullResults;
				renderingData.cameraData = cameraData;
				InitializeLightData(settings, visibleLights, mainLightIndex, out renderingData.lightData);
				InitializeShadowData(settings, visibleLights, mainLightCastShadows, flag && !renderingData.lightData.shadeAdditionalLightsPerVertex, out renderingData.shadowData);
				InitializePostProcessingData(settings, out renderingData.postProcessingData);
				renderingData.supportsDynamicBatching = settings.supportsDynamicBatching;
				renderingData.perObjectData = GetPerObjectLightFlags(renderingData.lightData.additionalLightsCount);
				renderingData.postProcessingEnabled = anyPostProcessingEnabled;
			}
		}

		private static void InitializeShadowData(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, bool mainLightCastShadows, bool additionalLightsCastShadows, out ShadowData shadowData)
		{
			using (new ProfilingScope(null, Profiling.Pipeline.initializeShadowData))
			{
				m_ShadowBiasData.Clear();
				for (int i = 0; i < visibleLights.Length; i++)
				{
					Light light = visibleLights[i].light;
					UniversalAdditionalLightData component = null;
					if (light != null)
					{
						light.gameObject.TryGetComponent<UniversalAdditionalLightData>(out component);
					}
					if ((bool)component && !component.usePipelineSettings)
					{
						m_ShadowBiasData.Add(new Vector4(light.shadowBias, light.shadowNormalBias, 0f, 0f));
					}
					else
					{
						m_ShadowBiasData.Add(new Vector4(settings.shadowDepthBias, settings.shadowNormalBias, 0f, 0f));
					}
				}
				shadowData.bias = m_ShadowBiasData;
				shadowData.supportsMainLightShadows = SystemInfo.supportsShadows && settings.supportsMainLightShadows && mainLightCastShadows;
				shadowData.requiresScreenSpaceShadowResolve = false;
				shadowData.mainLightShadowCascadesCount = settings.shadowCascadeCount;
				shadowData.mainLightShadowmapWidth = settings.mainLightShadowmapResolution;
				shadowData.mainLightShadowmapHeight = settings.mainLightShadowmapResolution;
				switch (shadowData.mainLightShadowCascadesCount)
				{
				case 1:
					shadowData.mainLightShadowCascadesSplit = new Vector3(1f, 0f, 0f);
					break;
				case 2:
					shadowData.mainLightShadowCascadesSplit = new Vector3(settings.cascade2Split, 1f, 0f);
					break;
				case 3:
					shadowData.mainLightShadowCascadesSplit = new Vector3(settings.cascade3Split.x, settings.cascade3Split.y, 0f);
					break;
				default:
					shadowData.mainLightShadowCascadesSplit = settings.cascade4Split;
					break;
				}
				shadowData.supportsAdditionalLightShadows = SystemInfo.supportsShadows && settings.supportsAdditionalLightShadows && additionalLightsCastShadows;
				shadowData.additionalLightsShadowmapWidth = (shadowData.additionalLightsShadowmapHeight = settings.additionalLightsShadowmapResolution);
				shadowData.supportsSoftShadows = settings.supportsSoftShadows && (shadowData.supportsMainLightShadows || shadowData.supportsAdditionalLightShadows);
				shadowData.shadowmapDepthBufferBits = 16;
			}
		}

		private static void InitializePostProcessingData(UniversalRenderPipelineAsset settings, out PostProcessingData postProcessingData)
		{
			postProcessingData.gradingMode = (settings.supportsHDR ? settings.colorGradingMode : ColorGradingMode.LowDynamicRange);
			postProcessingData.lutSize = settings.colorGradingLutSize;
		}

		private static void InitializeLightData(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, int mainLightIndex, out LightData lightData)
		{
			using (new ProfilingScope(null, Profiling.Pipeline.initializeLightData))
			{
				int val = maxPerObjectLights;
				int val2 = maxVisibleAdditionalLights;
				lightData.mainLightIndex = mainLightIndex;
				if (settings.additionalLightsRenderingMode != 0)
				{
					lightData.additionalLightsCount = Math.Min((mainLightIndex != -1) ? (visibleLights.Length - 1) : visibleLights.Length, val2);
					lightData.maxPerObjectAdditionalLightsCount = Math.Min(settings.maxAdditionalLightsCount, val);
				}
				else
				{
					lightData.additionalLightsCount = 0;
					lightData.maxPerObjectAdditionalLightsCount = 0;
				}
				lightData.shadeAdditionalLightsPerVertex = settings.additionalLightsRenderingMode == LightRenderingMode.PerVertex;
				lightData.visibleLights = visibleLights;
				lightData.supportsMixedLighting = settings.supportsMixedLighting;
			}
		}

		private static PerObjectData GetPerObjectLightFlags(int additionalLightsCount)
		{
			using (new ProfilingScope(null, Profiling.Pipeline.getPerObjectLightFlags))
			{
				PerObjectData perObjectData = PerObjectData.LightProbe | PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightData | PerObjectData.OcclusionProbe | PerObjectData.ShadowMask;
				if (additionalLightsCount > 0)
				{
					perObjectData |= PerObjectData.LightData;
					if (!RenderingUtils.useStructuredBuffer)
					{
						perObjectData |= PerObjectData.LightIndices;
					}
				}
				return perObjectData;
			}
		}

		private static int GetMainLightIndex(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights)
		{
			using (new ProfilingScope(null, Profiling.Pipeline.getMainLightIndex))
			{
				int length = visibleLights.Length;
				if (length == 0 || settings.mainLightRenderingMode != LightRenderingMode.PerPixel)
				{
					return -1;
				}
				Light sun = RenderSettings.sun;
				int result = -1;
				float num = 0f;
				for (int i = 0; i < length; i++)
				{
					VisibleLight visibleLight = visibleLights[i];
					Light light = visibleLight.light;
					if (light == null)
					{
						break;
					}
					if (visibleLight.lightType == LightType.Directional)
					{
						if (light == sun)
						{
							return i;
						}
						if (light.intensity > num)
						{
							num = light.intensity;
							result = i;
						}
					}
				}
				return result;
			}
		}

		private static void SetupPerFrameShaderConstants()
		{
			using (new ProfilingScope(null, Profiling.Pipeline.setupPerFrameShaderConstants))
			{
				SphericalHarmonicsL2 ambientProbe = RenderSettings.ambientProbe;
				Color color = CoreUtils.ConvertLinearToActiveColorSpace(new Color(ambientProbe[0, 0], ambientProbe[1, 0], ambientProbe[2, 0]) * RenderSettings.reflectionIntensity);
				Shader.SetGlobalVector(ShaderPropertyId.glossyEnvironmentColor, color);
				Shader.SetGlobalVector(ShaderPropertyId.ambientSkyColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientSkyColor));
				Shader.SetGlobalVector(ShaderPropertyId.ambientEquatorColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientEquatorColor));
				Shader.SetGlobalVector(ShaderPropertyId.ambientGroundColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientGroundColor));
				Shader.SetGlobalVector(ShaderPropertyId.subtractiveShadowColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));
				Shader.SetGlobalColor(ShaderPropertyId.rendererColor, Color.white);
			}
		}

		public static bool IsGameCamera(Camera camera)
		{
			if (camera == null)
			{
				throw new ArgumentNullException("camera");
			}
			if (camera.cameraType != CameraType.Game)
			{
				return camera.cameraType == CameraType.VR;
			}
			return true;
		}

		[Obsolete("Please use CameraData.xr.enabled instead.")]
		public static bool IsStereoEnabled(Camera camera)
		{
			if (camera == null)
			{
				throw new ArgumentNullException("camera");
			}
			if (IsGameCamera(camera))
			{
				return camera.stereoTargetEye == StereoTargetEyeMask.Both;
			}
			return false;
		}

		[Obsolete("Please use CameraData.xr.singlePassEnabled instead.")]
		private static bool IsMultiPassStereoEnabled(Camera camera)
		{
			if (camera == null)
			{
				throw new ArgumentNullException("camera");
			}
			return false;
		}

		private static XRDisplaySubsystem GetFirstXRDisplaySubsystem()
		{
			XRDisplaySubsystem result = null;
			SubsystemManager.GetInstances(displaySubsystemList);
			if (displaySubsystemList.Count > 0)
			{
				result = displaySubsystemList[0];
			}
			return result;
		}

		internal static bool IsRunningHololens(CameraData cameraData)
		{
			return false;
		}

		private void SortCameras(Camera[] cameras)
		{
			if (cameras.Length > 1)
			{
				Array.Sort(cameras, cameraComparison);
			}
		}

		private static RenderTextureDescriptor CreateRenderTextureDescriptor(Camera camera, float renderScale, bool isHdrEnabled, int msaaSamples, bool needsAlpha, bool requiresOpaqueTexture)
		{
			GraphicsFormat graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
			RenderTextureDescriptor renderTextureDescriptor;
			if (camera.targetTexture == null)
			{
				renderTextureDescriptor = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
				renderTextureDescriptor.width = (int)((float)renderTextureDescriptor.width * renderScale);
				renderTextureDescriptor.height = (int)((float)renderTextureDescriptor.height * renderScale);
				GraphicsFormat graphicsFormat2 = ((!needsAlpha && RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Blend)) ? GraphicsFormat.B10G11R11_UFloatPack32 : ((!RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Blend)) ? SystemInfo.GetGraphicsFormat(DefaultFormat.HDR) : GraphicsFormat.R16G16B16A16_SFloat));
				renderTextureDescriptor.graphicsFormat = (isHdrEnabled ? graphicsFormat2 : graphicsFormat);
				renderTextureDescriptor.depthBufferBits = 32;
				renderTextureDescriptor.msaaSamples = msaaSamples;
				renderTextureDescriptor.sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
			}
			else
			{
				renderTextureDescriptor = camera.targetTexture.descriptor;
				renderTextureDescriptor.width = camera.pixelWidth;
				renderTextureDescriptor.height = camera.pixelHeight;
				if (camera.cameraType == CameraType.SceneView && !isHdrEnabled)
				{
					renderTextureDescriptor.graphicsFormat = graphicsFormat;
				}
			}
			renderTextureDescriptor.enableRandomWrite = false;
			renderTextureDescriptor.bindMS = false;
			renderTextureDescriptor.useDynamicScale = camera.allowDynamicResolution;
			renderTextureDescriptor.msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(renderTextureDescriptor);
			if (!SystemInfo.supportsStoreAndResolveAction && requiresOpaqueTexture)
			{
				renderTextureDescriptor.msaaSamples = 1;
			}
			return renderTextureDescriptor;
		}

		public static void GetLightAttenuationAndSpotDirection(LightType lightType, float lightRange, Matrix4x4 lightLocalToWorldMatrix, float spotAngle, float? innerSpotAngle, out Vector4 lightAttenuation, out Vector4 lightSpotDir)
		{
			lightAttenuation = k_DefaultLightAttenuation;
			lightSpotDir = k_DefaultLightSpotDirection;
			if (lightType != LightType.Directional)
			{
				float num = lightRange * lightRange;
				float num2 = 0.64000005f * num - num;
				float num3 = 1f / num2;
				float y = (0f - num) / num2;
				float num4 = 1f / Mathf.Max(0.0001f, lightRange * lightRange);
				lightAttenuation.x = ((Application.isMobilePlatform || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch) ? num3 : num4);
				lightAttenuation.y = y;
			}
			if (lightType == LightType.Spot)
			{
				Vector4 column = lightLocalToWorldMatrix.GetColumn(2);
				lightSpotDir = new Vector4(0f - column.x, 0f - column.y, 0f - column.z, 0f);
				float num5 = Mathf.Cos((float)Math.PI / 180f * spotAngle * 0.5f);
				float num6 = ((!innerSpotAngle.HasValue) ? Mathf.Cos(2f * Mathf.Atan(Mathf.Tan(spotAngle * 0.5f * ((float)Math.PI / 180f)) * 46f / 64f) * 0.5f) : Mathf.Cos(innerSpotAngle.Value * ((float)Math.PI / 180f) * 0.5f));
				float num7 = Mathf.Max(0.001f, num6 - num5);
				float num8 = 1f / num7;
				float w = (0f - num5) * num8;
				lightAttenuation.z = num8;
				lightAttenuation.w = w;
			}
		}

		public static void InitializeLightConstants_Common(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
		{
			lightPos = k_DefaultLightPosition;
			lightColor = k_DefaultLightColor;
			lightOcclusionProbeChannel = k_DefaultLightsProbeChannel;
			lightAttenuation = k_DefaultLightAttenuation;
			lightSpotDir = k_DefaultLightSpotDirection;
			if (lightIndex >= 0)
			{
				VisibleLight visibleLight = lights[lightIndex];
				if (visibleLight.lightType == LightType.Directional)
				{
					Vector4 vector = -visibleLight.localToWorldMatrix.GetColumn(2);
					lightPos = new Vector4(vector.x, vector.y, vector.z, 0f);
				}
				else
				{
					Vector4 column = visibleLight.localToWorldMatrix.GetColumn(3);
					lightPos = new Vector4(column.x, column.y, column.z, 1f);
				}
				lightColor = visibleLight.finalColor;
				GetLightAttenuationAndSpotDirection(visibleLight.lightType, visibleLight.range, visibleLight.localToWorldMatrix, visibleLight.spotAngle, visibleLight.light?.innerSpotAngle, out lightAttenuation, out lightSpotDir);
				Light light = visibleLight.light;
				if (light != null && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed && 0 <= light.bakingOutput.occlusionMaskChannel && light.bakingOutput.occlusionMaskChannel < 4)
				{
					lightOcclusionProbeChannel[light.bakingOutput.occlusionMaskChannel] = 1f;
				}
			}
		}
	}
}
