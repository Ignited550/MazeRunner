using System;
using System.Collections.Generic;
using UnityEngine.XR;

namespace UnityEngine.Rendering.Universal
{
	internal class XRSystem
	{
		internal static class XRShaderIDs
		{
			public static readonly int _SourceTexArraySlice = Shader.PropertyToID("_SourceTexArraySlice");

			public static readonly int _SRGBRead = Shader.PropertyToID("_SRGBRead");

			public static readonly int _SRGBWrite = Shader.PropertyToID("_SRGBWrite");
		}

		internal readonly XRPass emptyPass = new XRPass();

		private List<XRPass> framePasses = new List<XRPass>();

		private static List<XRDisplaySubsystem> displayList = new List<XRDisplaySubsystem>();

		private XRDisplaySubsystem display;

		private static int msaaLevel = 1;

		private Material occlusionMeshMaterial;

		private Material mirrorViewMaterial;

		private MaterialPropertyBlock mirrorViewMaterialProperty = new MaterialPropertyBlock();

		private RenderTexture testRenderTexture;

		private const string k_XRMirrorTag = "XR Mirror View";

		private static ProfilingSampler _XRMirrorProfilingSampler = new ProfilingSampler("XR Mirror View");

		internal XRSystem()
		{
			RefreshXrSdk();
			TextureXR.maxViews = Math.Max(TextureXR.slices, GetMaxViews());
		}

		internal void InitializeXRSystemData(XRSystemData data)
		{
			if ((bool)data)
			{
				if (occlusionMeshMaterial != null)
				{
					CoreUtils.Destroy(occlusionMeshMaterial);
				}
				if (mirrorViewMaterial != null)
				{
					CoreUtils.Destroy(mirrorViewMaterial);
				}
				occlusionMeshMaterial = CoreUtils.CreateEngineMaterial(data.shaders.xrOcclusionMeshPS);
				mirrorViewMaterial = CoreUtils.CreateEngineMaterial(data.shaders.xrMirrorViewPS);
			}
		}

