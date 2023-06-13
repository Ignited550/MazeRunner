using System;
using UnityEngine.Animations;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.Universal
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[AddComponentMenu("Rendering/2D/Light 2D (Experimental)")]
	[HelpURL("https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/2DLightProperties.html")]
	public sealed class Light2D : MonoBehaviour
	{
		public enum LightType
		{
			Parametric = 0,
			Freeform = 1,
			Sprite = 2,
			Point = 3,
			Global = 4
		}

		public enum PointLightQuality
		{
			Fast = 0,
			Accurate = 1
		}

		[NotKeyable]
		[SerializeField]
		private LightType m_LightType;

		[SerializeField]
		[FormerlySerializedAs("m_LightOperationIndex")]
		private int m_BlendStyleIndex;

		[SerializeField]
		private float m_FalloffIntensity = 0.5f;

		[ColorUsage(false)]
		[SerializeField]
		private Color m_Color = Color.white;

		[SerializeField]
		private float m_Intensity = 1f;

		[SerializeField]
		private float m_LightVolumeOpacity;

		[SerializeField]
		private int[] m_ApplyToSortingLayers = new int[1];

		[SerializeField]
		private Sprite m_LightCookieSprite;

		[SerializeField]
		private bool m_UseNormalMap;

		[SerializeField]
		private int m_LightOrder;

		[SerializeField]
		private bool m_AlphaBlendOnOverlap;

		[Range(0f, 1f)]
		[SerializeField]
		private float m_ShadowIntensity;

		[Range(0f, 1f)]
		[SerializeField]
		private float m_ShadowVolumeIntensity;

		private int m_PreviousLightCookieSprite;

		private Mesh m_Mesh;

		private Bounds m_LocalBounds;

		[SerializeField]
		private float m_PointLightInnerAngle = 360f;

		[SerializeField]
		private float m_PointLightOuterAngle = 360f;

		[SerializeField]
		private float m_PointLightInnerRadius;

		[SerializeField]
		private float m_PointLightOuterRadius = 1f;

		[SerializeField]
		private float m_PointLightDistance = 3f;

		[NotKeyable]
		[SerializeField]
		private PointLightQuality m_PointLightQuality = PointLightQuality.Accurate;

		[SerializeField]
		private int m_ShapeLightParametricSides = 5;

		[SerializeField]
		private float m_ShapeLightParametricAngleOffset;

		[SerializeField]
		private float m_ShapeLightParametricRadius = 1f;

		[SerializeField]
		private float m_ShapeLightFalloffSize = 0.5f;

		[SerializeField]
		private Vector2 m_ShapeLightFalloffOffset = Vector2.zero;

		[SerializeField]
		private Vector3[] m_ShapePath;

		private float m_PreviousShapeLightFalloffSize = -1f;

		private int m_PreviousShapeLightParametricSides = -1;

		private float m_PreviousShapeLightParametricAngleOffset = -1f;

		private float m_PreviousShapeLightParametricRadius = -1f;

		internal int[] affectedSortingLayers => m_ApplyToSortingLayers;

		private int lightCookieSpriteInstanceID => m_LightCookieSprite?.GetInstanceID() ?? 0;

		internal BoundingSphere boundingSphere { get; private set; }

		internal Mesh lightMesh => m_Mesh;

		public LightType lightType
		{
			get
			{
				return m_LightType;
			}
			set
			{
				if (m_LightType != value)
				{
					UpdateMesh();
				}
				m_LightType = value;
				Light2DManager.ErrorIfDuplicateGlobalLight(this);
			}
		}

		public int blendStyleIndex
		{
			get
			{
				return m_BlendStyleIndex;
			}
			set
			{
				m_BlendStyleIndex = value;
			}
		}

		public float shadowIntensity
		{
			get
			{
				return m_ShadowIntensity;
			}
			set
			{
				m_ShadowIntensity = Mathf.Clamp01(value);
			}
		}

		public float shadowVolumeIntensity
		{
			get
			{
				return m_ShadowVolumeIntensity;
			}
			set
			{
				m_ShadowVolumeIntensity = Mathf.Clamp01(value);
			}
		}

		public Color color
		{
			get
			{
				return m_Color;
			}
			set
			{
				m_Color = value;
			}
		}

		public float intensity
		{
			get
			{
				return m_Intensity;
			}
			set
			{
				m_Intensity = value;
			}
		}

		public float volumeOpacity => m_LightVolumeOpacity;

		public Sprite lightCookieSprite => m_LightCookieSprite;

		public float falloffIntensity => m_FalloffIntensity;

		public bool useNormalMap => m_UseNormalMap;

		public bool alphaBlendOnOverlap => m_AlphaBlendOnOverlap;

		public int lightOrder
		{
			get
			{
				return m_LightOrder;
			}
			set
			{
				m_LightOrder = value;
			}
		}

		public float pointLightInnerAngle
		{
			get
			{
				return m_PointLightInnerAngle;
			}
			set
			{
				m_PointLightInnerAngle = value;
			}
		}

		public float pointLightOuterAngle
		{
			get
			{
				return m_PointLightOuterAngle;
			}
			set
			{
				m_PointLightOuterAngle = value;
			}
		}

		public float pointLightInnerRadius
		{
			get
			{
				return m_PointLightInnerRadius;
			}
			set
			{
				m_PointLightInnerRadius = value;
			}
		}

		public float pointLightOuterRadius
		{
			get
			{
				return m_PointLightOuterRadius;
			}
			set
			{
				m_PointLightOuterRadius = value;
			}
		}

		public float pointLightDistance => m_PointLightDistance;

		public PointLightQuality pointLightQuality => m_PointLightQuality;

		internal bool isPointLight => m_LightType == LightType.Point;

		public int shapeLightParametricSides => m_ShapeLightParametricSides;

		public float shapeLightParametricAngleOffset => m_ShapeLightParametricAngleOffset;

		public float shapeLightParametricRadius => m_ShapeLightParametricRadius;

		public float shapeLightFalloffSize => m_ShapeLightFalloffSize;

		public Vector2 shapeLightFalloffOffset => m_ShapeLightFalloffOffset;

		public Vector3[] shapePath => m_ShapePath;

		internal int GetTopMostLitLayer()
		{
			int num = -1;
			int num2 = 0;
			SortingLayer[] cachedSortingLayer = Light2DManager.GetCachedSortingLayer();
			for (int i = 0; i < m_ApplyToSortingLayers.Length; i++)
			{
				for (int num3 = cachedSortingLayer.Length - 1; num3 >= num2; num3--)
				{
					if (cachedSortingLayer[num3].id == m_ApplyToSortingLayers[i])
					{
						num = i;
						num2 = num3;
					}
				}
			}
			if (num >= 0)
			{
				return m_ApplyToSortingLayers[num];
			}
			return -1;
		}

		internal void UpdateMesh()
		{
			switch (m_LightType)
			{
			case LightType.Freeform:
				m_LocalBounds = LightUtility.GenerateShapeMesh(m_Mesh, m_ShapePath, m_ShapeLightFalloffSize);
				break;
			case LightType.Parametric:
				m_LocalBounds = LightUtility.GenerateParametricMesh(m_Mesh, m_ShapeLightParametricRadius, m_ShapeLightFalloffSize, m_ShapeLightParametricAngleOffset, m_ShapeLightParametricSides);
				break;
			case LightType.Sprite:
				m_LocalBounds = LightUtility.GenerateSpriteMesh(m_Mesh, m_LightCookieSprite);
				break;
			case LightType.Point:
				m_LocalBounds = LightUtility.GenerateParametricMesh(m_Mesh, 1.412135f, 0f, 0f, 4);
				break;
			}
		}

		internal void UpdateBoundingSphere()
		{
			if (isPointLight)
			{
				boundingSphere = new BoundingSphere(base.transform.position, m_PointLightOuterRadius);
				return;
			}
			Vector3 vector = base.transform.TransformPoint(Vector3.Max(m_LocalBounds.max, m_LocalBounds.max + (Vector3)m_ShapeLightFalloffOffset));
			Vector3 vector2 = base.transform.TransformPoint(Vector3.Min(m_LocalBounds.min, m_LocalBounds.min + (Vector3)m_ShapeLightFalloffOffset));
			Vector3 vector3 = 0.5f * (vector + vector2);
			float rad = Vector3.Magnitude(vector - vector3);
			boundingSphere = new BoundingSphere(vector3, rad);
		}

		internal bool IsLitLayer(int layer)
		{
			if (m_ApplyToSortingLayers == null)
			{
				return false;
			}
			return Array.IndexOf(m_ApplyToSortingLayers, layer) >= 0;
		}

		private void Awake()
		{
			m_Mesh = new Mesh();
			UpdateMesh();
		}

		private void OnEnable()
		{
			m_PreviousLightCookieSprite = lightCookieSpriteInstanceID;
			Light2DManager.RegisterLight(this);
		}

		private void OnDisable()
		{
			Light2DManager.DeregisterLight(this);
		}

		private void LateUpdate()
		{
			if (m_LightType != LightType.Global)
			{
				if (LightUtility.CheckForChange(m_ShapeLightFalloffSize, ref m_PreviousShapeLightFalloffSize) || LightUtility.CheckForChange(m_ShapeLightParametricRadius, ref m_PreviousShapeLightParametricRadius) || LightUtility.CheckForChange(m_ShapeLightParametricSides, ref m_PreviousShapeLightParametricSides) || LightUtility.CheckForChange(m_ShapeLightParametricAngleOffset, ref m_PreviousShapeLightParametricAngleOffset) || LightUtility.CheckForChange(lightCookieSpriteInstanceID, ref m_PreviousLightCookieSprite))
				{
					UpdateMesh();
				}
				UpdateBoundingSphere();
			}
		}
	}
}
