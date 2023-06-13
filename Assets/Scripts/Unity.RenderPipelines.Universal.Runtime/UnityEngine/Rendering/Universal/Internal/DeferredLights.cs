using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
	internal class DeferredLights
	{
		internal static class ShaderConstants
		{
			public static readonly int _LitStencilRef = Shader.PropertyToID("_LitStencilRef");

			public static readonly int _LitStencilReadMask = Shader.PropertyToID("_LitStencilReadMask");

			public static readonly int _LitStencilWriteMask = Shader.PropertyToID("_LitStencilWriteMask");

			public static readonly int _SimpleLitStencilRef = Shader.PropertyToID("_SimpleLitStencilRef");

			public static readonly int _SimpleLitStencilReadMask = Shader.PropertyToID("_SimpleLitStencilReadMask");

			public static readonly int _SimpleLitStencilWriteMask = Shader.PropertyToID("_SimpleLitStencilWriteMask");

			public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");

			public static readonly int _StencilReadMask = Shader.PropertyToID("_StencilReadMask");

			public static readonly int _StencilWriteMask = Shader.PropertyToID("_StencilWriteMask");

			public static readonly int _LitPunctualStencilRef = Shader.PropertyToID("_LitPunctualStencilRef");

			public static readonly int _LitPunctualStencilReadMask = Shader.PropertyToID("_LitPunctualStencilReadMask");

			public static readonly int _LitPunctualStencilWriteMask = Shader.PropertyToID("_LitPunctualStencilWriteMask");

			public static readonly int _SimpleLitPunctualStencilRef = Shader.PropertyToID("_SimpleLitPunctualStencilRef");

			public static readonly int _SimpleLitPunctualStencilReadMask = Shader.PropertyToID("_SimpleLitPunctualStencilReadMask");

			public static readonly int _SimpleLitPunctualStencilWriteMask = Shader.PropertyToID("_SimpleLitPunctualStencilWriteMask");

			public static readonly int _LitDirStencilRef = Shader.PropertyToID("_LitDirStencilRef");

			public static readonly int _LitDirStencilReadMask = Shader.PropertyToID("_LitDirStencilReadMask");

			public static readonly int _LitDirStencilWriteMask = Shader.PropertyToID("_LitDirStencilWriteMask");

			public static readonly int _SimpleLitDirStencilRef = Shader.PropertyToID("_SimpleLitDirStencilRef");

			public static readonly int _SimpleLitDirStencilReadMask = Shader.PropertyToID("_SimpleLitDirStencilReadMask");

			public static readonly int _SimpleLitDirStencilWriteMask = Shader.PropertyToID("_SimpleLitDirStencilWriteMask");

			public static readonly int _ClearStencilRef = Shader.PropertyToID("_ClearStencilRef");

			public static readonly int _ClearStencilReadMask = Shader.PropertyToID("_ClearStencilReadMask");

			public static readonly int _ClearStencilWriteMask = Shader.PropertyToID("_ClearStencilWriteMask");

			public static readonly int UDepthRanges = Shader.PropertyToID("UDepthRanges");

			public static readonly int _DepthRanges = Shader.PropertyToID("_DepthRanges");

			public static readonly int _DownsamplingWidth = Shader.PropertyToID("_DownsamplingWidth");

			public static readonly int _DownsamplingHeight = Shader.PropertyToID("_DownsamplingHeight");

			public static readonly int _SourceShiftX = Shader.PropertyToID("_SourceShiftX");

			public static readonly int _SourceShiftY = Shader.PropertyToID("_SourceShiftY");

			public static readonly int _TileShiftX = Shader.PropertyToID("_TileShiftX");

			public static readonly int _TileShiftY = Shader.PropertyToID("_TileShiftY");

			public static readonly int _tileXCount = Shader.PropertyToID("_tileXCount");

			public static readonly int _DepthRangeOffset = Shader.PropertyToID("_DepthRangeOffset");

			public static readonly int _BitmaskTex = Shader.PropertyToID("_BitmaskTex");

			public static readonly int UTileList = Shader.PropertyToID("UTileList");

			public static readonly int _TileList = Shader.PropertyToID("_TileList");

			public static readonly int UPunctualLightBuffer = Shader.PropertyToID("UPunctualLightBuffer");

			public static readonly int _PunctualLightBuffer = Shader.PropertyToID("_PunctualLightBuffer");

			public static readonly int URelLightList = Shader.PropertyToID("URelLightList");

			public static readonly int _RelLightList = Shader.PropertyToID("_RelLightList");

			public static readonly int _TilePixelWidth = Shader.PropertyToID("_TilePixelWidth");

			public static readonly int _TilePixelHeight = Shader.PropertyToID("_TilePixelHeight");

			public static readonly int _InstanceOffset = Shader.PropertyToID("_InstanceOffset");

			public static readonly int _DepthTex = Shader.PropertyToID("_DepthTex");

			public static readonly int _DepthTexSize = Shader.PropertyToID("_DepthTexSize");

			public static readonly int _ScreenSize = Shader.PropertyToID("_ScreenSize");

			public static readonly int _ScreenToWorld = Shader.PropertyToID("_ScreenToWorld");

			public static readonly int _unproject0 = Shader.PropertyToID("_unproject0");

			public static readonly int _unproject1 = Shader.PropertyToID("_unproject1");

			public static int _MainLightPosition = Shader.PropertyToID("_MainLightPosition");

			public static int _MainLightColor = Shader.PropertyToID("_MainLightColor");

			public static int _SpotLightScale = Shader.PropertyToID("_SpotLightScale");

			public static int _SpotLightBias = Shader.PropertyToID("_SpotLightBias");

			public static int _SpotLightGuard = Shader.PropertyToID("_SpotLightGuard");

			public static int _LightPosWS = Shader.PropertyToID("_LightPosWS");

			public static int _LightColor = Shader.PropertyToID("_LightColor");

			public static int _LightAttenuation = Shader.PropertyToID("_LightAttenuation");

			public static int _LightOcclusionProbInfo = Shader.PropertyToID("_LightOcclusionProbInfo");

			public static int _LightDirection = Shader.PropertyToID("_LightDirection");

			public static int _LightFlags = Shader.PropertyToID("_LightFlags");

			public static int _ShadowLightIndex = Shader.PropertyToID("_ShadowLightIndex");
		}

		private struct CullLightsJob : IJob
		{
			public DeferredTiler tiler;

			[ReadOnly]
			[NativeDisableContainerSafetyRestriction]
			public NativeArray<DeferredTiler.PrePunctualLight> prePunctualLights;

			[ReadOnly]
			[NativeDisableContainerSafetyRestriction]
			public NativeArray<ushort> coarseTiles;

			[ReadOnly]
			[NativeDisableContainerSafetyRestriction]
			public NativeArray<uint> coarseTileHeaders;

			public int coarseHeaderOffset;

			public int istart;

			public int iend;

			public int jstart;

			public int jend;

			public void Execute()
			{
				int lightStartIndex = (int)coarseTileHeaders[coarseHeaderOffset];
				int lightCount = (int)coarseTileHeaders[coarseHeaderOffset + 1];
				if (tiler.TilerLevel != 0)
				{
					tiler.CullIntermediateLights(ref prePunctualLights, ref coarseTiles, lightStartIndex, lightCount, istart, iend, jstart, jend);
				}
				else
				{
					tiler.CullFinalLights(ref prePunctualLights, ref coarseTiles, lightStartIndex, lightCount, istart, iend, jstart, jend);
				}
			}
		}

		private struct DrawCall
		{
			public ComputeBuffer tileList;

			public ComputeBuffer punctualLightBuffer;

			public ComputeBuffer relLightList;

			public int tileListSize;

			public int punctualLightBufferSize;

			public int relLightListSize;

			public int instanceOffset;

			public int instanceCount;
		}

		internal enum GBufferHandles
		{
			DepthAsColor = 0,
			Albedo = 1,
			SpecularMetallic = 2,
			NormalSmoothness = 3,
			Lighting = 4,
			ShadowMask = 5,
			Count = 6
		}

		private static readonly string k_SetupLights = "SetupLights";

		private static readonly string k_DeferredPass = "Deferred Pass";

		private static readonly string k_TileDepthInfo = "Tile Depth Info";

		private static readonly string k_DeferredTiledPass = "Deferred Shading (Tile-Based)";

		private static readonly string k_DeferredStencilPass = "Deferred Shading (Stencil)";

		private static readonly string k_DeferredFogPass = "Deferred Fog";

		private static readonly string k_ClearStencilPartial = "Clear Stencil Partial";

		private static readonly string k_SetupLightConstants = "Setup Light Constants";

		private static readonly float kStencilShapeGuard = 1.06067f;

		private static readonly ProfilingSampler m_ProfilingSetupLights = new ProfilingSampler(k_SetupLights);

		private static readonly ProfilingSampler m_ProfilingDeferredPass = new ProfilingSampler(k_DeferredPass);

		private static readonly ProfilingSampler m_ProfilingTileDepthInfo = new ProfilingSampler(k_TileDepthInfo);

		private static readonly ProfilingSampler m_ProfilingSetupLightConstants = new ProfilingSampler(k_SetupLightConstants);

		private int m_CachedRenderWidth;

		private int m_CachedRenderHeight;

		private Matrix4x4 m_CachedProjectionMatrix;

		private DeferredTiler[] m_Tilers;

		private int[] m_TileDataCapacities;

		private bool m_HasTileVisLights;

		private NativeArray<ushort> m_stencilVisLights;

		private NativeArray<ushort> m_stencilVisLightOffsets;

		private AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;

		private Mesh m_SphereMesh;

		private Mesh m_HemisphereMesh;

		private Mesh m_FullscreenMesh;

		private int m_MaxDepthRangePerBatch;

		private int m_MaxTilesPerBatch;

		private int m_MaxPunctualLightPerBatch;

		private int m_MaxRelLightIndicesPerBatch;

		private Material m_TileDepthInfoMaterial;

		private Material m_TileDeferredMaterial;

		private Material m_StencilDeferredMaterial;

		private Matrix4x4[] m_ScreenToWorld = new Matrix4x4[2];

		private ProfilingSampler m_ProfilingSamplerDeferredTiledPass = new ProfilingSampler(k_DeferredTiledPass);

		private ProfilingSampler m_ProfilingSamplerDeferredStencilPass = new ProfilingSampler(k_DeferredStencilPass);

		private ProfilingSampler m_ProfilingSamplerDeferredFogPass = new ProfilingSampler(k_DeferredFogPass);

		private ProfilingSampler m_ProfilingSamplerClearStencilPartialPass = new ProfilingSampler(k_ClearStencilPartial);

		internal int GbufferDepthIndex
		{
			get
			{
				if (!UseRenderPass)
				{
					return -1;
				}
				return 0;
			}
		}

		internal int GBufferAlbedoIndex => GbufferDepthIndex + 1;

		internal int GBufferSpecularMetallicIndex => GBufferAlbedoIndex + 1;

		internal int GBufferNormalSmoothnessIndex => GBufferSpecularMetallicIndex + 1;

		internal int GBufferLightingIndex => GBufferNormalSmoothnessIndex + 1;

		internal int GBufferShadowMask
		{
			get
			{
				if (!UseShadowMask)
				{
					return -1;
				}
				return GBufferLightingIndex + 1;
			}
		}

		internal int GBufferSliceCount => 4 + (UseRenderPass ? 1 : 0) + (UseShadowMask ? 1 : 0);

		internal bool UseShadowMask => MixedLightingSetup == MixedLightingSetup.Subtractive;

		internal bool UseRenderPass { get; set; }

		internal bool HasDepthPrepass { get; set; }

		internal bool IsOverlay { get; set; }

		internal bool AccurateGbufferNormals { get; set; }

		internal bool TiledDeferredShading { get; set; }

		internal MixedLightingSetup MixedLightingSetup { get; set; }

		internal bool UseJobSystem { get; set; }

		internal int RenderWidth { get; set; }

		internal int RenderHeight { get; set; }

		internal RenderTargetHandle[] GbufferAttachments { get; set; }

		internal RenderTargetHandle DepthAttachment { get; set; }

		internal RenderTargetHandle DepthCopyTexture { get; set; }

		internal RenderTargetHandle DepthInfoTexture { get; set; }

		internal RenderTargetHandle TileDepthInfoTexture { get; set; }

		internal RenderTargetIdentifier[] GbufferAttachmentIdentifiers { get; set; }

		internal RenderTargetIdentifier DepthAttachmentIdentifier { get; set; }

		internal RenderTargetIdentifier DepthCopyTextureIdentifier { get; set; }

		internal RenderTargetIdentifier DepthInfoTextureIdentifier { get; set; }

		internal RenderTargetIdentifier TileDepthInfoTextureIdentifier { get; set; }

		internal GraphicsFormat GetGBufferFormat(int index)
		{
			if (index == GBufferAlbedoIndex)
			{
				if (QualitySettings.activeColorSpace != ColorSpace.Linear)
				{
					return GraphicsFormat.R8G8B8A8_UNorm;
				}
				return GraphicsFormat.R8G8B8A8_SRGB;
			}
			if (index == GBufferSpecularMetallicIndex)
			{
				if (QualitySettings.activeColorSpace != ColorSpace.Linear)
				{
					return GraphicsFormat.R8G8B8A8_UNorm;
				}
				return GraphicsFormat.R8G8B8A8_SRGB;
			}
			if (index == GBufferNormalSmoothnessIndex)
			{
				if (!AccurateGbufferNormals)
				{
					return GraphicsFormat.R8G8B8A8_SNorm;
				}
				return GraphicsFormat.R8G8B8A8_UNorm;
			}
			if (index == GBufferLightingIndex)
			{
				return GraphicsFormat.None;
			}
			if (index == GbufferDepthIndex)
			{
				return GraphicsFormat.R32_SFloat;
			}
			if (index == GBufferShadowMask)
			{
				return GraphicsFormat.R8G8B8A8_UNorm;
			}
			return GraphicsFormat.None;
		}

		internal DeferredLights(Material tileDepthInfoMaterial, Material tileDeferredMaterial, Material stencilDeferredMaterial)
		{
			DeferredConfig.IsOpenGL = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
			m_TileDepthInfoMaterial = tileDepthInfoMaterial;
			m_TileDeferredMaterial = tileDeferredMaterial;
			m_StencilDeferredMaterial = stencilDeferredMaterial;
			if (m_TileDeferredMaterial != null)
			{
				m_TileDeferredMaterial.SetInt(ShaderConstants._LitStencilRef, 32);
				m_TileDeferredMaterial.SetInt(ShaderConstants._LitStencilReadMask, 96);
				m_TileDeferredMaterial.SetInt(ShaderConstants._LitStencilWriteMask, 0);
				m_TileDeferredMaterial.SetInt(ShaderConstants._SimpleLitStencilRef, 64);
				m_TileDeferredMaterial.SetInt(ShaderConstants._SimpleLitStencilReadMask, 96);
				m_TileDeferredMaterial.SetInt(ShaderConstants._SimpleLitStencilWriteMask, 0);
			}
			if (m_StencilDeferredMaterial != null)
			{
				m_StencilDeferredMaterial.SetInt(ShaderConstants._StencilRef, 0);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._StencilReadMask, 96);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._StencilWriteMask, 16);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._LitPunctualStencilRef, 48);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._LitPunctualStencilReadMask, 112);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._LitPunctualStencilWriteMask, 16);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._SimpleLitPunctualStencilRef, 80);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._SimpleLitPunctualStencilReadMask, 112);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._SimpleLitPunctualStencilWriteMask, 16);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._LitDirStencilRef, 32);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._LitDirStencilReadMask, 96);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._LitDirStencilWriteMask, 0);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._SimpleLitDirStencilRef, 64);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._SimpleLitDirStencilReadMask, 96);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._SimpleLitDirStencilWriteMask, 0);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._ClearStencilRef, 0);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._ClearStencilReadMask, 96);
				m_StencilDeferredMaterial.SetInt(ShaderConstants._ClearStencilWriteMask, 96);
			}
			m_MaxDepthRangePerBatch = (DeferredConfig.UseCBufferForDepthRange ? 65536 : 131072) / 4;
			m_MaxTilesPerBatch = (DeferredConfig.UseCBufferForTileList ? 65536 : 131072) / Marshal.SizeOf(typeof(TileData));
			m_MaxPunctualLightPerBatch = (DeferredConfig.UseCBufferForLightData ? 65536 : 131072) / Marshal.SizeOf(typeof(PunctualLightData));
			m_MaxRelLightIndicesPerBatch = (DeferredConfig.UseCBufferForLightList ? 65536 : 131072) / 4;
			m_Tilers = new DeferredTiler[3];
			m_TileDataCapacities = new int[3];
			for (int i = 0; i < 3; i++)
			{
				int num = (int)Mathf.Pow(4f, i);
				m_Tilers[i] = new DeferredTiler(16 * num, 16 * num, 32 * num * num, i);
				m_TileDataCapacities[i] = 0;
			}
			AccurateGbufferNormals = true;
			TiledDeferredShading = true;
			UseJobSystem = true;
			m_HasTileVisLights = false;
		}

		internal ref DeferredTiler GetTiler(int i)
		{
			return ref m_Tilers[i];
		}

		internal void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			DeferredShaderData.instance.ResetBuffers();
			RenderWidth = renderingData.cameraData.cameraTargetDescriptor.width;
			RenderHeight = renderingData.cameraData.cameraTargetDescriptor.height;
			if (TiledDeferredShading)
			{
				if (m_CachedRenderWidth != renderingData.cameraData.cameraTargetDescriptor.width || m_CachedRenderHeight != renderingData.cameraData.cameraTargetDescriptor.height || m_CachedProjectionMatrix != renderingData.cameraData.camera.projectionMatrix)
				{
					m_CachedRenderWidth = renderingData.cameraData.cameraTargetDescriptor.width;
					m_CachedRenderHeight = renderingData.cameraData.cameraTargetDescriptor.height;
					m_CachedProjectionMatrix = renderingData.cameraData.camera.projectionMatrix;
					for (int i = 0; i < m_Tilers.Length; i++)
					{
						m_Tilers[i].PrecomputeTiles(renderingData.cameraData.camera.projectionMatrix, renderingData.cameraData.camera.orthographic, m_CachedRenderWidth, m_CachedRenderHeight);
					}
				}
				for (int j = 0; j < m_Tilers.Length; j++)
				{
					m_Tilers[j].Setup(m_TileDataCapacities[j]);
				}
			}
			PrecomputeLights(out var prePunctualLights, out m_stencilVisLights, out m_stencilVisLightOffsets, ref renderingData.lightData.visibleLights, renderingData.lightData.additionalLightsCount != 0 || renderingData.lightData.mainLightIndex >= 0, renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.orthographic, renderingData.cameraData.camera.nearClipPlane);
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingSetupLightConstants))
			{
				SetupShaderLightConstants(commandBuffer, ref renderingData);
				CoreUtils.SetKeyword(commandBuffer, ShaderKeywordStrings._GBUFFER_NORMALS_OCT, AccurateGbufferNormals);
				CoreUtils.SetKeyword(commandBuffer, ShaderKeywordStrings.MixedLightingSubtractive, renderingData.lightData.supportsMixedLighting && MixedLightingSetup == MixedLightingSetup.Subtractive);
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
			if (TiledDeferredShading)
			{
				SortLights(ref prePunctualLights);
				NativeArray<ushort> lightIndices = new NativeArray<ushort>(prePunctualLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				for (int k = 0; k < prePunctualLights.Length; k++)
				{
					lightIndices[k] = (ushort)k;
				}
				NativeArray<uint> coarseTileHeaders = new NativeArray<uint>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				coarseTileHeaders[0] = 0u;
				coarseTileHeaders[1] = (uint)prePunctualLights.Length;
				ref DeferredTiler reference = ref m_Tilers[m_Tilers.Length - 1];
				if (m_Tilers.Length != 1)
				{
					NativeArray<JobHandle> jobs = default(NativeArray<JobHandle>);
					int num = 0;
					int num2 = 0;
					if (UseJobSystem)
					{
						int num3 = 1;
						for (int num4 = m_Tilers.Length - 1; num4 > 0; num4--)
						{
							ref DeferredTiler reference2 = ref m_Tilers[num4];
							num3 += reference2.TileXCount * reference2.TileYCount;
						}
						jobs = new NativeArray<JobHandle>(num3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
					}
					CullLightsJob cullLightsJob = default(CullLightsJob);
					cullLightsJob.tiler = reference;
					cullLightsJob.prePunctualLights = prePunctualLights;
					cullLightsJob.coarseTiles = lightIndices;
					cullLightsJob.coarseTileHeaders = coarseTileHeaders;
					cullLightsJob.coarseHeaderOffset = 0;
					cullLightsJob.istart = 0;
					cullLightsJob.iend = reference.TileXCount;
					cullLightsJob.jstart = 0;
					cullLightsJob.jend = reference.TileYCount;
					CullLightsJob jobData = cullLightsJob;
					if (UseJobSystem)
					{
						jobs[num2++] = jobData.Schedule();
						JobHandle.ScheduleBatchedJobs();
					}
					else
					{
						jobData.Execute();
					}
					for (int num5 = m_Tilers.Length - 1; num5 > 0; num5--)
					{
						ref DeferredTiler reference3 = ref m_Tilers[num5 - 1];
						ref DeferredTiler reference4 = ref m_Tilers[num5];
						int tileXCount = reference3.TileXCount;
						int tileYCount = reference3.TileYCount;
						int tileXCount2 = reference4.TileXCount;
						int tileYCount2 = reference4.TileYCount;
						int num6 = ((num5 == m_Tilers.Length - 1) ? tileXCount2 : 4);
						int num7 = ((num5 == m_Tilers.Length - 1) ? tileYCount2 : 4);
						int num8 = (tileXCount2 + num6 - 1) / num6;
						int num9 = (tileYCount2 + num7 - 1) / num7;
						NativeArray<ushort> tiles = reference4.Tiles;
						NativeArray<uint> tileHeaders = reference4.TileHeaders;
						int num10 = reference4.TilePixelWidth / reference3.TilePixelWidth;
						int num11 = reference4.TilePixelHeight / reference3.TilePixelHeight;
						for (int l = 0; l < tileYCount2; l++)
						{
							for (int m = 0; m < tileXCount2; m++)
							{
								int num12 = m * num10;
								int num13 = l * num11;
								int iend = Mathf.Min(num12 + num10, tileXCount);
								int jend = Mathf.Min(num13 + num11, tileYCount);
								int tileHeaderOffset = reference4.GetTileHeaderOffset(m, l);
								cullLightsJob = default(CullLightsJob);
								cullLightsJob.tiler = m_Tilers[num5 - 1];
								cullLightsJob.prePunctualLights = prePunctualLights;
								cullLightsJob.coarseTiles = tiles;
								cullLightsJob.coarseTileHeaders = tileHeaders;
								cullLightsJob.coarseHeaderOffset = tileHeaderOffset;
								cullLightsJob.istart = num12;
								cullLightsJob.iend = iend;
								cullLightsJob.jstart = num13;
								cullLightsJob.jend = jend;
								CullLightsJob jobData2 = cullLightsJob;
								if (UseJobSystem)
								{
									jobs[num2++] = jobData2.Schedule(jobs[num + m / num6 + l / num7 * num8]);
								}
								else
								{
									jobData2.Execute();
								}
							}
						}
						num += num8 * num9;
					}
					if (UseJobSystem)
					{
						JobHandle.CompleteAll(jobs);
						jobs.Dispose();
					}
				}
				else
				{
					reference.CullFinalLights(ref prePunctualLights, ref lightIndices, 0, prePunctualLights.Length, 0, reference.TileXCount, 0, reference.TileYCount);
				}
				lightIndices.Dispose();
				coarseTileHeaders.Dispose();
			}
			if (prePunctualLights.IsCreated)
			{
				prePunctualLights.Dispose();
			}
		}

		public void ResolveMixedLightingMode(ref RenderingData renderingData)
		{
			MixedLightingSetup = MixedLightingSetup.None;
			if (!renderingData.lightData.supportsMixedLighting)
			{
				return;
			}
			NativeArray<VisibleLight> visibleLights = renderingData.lightData.visibleLights;
			for (int i = 0; i < renderingData.lightData.visibleLights.Length; i++)
			{
				Light light = visibleLights[i].light;
				if (light != null && light.bakingOutput.mixedLightingMode == MixedLightingMode.Subtractive && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed && light.shadows != 0)
				{
					MixedLightingSetup = MixedLightingSetup.Subtractive;
					break;
				}
			}
		}

		public bool IsRuntimeSupportedThisFrame()
		{
			if (GBufferSliceCount <= SystemInfo.supportedRenderTargetCount)
			{
				return !DeferredConfig.IsOpenGL;
			}
			return false;
		}

		public void Setup(ref RenderingData renderingData, AdditionalLightsShadowCasterPass additionalLightsShadowCasterPass, bool hasDepthPrepass, bool isOverlay, RenderTargetHandle depthCopyTexture, RenderTargetHandle depthInfoTexture, RenderTargetHandle tileDepthInfoTexture, RenderTargetHandle depthAttachment, RenderTargetHandle[] gbufferHandles)
		{
			m_AdditionalLightsShadowCasterPass = additionalLightsShadowCasterPass;
			HasDepthPrepass = hasDepthPrepass;
			IsOverlay = isOverlay;
			DepthCopyTexture = depthCopyTexture;
			DepthInfoTexture = depthInfoTexture;
			TileDepthInfoTexture = tileDepthInfoTexture;
			if (GbufferAttachments == null || GbufferAttachments.Length != GBufferSliceCount)
			{
				GbufferAttachments = new RenderTargetHandle[GBufferSliceCount];
			}
			GbufferAttachments[GBufferAlbedoIndex] = gbufferHandles[1];
			GbufferAttachments[GBufferSpecularMetallicIndex] = gbufferHandles[2];
			GbufferAttachments[GBufferNormalSmoothnessIndex] = gbufferHandles[3];
			GbufferAttachments[GBufferLightingIndex] = gbufferHandles[4];
			if (GbufferDepthIndex >= 0)
			{
				GbufferAttachments[GbufferDepthIndex] = gbufferHandles[0];
			}
			if (GBufferShadowMask >= 0)
			{
				GbufferAttachments[GBufferShadowMask] = gbufferHandles[5];
			}
			DepthAttachment = depthAttachment;
			DepthCopyTextureIdentifier = DepthCopyTexture.Identifier();
			DepthInfoTextureIdentifier = DepthInfoTexture.Identifier();
			TileDepthInfoTextureIdentifier = TileDepthInfoTexture.Identifier();
			if (GbufferAttachmentIdentifiers == null || GbufferAttachmentIdentifiers.Length != GbufferAttachments.Length)
			{
				GbufferAttachmentIdentifiers = new RenderTargetIdentifier[GbufferAttachments.Length];
			}
			for (int i = 0; i < GbufferAttachments.Length; i++)
			{
				GbufferAttachmentIdentifiers[i] = GbufferAttachments[i].Identifier();
			}
			DepthAttachmentIdentifier = depthAttachment.Identifier();
			if (renderingData.cameraData.xr.enabled)
			{
				DepthCopyTextureIdentifier = new RenderTargetIdentifier(DepthCopyTextureIdentifier, 0, CubemapFace.Unknown, -1);
				DepthInfoTextureIdentifier = new RenderTargetIdentifier(DepthInfoTextureIdentifier, 0, CubemapFace.Unknown, -1);
				TileDepthInfoTextureIdentifier = new RenderTargetIdentifier(TileDepthInfoTextureIdentifier, 0, CubemapFace.Unknown, -1);
				for (int j = 0; j < GbufferAttachmentIdentifiers.Length; j++)
				{
					GbufferAttachmentIdentifiers[j] = new RenderTargetIdentifier(GbufferAttachmentIdentifiers[j], 0, CubemapFace.Unknown, -1);
				}
				DepthAttachmentIdentifier = new RenderTargetIdentifier(DepthAttachmentIdentifier, 0, CubemapFace.Unknown, -1);
			}
			m_HasTileVisLights = TiledDeferredShading && CheckHasTileLights(ref renderingData.lightData.visibleLights);
		}

		public void OnCameraCleanup(CommandBuffer cmd)
		{
			CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._GBUFFER_NORMALS_OCT, state: false);
			CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MixedLightingSubtractive, state: false);
			for (int i = 0; i < m_Tilers.Length; i++)
			{
				m_TileDataCapacities[i] = math.max(m_TileDataCapacities[i], m_Tilers[i].TileDataCapacity);
				m_Tilers[i].OnCameraCleanup();
			}
			if (m_stencilVisLights.IsCreated)
			{
				m_stencilVisLights.Dispose();
			}
			if (m_stencilVisLightOffsets.IsCreated)
			{
				m_stencilVisLightOffsets.Dispose();
			}
		}

		internal static StencilState OverwriteStencil(StencilState s, int stencilWriteMask)
		{
			if (!s.enabled)
			{
				return new StencilState(enabled: true, 0, (byte)stencilWriteMask, CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep, CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep);
			}
			CompareFunction compareFunctionFront = ((s.compareFunctionFront != 0) ? s.compareFunctionFront : CompareFunction.Always);
			CompareFunction compareFunctionBack = ((s.compareFunctionBack != 0) ? s.compareFunctionBack : CompareFunction.Always);
			StencilOp passOperationFront = s.passOperationFront;
			StencilOp failOperationFront = s.failOperationFront;
			StencilOp zFailOperationFront = s.zFailOperationFront;
			StencilOp passOperationBack = s.passOperationBack;
			StencilOp failOperationBack = s.failOperationBack;
			StencilOp zFailOperationBack = s.zFailOperationBack;
			return new StencilState(enabled: true, (byte)(s.readMask & 0xFu), (byte)(s.writeMask | stencilWriteMask), compareFunctionFront, passOperationFront, failOperationFront, zFailOperationFront, compareFunctionBack, passOperationBack, failOperationBack, zFailOperationBack);
		}

		internal static RenderStateBlock OverwriteStencil(RenderStateBlock block, int stencilWriteMask, int stencilRef)
		{
			if (!block.stencilState.enabled)
			{
				block.stencilState = new StencilState(enabled: true, 0, (byte)stencilWriteMask, CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep, CompareFunction.Always, StencilOp.Replace, StencilOp.Keep, StencilOp.Keep);
			}
			else
			{
				StencilState stencilState = block.stencilState;
				CompareFunction compareFunctionFront = ((stencilState.compareFunctionFront != 0) ? stencilState.compareFunctionFront : CompareFunction.Always);
				CompareFunction compareFunctionBack = ((stencilState.compareFunctionBack != 0) ? stencilState.compareFunctionBack : CompareFunction.Always);
				StencilOp passOperationFront = stencilState.passOperationFront;
				StencilOp failOperationFront = stencilState.failOperationFront;
				StencilOp zFailOperationFront = stencilState.zFailOperationFront;
				StencilOp passOperationBack = stencilState.passOperationBack;
				StencilOp failOperationBack = stencilState.failOperationBack;
				StencilOp zFailOperationBack = stencilState.zFailOperationBack;
				block.stencilState = new StencilState(enabled: true, (byte)(stencilState.readMask & 0xFu), (byte)(stencilState.writeMask | stencilWriteMask), compareFunctionFront, passOperationFront, failOperationFront, zFailOperationFront, compareFunctionBack, passOperationBack, failOperationBack, zFailOperationBack);
			}
			block.mask |= RenderStateMask.Stencil;
			block.stencilReference = (block.stencilReference & 0xF) | stencilRef;
			return block;
		}

		internal bool HasTileLights()
		{
			return m_HasTileVisLights;
		}

		internal bool HasTileDepthRangeExtraPass()
		{
			ref DeferredTiler reference = ref m_Tilers[0];
			int tilePixelWidth = reference.TilePixelWidth;
			int tilePixelHeight = reference.TilePixelHeight;
			Mathf.Log(Mathf.Min(tilePixelWidth, tilePixelHeight), 2f);
			return false;
		}

		internal void ExecuteTileDepthInfoPass(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (m_TileDepthInfoMaterial == null)
			{
				Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDepthInfoMaterial, GetType().Name);
				return;
			}
			uint num = (uint)(Mathf.FloatToHalf(-2f) | (Mathf.FloatToHalf(-1f) << 16));
			ref DeferredTiler reference = ref m_Tilers[0];
			int tileXCount = reference.TileXCount;
			int tileYCount = reference.TileYCount;
			int tilePixelWidth = reference.TilePixelWidth;
			int tilePixelHeight = reference.TilePixelHeight;
			int num2 = (int)Mathf.Log(Mathf.Min(tilePixelWidth, tilePixelHeight), 2f);
			int num3 = num2;
			int num4 = num2 - num3;
			int num5 = 1 << num3;
			int num6 = RenderWidth + num5 - 1 >> num3;
			_ = RenderHeight;
			_ = reference.Tiles;
			NativeArray<uint> tileHeaders = reference.TileHeaders;
			NativeArray<uint> data = new NativeArray<uint>(m_MaxDepthRangePerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingTileDepthInfo))
			{
				RenderTargetIdentifier depthAttachmentIdentifier = DepthAttachmentIdentifier;
				RenderTargetIdentifier dest = ((num2 == num3) ? TileDepthInfoTextureIdentifier : DepthInfoTextureIdentifier);
				commandBuffer.SetGlobalTexture(ShaderConstants._DepthTex, depthAttachmentIdentifier);
				commandBuffer.SetGlobalVector(ShaderConstants._DepthTexSize, new Vector4(RenderWidth, RenderHeight, 1f / (float)RenderWidth, 1f / (float)RenderHeight));
				commandBuffer.SetGlobalInt(ShaderConstants._DownsamplingWidth, tilePixelWidth);
				commandBuffer.SetGlobalInt(ShaderConstants._DownsamplingHeight, tilePixelHeight);
				commandBuffer.SetGlobalInt(ShaderConstants._SourceShiftX, num3);
				commandBuffer.SetGlobalInt(ShaderConstants._SourceShiftY, num3);
				commandBuffer.SetGlobalInt(ShaderConstants._TileShiftX, num4);
				commandBuffer.SetGlobalInt(ShaderConstants._TileShiftY, num4);
				Matrix4x4 projectionMatrix = renderingData.cameraData.camera.projectionMatrix;
				Matrix4x4 matrix4x = Matrix4x4.Inverse(new Matrix4x4(new Vector4(1f, 0f, 0f, 0f), new Vector4(0f, 1f, 0f, 0f), new Vector4(0f, 0f, 0.5f, 0f), new Vector4(0f, 0f, 0.5f, 1f)) * projectionMatrix);
				commandBuffer.SetGlobalVector(ShaderConstants._unproject0, matrix4x.GetRow(2));
				commandBuffer.SetGlobalVector(ShaderConstants._unproject1, matrix4x.GetRow(3));
				string text = null;
				if (tilePixelWidth == tilePixelHeight)
				{
					switch (num3)
					{
					case 1:
						text = ShaderKeywordStrings.DOWNSAMPLING_SIZE_2;
						break;
					case 2:
						text = ShaderKeywordStrings.DOWNSAMPLING_SIZE_4;
						break;
					case 3:
						text = ShaderKeywordStrings.DOWNSAMPLING_SIZE_8;
						break;
					case 4:
						text = ShaderKeywordStrings.DOWNSAMPLING_SIZE_16;
						break;
					}
				}
				if (text != null)
				{
					commandBuffer.EnableShaderKeyword(text);
				}
				int num7 = 0;
				int num8 = (DeferredConfig.UseCBufferForDepthRange ? 65536 : 131072) / (tileXCount * 4);
				while (num7 < tileYCount)
				{
					int num9 = Mathf.Min(tileYCount, num7 + num8);
					for (int i = num7; i < num9; i++)
					{
						for (int j = 0; j < tileXCount; j++)
						{
							int tileHeaderOffset = reference.GetTileHeaderOffset(j, i);
							uint value = ((tileHeaders[tileHeaderOffset + 1] == 0) ? num : tileHeaders[tileHeaderOffset + 2]);
							data[j + (i - num7) * tileXCount] = value;
						}
					}
					ComputeBuffer computeBuffer = DeferredShaderData.instance.ReserveBuffer<uint>(m_MaxDepthRangePerBatch, DeferredConfig.UseCBufferForDepthRange);
					computeBuffer.SetData(data, 0, 0, data.Length);
					if (DeferredConfig.UseCBufferForDepthRange)
					{
						commandBuffer.SetGlobalConstantBuffer(computeBuffer, ShaderConstants.UDepthRanges, 0, m_MaxDepthRangePerBatch * 4);
					}
					else
					{
						commandBuffer.SetGlobalBuffer(ShaderConstants._DepthRanges, computeBuffer);
					}
					commandBuffer.SetGlobalInt(ShaderConstants._tileXCount, tileXCount);
					commandBuffer.SetGlobalInt(ShaderConstants._DepthRangeOffset, num7 * tileXCount);
					commandBuffer.EnableScissorRect(new Rect(0f, num7 << num4, num6, num9 - num7 << num4));
					commandBuffer.Blit(depthAttachmentIdentifier, dest, m_TileDepthInfoMaterial, 0);
					num7 = num9;
				}
				commandBuffer.DisableScissorRect();
				if (text != null)
				{
					commandBuffer.DisableShaderKeyword(text);
				}
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
			data.Dispose();
		}

		internal void ExecuteDownsampleBitmaskPass(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (m_TileDepthInfoMaterial == null)
			{
				Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDepthInfoMaterial, GetType().Name);
				return;
			}
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingTileDepthInfo))
			{
				RenderTargetIdentifier depthInfoTextureIdentifier = DepthInfoTextureIdentifier;
				RenderTargetIdentifier tileDepthInfoTextureIdentifier = TileDepthInfoTextureIdentifier;
				ref DeferredTiler reference = ref m_Tilers[0];
				int tilePixelWidth = reference.TilePixelWidth;
				int tilePixelHeight = reference.TilePixelHeight;
				int num = (int)Mathf.Log(tilePixelWidth, 2f);
				int num2 = (int)Mathf.Log(tilePixelHeight, 2f);
				int num3 = -1;
				int num4 = num - num3;
				int num5 = num2 - num3;
				commandBuffer.SetGlobalTexture(ShaderConstants._BitmaskTex, depthInfoTextureIdentifier);
				commandBuffer.SetGlobalInt(ShaderConstants._DownsamplingWidth, tilePixelWidth);
				commandBuffer.SetGlobalInt(ShaderConstants._DownsamplingHeight, tilePixelHeight);
				int num6 = int.MinValue;
				int num7 = RenderWidth + num6 - 1 >> 31;
				int num8 = RenderHeight + num6 - 1 >> 31;
				commandBuffer.SetGlobalVector("_BitmaskTexSize", new Vector4(num7, num8, 1f / (float)num7, 1f / (float)num8));
				string text = null;
				if (num4 == 1 && num5 == 1)
				{
					text = ShaderKeywordStrings.DOWNSAMPLING_SIZE_2;
				}
				else if (num4 == 2 && num5 == 2)
				{
					text = ShaderKeywordStrings.DOWNSAMPLING_SIZE_4;
				}
				else if (num4 == 3 && num5 == 3)
				{
					text = ShaderKeywordStrings.DOWNSAMPLING_SIZE_8;
				}
				if (text != null)
				{
					commandBuffer.EnableShaderKeyword(text);
				}
				commandBuffer.Blit(depthInfoTextureIdentifier, tileDepthInfoTextureIdentifier, m_TileDepthInfoMaterial, 1);
				if (text != null)
				{
					commandBuffer.DisableShaderKeyword(text);
				}
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		internal void ClearStencilPartial(CommandBuffer cmd)
		{
			if (m_FullscreenMesh == null)
			{
				m_FullscreenMesh = CreateFullscreenMesh();
			}
			using (new ProfilingScope(cmd, m_ProfilingSamplerClearStencilPartialPass))
			{
				cmd.DrawMesh(m_FullscreenMesh, Matrix4x4.identity, m_StencilDeferredMaterial, 0, 6);
			}
		}

		internal void ExecuteDeferredPass(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			CommandBuffer commandBuffer = CommandBufferPool.Get();
			using (new ProfilingScope(commandBuffer, m_ProfilingDeferredPass))
			{
				if (renderingData.lightData.supportsMixedLighting && MixedLightingSetup == MixedLightingSetup.Subtractive)
				{
					commandBuffer.EnableShaderKeyword(ShaderKeywordStrings._DEFERRED_SUBTRACTIVE_LIGHTING);
				}
				SetupMatrixConstants(commandBuffer, ref renderingData);
				RenderTileLights(context, commandBuffer, ref renderingData);
				RenderStencilLights(context, commandBuffer, ref renderingData);
				if (renderingData.lightData.supportsMixedLighting && MixedLightingSetup == MixedLightingSetup.Subtractive)
				{
					commandBuffer.DisableShaderKeyword(ShaderKeywordStrings._DEFERRED_SUBTRACTIVE_LIGHTING);
				}
				RenderFog(context, commandBuffer, ref renderingData);
			}
			context.ExecuteCommandBuffer(commandBuffer);
			CommandBufferPool.Release(commandBuffer);
		}

		private void SetupShaderLightConstants(CommandBuffer cmd, ref RenderingData renderingData)
		{
			SetupMainLightConstants(cmd, ref renderingData.lightData);
		}

		private void SetupMainLightConstants(CommandBuffer cmd, ref LightData lightData)
		{
			UniversalRenderPipeline.InitializeLightConstants_Common(lightData.visibleLights, lightData.mainLightIndex, out var lightPos, out var lightColor, out var _, out var _, out var _);
			cmd.SetGlobalVector(ShaderConstants._MainLightPosition, lightPos);
			cmd.SetGlobalVector(ShaderConstants._MainLightColor, lightColor);
		}

		private void SetupMatrixConstants(CommandBuffer cmd, ref RenderingData renderingData)
		{
			ref CameraData cameraData = ref renderingData.cameraData;
			int num = ((!cameraData.xr.enabled || !cameraData.xr.singlePassEnabled) ? 1 : 2);
			Matrix4x4[] screenToWorld = m_ScreenToWorld;
			for (int i = 0; i < num; i++)
			{
				Matrix4x4 projectionMatrix = cameraData.GetProjectionMatrix(i);
				Matrix4x4 viewMatrix = cameraData.GetViewMatrix(i);
				Matrix4x4 gPUProjectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, renderIntoTexture: false);
				Matrix4x4 matrix4x = new Matrix4x4(new Vector4(0.5f * (float)RenderWidth, 0f, 0f, 0f), new Vector4(0f, 0.5f * (float)RenderHeight, 0f, 0f), new Vector4(0f, 0f, 1f, 0f), new Vector4(0.5f * (float)RenderWidth, 0.5f * (float)RenderHeight, 0f, 1f));
				Matrix4x4 matrix4x2 = Matrix4x4.identity;
				if (DeferredConfig.IsOpenGL)
				{
					matrix4x2 = new Matrix4x4(new Vector4(1f, 0f, 0f, 0f), new Vector4(0f, 1f, 0f, 0f), new Vector4(0f, 0f, 0.5f, 0f), new Vector4(0f, 0f, 0.5f, 1f));
				}
				screenToWorld[i] = Matrix4x4.Inverse(matrix4x * matrix4x2 * gPUProjectionMatrix * viewMatrix);
			}
			cmd.SetGlobalMatrixArray(ShaderConstants._ScreenToWorld, screenToWorld);
		}

		private void SortLights(ref NativeArray<DeferredTiler.PrePunctualLight> prePunctualLights)
		{
			DeferredTiler.PrePunctualLight[] array = prePunctualLights.ToArray();
			Array.Sort(array, new SortPrePunctualLight());
			prePunctualLights.CopyFrom(array);
		}

		private bool CheckHasTileLights(ref NativeArray<VisibleLight> visibleLights)
		{
			for (int i = 0; i < visibleLights.Length; i++)
			{
				if (IsTileLight(visibleLights[i]))
				{
					return true;
				}
			}
			return false;
		}

		private void PrecomputeLights(out NativeArray<DeferredTiler.PrePunctualLight> prePunctualLights, out NativeArray<ushort> stencilVisLights, out NativeArray<ushort> stencilVisLightOffsets, ref NativeArray<VisibleLight> visibleLights, bool hasAdditionalLights, Matrix4x4 view, bool isOrthographic, float zNear)
		{
			if (!hasAdditionalLights)
			{
				prePunctualLights = new NativeArray<DeferredTiler.PrePunctualLight>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				stencilVisLights = new NativeArray<ushort>(0, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				stencilVisLightOffsets = new NativeArray<ushort>(5, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
				return;
			}
			NativeArray<int> nativeArray = new NativeArray<int>(5, Allocator.Temp);
			NativeArray<int> nativeArray2 = new NativeArray<int>(5, Allocator.Temp);
			NativeArray<int> nativeArray3 = new NativeArray<int>(5, Allocator.Temp);
			stencilVisLightOffsets = new NativeArray<ushort>(5, Allocator.Temp);
			for (ushort num = 0; num < visibleLights.Length; num = (ushort)(num + 1))
			{
				VisibleLight visibleLight = visibleLights[num];
				if (TiledDeferredShading && IsTileLight(visibleLight))
				{
					int lightType = (int)visibleLight.lightType;
					int value = nativeArray[lightType] + 1;
					nativeArray[lightType] = value;
				}
				else
				{
					int value = (int)visibleLight.lightType;
					ushort value2 = (ushort)(stencilVisLightOffsets[value] + 1);
					stencilVisLightOffsets[value] = value2;
				}
			}
			int length = nativeArray[2] + nativeArray[0];
			int length2 = stencilVisLightOffsets[0] + stencilVisLightOffsets[1] + stencilVisLightOffsets[2];
			prePunctualLights = new NativeArray<DeferredTiler.PrePunctualLight>(length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			stencilVisLights = new NativeArray<ushort>(length2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			int i = 0;
			int num2 = 0;
			for (; i < nativeArray.Length; i++)
			{
				int num3 = nativeArray[i];
				nativeArray[i] = num2;
				num2 += num3;
			}
			int j = 0;
			int num4 = 0;
			for (; j < stencilVisLightOffsets.Length; j++)
			{
				int num5 = stencilVisLightOffsets[j];
				stencilVisLightOffsets[j] = (ushort)num4;
				num4 += num5;
			}
			DeferredTiler.PrePunctualLight value3 = default(DeferredTiler.PrePunctualLight);
			for (ushort num6 = 0; num6 < visibleLights.Length; num6 = (ushort)(num6 + 1))
			{
				VisibleLight visibleLight2 = visibleLights[num6];
				if (TiledDeferredShading && IsTileLight(visibleLight2))
				{
					value3.posVS = view.MultiplyPoint(visibleLight2.localToWorldMatrix.GetColumn(3));
					value3.radius = visibleLight2.range;
					value3.minDist = math.max(0f, math.length(value3.posVS) - value3.radius);
					value3.screenPos = new Vector2(value3.posVS.x, value3.posVS.y);
					if (!isOrthographic && value3.posVS.z <= zNear)
					{
						value3.screenPos *= (0f - zNear) / value3.posVS.z;
					}
					value3.visLightIndex = num6;
					int num7 = nativeArray2[(int)visibleLight2.lightType]++;
					prePunctualLights[nativeArray[(int)visibleLight2.lightType] + num7] = value3;
				}
				else
				{
					int num8 = nativeArray3[(int)visibleLight2.lightType]++;
					stencilVisLights[stencilVisLightOffsets[(int)visibleLight2.lightType] + num8] = num6;
				}
			}
			nativeArray.Dispose();
			nativeArray2.Dispose();
			nativeArray3.Dispose();
		}

		private void RenderTileLights(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
		{
			if (!m_HasTileVisLights)
			{
				return;
			}
			if (m_TileDeferredMaterial == null)
			{
				Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_TileDeferredMaterial, GetType().Name);
				return;
			}
			DrawCall[] array = new DrawCall[256];
			int num = 0;
			ref DeferredTiler reference = ref m_Tilers[0];
			int num2 = 16;
			int num3 = num2 >> 4;
			int num4 = Marshal.SizeOf(typeof(PunctualLightData));
			int num5 = num4 >> 4;
			int tileXCount = reference.TileXCount;
			int tileYCount = reference.TileYCount;
			int maxLightPerTile = reference.MaxLightPerTile;
			NativeArray<ushort> tiles = reference.Tiles;
			NativeArray<uint> tileHeaders = reference.TileHeaders;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			ComputeBuffer computeBuffer = DeferredShaderData.instance.ReserveBuffer<TileData>(m_MaxTilesPerBatch, DeferredConfig.UseCBufferForTileList);
			ComputeBuffer computeBuffer2 = DeferredShaderData.instance.ReserveBuffer<PunctualLightData>(m_MaxPunctualLightPerBatch, DeferredConfig.UseCBufferForLightData);
			ComputeBuffer computeBuffer3 = DeferredShaderData.instance.ReserveBuffer<uint>(m_MaxRelLightIndicesPerBatch, DeferredConfig.UseCBufferForLightList);
			NativeArray<uint4> tileList = new NativeArray<uint4>(m_MaxTilesPerBatch * num3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			NativeArray<uint4> punctualLightBuffer = new NativeArray<uint4>(m_MaxPunctualLightPerBatch * num5, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			NativeArray<uint> data = new NativeArray<uint>(m_MaxRelLightIndicesPerBatch, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			NativeArray<ushort> trimmedLights = new NativeArray<ushort>(maxLightPerTile, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			NativeArray<ushort> nativeArray = new NativeArray<ushort>(renderingData.lightData.visibleLights.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
			BitArray usedLights = new BitArray(renderingData.lightData.visibleLights.Length, Allocator.Temp);
			for (int i = 0; i < tileYCount; i++)
			{
				for (int j = 0; j < tileXCount; j++)
				{
					reference.GetTileOffsetAndCount(j, i, out var offset, out var count);
					if (count == 0)
					{
						continue;
					}
					int num10 = TrimLights(ref trimmedLights, ref tiles, offset, count, ref usedLights);
					bool flag = num7 == m_MaxTilesPerBatch;
					bool flag2 = num8 + num10 > m_MaxPunctualLightPerBatch;
					bool flag3 = num9 + count > m_MaxRelLightIndicesPerBatch;
					if (flag || flag2 || flag3)
					{
						array[num++] = new DrawCall
						{
							tileList = computeBuffer,
							punctualLightBuffer = computeBuffer2,
							relLightList = computeBuffer3,
							tileListSize = num7 * num2,
							punctualLightBufferSize = num8 * num4,
							relLightListSize = Align(num9, 4) * 4,
							instanceOffset = num6,
							instanceCount = num7 - num6
						};
						if (flag)
						{
							computeBuffer.SetData(tileList, 0, 0, tileList.Length);
							computeBuffer = DeferredShaderData.instance.ReserveBuffer<TileData>(m_MaxTilesPerBatch, DeferredConfig.UseCBufferForTileList);
							num7 = 0;
						}
						if (flag2)
						{
							computeBuffer2.SetData(punctualLightBuffer, 0, 0, punctualLightBuffer.Length);
							computeBuffer2 = DeferredShaderData.instance.ReserveBuffer<PunctualLightData>(m_MaxPunctualLightPerBatch, DeferredConfig.UseCBufferForLightData);
							num8 = 0;
							num10 = count;
							for (int k = 0; k < count; k++)
							{
								trimmedLights[k] = tiles[offset + k];
							}
							usedLights.Clear();
						}
						if (flag3)
						{
							computeBuffer3.SetData(data, 0, 0, data.Length);
							computeBuffer3 = DeferredShaderData.instance.ReserveBuffer<uint>(m_MaxRelLightIndicesPerBatch, DeferredConfig.UseCBufferForLightList);
							num9 = 0;
						}
						num6 = num7;
					}
					int tileHeaderOffset = reference.GetTileHeaderOffset(j, i);
					uint listBitMask = tileHeaders[tileHeaderOffset + 3];
					StoreTileData(ref tileList, num7, PackTileID((uint)j, (uint)i), listBitMask, (ushort)num9, (ushort)count);
					num7++;
					for (int l = 0; l < num10; l++)
					{
						int num11 = trimmedLights[l];
						StorePunctualLightData(ref punctualLightBuffer, num8, ref renderingData.lightData.visibleLights, num11);
						nativeArray[num11] = (ushort)num8;
						num8++;
						usedLights.Set(num11, val: true);
					}
					for (int m = 0; m < count; m++)
					{
						ushort index = tiles[offset + m];
						ushort num12 = tiles[offset + count + m];
						ushort num13 = nativeArray[index];
						data[num9++] = (uint)(num13 | (num12 << 16));
					}
				}
			}
			int num14 = num7 - num6;
			if (num14 > 0)
			{
				computeBuffer.SetData(tileList, 0, 0, tileList.Length);
				computeBuffer2.SetData(punctualLightBuffer, 0, 0, punctualLightBuffer.Length);
				computeBuffer3.SetData(data, 0, 0, data.Length);
				array[num++] = new DrawCall
				{
					tileList = computeBuffer,
					punctualLightBuffer = computeBuffer2,
					relLightList = computeBuffer3,
					tileListSize = num7 * num2,
					punctualLightBufferSize = num8 * num4,
					relLightListSize = Align(num9, 4) * 4,
					instanceOffset = num6,
					instanceCount = num14
				};
			}
			tileList.Dispose();
			punctualLightBuffer.Dispose();
			data.Dispose();
			trimmedLights.Dispose();
			nativeArray.Dispose();
			usedLights.Dispose();
			using (new ProfilingScope(cmd, m_ProfilingSamplerDeferredTiledPass))
			{
				MeshTopology topology = MeshTopology.Triangles;
				int vertexCount = 6;
				cmd.SetGlobalVector(value: new Vector4(RenderWidth, RenderHeight, 1f / (float)RenderWidth, 1f / (float)RenderHeight), nameID: ShaderConstants._ScreenSize);
				int tilePixelWidth = m_Tilers[0].TilePixelWidth;
				int tilePixelHeight = m_Tilers[0].TilePixelHeight;
				cmd.SetGlobalInt(ShaderConstants._TilePixelWidth, tilePixelWidth);
				cmd.SetGlobalInt(ShaderConstants._TilePixelHeight, tilePixelHeight);
				cmd.SetGlobalTexture(TileDepthInfoTexture.id, TileDepthInfoTextureIdentifier);
				for (int n = 0; n < num; n++)
				{
					DrawCall drawCall = array[n];
					if (DeferredConfig.UseCBufferForTileList)
					{
						cmd.SetGlobalConstantBuffer(drawCall.tileList, ShaderConstants.UTileList, 0, drawCall.tileListSize);
					}
					else
					{
						cmd.SetGlobalBuffer(ShaderConstants._TileList, drawCall.tileList);
					}
					if (DeferredConfig.UseCBufferForLightData)
					{
						cmd.SetGlobalConstantBuffer(drawCall.punctualLightBuffer, ShaderConstants.UPunctualLightBuffer, 0, drawCall.punctualLightBufferSize);
					}
					else
					{
						cmd.SetGlobalBuffer(ShaderConstants._PunctualLightBuffer, drawCall.punctualLightBuffer);
					}
					if (DeferredConfig.UseCBufferForLightList)
					{
						cmd.SetGlobalConstantBuffer(drawCall.relLightList, ShaderConstants.URelLightList, 0, drawCall.relLightListSize);
					}
					else
					{
						cmd.SetGlobalBuffer(ShaderConstants._RelLightList, drawCall.relLightList);
					}
					cmd.SetGlobalInt(ShaderConstants._InstanceOffset, drawCall.instanceOffset);
					cmd.DrawProcedural(Matrix4x4.identity, m_TileDeferredMaterial, 0, topology, vertexCount, drawCall.instanceCount);
					cmd.DrawProcedural(Matrix4x4.identity, m_TileDeferredMaterial, 1, topology, vertexCount, drawCall.instanceCount);
				}
			}
		}

		private void RenderStencilLights(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
		{
			if (m_stencilVisLights.Length == 0)
			{
				return;
			}
			if (m_StencilDeferredMaterial == null)
			{
				Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_StencilDeferredMaterial, GetType().Name);
				return;
			}
			if (m_SphereMesh == null)
			{
				m_SphereMesh = CreateSphereMesh();
			}
			if (m_HemisphereMesh == null)
			{
				m_HemisphereMesh = CreateHemisphereMesh();
			}
			if (m_FullscreenMesh == null)
			{
				m_FullscreenMesh = CreateFullscreenMesh();
			}
			using (new ProfilingScope(cmd, m_ProfilingSamplerDeferredStencilPass))
			{
				NativeArray<VisibleLight> visibleLights = renderingData.lightData.visibleLights;
				RenderStencilDirectionalLights(cmd, ref renderingData, visibleLights, renderingData.lightData.mainLightIndex);
				RenderStencilPointLights(cmd, ref renderingData, visibleLights);
				RenderStencilSpotLights(cmd, ref renderingData, visibleLights);
			}
		}

		private void RenderStencilDirectionalLights(CommandBuffer cmd, ref RenderingData renderingData, NativeArray<VisibleLight> visibleLights, int mainLightIndex)
		{
			cmd.EnableShaderKeyword(ShaderKeywordStrings._DIRECTIONAL);
			for (int i = m_stencilVisLightOffsets[1]; i < m_stencilVisLights.Length; i++)
			{
				ushort num = m_stencilVisLights[i];
				VisibleLight visibleLight = visibleLights[num];
				if (visibleLight.lightType != LightType.Directional)
				{
					break;
				}
				UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, num, out var lightPos, out var lightColor, out var _, out var _, out var _);
				int num2 = 0;
				if (visibleLight.light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
				{
					num2 |= 4;
				}
				bool flag;
				if (num == mainLightIndex)
				{
					flag = (bool)visibleLight.light && visibleLight.light.shadows != LightShadows.None;
					cmd.DisableShaderKeyword(ShaderKeywordStrings._DEFERRED_ADDITIONAL_LIGHT_SHADOWS);
				}
				else
				{
					int num3 = ((m_AdditionalLightsShadowCasterPass != null) ? m_AdditionalLightsShadowCasterPass.GetShadowLightIndexFromLightIndex(num) : (-1));
					flag = (bool)visibleLight.light && visibleLight.light.shadows != 0 && num3 >= 0;
					CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._DEFERRED_ADDITIONAL_LIGHT_SHADOWS, flag);
					cmd.SetGlobalInt(ShaderConstants._ShadowLightIndex, num3);
				}
				bool state = flag && renderingData.shadowData.supportsSoftShadows && visibleLight.light.shadows == LightShadows.Soft;
				CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, state);
				cmd.SetGlobalVector(ShaderConstants._LightColor, lightColor);
				cmd.SetGlobalVector(ShaderConstants._LightDirection, lightPos);
				cmd.SetGlobalInt(ShaderConstants._LightFlags, num2);
				cmd.DrawMesh(m_FullscreenMesh, Matrix4x4.identity, m_StencilDeferredMaterial, 0, 3);
				cmd.DrawMesh(m_FullscreenMesh, Matrix4x4.identity, m_StencilDeferredMaterial, 0, 4);
			}
			cmd.DisableShaderKeyword(ShaderKeywordStrings._DEFERRED_ADDITIONAL_LIGHT_SHADOWS);
			cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadows);
			cmd.DisableShaderKeyword(ShaderKeywordStrings._DIRECTIONAL);
		}

		private void RenderStencilPointLights(CommandBuffer cmd, ref RenderingData renderingData, NativeArray<VisibleLight> visibleLights)
		{
			cmd.EnableShaderKeyword(ShaderKeywordStrings._POINT);
			for (int i = m_stencilVisLightOffsets[2]; i < m_stencilVisLights.Length; i++)
			{
				ushort num = m_stencilVisLights[i];
				VisibleLight visibleLight = visibleLights[num];
				if (visibleLight.lightType != LightType.Point)
				{
					break;
				}
				Vector3 vector = visibleLight.localToWorldMatrix.GetColumn(3);
				Matrix4x4 matrix = new Matrix4x4(new Vector4(visibleLight.range, 0f, 0f, 0f), new Vector4(0f, visibleLight.range, 0f, 0f), new Vector4(0f, 0f, visibleLight.range, 0f), new Vector4(vector.x, vector.y, vector.z, 1f));
				UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, num, out var lightPos, out var lightColor, out var lightAttenuation, out var _, out var lightOcclusionProbeChannel);
				int num2 = 0;
				if (visibleLight.light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
				{
					num2 |= 4;
				}
				int num3 = ((m_AdditionalLightsShadowCasterPass != null) ? m_AdditionalLightsShadowCasterPass.GetShadowLightIndexFromLightIndex(num) : (-1));
				bool flag = (bool)visibleLight.light && visibleLight.light.shadows != 0 && num3 >= 0;
				bool state = flag && renderingData.shadowData.supportsSoftShadows && visibleLight.light.shadows == LightShadows.Soft;
				CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._DEFERRED_ADDITIONAL_LIGHT_SHADOWS, flag);
				CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, state);
				cmd.SetGlobalVector(ShaderConstants._LightPosWS, lightPos);
				cmd.SetGlobalVector(ShaderConstants._LightColor, lightColor);
				cmd.SetGlobalVector(ShaderConstants._LightAttenuation, lightAttenuation);
				cmd.SetGlobalVector(ShaderConstants._LightOcclusionProbInfo, lightOcclusionProbeChannel);
				cmd.SetGlobalInt(ShaderConstants._LightFlags, num2);
				cmd.SetGlobalInt(ShaderConstants._ShadowLightIndex, num3);
				cmd.DrawMesh(m_SphereMesh, matrix, m_StencilDeferredMaterial, 0, 0);
				cmd.DrawMesh(m_SphereMesh, matrix, m_StencilDeferredMaterial, 0, 1);
				cmd.DrawMesh(m_SphereMesh, matrix, m_StencilDeferredMaterial, 0, 2);
			}
			cmd.DisableShaderKeyword(ShaderKeywordStrings._DEFERRED_ADDITIONAL_LIGHT_SHADOWS);
			cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadows);
			cmd.DisableShaderKeyword(ShaderKeywordStrings._POINT);
		}

		private void RenderStencilSpotLights(CommandBuffer cmd, ref RenderingData renderingData, NativeArray<VisibleLight> visibleLights)
		{
			cmd.EnableShaderKeyword(ShaderKeywordStrings._SPOT);
			for (int i = m_stencilVisLightOffsets[0]; i < m_stencilVisLights.Length; i++)
			{
				ushort num = m_stencilVisLights[i];
				VisibleLight visibleLight = visibleLights[num];
				if (visibleLight.lightType != 0)
				{
					break;
				}
				float f = (float)Math.PI / 180f * visibleLight.spotAngle * 0.5f;
				float num2 = Mathf.Cos(f);
				float num3 = Mathf.Sin(f);
				float num4 = Mathf.Lerp(1f, kStencilShapeGuard, num3);
				UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, num, out var lightPos, out var lightColor, out var lightAttenuation, out var lightSpotDir, out var lightOcclusionProbeChannel);
				int num5 = 0;
				if (visibleLight.light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
				{
					num5 |= 4;
				}
				int num6 = ((m_AdditionalLightsShadowCasterPass != null) ? m_AdditionalLightsShadowCasterPass.GetShadowLightIndexFromLightIndex(num) : (-1));
				bool flag = (bool)visibleLight.light && visibleLight.light.shadows != 0 && num6 >= 0;
				bool state = flag && renderingData.shadowData.supportsSoftShadows && visibleLight.light.shadows == LightShadows.Soft;
				CoreUtils.SetKeyword(cmd, ShaderKeywordStrings._DEFERRED_ADDITIONAL_LIGHT_SHADOWS, flag);
				CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, state);
				cmd.SetGlobalVector(ShaderConstants._SpotLightScale, new Vector4(num3, num3, 1f - num2, visibleLight.range));
				cmd.SetGlobalVector(ShaderConstants._SpotLightBias, new Vector4(0f, 0f, num2, 0f));
				cmd.SetGlobalVector(ShaderConstants._SpotLightGuard, new Vector4(num4, num4, num4, num2 * visibleLight.range));
				cmd.SetGlobalVector(ShaderConstants._LightPosWS, lightPos);
				cmd.SetGlobalVector(ShaderConstants._LightColor, lightColor);
				cmd.SetGlobalVector(ShaderConstants._LightAttenuation, lightAttenuation);
				cmd.SetGlobalVector(ShaderConstants._LightDirection, new Vector3(lightSpotDir.x, lightSpotDir.y, lightSpotDir.z));
				cmd.SetGlobalVector(ShaderConstants._LightOcclusionProbInfo, lightOcclusionProbeChannel);
				cmd.SetGlobalInt(ShaderConstants._LightFlags, num5);
				cmd.SetGlobalInt(ShaderConstants._ShadowLightIndex, num6);
				cmd.DrawMesh(m_HemisphereMesh, visibleLight.localToWorldMatrix, m_StencilDeferredMaterial, 0, 0);
				cmd.DrawMesh(m_HemisphereMesh, visibleLight.localToWorldMatrix, m_StencilDeferredMaterial, 0, 1);
				cmd.DrawMesh(m_HemisphereMesh, visibleLight.localToWorldMatrix, m_StencilDeferredMaterial, 0, 2);
			}
			cmd.DisableShaderKeyword(ShaderKeywordStrings._DEFERRED_ADDITIONAL_LIGHT_SHADOWS);
			cmd.DisableShaderKeyword(ShaderKeywordStrings.SoftShadows);
			cmd.DisableShaderKeyword(ShaderKeywordStrings._SPOT);
		}

		private void RenderFog(ScriptableRenderContext context, CommandBuffer cmd, ref RenderingData renderingData)
		{
			if (!RenderSettings.fog || renderingData.cameraData.camera.orthographic)
			{
				return;
			}
			if (m_FullscreenMesh == null)
			{
				m_FullscreenMesh = CreateFullscreenMesh();
			}
			using (new ProfilingScope(cmd, m_ProfilingSamplerDeferredFogPass))
			{
				cmd.DrawMesh(m_FullscreenMesh, Matrix4x4.identity, m_StencilDeferredMaterial, 0, 5);
			}
		}

		private int TrimLights(ref NativeArray<ushort> trimmedLights, ref NativeArray<ushort> tiles, int offset, int lightCount, ref BitArray usedLights)
		{
			int result = 0;
			for (int i = 0; i < lightCount; i++)
			{
				ushort num = tiles[offset + i];
				if (!usedLights.IsSet(num))
				{
					trimmedLights[result++] = num;
				}
			}
			return result;
		}

		private void StorePunctualLightData(ref NativeArray<uint4> punctualLightBuffer, int storeIndex, ref NativeArray<VisibleLight> visibleLights, int index)
		{
			int num = 0;
			if (visibleLights[index].light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
			{
				num |= 4;
			}
			UniversalRenderPipeline.InitializeLightConstants_Common(visibleLights, index, out var lightPos, out var lightColor, out var lightAttenuation, out var lightSpotDir, out var lightOcclusionProbeChannel);
			punctualLightBuffer[storeIndex * 5] = new uint4(FloatToUInt(lightPos.x), FloatToUInt(lightPos.y), FloatToUInt(lightPos.z), FloatToUInt(visibleLights[index].range * visibleLights[index].range));
			punctualLightBuffer[storeIndex * 5 + 1] = new uint4(FloatToUInt(lightColor.x), FloatToUInt(lightColor.y), FloatToUInt(lightColor.z), 0u);
			punctualLightBuffer[storeIndex * 5 + 2] = new uint4(FloatToUInt(lightAttenuation.x), FloatToUInt(lightAttenuation.y), FloatToUInt(lightAttenuation.z), FloatToUInt(lightAttenuation.w));
			punctualLightBuffer[storeIndex * 5 + 3] = new uint4(FloatToUInt(lightSpotDir.x), FloatToUInt(lightSpotDir.y), FloatToUInt(lightSpotDir.z), (uint)num);
			punctualLightBuffer[storeIndex * 5 + 4] = new uint4(FloatToUInt(lightOcclusionProbeChannel.x), FloatToUInt(lightOcclusionProbeChannel.y), FloatToUInt(lightOcclusionProbeChannel.z), FloatToUInt(lightOcclusionProbeChannel.w));
		}

		private void StoreTileData(ref NativeArray<uint4> tileList, int storeIndex, uint tileID, uint listBitMask, ushort relLightOffset, ushort lightCount)
		{
			tileList[storeIndex] = new uint4
			{
				x = tileID,
				y = listBitMask,
				z = (uint)(relLightOffset | (lightCount << 16)),
				w = 0u
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsTileLight(VisibleLight visibleLight)
		{
			if (visibleLight.lightType != LightType.Point || (!(visibleLight.light == null) && visibleLight.light.shadows != 0))
			{
				if (visibleLight.lightType == LightType.Spot)
				{
					if (!(visibleLight.light == null))
					{
						return visibleLight.light.shadows == LightShadows.None;
					}
					return true;
				}
				return false;
			}
			return true;
		}

		private static Mesh CreateSphereMesh()
		{
			Vector3[] vertices = new Vector3[42]
			{
				new Vector3(0f, 0f, -1.07f),
				new Vector3(0.174f, -0.535f, -0.91f),
				new Vector3(-0.455f, -0.331f, -0.91f),
				new Vector3(0.562f, 0f, -0.91f),
				new Vector3(-0.455f, 0.331f, -0.91f),
				new Vector3(0.174f, 0.535f, -0.91f),
				new Vector3(-0.281f, -0.865f, -0.562f),
				new Vector3(0.736f, -0.535f, -0.562f),
				new Vector3(0.296f, -0.91f, -0.468f),
				new Vector3(-0.91f, 0f, -0.562f),
				new Vector3(-0.774f, -0.562f, -0.478f),
				new Vector3(0f, -1.07f, 0f),
				new Vector3(-0.629f, -0.865f, 0f),
				new Vector3(0.629f, -0.865f, 0f),
				new Vector3(-1.017f, -0.331f, 0f),
				new Vector3(0.957f, 0f, -0.478f),
				new Vector3(0.736f, 0.535f, -0.562f),
				new Vector3(1.017f, -0.331f, 0f),
				new Vector3(1.017f, 0.331f, 0f),
				new Vector3(-0.296f, -0.91f, 0.478f),
				new Vector3(0.281f, -0.865f, 0.562f),
				new Vector3(0.774f, -0.562f, 0.478f),
				new Vector3(-0.736f, -0.535f, 0.562f),
				new Vector3(0.91f, 0f, 0.562f),
				new Vector3(0.455f, -0.331f, 0.91f),
				new Vector3(-0.174f, -0.535f, 0.91f),
				new Vector3(0.629f, 0.865f, 0f),
				new Vector3(0.774f, 0.562f, 0.478f),
				new Vector3(0.455f, 0.331f, 0.91f),
				new Vector3(0f, 0f, 1.07f),
				new Vector3(-0.562f, 0f, 0.91f),
				new Vector3(-0.957f, 0f, 0.478f),
				new Vector3(0.281f, 0.865f, 0.562f),
				new Vector3(-0.174f, 0.535f, 0.91f),
				new Vector3(0.296f, 0.91f, -0.478f),
				new Vector3(-1.017f, 0.331f, 0f),
				new Vector3(-0.736f, 0.535f, 0.562f),
				new Vector3(-0.296f, 0.91f, 0.478f),
				new Vector3(0f, 1.07f, 0f),
				new Vector3(-0.281f, 0.865f, -0.562f),
				new Vector3(-0.774f, 0.562f, -0.478f),
				new Vector3(-0.629f, 0.865f, 0f)
			};
			int[] triangles = new int[240]
			{
				0, 1, 2, 0, 3, 1, 2, 4, 0, 0,
				5, 3, 0, 4, 5, 1, 6, 2, 3, 7,
				1, 1, 8, 6, 1, 7, 8, 9, 4, 2,
				2, 6, 10, 10, 9, 2, 8, 11, 6, 6,
				12, 10, 11, 12, 6, 7, 13, 8, 8, 13,
				11, 10, 14, 9, 10, 12, 14, 3, 15, 7,
				5, 16, 3, 3, 16, 15, 15, 17, 7, 17,
				13, 7, 16, 18, 15, 15, 18, 17, 11, 19,
				12, 13, 20, 11, 11, 20, 19, 17, 21, 13,
				13, 21, 20, 12, 19, 22, 12, 22, 14, 17,
				23, 21, 18, 23, 17, 21, 24, 20, 23, 24,
				21, 20, 25, 19, 19, 25, 22, 24, 25, 20,
				26, 18, 16, 18, 27, 23, 26, 27, 18, 28,
				24, 23, 27, 28, 23, 24, 29, 25, 28, 29,
				24, 25, 30, 22, 25, 29, 30, 14, 22, 31,
				22, 30, 31, 32, 28, 27, 26, 32, 27, 33,
				29, 28, 30, 29, 33, 33, 28, 32, 34, 26,
				16, 5, 34, 16, 14, 31, 35, 14, 35, 9,
				31, 30, 36, 30, 33, 36, 35, 31, 36, 37,
				33, 32, 36, 33, 37, 38, 32, 26, 34, 38,
				26, 38, 37, 32, 5, 39, 34, 39, 38, 34,
				4, 39, 5, 9, 40, 4, 9, 35, 40, 4,
				40, 39, 35, 36, 41, 41, 36, 37, 41, 37,
				38, 40, 35, 41, 40, 41, 39, 41, 38, 39
			};
			return new Mesh
			{
				indexFormat = IndexFormat.UInt16,
				vertices = vertices,
				triangles = triangles
			};
		}

		private static Mesh CreateHemisphereMesh()
		{
			Vector3[] vertices = new Vector3[42]
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(1f, 0f, 0f),
				new Vector3(0.92388f, 0.382683f, 0f),
				new Vector3(0.707107f, 0.707107f, 0f),
				new Vector3(0.382683f, 0.92388f, 0f),
				new Vector3(-0f, 1f, 0f),
				new Vector3(-0.382684f, 0.92388f, 0f),
				new Vector3(-0.707107f, 0.707107f, 0f),
				new Vector3(-0.92388f, 0.382683f, 0f),
				new Vector3(-1f, -0f, 0f),
				new Vector3(-0.92388f, -0.382683f, 0f),
				new Vector3(-0.707107f, -0.707107f, 0f),
				new Vector3(-0.382683f, -0.92388f, 0f),
				new Vector3(0f, -1f, 0f),
				new Vector3(0.382684f, -0.923879f, 0f),
				new Vector3(0.707107f, -0.707107f, 0f),
				new Vector3(0.92388f, -0.382683f, 0f),
				new Vector3(0f, 0f, 1f),
				new Vector3(0.707107f, 0f, 0.707107f),
				new Vector3(0f, -0.707107f, 0.707107f),
				new Vector3(0f, 0.707107f, 0.707107f),
				new Vector3(-0.707107f, 0f, 0.707107f),
				new Vector3(0.816497f, -0.408248f, 0.408248f),
				new Vector3(0.408248f, -0.408248f, 0.816497f),
				new Vector3(0.408248f, -0.816497f, 0.408248f),
				new Vector3(0.408248f, 0.816497f, 0.408248f),
				new Vector3(0.408248f, 0.408248f, 0.816497f),
				new Vector3(0.816497f, 0.408248f, 0.408248f),
				new Vector3(-0.816497f, 0.408248f, 0.408248f),
				new Vector3(-0.408248f, 0.408248f, 0.816497f),
				new Vector3(-0.408248f, 0.816497f, 0.408248f),
				new Vector3(-0.408248f, -0.816497f, 0.408248f),
				new Vector3(-0.408248f, -0.408248f, 0.816497f),
				new Vector3(-0.816497f, -0.408248f, 0.408248f),
				new Vector3(0f, -0.92388f, 0.382683f),
				new Vector3(0.92388f, 0f, 0.382683f),
				new Vector3(0f, -0.382683f, 0.92388f),
				new Vector3(0.382683f, 0f, 0.92388f),
				new Vector3(0f, 0.92388f, 0.382683f),
				new Vector3(0f, 0.382683f, 0.92388f),
				new Vector3(-0.92388f, 0f, 0.382683f),
				new Vector3(-0.382683f, 0f, 0.92388f)
			};
			int[] triangles = new int[240]
			{
				0, 2, 1, 0, 3, 2, 0, 4, 3, 0,
				5, 4, 0, 6, 5, 0, 7, 6, 0, 8,
				7, 0, 9, 8, 0, 10, 9, 0, 11, 10,
				0, 12, 11, 0, 13, 12, 0, 14, 13, 0,
				15, 14, 0, 16, 15, 0, 1, 16, 22, 23,
				24, 25, 26, 27, 28, 29, 30, 31, 32, 33,
				14, 24, 34, 35, 22, 16, 36, 23, 37, 2,
				27, 35, 38, 25, 4, 37, 26, 39, 6, 30,
				38, 40, 28, 8, 39, 29, 41, 10, 33, 40,
				34, 31, 12, 41, 32, 36, 15, 22, 24, 18,
				23, 22, 19, 24, 23, 3, 25, 27, 20, 26,
				25, 18, 27, 26, 7, 28, 30, 21, 29, 28,
				20, 30, 29, 11, 31, 33, 19, 32, 31, 21,
				33, 32, 13, 14, 34, 15, 24, 14, 19, 34,
				24, 1, 35, 16, 18, 22, 35, 15, 16, 22,
				17, 36, 37, 19, 23, 36, 18, 37, 23, 1,
				2, 35, 3, 27, 2, 18, 35, 27, 5, 38,
				4, 20, 25, 38, 3, 4, 25, 17, 37, 39,
				18, 26, 37, 20, 39, 26, 5, 6, 38, 7,
				30, 6, 20, 38, 30, 9, 40, 8, 21, 28,
				40, 7, 8, 28, 17, 39, 41, 20, 29, 39,
				21, 41, 29, 9, 10, 40, 11, 33, 10, 21,
				40, 33, 13, 34, 12, 19, 31, 34, 11, 12,
				31, 17, 41, 36, 21, 32, 41, 19, 36, 32
			};
			return new Mesh
			{
				indexFormat = IndexFormat.UInt16,
				vertices = vertices,
				triangles = triangles
			};
		}

		private static Mesh CreateFullscreenMesh()
		{
			Vector3[] vertices = new Vector3[3]
			{
				new Vector3(-1f, 1f, 0f),
				new Vector3(-1f, -3f, 0f),
				new Vector3(3f, 1f, 0f)
			};
			int[] triangles = new int[3] { 0, 1, 2 };
			return new Mesh
			{
				indexFormat = IndexFormat.UInt16,
				vertices = vertices,
				triangles = triangles
			};
		}

		private static int Align(int s, int alignment)
		{
			return (s + alignment - 1) / alignment * alignment;
		}

		private static uint PackTileID(uint i, uint j)
		{
			return i | (j << 16);
		}

		private static uint FloatToUInt(float val)
		{
			byte[] bytes = BitConverter.GetBytes(val);
			return (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
		}

		private static uint Half2ToUInt(float x, float y)
		{
			ushort num = Mathf.FloatToHalf(x);
			uint num2 = Mathf.FloatToHalf(y);
			return num | (num2 << 16);
		}
	}
}
