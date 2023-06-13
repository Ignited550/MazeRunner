using System;
using System.Collections.Generic;
using UnityEngine.XR;

namespace UnityEngine.Rendering.Universal
{
	internal class XRPass
	{
		internal delegate void CustomMirrorView(XRPass pass, CommandBuffer cmd, RenderTexture rt, Rect viewport);

		internal List<XRView> views = new List<XRView>(2);

		private static RenderTargetIdentifier invalidRT = -1;

		private Material occlusionMeshMaterial;

		private Mesh occlusionMeshCombined;

		private int occlusionMeshCombinedHashCode;

		private CustomMirrorView customMirrorView;

		private const string k_XRCustomMirrorTag = "XR Custom Mirror View";

		private static ProfilingSampler _XRCustomMirrorProfilingSampler = new ProfilingSampler("XR Custom Mirror View");

		private const string k_XROcclusionTag = "XR Occlusion Mesh";

		private static ProfilingSampler _XROcclusionProfilingSampler = new ProfilingSampler("XR Occlusion Mesh");

		private Vector4[] stereoEyeIndices = new Vector4[2]
		{
			Vector4.zero,
			Vector4.one
		};

		private Matrix4x4[] stereoProjectionMatrix = new Matrix4x4[2];

		private Matrix4x4[] stereoViewMatrix = new Matrix4x4[2];

		private Matrix4x4[] stereoCameraProjectionMatrix = new Matrix4x4[2];

		internal bool enabled => views.Count > 0;

		internal bool xrSdkEnabled { get; private set; }

		internal bool copyDepth { get; private set; }

		internal int multipassId { get; private set; }

		internal int cullingPassId { get; private set; }

		internal RenderTargetIdentifier renderTarget { get; private set; }

		internal RenderTextureDescriptor renderTargetDesc { get; private set; }

		internal bool renderTargetValid => renderTarget != invalidRT;

		internal bool renderTargetIsRenderTexture { get; private set; }

		internal ScriptableCullingParameters cullingParams { get; private set; }

		internal int viewCount => views.Count;

		internal bool singlePassEnabled => viewCount > 1;

		internal bool isOcclusionMeshSupported
		{
			get
			{
				if (enabled && xrSdkEnabled)
				{
					return occlusionMeshMaterial != null;
				}
				return false;
			}
		}

		internal bool hasValidOcclusionMesh
		{
			get
			{
				if (isOcclusionMeshSupported)
				{
					if (singlePassEnabled)
					{
						return occlusionMeshCombined != null;
					}
					return views[0].occlusionMesh != null;
				}
				return false;
			}
		}

		internal Matrix4x4 GetProjMatrix(int viewIndex = 0)
		{
			return views[viewIndex].projMatrix;
		}

		internal Matrix4x4 GetViewMatrix(int viewIndex = 0)
		{
			return views[viewIndex].viewMatrix;
		}

		internal int GetTextureArraySlice(int viewIndex = 0)
		{
			return views[viewIndex].textureArraySlice;
		}

		internal Rect GetViewport(int viewIndex = 0)
		{
			return views[viewIndex].viewport;
		}

		internal void SetCustomMirrorView(CustomMirrorView callback)
		{
			customMirrorView = callback;
		}

		internal static XRPass Create(XRPassCreateInfo createInfo)
		{
			XRPass xRPass = GenericPool<XRPass>.Get();
			xRPass.multipassId = createInfo.multipassId;
			xRPass.cullingPassId = createInfo.cullingPassId;
			xRPass.cullingParams = createInfo.cullingParameters;
			xRPass.customMirrorView = createInfo.customMirrorView;
			xRPass.views.Clear();
			if (createInfo.renderTarget != null)
			{
				xRPass.renderTarget = new RenderTargetIdentifier(createInfo.renderTarget, 0, CubemapFace.Unknown, -1);
				xRPass.renderTargetDesc = createInfo.renderTarget.descriptor;
				xRPass.renderTargetIsRenderTexture = createInfo.renderTargetIsRenderTexture;
			}
			else
			{
				xRPass.renderTarget = invalidRT;
				xRPass.renderTargetDesc = createInfo.renderTargetDesc;
				xRPass.renderTargetIsRenderTexture = createInfo.renderTargetIsRenderTexture;
			}
			xRPass.occlusionMeshMaterial = null;
			xRPass.xrSdkEnabled = false;
			xRPass.copyDepth = false;
			return xRPass;
		}

		internal void UpdateView(int viewId, XRDisplaySubsystem.XRRenderPass xrSdkRenderPass, XRDisplaySubsystem.XRRenderParameter xrSdkRenderParameter)
		{
			if (viewId >= views.Count)
			{
				throw new NotImplementedException("Invalid XR setup to update, trying to update non-existing xr view.");
			}
			views[viewId] = new XRView(xrSdkRenderPass, xrSdkRenderParameter);
		}

