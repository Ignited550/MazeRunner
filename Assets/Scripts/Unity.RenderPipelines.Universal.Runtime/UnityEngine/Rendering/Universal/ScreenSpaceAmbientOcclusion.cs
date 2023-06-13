using System;

namespace UnityEngine.Rendering.Universal
{
	[DisallowMultipleRendererFeature]
	internal class ScreenSpaceAmbientOcclusion : ScriptableRendererFeature
	{
		private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
		{
			private enum ShaderPasses
			{
				AO = 0,
				BlurHorizontal = 1,
				BlurVertical = 2,
				BlurFinal = 3
			}

			internal string profilerTag;

			internal Material material;

			private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;

			private ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(URPProfileId.SSAO);

			private RenderTargetIdentifier m_SSAOTexture1Target = new RenderTargetIdentifier(s_SSAOTexture1ID, 0, CubemapFace.Unknown, -1);

			private RenderTargetIdentifier m_SSAOTexture2Target = new RenderTargetIdentifier(s_SSAOTexture2ID, 0, CubemapFace.Unknown, -1);

			private RenderTargetIdentifier m_SSAOTexture3Target = new RenderTargetIdentifier(s_SSAOTexture3ID, 0, CubemapFace.Unknown, -1);

			private RenderTextureDescriptor m_Descriptor;

			private const string k_SSAOAmbientOcclusionParamName = "_AmbientOcclusionParam";

			private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";

			private static readonly int s_BaseMapID = Shader.PropertyToID("_BaseMap");

			private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");

			private static readonly int s_SSAOTexture1ID = Shader.PropertyToID("_SSAO_OcclusionTexture1");

			private static readonly int s_SSAOTexture2ID = Shader.PropertyToID("_SSAO_OcclusionTexture2");

			private static readonly int s_SSAOTexture3ID = Shader.PropertyToID("_SSAO_OcclusionTexture3");

			internal ScreenSpaceAmbientOcclusionPass()
			{
				m_CurrentSettings = new ScreenSpaceAmbientOcclusionSettings();
			}

			internal bool Setup(ScreenSpaceAmbientOcclusionSettings featureSettings)
			{
				m_CurrentSettings = featureSettings;
				switch (m_CurrentSettings.Source)
				{
				case ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth:
					ConfigureInput(ScriptableRenderPassInput.Depth);
					break;
				case ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals:
					ConfigureInput(ScriptableRenderPassInput.Normal);
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
				if (material != null && m_CurrentSettings.Intensity > 0f && m_CurrentSettings.Radius > 0f)
				{
					return m_CurrentSettings.SampleCount > 0;
				}
				return false;
			}

			public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
			{
				RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
				int num = ((!m_CurrentSettings.Downsample) ? 1 : 2);
				Vector4 value = new Vector4(m_CurrentSettings.Intensity, m_CurrentSettings.Radius, 1f / (float)num, m_CurrentSettings.SampleCount);
				material.SetVector(s_SSAOParamsID, value);
				CoreUtils.SetKeyword(material, "_ORTHOGRAPHIC", renderingData.cameraData.camera.orthographic);
				if (m_CurrentSettings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth)
				{
					switch (m_CurrentSettings.NormalSamples)
					{
					case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low:
						CoreUtils.SetKeyword(material, "_RECONSTRUCT_NORMAL_LOW", state: true);
						CoreUtils.SetKeyword(material, "_RECONSTRUCT_NORMAL_MEDIUM", state: false);
						CoreUtils.SetKeyword(material, "_RECONSTRUCT_NORMAL_HIGH", state: false);
						break;
					case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium:
						CoreUtils.SetKeyword(material, "_RECONSTRUCT_NORMAL_LOW", state: false);
						CoreUtils.SetKeyword(material, "_RECONSTRUCT_NORMAL_MEDIUM", state: true);
						CoreUtils.SetKeyword(material, "_RECONSTRUCT_NORMAL_HIGH", state: false);
						break;
					case ScreenSpaceAmbientOcclusionSettings.NormalQuality.High:
						CoreUtils.SetKeyword(material, "_RECONSTRUCT_NORMAL_LOW", state: false);
						CoreUtils.SetKeyword(material, "_RECONSTRUCT_NORMAL_MEDIUM", state: false);
						CoreUtils.SetKeyword(material, "_RECONSTRUCT_NORMAL_HIGH", state: true);
						break;
					default:
						throw new ArgumentOutOfRangeException();
					}
				}
				if (m_CurrentSettings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals)
				{
					CoreUtils.SetKeyword(material, "_SOURCE_DEPTH", state: false);
					CoreUtils.SetKeyword(material, "_SOURCE_DEPTH_NORMALS", state: true);
					CoreUtils.SetKeyword(material, "_SOURCE_GBUFFER", state: false);
				}
				else
				{
					CoreUtils.SetKeyword(material, "_SOURCE_DEPTH", state: true);
					CoreUtils.SetKeyword(material, "_SOURCE_DEPTH_NORMALS", state: false);
					CoreUtils.SetKeyword(material, "_SOURCE_GBUFFER", state: false);
				}
				m_Descriptor = cameraTargetDescriptor;
				m_Descriptor.msaaSamples = 1;
				m_Descriptor.depthBufferBits = 0;
				m_Descriptor.width /= num;
				m_Descriptor.height /= num;
				m_Descriptor.colorFormat = RenderTextureFormat.ARGB32;
				cmd.GetTemporaryRT(s_SSAOTexture1ID, m_Descriptor, FilterMode.Bilinear);
				m_Descriptor.width *= num;
				m_Descriptor.height *= num;
				cmd.GetTemporaryRT(s_SSAOTexture2ID, m_Descriptor, FilterMode.Bilinear);
				cmd.GetTemporaryRT(s_SSAOTexture3ID, m_Descriptor, FilterMode.Bilinear);
				ConfigureTarget(s_SSAOTexture2ID);
				ConfigureClear(ClearFlag.None, Color.white);
			}

			public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
			{
				if (material == null)
				{
					Debug.LogErrorFormat("{0}.Execute(): Missing material. {1} render pass will not execute. Check for missing reference in the renderer resources.", GetType().Name, profilerTag);
					return;
				}
				CommandBuffer commandBuffer = CommandBufferPool.Get();
				using (new ProfilingScope(commandBuffer, m_ProfilingSampler))
				{
					CoreUtils.SetKeyword(commandBuffer, ShaderKeywordStrings.ScreenSpaceOcclusion, state: true);
					PostProcessUtils.SetSourceSize(commandBuffer, m_Descriptor);
					Render(commandBuffer, m_SSAOTexture1Target, ShaderPasses.AO);
					RenderAndSetBaseMap(commandBuffer, m_SSAOTexture1Target, m_SSAOTexture2Target, ShaderPasses.BlurHorizontal);
					RenderAndSetBaseMap(commandBuffer, m_SSAOTexture2Target, m_SSAOTexture3Target, ShaderPasses.BlurVertical);
					RenderAndSetBaseMap(commandBuffer, m_SSAOTexture3Target, m_SSAOTexture2Target, ShaderPasses.BlurFinal);
					commandBuffer.SetGlobalTexture("_ScreenSpaceOcclusionTexture", m_SSAOTexture2Target);
					commandBuffer.SetGlobalVector("_AmbientOcclusionParam", new Vector4(0f, 0f, 0f, m_CurrentSettings.DirectLightingStrength));
				}
				context.ExecuteCommandBuffer(commandBuffer);
				CommandBufferPool.Release(commandBuffer);
			}

			private void Render(CommandBuffer cmd, RenderTargetIdentifier target, ShaderPasses pass)
			{
				cmd.SetRenderTarget(target, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, target, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
				cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, (int)pass);
			}

			private void RenderAndSetBaseMap(CommandBuffer cmd, RenderTargetIdentifier baseMap, RenderTargetIdentifier target, ShaderPasses pass)
			{
				cmd.SetGlobalTexture(s_BaseMapID, baseMap);
				Render(cmd, target, pass);
			}

			public override void OnCameraCleanup(CommandBuffer cmd)
			{
				if (cmd == null)
				{
					throw new ArgumentNullException("cmd");
				}
				CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, state: false);
				cmd.ReleaseTemporaryRT(s_SSAOTexture1ID);
				cmd.ReleaseTemporaryRT(s_SSAOTexture2ID);
				cmd.ReleaseTemporaryRT(s_SSAOTexture3ID);
			}
		}

