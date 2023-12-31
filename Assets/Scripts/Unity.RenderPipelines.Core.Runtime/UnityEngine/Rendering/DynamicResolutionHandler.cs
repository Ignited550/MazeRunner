using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
	public class DynamicResolutionHandler
	{
		private bool m_Enabled;

		private float m_MinScreenFraction;

		private float m_MaxScreenFraction;

		private float m_CurrentFraction;

		private bool m_ForcingRes;

		private bool m_CurrentCameraRequest;

		private float m_PrevFraction;

		private bool m_ForceSoftwareFallback;

		private float m_PrevHWScaleWidth;

		private float m_PrevHWScaleHeight;

		private Vector2Int m_LastScaledSize;

		private static DynamicResScalePolicyType s_ScalerType = DynamicResScalePolicyType.ReturnsMinMaxLerpFactor;

		private static PerformDynamicRes s_DynamicResMethod = DefaultDynamicResMethod;

		private Vector2Int cachedOriginalSize;

		private DynamicResolutionType type;

		private GlobalDynamicResolutionSettings m_CachedSettings = GlobalDynamicResolutionSettings.NewDefault();

		private const int CameraDictionaryMaxcCapacity = 32;

		private WeakReference m_OwnerCameraWeakRef;

		private static Dictionary<int, DynamicResolutionHandler> s_CameraInstances = new Dictionary<int, DynamicResolutionHandler>(32);

		private static DynamicResolutionHandler s_DefaultInstance = new DynamicResolutionHandler();

		private static int s_ActiveCameraId = 0;

		private static DynamicResolutionHandler s_ActiveInstance = s_DefaultInstance;

		private static bool s_ActiveInstanceDirty = true;

		private static float s_GlobalHwFraction = 1f;

		private static bool s_GlobalHwUpresActive = false;

		public DynamicResUpscaleFilter filter { get; set; }

		public Vector2Int finalViewport { get; set; }

		public static DynamicResolutionHandler instance => s_ActiveInstance;

		private void Reset()
		{
			m_Enabled = false;
			m_MinScreenFraction = 1f;
			m_MaxScreenFraction = 1f;
			m_CurrentFraction = 1f;
			m_ForcingRes = false;
			m_CurrentCameraRequest = true;
			m_PrevFraction = -1f;
			m_ForceSoftwareFallback = false;
			m_PrevHWScaleWidth = 1f;
			m_PrevHWScaleHeight = 1f;
			m_LastScaledSize = new Vector2Int(0, 0);
			filter = DynamicResUpscaleFilter.Bilinear;
		}

		private bool FlushScalableBufferManagerState()
		{
			if (s_GlobalHwUpresActive == HardwareDynamicResIsEnabled() && s_GlobalHwFraction == m_CurrentFraction)
			{
				return false;
			}
			s_GlobalHwUpresActive = HardwareDynamicResIsEnabled();
			s_GlobalHwFraction = m_CurrentFraction;
			ScalableBufferManager.ResizeBuffers(s_GlobalHwFraction, s_GlobalHwFraction);
			return true;
		}

		private static DynamicResolutionHandler GetOrCreateDrsInstanceHandler(Camera camera)
		{
			if (camera == null)
			{
				return null;
			}
			DynamicResolutionHandler value = null;
			int instanceID = camera.GetInstanceID();
			if (!s_CameraInstances.TryGetValue(instanceID, out value))
			{
				if (s_CameraInstances.Count >= 32)
				{
					int key = 0;
					DynamicResolutionHandler dynamicResolutionHandler = null;
					foreach (KeyValuePair<int, DynamicResolutionHandler> s_CameraInstance in s_CameraInstances)
					{
						if (s_CameraInstance.Value.m_OwnerCameraWeakRef == null || !s_CameraInstance.Value.m_OwnerCameraWeakRef.IsAlive)
						{
							dynamicResolutionHandler = s_CameraInstance.Value;
							key = s_CameraInstance.Key;
							break;
						}
					}
					if (dynamicResolutionHandler != null)
					{
						value = dynamicResolutionHandler;
						s_CameraInstances.Remove(key);
					}
				}
				if (value == null)
				{
					value = new DynamicResolutionHandler();
					value.m_OwnerCameraWeakRef = new WeakReference(camera);
				}
				else
				{
					value.Reset();
					value.m_OwnerCameraWeakRef.Target = camera;
				}
				s_CameraInstances.Add(instanceID, value);
			}
			return value;
		}

		private DynamicResolutionHandler()
		{
			Reset();
		}

		private static float DefaultDynamicResMethod()
		{
			return 1f;
		}

		private void ProcessSettings(GlobalDynamicResolutionSettings settings)
		{
			m_Enabled = settings.enabled && (Application.isPlaying || settings.forceResolution);
			if (!m_Enabled)
			{
				m_CurrentFraction = 1f;
			}
			else
			{
				type = settings.dynResType;
				float minScreenFraction = Mathf.Clamp(settings.minPercentage / 100f, 0.1f, 1f);
				m_MinScreenFraction = minScreenFraction;
				float maxScreenFraction = Mathf.Clamp(settings.maxPercentage / 100f, m_MinScreenFraction, 3f);
				m_MaxScreenFraction = maxScreenFraction;
				filter = settings.upsampleFilter;
				m_ForcingRes = settings.forceResolution;
				if (m_ForcingRes)
				{
					float currentFraction = Mathf.Clamp(settings.forcedPercentage / 100f, 0.1f, 1.5f);
					m_CurrentFraction = currentFraction;
				}
			}
			m_CachedSettings = settings;
		}

		public Vector2 GetResolvedScale()
		{
			if (!m_Enabled || !m_CurrentCameraRequest)
			{
				return new Vector2(1f, 1f);
			}
			float x = m_CurrentFraction;
			float y = m_CurrentFraction;
			if (!m_ForceSoftwareFallback && type == DynamicResolutionType.Hardware)
			{
				x = ScalableBufferManager.widthScaleFactor;
				y = ScalableBufferManager.heightScaleFactor;
			}
			return new Vector2(x, y);
		}

		public static void SetDynamicResScaler(PerformDynamicRes scaler, DynamicResScalePolicyType scalerType = DynamicResScalePolicyType.ReturnsMinMaxLerpFactor)
		{
			s_ScalerType = scalerType;
			s_DynamicResMethod = scaler;
		}

		public static void ClearSelectedCamera()
		{
			s_ActiveInstance = s_DefaultInstance;
			s_ActiveCameraId = 0;
			s_ActiveInstanceDirty = true;
		}

		public void SetCurrentCameraRequest(bool cameraRequest)
		{
			m_CurrentCameraRequest = cameraRequest;
		}

		public static void UpdateAndUseCamera(Camera camera, GlobalDynamicResolutionSettings? settings = null, Action OnResolutionChange = null)
		{
			int num;
			if (camera == null)
			{
				s_ActiveInstance = s_DefaultInstance;
				num = 0;
			}
			else
			{
				s_ActiveInstance = GetOrCreateDrsInstanceHandler(camera);
				num = camera.GetInstanceID();
			}
			s_ActiveInstanceDirty = num != s_ActiveCameraId;
			s_ActiveCameraId = num;
			s_ActiveInstance.Update(settings.HasValue ? settings.Value : s_ActiveInstance.m_CachedSettings, OnResolutionChange);
		}

		public void Update(GlobalDynamicResolutionSettings settings, Action OnResolutionChange = null)
		{
			ProcessSettings(settings);
			if (!m_Enabled && !s_ActiveInstanceDirty)
			{
				s_ActiveInstanceDirty = false;
				return;
			}
			if (!m_ForcingRes)
			{
				if (s_ScalerType == DynamicResScalePolicyType.ReturnsMinMaxLerpFactor)
				{
					float t = Mathf.Clamp(s_DynamicResMethod(), 0f, 1f);
					m_CurrentFraction = Mathf.Lerp(m_MinScreenFraction, m_MaxScreenFraction, t);
				}
				else if (s_ScalerType == DynamicResScalePolicyType.ReturnsPercentage)
				{
					float num = Mathf.Max(s_DynamicResMethod(), 5f);
					m_CurrentFraction = Mathf.Clamp(num / 100f, m_MinScreenFraction, m_MaxScreenFraction);
				}
			}
			bool flag = false;
			bool num2 = m_CurrentFraction != m_PrevFraction;
			m_PrevFraction = m_CurrentFraction;
			if (!m_ForceSoftwareFallback && type == DynamicResolutionType.Hardware)
			{
				flag = FlushScalableBufferManagerState();
				if (ScalableBufferManager.widthScaleFactor != m_PrevHWScaleWidth || ScalableBufferManager.heightScaleFactor != m_PrevHWScaleHeight)
				{
					flag = true;
				}
			}
			if (num2 || flag)
			{
				OnResolutionChange?.Invoke();
			}
			s_ActiveInstanceDirty = false;
			m_PrevHWScaleWidth = ScalableBufferManager.widthScaleFactor;
			m_PrevHWScaleHeight = ScalableBufferManager.heightScaleFactor;
		}

		public bool SoftwareDynamicResIsEnabled()
		{
			if (m_CurrentCameraRequest && m_Enabled && m_CurrentFraction != 1f)
			{
				if (!m_ForceSoftwareFallback)
				{
					return type == DynamicResolutionType.Software;
				}
				return true;
			}
			return false;
		}

		public bool HardwareDynamicResIsEnabled()
		{
			if (!m_ForceSoftwareFallback && m_CurrentCameraRequest && m_Enabled)
			{
				return type == DynamicResolutionType.Hardware;
			}
			return false;
		}

		public bool RequestsHardwareDynamicResolution()
		{
			if (m_ForceSoftwareFallback)
			{
				return false;
			}
			return type == DynamicResolutionType.Hardware;
		}

		public bool DynamicResolutionEnabled()
		{
			if (m_CurrentCameraRequest && m_Enabled)
			{
				return m_CurrentFraction != 1f;
			}
			return false;
		}

		public void ForceSoftwareFallback()
		{
			m_ForceSoftwareFallback = true;
		}

		public Vector2Int GetScaledSize(Vector2Int size)
		{
			cachedOriginalSize = size;
			if (!m_Enabled || !m_CurrentCameraRequest)
			{
				return size;
			}
			return m_LastScaledSize = ApplyScalesOnSize(size);
		}

		public Vector2Int ApplyScalesOnSize(Vector2Int size)
		{
			Vector2 resolvedScale = GetResolvedScale();
			Vector2Int result = new Vector2Int(Mathf.CeilToInt((float)size.x * resolvedScale.x), Mathf.CeilToInt((float)size.y * resolvedScale.y));
			if (m_ForceSoftwareFallback || type != DynamicResolutionType.Hardware)
			{
				result.x += 1 & result.x;
				result.y += 1 & result.y;
			}
			return result;
		}

		public float GetCurrentScale()
		{
			if (!m_Enabled || !m_CurrentCameraRequest)
			{
				return 1f;
			}
			return m_CurrentFraction;
		}

		public Vector2Int GetLastScaledSize()
		{
			return m_LastScaledSize;
		}
	}
}