		internal void UpdateView(int viewId, Matrix4x4 proj, Matrix4x4 view, Rect vp, int textureArraySlice = -1)
		{
			if (viewId >= views.Count)
			{
				throw new NotImplementedException("Invalid XR setup to update, trying to update non-existing xr view.");
			}
			views[viewId] = new XRView(proj, view, vp, textureArraySlice);
		}

		internal void UpdateCullingParams(int cullingPassId, ScriptableCullingParameters cullingParams)
		{
			this.cullingPassId = cullingPassId;
			this.cullingParams = cullingParams;
		}

		internal void AddView(Matrix4x4 proj, Matrix4x4 view, Rect vp, int textureArraySlice = -1)
		{
			AddViewInternal(new XRView(proj, view, vp, textureArraySlice));
		}

		internal static XRPass Create(XRDisplaySubsystem.XRRenderPass xrRenderPass, int multipassId, ScriptableCullingParameters cullingParameters, Material occlusionMeshMaterial)
		{
			XRPass xRPass = GenericPool<XRPass>.Get();
			xRPass.multipassId = multipassId;
			xRPass.cullingPassId = xrRenderPass.cullingPassIndex;
			xRPass.cullingParams = cullingParameters;
			xRPass.views.Clear();
			xRPass.renderTarget = new RenderTargetIdentifier(xrRenderPass.renderTarget, 0, CubemapFace.Unknown, -1);
			RenderTextureDescriptor renderTextureDescriptor = xrRenderPass.renderTargetDesc;
			xRPass.renderTargetDesc = new RenderTextureDescriptor(renderTextureDescriptor.width, renderTextureDescriptor.height, renderTextureDescriptor.colorFormat, renderTextureDescriptor.depthBufferBits, renderTextureDescriptor.mipCount)
			{
				dimension = xrRenderPass.renderTargetDesc.dimension,
				volumeDepth = xrRenderPass.renderTargetDesc.volumeDepth,
				vrUsage = xrRenderPass.renderTargetDesc.vrUsage,
				sRGB = xrRenderPass.renderTargetDesc.sRGB
			};
			xRPass.renderTargetIsRenderTexture = false;
			xRPass.occlusionMeshMaterial = occlusionMeshMaterial;
			xRPass.xrSdkEnabled = true;
			xRPass.copyDepth = xrRenderPass.shouldFillOutDepth;
			xRPass.customMirrorView = null;
			return xRPass;
		}

		internal void AddView(XRDisplaySubsystem.XRRenderPass xrSdkRenderPass, XRDisplaySubsystem.XRRenderParameter xrSdkRenderParameter)
		{
			AddViewInternal(new XRView(xrSdkRenderPass, xrSdkRenderParameter));
		}

		internal static void Release(XRPass xrPass)
		{
			GenericPool<XRPass>.Release(xrPass);
		}

		internal void AddViewInternal(XRView xrView)
		{
			int num = Math.Min(TextureXR.slices, 2);
			if (views.Count < num)
			{
				views.Add(xrView);
				return;
			}
			throw new NotImplementedException($"Invalid XR setup for single-pass, trying to add too many views! Max supported: {num}");
		}

		internal void UpdateOcclusionMesh()
		{
			if (isOcclusionMeshSupported && singlePassEnabled && TryGetOcclusionMeshCombinedHashCode(out var hashCode))
			{
				if (occlusionMeshCombined == null || hashCode != occlusionMeshCombinedHashCode)
				{
					CreateOcclusionMeshCombined();
					occlusionMeshCombinedHashCode = hashCode;
				}
			}
			else
			{
				occlusionMeshCombined = null;
				occlusionMeshCombinedHashCode = 0;
			}
		}

		private bool TryGetOcclusionMeshCombinedHashCode(out int hashCode)
		{
			hashCode = 17;
			for (int i = 0; i < viewCount; i++)
			{
				if (views[i].occlusionMesh != null)
				{
					hashCode = hashCode * 23 + views[i].occlusionMesh.GetHashCode();
					continue;
				}
				hashCode = 0;
				return false;
			}
			return true;
		}

