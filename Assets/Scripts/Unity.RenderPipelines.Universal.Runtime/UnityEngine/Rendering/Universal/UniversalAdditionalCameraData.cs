using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.Universal
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]
	[ImageEffectAllowedInSceneView]
	[MovedFrom("UnityEngine.Rendering.LWRP")]
	public class UniversalAdditionalCameraData : MonoBehaviour, ISerializationCallbackReceiver
	{
		[FormerlySerializedAs("renderShadows")]
		[SerializeField]
		private bool m_RenderShadows = true;

		[SerializeField]
		private CameraOverrideOption m_RequiresDepthTextureOption = CameraOverrideOption.UsePipelineSettings;

		[SerializeField]
		private CameraOverrideOption m_RequiresOpaqueTextureOption = CameraOverrideOption.UsePipelineSettings;

		[SerializeField]
		private CameraRenderType m_CameraType;

		[SerializeField]
		private List<Camera> m_Cameras = new List<Camera>();

		[SerializeField]
		private int m_RendererIndex = -1;

		[SerializeField]
		private LayerMask m_VolumeLayerMask = 1;

		[SerializeField]
		private Transform m_VolumeTrigger;

		[SerializeField]
		private bool m_RenderPostProcessing;

		[SerializeField]
		private AntialiasingMode m_Antialiasing;

		[SerializeField]
		private AntialiasingQuality m_AntialiasingQuality = AntialiasingQuality.High;

		[SerializeField]
		private bool m_StopNaN;

		[SerializeField]
		private bool m_Dithering;

		[SerializeField]
		private bool m_ClearDepth = true;

		[SerializeField]
		private bool m_AllowXRRendering = true;

		[NonSerialized]
		private Camera m_Camera;

		[FormerlySerializedAs("requiresDepthTexture")]
		[SerializeField]
		private bool m_RequiresDepthTexture;

		[FormerlySerializedAs("requiresColorTexture")]
		[SerializeField]
		private bool m_RequiresColorTexture;

		[HideInInspector]
		[SerializeField]
		private float m_Version = 2f;

		private static UniversalAdditionalCameraData s_DefaultAdditionalCameraData;

		public float version => m_Version;

		internal static UniversalAdditionalCameraData defaultAdditionalCameraData
		{
			get
			{
				if (s_DefaultAdditionalCameraData == null)
				{
					s_DefaultAdditionalCameraData = new UniversalAdditionalCameraData();
				}
				return s_DefaultAdditionalCameraData;
			}
		}

		internal Camera camera
		{
			get
			{
				if (!m_Camera)
				{
					base.gameObject.TryGetComponent<Camera>(out m_Camera);
				}
				return m_Camera;
			}
		}

		public bool renderShadows
		{
			get
			{
				return m_RenderShadows;
			}
			set
			{
				m_RenderShadows = value;
			}
		}

		public CameraOverrideOption requiresDepthOption
		{
			get
			{
				return m_RequiresDepthTextureOption;
			}
			set
			{
				m_RequiresDepthTextureOption = value;
			}
		}

		public CameraOverrideOption requiresColorOption
		{
			get
			{
				return m_RequiresOpaqueTextureOption;
			}
			set
			{
				m_RequiresOpaqueTextureOption = value;
			}
		}

		public CameraRenderType renderType
		{
			get
			{
				return m_CameraType;
			}
			set
			{
				m_CameraType = value;
			}
		}

		public List<Camera> cameraStack
		{
			get
			{
				if (renderType != 0)
				{
					Camera component = base.gameObject.GetComponent<Camera>();
					Debug.LogWarning($"{component.name}: This camera is of {renderType} type. Only Base cameras can have a camera stack.");
					return null;
				}
				if (!scriptableRenderer.supportedRenderingFeatures.cameraStacking)
				{
					Camera component2 = base.gameObject.GetComponent<Camera>();
					Debug.LogWarning($"{component2.name}: This camera has a ScriptableRenderer that doesn't support camera stacking. Camera stack is null.");
					return null;
				}
				return m_Cameras;
			}
		}

		public bool clearDepth => m_ClearDepth;

		public bool requiresDepthTexture
		{
			get
			{
				if (m_RequiresDepthTextureOption == CameraOverrideOption.UsePipelineSettings)
				{
					return UniversalRenderPipeline.asset.supportsCameraDepthTexture;
				}
				return m_RequiresDepthTextureOption == CameraOverrideOption.On;
			}
			set
			{
				m_RequiresDepthTextureOption = (value ? CameraOverrideOption.On : CameraOverrideOption.Off);
			}
		}

		public bool requiresColorTexture
		{
			get
			{
				if (m_RequiresOpaqueTextureOption == CameraOverrideOption.UsePipelineSettings)
				{
					return UniversalRenderPipeline.asset.supportsCameraOpaqueTexture;
				}
				return m_RequiresOpaqueTextureOption == CameraOverrideOption.On;
			}
			set
			{
				m_RequiresOpaqueTextureOption = (value ? CameraOverrideOption.On : CameraOverrideOption.Off);
			}
		}

		public ScriptableRenderer scriptableRenderer
		{
			get
			{
				if ((object)UniversalRenderPipeline.asset == null)
				{
					return null;
				}
				if (!UniversalRenderPipeline.asset.ValidateRendererData(m_RendererIndex))
				{
					int defaultRendererIndex = UniversalRenderPipeline.asset.m_DefaultRendererIndex;
					Debug.LogWarning("Renderer at <b>index " + m_RendererIndex + "</b> is missing for camera <b>" + camera.name + "</b>, falling back to Default Renderer. <b>" + UniversalRenderPipeline.asset.m_RendererDataList[defaultRendererIndex].name + "</b>", UniversalRenderPipeline.asset);
					return UniversalRenderPipeline.asset.GetRenderer(defaultRendererIndex);
				}
				return UniversalRenderPipeline.asset.GetRenderer(m_RendererIndex);
			}
		}

		public LayerMask volumeLayerMask
		{
			get
			{
				return m_VolumeLayerMask;
			}
			set
			{
				m_VolumeLayerMask = value;
			}
		}

		public Transform volumeTrigger
		{
			get
			{
				return m_VolumeTrigger;
			}
			set
			{
				m_VolumeTrigger = value;
			}
		}

		public bool renderPostProcessing
		{
			get
			{
				return m_RenderPostProcessing;
			}
			set
			{
				m_RenderPostProcessing = value;
			}
		}

		public AntialiasingMode antialiasing
		{
			get
			{
				return m_Antialiasing;
			}
			set
			{
				m_Antialiasing = value;
			}
		}

		public AntialiasingQuality antialiasingQuality
		{
			get
			{
				return m_AntialiasingQuality;
			}
			set
			{
				m_AntialiasingQuality = value;
			}
		}

		public bool stopNaN
		{
			get
			{
				return m_StopNaN;
			}
			set
			{
				m_StopNaN = value;
			}
		}

		public bool dithering
		{
			get
			{
				return m_Dithering;
			}
			set
			{
				m_Dithering = value;
			}
		}

		public bool allowXRRendering
		{
			get
			{
				return m_AllowXRRendering;
			}
			set
			{
				m_AllowXRRendering = value;
			}
		}

		internal void UpdateCameraStack()
		{
			int count = m_Cameras.Count;
			m_Cameras.RemoveAll((Camera cam) => cam == null);
			int count2 = m_Cameras.Count;
			int num = count - count2;
			if (num != 0)
			{
				Debug.LogWarning(base.name + ": " + num + " camera overlay" + ((num > 1) ? "s" : "") + " no longer exists and will be removed from the camera stack.");
			}
		}

		public void SetRenderer(int index)
		{
			m_RendererIndex = index;
		}

		public void OnBeforeSerialize()
		{
		}

		public void OnAfterDeserialize()
		{
			if (version <= 1f)
			{
				m_RequiresDepthTextureOption = (m_RequiresDepthTexture ? CameraOverrideOption.On : CameraOverrideOption.Off);
				m_RequiresOpaqueTextureOption = (m_RequiresColorTexture ? CameraOverrideOption.On : CameraOverrideOption.Off);
			}
		}

		public void OnDrawGizmos()
		{
			string text = "Packages/com.unity.render-pipelines.universal/Editor/Gizmos/";
			string value = "";
			Color white = Color.white;
			if (m_CameraType == CameraRenderType.Base)
			{
				value = text + "Camera_Base.png";
			}
			else if (m_CameraType == CameraRenderType.Overlay)
			{
				value = text + "Camera_Overlay.png";
			}
			if (!string.IsNullOrEmpty(value))
			{
				Gizmos.DrawIcon(base.transform.position, value, allowScaling: true, white);
			}
			if (renderPostProcessing)
			{
				Gizmos.DrawIcon(base.transform.position, text + "Camera_PostProcessing.png", allowScaling: true, white);
			}
		}
	}
}