		private static void GetDisplaySubsystem()
		{
			SubsystemManager.GetInstances(displayList);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
		internal static void XRSystemInit()
		{
			if (!(GraphicsSettings.currentRenderPipeline == null))
			{
				GetDisplaySubsystem();
				for (int i = 0; i < displayList.Count; i++)
				{
					displayList[i].disableLegacyRenderer = true;
					displayList[i].textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
					displayList[i].sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
				}
			}
		}

		internal static void UpdateMSAALevel(int level)
		{
			if (msaaLevel != level)
			{
				level = Mathf.NextPowerOfTwo(level);
				level = Mathf.Clamp(level, 1, 8);
				GetDisplaySubsystem();
				for (int i = 0; i < displayList.Count; i++)
				{
					displayList[i].SetMSAALevel(level);
				}
				msaaLevel = level;
			}
		}

		internal static int GetMSAALevel()
		{
			return msaaLevel;
		}

		internal static void UpdateRenderScale(float renderScale)
		{
			GetDisplaySubsystem();
			for (int i = 0; i < displayList.Count; i++)
			{
				displayList[i].scaleOfAllRenderTargets = renderScale;
			}
		}

		internal int GetMaxViews()
		{
			int result = 1;
			if (display != null)
			{
				result = 2;
			}
			return result;
		}

		internal List<XRPass> SetupFrame(CameraData cameraData)
		{
			Camera camera = cameraData.camera;
			bool flag = RefreshXrSdk();
			if (display != null)
			{
				display.textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
				display.zNear = camera.nearClipPlane;
				display.zFar = camera.farClipPlane;
				display.sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
			}
			if (framePasses.Count > 0)
			{
				Debug.LogWarning("XRSystem.ReleaseFrame() was not called!");
				ReleaseFrame();
			}
			if (camera == null)
			{
				return framePasses;
			}
			bool flag2 = (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.VR) && camera.targetTexture == null && cameraData.xrRendering;
			if (flag && flag2)
			{
				if (Application.platform == RuntimePlatform.Android)
				{
					QualitySettings.vSyncCount = 1;
				}
				else
				{
					QualitySettings.vSyncCount = 0;
				}
				CreateLayoutFromXrSdk(camera, singlePassAllowed: true);
			}
			else
			{
				AddPassToFrame(emptyPass);
			}
			return framePasses;
		}

		internal void ReleaseFrame()
		{
			foreach (XRPass framePass in framePasses)
			{
				if (framePass != emptyPass)
				{
					XRPass.Release(framePass);
				}
			}
			framePasses.Clear();
			if ((bool)testRenderTexture)
			{
				RenderTexture.ReleaseTemporary(testRenderTexture);
			}
		}

		internal bool RefreshXrSdk()
		{
			GetDisplaySubsystem();
			if (displayList.Count > 0)
			{
				if (displayList.Count > 1)
				{
					throw new NotImplementedException("Only 1 XR display is supported.");
				}
				display = displayList[0];
				display.disableLegacyRenderer = true;
				TextureXR.maxViews = Math.Max(TextureXR.slices, GetMaxViews());
				return display.running;
			}
			display = null;
			return false;
		}

		internal void UpdateCameraData(ref CameraData baseCameraData, in XRPass xr)
		{
			Rect rect = baseCameraData.camera.rect;
			Rect viewport = xr.GetViewport();
			baseCameraData.pixelRect = new Rect(rect.x * viewport.width + viewport.x, rect.y * viewport.height + viewport.y, rect.width * viewport.width, rect.height * viewport.height);
			Rect pixelRect = baseCameraData.pixelRect;
			baseCameraData.pixelWidth = (int)Math.Round(pixelRect.width + pixelRect.x) - (int)Math.Round(pixelRect.x);
			baseCameraData.pixelHeight = (int)Math.Round(pixelRect.height + pixelRect.y) - (int)Math.Round(pixelRect.y);
			baseCameraData.aspectRatio = (float)baseCameraData.pixelWidth / (float)baseCameraData.pixelHeight;
			bool flag = !(Math.Abs(viewport.x) > 0f) && !(Math.Abs(viewport.y) > 0f) && !(Math.Abs(viewport.width) < (float)xr.renderTargetDesc.width) && !(Math.Abs(viewport.height) < (float)xr.renderTargetDesc.height);
			baseCameraData.isDefaultViewport &= flag;
			RenderTextureDescriptor cameraTargetDescriptor = baseCameraData.cameraTargetDescriptor;
			baseCameraData.cameraTargetDescriptor = xr.renderTargetDesc;
			if (baseCameraData.isHdrEnabled)
			{
				baseCameraData.cameraTargetDescriptor.graphicsFormat = cameraTargetDescriptor.graphicsFormat;
			}
			baseCameraData.cameraTargetDescriptor.msaaSamples = cameraTargetDescriptor.msaaSamples;
			baseCameraData.cameraTargetDescriptor.width = baseCameraData.pixelWidth;
			baseCameraData.cameraTargetDescriptor.height = baseCameraData.pixelHeight;
		}

		internal void UpdateFromCamera(ref XRPass xrPass, CameraData cameraData)
		{
			bool flag = cameraData.camera.cameraType == CameraType.Game || cameraData.camera.cameraType == CameraType.VR;
			if (XRGraphicsAutomatedTests.enabled && XRGraphicsAutomatedTests.running && flag)
			{
				Matrix4x4 projectionMatrix = cameraData.camera.projectionMatrix;
				Matrix4x4 worldToCameraMatrix = cameraData.camera.worldToCameraMatrix;
				Rect vp = new Rect(0f, 0f, testRenderTexture.width, testRenderTexture.height);
				int textureArraySlice = -1;
				xrPass.UpdateView(1, projectionMatrix, worldToCameraMatrix, vp, textureArraySlice);
				cameraData.camera.TryGetCullingParameters(stereoAware: false, out var cullingParameters);
				cullingParameters.stereoProjectionMatrix = cameraData.camera.projectionMatrix;
				cullingParameters.stereoViewMatrix = cameraData.camera.worldToCameraMatrix;
				cullingParameters.cullingOptions &= ~CullingOptions.Stereo;
				xrPass.UpdateCullingParams(0, cullingParameters);
			}
			else
			{
				if (!xrPass.enabled || display == null)
				{
					return;
				}
				display.GetRenderPass(xrPass.multipassId, out var renderPass);
				display.GetCullingParameters(cameraData.camera, renderPass.cullingPassIndex, out var scriptableCullingParameters);
				scriptableCullingParameters.cullingOptions &= ~CullingOptions.Stereo;
				xrPass.UpdateCullingParams(renderPass.cullingPassIndex, scriptableCullingParameters);
				if (xrPass.singlePassEnabled)
				{
					for (int i = 0; i < renderPass.GetRenderParameterCount(); i++)
					{
						renderPass.GetRenderParameter(cameraData.camera, i, out var renderParameter);
						xrPass.UpdateView(i, renderPass, renderParameter);
					}
				}
				else
				{
					renderPass.GetRenderParameter(cameraData.camera, 0, out var renderParameter2);
					xrPass.UpdateView(0, renderPass, renderParameter2);
				}
			}
		}

		private void CreateLayoutFromXrSdk(Camera camera, bool singlePassAllowed)
		{
			for (int i = 0; i < display.GetRenderPassCount(); i++)
			{
				display.GetRenderPass(i, out var renderPass2);
				display.GetCullingParameters(camera, renderPass2.cullingPassIndex, out var scriptableCullingParameters);
				scriptableCullingParameters.cullingOptions &= ~CullingOptions.Stereo;
				if (singlePassAllowed && CanUseSinglePass(renderPass2))
				{
					XRPass xRPass = XRPass.Create(renderPass2, framePasses.Count, scriptableCullingParameters, occlusionMeshMaterial);
					for (int j = 0; j < renderPass2.GetRenderParameterCount(); j++)
					{
						renderPass2.GetRenderParameter(camera, j, out var renderParameter);
						xRPass.AddView(renderPass2, renderParameter);
					}
					AddPassToFrame(xRPass);
				}
				else
				{
					for (int k = 0; k < renderPass2.GetRenderParameterCount(); k++)
					{
						renderPass2.GetRenderParameter(camera, k, out var renderParameter2);
						XRPass xRPass2 = XRPass.Create(renderPass2, framePasses.Count, scriptableCullingParameters, occlusionMeshMaterial);
						xRPass2.AddView(renderPass2, renderParameter2);
						AddPassToFrame(xRPass2);
					}
				}
			}
			bool CanUseSinglePass(XRDisplaySubsystem.XRRenderPass renderPass)
			{
				if (renderPass.renderTargetDesc.dimension != TextureDimension.Tex2DArray)
				{
					return false;
				}
				if (renderPass.GetRenderParameterCount() != 2 || renderPass.renderTargetDesc.volumeDepth != 2)
				{
					return false;
				}
				renderPass.GetRenderParameter(camera, 0, out var renderParameter3);
				renderPass.GetRenderParameter(camera, 1, out var renderParameter4);
				if (renderParameter3.textureArraySlice != 0 || renderParameter4.textureArraySlice != 1)
				{
					return false;
				}
				if (renderParameter3.viewport != renderParameter4.viewport)
				{
					return false;
				}
				return true;
			}
		}

		internal void Dispose()
		{
			CoreUtils.Destroy(occlusionMeshMaterial);
			CoreUtils.Destroy(mirrorViewMaterial);
		}

		internal void AddPassToFrame(XRPass xrPass)
		{
			xrPass.UpdateOcclusionMesh();
			framePasses.Add(xrPass);
		}

		internal void RenderMirrorView(CommandBuffer cmd, Camera camera)
		{
			if (Application.platform == RuntimePlatform.Android || display == null || !display.running || !mirrorViewMaterial)
			{
				return;
			}
			using (new ProfilingScope(cmd, _XRMirrorProfilingSampler))
			{
				cmd.SetRenderTarget((camera.targetTexture != null) ? ((RenderTargetIdentifier)camera.targetTexture) : new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
				bool flag = camera.targetTexture != null || camera.cameraType == CameraType.SceneView || camera.cameraType == CameraType.Preview;
				int preferredMirrorBlitMode = display.GetPreferredMirrorBlitMode();
				if (display.GetMirrorViewBlitDesc(null, out var outDesc, preferredMirrorBlitMode))
				{
					if (outDesc.nativeBlitAvailable)
					{
						display.AddGraphicsThreadMirrorViewBlit(cmd, outDesc.nativeBlitInvalidStates, preferredMirrorBlitMode);
						return;
					}
					for (int i = 0; i < outDesc.blitParamsCount; i++)
					{
						outDesc.GetBlitParameter(i, out var blitParameter);
						Vector4 value = (flag ? new Vector4(blitParameter.srcRect.width, 0f - blitParameter.srcRect.height, blitParameter.srcRect.x, blitParameter.srcRect.height + blitParameter.srcRect.y) : new Vector4(blitParameter.srcRect.width, blitParameter.srcRect.height, blitParameter.srcRect.x, blitParameter.srcRect.y));
						Vector4 value2 = new Vector4(blitParameter.destRect.width, blitParameter.destRect.height, blitParameter.destRect.x, blitParameter.destRect.y);
						mirrorViewMaterialProperty.SetInt(XRShaderIDs._SRGBRead, (!blitParameter.srcTex.sRGB) ? 1 : 0);
						mirrorViewMaterialProperty.SetInt(XRShaderIDs._SRGBWrite, (QualitySettings.activeColorSpace != ColorSpace.Linear) ? 1 : 0);
						mirrorViewMaterialProperty.SetTexture(ShaderPropertyId.sourceTex, blitParameter.srcTex);
						mirrorViewMaterialProperty.SetVector(ShaderPropertyId.scaleBias, value);
						mirrorViewMaterialProperty.SetVector(ShaderPropertyId.scaleBiasRt, value2);
						mirrorViewMaterialProperty.SetInt(XRShaderIDs._SourceTexArraySlice, blitParameter.srcTexArraySlice);
						int shaderPass = ((blitParameter.srcTex.dimension == TextureDimension.Tex2DArray) ? 1 : 0);
						cmd.DrawProcedural(Matrix4x4.identity, mirrorViewMaterial, shaderPass, MeshTopology.Quads, 4, 1, mirrorViewMaterialProperty);
					}
				}
				else
				{
					cmd.ClearRenderTarget(clearDepth: true, clearColor: true, Color.black);
				}
			}
		}
	}
}