		private void CreateOcclusionMeshCombined()
		{
			occlusionMeshCombined = new Mesh();
			occlusionMeshCombined.indexFormat = IndexFormat.UInt16;
			int num = 0;
			uint num2 = 0u;
			for (int i = 0; i < viewCount; i++)
			{
				Mesh occlusionMesh = views[i].occlusionMesh;
				num += occlusionMesh.vertexCount;
				num2 += occlusionMesh.GetIndexCount(0);
			}
			Vector3[] array = new Vector3[num];
			ushort[] array2 = new ushort[num2];
			int num3 = 0;
			int num4 = 0;
			for (int j = 0; j < viewCount; j++)
			{
				Mesh occlusionMesh2 = views[j].occlusionMesh;
				int[] indices = occlusionMesh2.GetIndices(0);
				occlusionMesh2.vertices.CopyTo(array, num3);
				for (int k = 0; k < occlusionMesh2.vertices.Length; k++)
				{
					array[num3 + k].z = j;
				}
				for (int l = 0; l < indices.Length; l++)
				{
					int num5 = num3 + indices[l];
					array2[num4 + l] = (ushort)num5;
				}
				num3 += occlusionMesh2.vertexCount;
				num4 += indices.Length;
			}
			occlusionMeshCombined.vertices = array;
			occlusionMeshCombined.SetIndices(array2, MeshTopology.Triangles, 0);
		}

		internal void StartSinglePass(CommandBuffer cmd)
		{
			if (enabled && singlePassEnabled)
			{
				if (viewCount > TextureXR.slices)
				{
					throw new NotImplementedException($"Invalid XR setup for single-pass, trying to render too many views! Max supported: {TextureXR.slices}");
				}
				if (SystemInfo.supportsMultiview)
				{
					cmd.EnableShaderKeyword("STEREO_MULTIVIEW_ON");
					cmd.SetGlobalVectorArray("unity_StereoEyeIndices", stereoEyeIndices);
				}
				else
				{
					cmd.EnableShaderKeyword("STEREO_INSTANCING_ON");
					cmd.SetInstanceMultiplier((uint)viewCount);
				}
			}
		}

		internal void StopSinglePass(CommandBuffer cmd)
		{
			if (enabled && singlePassEnabled)
			{
				if (SystemInfo.supportsMultiview)
				{
					cmd.DisableShaderKeyword("STEREO_MULTIVIEW_ON");
					return;
				}
				cmd.DisableShaderKeyword("STEREO_INSTANCING_ON");
				cmd.SetInstanceMultiplier(1u);
			}
		}

		internal void EndCamera(CommandBuffer cmd, CameraData cameraData)
		{
			if (!enabled)
			{
				return;
			}
			StopSinglePass(cmd);
			if (customMirrorView == null)
			{
				return;
			}
			using (new ProfilingScope(cmd, _XRCustomMirrorProfilingSampler))
			{
				customMirrorView(this, cmd, cameraData.targetTexture, cameraData.pixelRect);
			}
		}

		internal void RenderOcclusionMesh(CommandBuffer cmd)
		{
			if (!isOcclusionMeshSupported)
			{
				return;
			}
			using (new ProfilingScope(cmd, _XROcclusionProfilingSampler))
			{
				if (singlePassEnabled)
				{
					if (occlusionMeshCombined != null && SystemInfo.supportsRenderTargetArrayIndexFromVertexShader)
					{
						StopSinglePass(cmd);
						cmd.EnableShaderKeyword("XR_OCCLUSION_MESH_COMBINED");
						cmd.DrawMesh(occlusionMeshCombined, Matrix4x4.identity, occlusionMeshMaterial);
						cmd.DisableShaderKeyword("XR_OCCLUSION_MESH_COMBINED");
						StartSinglePass(cmd);
					}
				}
				else if (views[0].occlusionMesh != null)
				{
					cmd.DrawMesh(views[0].occlusionMesh, Matrix4x4.identity, occlusionMeshMaterial);
				}
			}
		}

		internal void UpdateGPUViewAndProjectionMatrices(CommandBuffer cmd, ref CameraData cameraData, bool isRenderToTexture)
		{
			Matrix4x4 gPUProjectionMatrix = GL.GetGPUProjectionMatrix(cameraData.xr.GetProjMatrix(), isRenderToTexture);
			RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.xr.GetViewMatrix(), gPUProjectionMatrix, setInverseMatrices: true);
			if (cameraData.xr.singlePassEnabled)
			{
				for (int i = 0; i < 2; i++)
				{
					stereoCameraProjectionMatrix[i] = cameraData.xr.GetProjMatrix(i);
					stereoViewMatrix[i] = cameraData.xr.GetViewMatrix(i);
					stereoProjectionMatrix[i] = GL.GetGPUProjectionMatrix(stereoCameraProjectionMatrix[i], isRenderToTexture);
				}
				RenderingUtils.SetStereoViewAndProjectionMatrices(cmd, stereoViewMatrix, stereoProjectionMatrix, stereoCameraProjectionMatrix, setInverseMatrices: true);
			}
		}
	}
}