		[SerializeField]
		[HideInInspector]
		private Shader m_Shader;

		[SerializeField]
		private ScreenSpaceAmbientOcclusionSettings m_Settings = new ScreenSpaceAmbientOcclusionSettings();

		private Material m_Material;

		private ScreenSpaceAmbientOcclusionPass m_SSAOPass;

		private const string k_ShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion";

		private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";

		private const string k_NormalReconstructionLowKeyword = "_RECONSTRUCT_NORMAL_LOW";

		private const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";

		private const string k_NormalReconstructionHighKeyword = "_RECONSTRUCT_NORMAL_HIGH";

		private const string k_SourceDepthKeyword = "_SOURCE_DEPTH";

		private const string k_SourceDepthNormalsKeyword = "_SOURCE_DEPTH_NORMALS";

		private const string k_SourceGBufferKeyword = "_SOURCE_GBUFFER";

		public override void Create()
		{
			if (m_SSAOPass == null)
			{
				m_SSAOPass = new ScreenSpaceAmbientOcclusionPass();
			}
			GetMaterial();
			m_SSAOPass.profilerTag = base.name;
			m_SSAOPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (!GetMaterial())
			{
				Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.", GetType().Name, m_SSAOPass.profilerTag);
			}
			else if (m_SSAOPass.Setup(m_Settings))
			{
				renderer.EnqueuePass(m_SSAOPass);
			}
		}

		protected override void Dispose(bool disposing)
		{
			CoreUtils.Destroy(m_Material);
		}

		private bool GetMaterial()
		{
			if (m_Material != null)
			{
				return true;
			}
			if (m_Shader == null)
			{
				m_Shader = Shader.Find("Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion");
				if (m_Shader == null)
				{
					return false;
				}
			}
			m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
			m_SSAOPass.material = m_Material;
			return m_Material != null;
		}
	}
}
