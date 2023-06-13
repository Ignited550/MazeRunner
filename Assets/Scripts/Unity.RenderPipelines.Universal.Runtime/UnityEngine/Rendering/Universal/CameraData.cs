using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
	[MovedFrom("UnityEngine.Rendering.LWRP")]
	public struct CameraData
	{
		private Matrix4x4 m_ViewMatrix;

		private Matrix4x4 m_ProjectionMatrix;

		public Camera camera;

		public CameraRenderType renderType;

		public RenderTexture targetTexture;

		public RenderTextureDescriptor cameraTargetDescriptor;

		internal Rect pixelRect;

		internal int pixelWidth;

		internal int pixelHeight;

		internal float aspectRatio;

		public float renderScale;

		public bool clearDepth;

		public CameraType cameraType;

		public bool isDefaultViewport;

		public bool isHdrEnabled;

		public bool requiresDepthTexture;

		public bool requiresOpaqueTexture;

		public bool xrRendering;

		public SortingCriteria defaultOpaqueSortFlags;

		internal XRPass xr;

		[Obsolete("Please use xr.enabled instead.")]
		public bool isStereoEnabled;

		public float maxShadowDistance;

		public bool postProcessEnabled;

		public IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> captureActions;

		public LayerMask volumeLayerMask;

		public Transform volumeTrigger;

		public bool isStopNaNEnabled;

		public bool isDitheringEnabled;

		public AntialiasingMode antialiasing;

		public AntialiasingQuality antialiasingQuality;

		public ScriptableRenderer renderer;

		public bool resolveFinalTarget;

		internal bool requireSrgbConversion
		{
			get
			{
				if (xr.enabled)
				{
					if (!xr.renderTargetDesc.sRGB)
					{
						return QualitySettings.activeColorSpace == ColorSpace.Linear;
					}
					return false;
				}
				return Display.main.requiresSrgbBlitToBackbuffer;
			}
		}

		public bool isSceneViewCamera => cameraType == CameraType.SceneView;

		public bool isPreviewCamera => cameraType == CameraType.Preview;

		internal void SetViewAndProjectionMatrix(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
		{
			m_ViewMatrix = viewMatrix;
			m_ProjectionMatrix = projectionMatrix;
		}

		public Matrix4x4 GetViewMatrix(int viewIndex = 0)
		{
			if (xr.enabled)
			{
				return xr.GetViewMatrix(viewIndex);
			}
			return m_ViewMatrix;
		}

		public Matrix4x4 GetProjectionMatrix(int viewIndex = 0)
		{
			if (xr.enabled)
			{
				return xr.GetProjMatrix(viewIndex);
			}
			return m_ProjectionMatrix;
		}

		public Matrix4x4 GetGPUProjectionMatrix(int viewIndex = 0)
		{
			return GL.GetGPUProjectionMatrix(GetProjectionMatrix(viewIndex), IsCameraProjectionMatrixFlipped());
		}

		public bool IsCameraProjectionMatrixFlipped()
		{
			ScriptableRenderer current = ScriptableRenderer.current;
			if (current != null)
			{
				bool flag = current.cameraColorTarget == BuiltinRenderTextureType.CameraTarget;
				if (xr.enabled)
				{
					flag |= current.cameraColorTarget == xr.renderTarget && !xr.renderTargetIsRenderTexture;
				}
				bool flag2 = !flag || targetTexture != null;
				return SystemInfo.graphicsUVStartsAtTop && flag2;
			}
			return true;
		}
	}
}
