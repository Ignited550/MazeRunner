using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.Universal
{
	[ExcludeFromPreset]
	public class UniversalRenderPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
	{
		private Shader m_DefaultShader;

		private ScriptableRenderer[] m_Renderers = new ScriptableRenderer[1];

		[SerializeField]
		private int k_AssetVersion = 5;

		[SerializeField]
		private int k_AssetPreviousVersion = 5;

		[SerializeField]
		private RendererType m_RendererType = RendererType.ForwardRenderer;

		[EditorBrowsable(EditorBrowsableState.Never)]
		[SerializeField]
		internal ScriptableRendererData m_RendererData;

		[SerializeField]
		internal ScriptableRendererData[] m_RendererDataList = new ScriptableRendererData[1];

		[SerializeField]
		internal int m_DefaultRendererIndex;

		[SerializeField]
		private bool m_RequireDepthTexture;

		[SerializeField]
		private bool m_RequireOpaqueTexture;

		[SerializeField]
		private Downsampling m_OpaqueDownsampling = Downsampling._2xBilinear;

		[SerializeField]
		private bool m_SupportsTerrainHoles = true;

		[SerializeField]
		private bool m_SupportsHDR = true;

		[SerializeField]
		private MsaaQuality m_MSAA = MsaaQuality.Disabled;

		[SerializeField]
		private float m_RenderScale = 1f;

		[SerializeField]
		private LightRenderingMode m_MainLightRenderingMode = LightRenderingMode.PerPixel;

		[SerializeField]
		private bool m_MainLightShadowsSupported = true;

		[SerializeField]
		private ShadowResolution m_MainLightShadowmapResolution = ShadowResolution._2048;

		[SerializeField]
		private LightRenderingMode m_AdditionalLightsRenderingMode = LightRenderingMode.PerPixel;

		[SerializeField]
		private int m_AdditionalLightsPerObjectLimit = 4;

		[SerializeField]
		private bool m_AdditionalLightShadowsSupported;

		[SerializeField]
		private ShadowResolution m_AdditionalLightsShadowmapResolution = ShadowResolution._512;

		[SerializeField]
		private float m_ShadowDistance = 50f;

		[SerializeField]
		private int m_ShadowCascadeCount = 1;

		[SerializeField]
		private float m_Cascade2Split = 0.25f;

		[SerializeField]
		private Vector2 m_Cascade3Split = new Vector2(0.1f, 0.3f);

		[SerializeField]
		private Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);

		[SerializeField]
		private float m_ShadowDepthBias = 1f;

		[SerializeField]
		private float m_ShadowNormalBias = 1f;

		[SerializeField]
		private bool m_SoftShadowsSupported;

		[SerializeField]
		private bool m_UseSRPBatcher = true;

		[SerializeField]
		private bool m_SupportsDynamicBatching;

		[SerializeField]
		private bool m_MixedLightingSupported = true;

		[SerializeField]
		[Obsolete]
		private PipelineDebugLevel m_DebugLevel;

		[SerializeField]
		private bool m_UseAdaptivePerformance = true;

		[SerializeField]
		private ColorGradingMode m_ColorGradingMode;

		[SerializeField]
		private int m_ColorGradingLutSize = 32;

		[SerializeField]
		private ShadowQuality m_ShadowType = ShadowQuality.HardShadows;

		[SerializeField]
		private bool m_LocalShadowsSupported;

		[SerializeField]
		private ShadowResolution m_LocalShadowsAtlasResolution = ShadowResolution._256;

		[SerializeField]
		private int m_MaxPixelLights;

		[SerializeField]
		private ShadowResolution m_ShadowAtlasResolution = ShadowResolution._256;

		[SerializeField]
		private ShaderVariantLogLevel m_ShaderVariantLogLevel;

		public const int k_MinLutSize = 16;

		public const int k_MaxLutSize = 65;

		internal const int k_ShadowCascadeMinCount = 1;

		internal const int k_ShadowCascadeMaxCount = 4;

		[Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
		[SerializeField]
		private ShadowCascadesOption m_ShadowCascades;

		public ScriptableRenderer scriptableRenderer
		{
			get
			{
				if (m_RendererDataList?.Length > m_DefaultRendererIndex && m_RendererDataList[m_DefaultRendererIndex] == null)
				{
					Debug.LogError("Default renderer is missing from the current Pipeline Asset.", this);
					return null;
				}
				if (scriptableRendererData.isInvalidated || m_Renderers[m_DefaultRendererIndex] == null)
				{
					DestroyRenderer(ref m_Renderers[m_DefaultRendererIndex]);
					m_Renderers[m_DefaultRendererIndex] = scriptableRendererData.InternalCreateRenderer();
				}
				return m_Renderers[m_DefaultRendererIndex];
			}
		}

		internal ScriptableRendererData scriptableRendererData
		{
			get
			{
				if (m_RendererDataList[m_DefaultRendererIndex] == null)
				{
					CreatePipeline();
				}
				return m_RendererDataList[m_DefaultRendererIndex];
			}
		}

		internal int[] rendererIndexList
		{
			get
			{
				int[] array = new int[m_RendererDataList.Length + 1];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = i - 1;
				}
				return array;
			}
		}

		public bool supportsCameraDepthTexture
		{
			get
			{
				return m_RequireDepthTexture;
			}
			set
			{
				m_RequireDepthTexture = value;
			}
		}

		public bool supportsCameraOpaqueTexture
		{
			get
			{
				return m_RequireOpaqueTexture;
			}
			set
			{
				m_RequireOpaqueTexture = value;
			}
		}

		public Downsampling opaqueDownsampling => m_OpaqueDownsampling;

		public bool supportsTerrainHoles => m_SupportsTerrainHoles;

		public bool supportsHDR
		{
			get
			{
				return m_SupportsHDR;
			}
			set
			{
				m_SupportsHDR = value;
			}
		}

		public int msaaSampleCount
		{
			get
			{
				return (int)m_MSAA;
			}
			set
			{
				m_MSAA = (MsaaQuality)value;
			}
		}

		public float renderScale
		{
			get
			{
				return m_RenderScale;
			}
			set
			{
				m_RenderScale = ValidateRenderScale(value);
			}
		}

		public LightRenderingMode mainLightRenderingMode => m_MainLightRenderingMode;

		public bool supportsMainLightShadows => m_MainLightShadowsSupported;

		public int mainLightShadowmapResolution => (int)m_MainLightShadowmapResolution;

		public LightRenderingMode additionalLightsRenderingMode => m_AdditionalLightsRenderingMode;

		public int maxAdditionalLightsCount
		{
			get
			{
				return m_AdditionalLightsPerObjectLimit;
			}
			set
			{
				m_AdditionalLightsPerObjectLimit = ValidatePerObjectLights(value);
			}
		}

		public bool supportsAdditionalLightShadows => m_AdditionalLightShadowsSupported;

		public int additionalLightsShadowmapResolution => (int)m_AdditionalLightsShadowmapResolution;

		public float shadowDistance
		{
			get
			{
				return m_ShadowDistance;
			}
			set
			{
				m_ShadowDistance = Mathf.Max(0f, value);
			}
		}

		public int shadowCascadeCount
		{
			get
			{
				return m_ShadowCascadeCount;
			}
			set
			{
				if (value < 1 || value > 4)
				{
					throw new ArgumentException($"Value ({value}) needs to be between {1} and {4}.");
				}
				m_ShadowCascadeCount = value;
			}
		}

		public float cascade2Split => m_Cascade2Split;

		public Vector2 cascade3Split => m_Cascade3Split;

		public Vector3 cascade4Split => m_Cascade4Split;

		public float shadowDepthBias
		{
			get
			{
				return m_ShadowDepthBias;
			}
			set
			{
				m_ShadowDepthBias = ValidateShadowBias(value);
			}
		}

		public float shadowNormalBias
		{
			get
			{
				return m_ShadowNormalBias;
			}
			set
			{
				m_ShadowNormalBias = ValidateShadowBias(value);
			}
		}

		public bool supportsSoftShadows => m_SoftShadowsSupported;

		public bool supportsDynamicBatching
		{
			get
			{
				return m_SupportsDynamicBatching;
			}
			set
			{
				m_SupportsDynamicBatching = value;
			}
		}

		public bool supportsMixedLighting => m_MixedLightingSupported;

		public ShaderVariantLogLevel shaderVariantLogLevel
		{
			get
			{
				return m_ShaderVariantLogLevel;
			}
			set
			{
				m_ShaderVariantLogLevel = value;
			}
		}

		[Obsolete("PipelineDebugLevel is deprecated. Calling debugLevel is not necessary.", false)]
		public PipelineDebugLevel debugLevel => PipelineDebugLevel.Disabled;

		public bool useSRPBatcher
		{
			get
			{
				return m_UseSRPBatcher;
			}
			set
			{
				m_UseSRPBatcher = value;
			}
		}

		public ColorGradingMode colorGradingMode
		{
			get
			{
				return m_ColorGradingMode;
			}
			set
			{
				m_ColorGradingMode = value;
			}
		}

		public int colorGradingLutSize
		{
			get
			{
				return m_ColorGradingLutSize;
			}
			set
			{
				m_ColorGradingLutSize = Mathf.Clamp(value, 16, 65);
			}
		}

		public bool useAdaptivePerformance
		{
			get
			{
				return m_UseAdaptivePerformance;
			}
			set
			{
				m_UseAdaptivePerformance = value;
			}
		}

		public override Material defaultMaterial => GetMaterial(DefaultMaterialType.Standard);

		public override Material defaultParticleMaterial => GetMaterial(DefaultMaterialType.Particle);

		public override Material defaultLineMaterial => GetMaterial(DefaultMaterialType.Particle);

		public override Material defaultTerrainMaterial => GetMaterial(DefaultMaterialType.Terrain);

		public override Material defaultUIMaterial => GetMaterial(DefaultMaterialType.UnityBuiltinDefault);

		public override Material defaultUIOverdrawMaterial => GetMaterial(DefaultMaterialType.UnityBuiltinDefault);

		public override Material defaultUIETC1SupportedMaterial => GetMaterial(DefaultMaterialType.UnityBuiltinDefault);

		public override Material default2DMaterial => GetMaterial(DefaultMaterialType.Sprite);

		public override Shader defaultShader
		{
			get
			{
				if (m_DefaultShader == null)
				{
					m_DefaultShader = Shader.Find(ShaderUtils.GetShaderPath(ShaderPathID.Lit));
				}
				return m_DefaultShader;
			}
		}

		[Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
		public ShadowCascadesOption shadowCascadeOption
		{
			get
			{
				return shadowCascadeCount switch
				{
					1 => ShadowCascadesOption.NoCascades, 
					2 => ShadowCascadesOption.TwoCascades, 
					4 => ShadowCascadesOption.FourCascades, 
					_ => throw new InvalidOperationException("Cascade count is not compatible with obsolete API, please use shadowCascadeCount instead."), 
				};
			}
			set
			{
				switch (value)
				{
				case ShadowCascadesOption.NoCascades:
					shadowCascadeCount = 1;
					break;
				case ShadowCascadesOption.TwoCascades:
					shadowCascadeCount = 2;
					break;
				case ShadowCascadesOption.FourCascades:
					shadowCascadeCount = 4;
					break;
				default:
					throw new InvalidOperationException("Cascade count is not compatible with obsolete API, please use shadowCascadeCount instead.");
				}
			}
		}

		public ScriptableRendererData LoadBuiltinRendererData(RendererType type = RendererType.ForwardRenderer)
		{
			m_RendererDataList[0] = null;
			return m_RendererDataList[0];
		}

		protected override RenderPipeline CreatePipeline()
		{
			if (m_RendererDataList == null)
			{
				m_RendererDataList = new ScriptableRendererData[1];
			}
			if (m_RendererDataList[m_DefaultRendererIndex] == null)
			{
				if (k_AssetPreviousVersion != k_AssetVersion)
				{
					return null;
				}
				Debug.LogError("Default Renderer is missing, make sure there is a Renderer assigned as the default on the current Universal RP asset:" + UniversalRenderPipeline.asset.name, this);
				return null;
			}
			CreateRenderers();
			return new UniversalRenderPipeline(this);
		}

		private void DestroyRenderers()
		{
			if (m_Renderers != null)
			{
				for (int i = 0; i < m_Renderers.Length; i++)
				{
					DestroyRenderer(ref m_Renderers[i]);
				}
			}
		}

		private void DestroyRenderer(ref ScriptableRenderer renderer)
		{
			if (renderer != null)
			{
				renderer.Dispose();
				renderer = null;
			}
		}

		protected override void OnValidate()
		{
			DestroyRenderers();
			base.OnValidate();
		}

		protected override void OnDisable()
		{
			DestroyRenderers();
			base.OnDisable();
		}

		private void CreateRenderers()
		{
			DestroyRenderers();
			if (m_Renderers == null || m_Renderers.Length != m_RendererDataList.Length)
			{
				m_Renderers = new ScriptableRenderer[m_RendererDataList.Length];
			}
			for (int i = 0; i < m_RendererDataList.Length; i++)
			{
				if (m_RendererDataList[i] != null)
				{
					m_Renderers[i] = m_RendererDataList[i].InternalCreateRenderer();
				}
			}
		}

		private Material GetMaterial(DefaultMaterialType materialType)
		{
			return null;
		}

		public ScriptableRenderer GetRenderer(int index)
		{
			if (index == -1)
			{
				index = m_DefaultRendererIndex;
			}
			if (index >= m_RendererDataList.Length || index < 0 || m_RendererDataList[index] == null)
			{
				Debug.LogWarning("Renderer at index " + index + " is missing, falling back to Default Renderer " + m_RendererDataList[m_DefaultRendererIndex].name, this);
				index = m_DefaultRendererIndex;
			}
			if (m_Renderers == null || m_Renderers.Length < m_RendererDataList.Length)
			{
				CreateRenderers();
			}
			if (m_RendererDataList[index].isInvalidated || m_Renderers[index] == null)
			{
				DestroyRenderer(ref m_Renderers[index]);
				m_Renderers[index] = m_RendererDataList[index].InternalCreateRenderer();
			}
			return m_Renderers[index];
		}

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			if (k_AssetVersion < 3)
			{
				m_SoftShadowsSupported = m_ShadowType == ShadowQuality.SoftShadows;
				k_AssetPreviousVersion = k_AssetVersion;
				k_AssetVersion = 3;
			}
			if (k_AssetVersion < 4)
			{
				m_AdditionalLightShadowsSupported = m_LocalShadowsSupported;
				m_AdditionalLightsShadowmapResolution = m_LocalShadowsAtlasResolution;
				m_AdditionalLightsPerObjectLimit = m_MaxPixelLights;
				m_MainLightShadowmapResolution = m_ShadowAtlasResolution;
				k_AssetPreviousVersion = k_AssetVersion;
				k_AssetVersion = 4;
			}
			if (k_AssetVersion < 5)
			{
				if (m_RendererType == RendererType.Custom)
				{
					m_RendererDataList[0] = m_RendererData;
				}
				k_AssetPreviousVersion = k_AssetVersion;
				k_AssetVersion = 5;
			}
			if (k_AssetVersion < 6)
			{
				int shadowCascades = (int)m_ShadowCascades;
				if (shadowCascades == 2)
				{
					m_ShadowCascadeCount = 4;
				}
				else
				{
					m_ShadowCascadeCount = shadowCascades + 1;
				}
				k_AssetVersion = 6;
			}
		}

		private float ValidateShadowBias(float value)
		{
			return Mathf.Max(0f, Mathf.Min(value, UniversalRenderPipeline.maxShadowBias));
		}

		private int ValidatePerObjectLights(int value)
		{
			return Math.Max(0, Math.Min(value, UniversalRenderPipeline.maxPerObjectLights));
		}

		private float ValidateRenderScale(float value)
		{
			return Mathf.Max(UniversalRenderPipeline.minRenderScale, Mathf.Min(value, UniversalRenderPipeline.maxRenderScale));
		}

		internal bool ValidateRendererDataList(bool partial = false)
		{
			int num = 0;
			for (int i = 0; i < m_RendererDataList.Length; i++)
			{
				num += ((!ValidateRendererData(i)) ? 1 : 0);
			}
			if (partial)
			{
				return num == 0;
			}
			return num != m_RendererDataList.Length;
		}

		internal bool ValidateRendererData(int index)
		{
			if (index == -1)
			{
				index = m_DefaultRendererIndex;
			}
			if (index >= m_RendererDataList.Length)
			{
				return false;
			}
			return m_RendererDataList[index] != null;
		}
	}
}
