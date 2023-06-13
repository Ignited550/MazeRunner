using System;
using System.ComponentModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
	[Serializable]
	[ReloadGroup]
	[ExcludeFromPreset]
	[MovedFrom("UnityEngine.Rendering.LWRP")]
	public class ForwardRendererData : ScriptableRendererData
	{
		[Serializable]
		[ReloadGroup]
		public sealed class ShaderResources
		{
			[Reload("Shaders/Utils/Blit.shader", ReloadAttribute.Package.Root)]
			public Shader blitPS;

			[Reload("Shaders/Utils/CopyDepth.shader", ReloadAttribute.Package.Root)]
			public Shader copyDepthPS;

			[Reload("Shaders/Utils/ScreenSpaceShadows.shader", ReloadAttribute.Package.Root)]
			public Shader screenSpaceShadowPS;

			[Reload("Shaders/Utils/Sampling.shader", ReloadAttribute.Package.Root)]
			public Shader samplingPS;

			[EditorBrowsable(EditorBrowsableState.Never)]
			public Shader tileDepthInfoPS;

			[EditorBrowsable(EditorBrowsableState.Never)]
			public Shader tileDeferredPS;

			[Reload("Shaders/Utils/StencilDeferred.shader", ReloadAttribute.Package.Root)]
			public Shader stencilDeferredPS;

			[Reload("Shaders/Utils/FallbackError.shader", ReloadAttribute.Package.Root)]
			public Shader fallbackErrorPS;

			[Reload("Shaders/Utils/MaterialError.shader", ReloadAttribute.Package.Root)]
			public Shader materialErrorPS;
		}

		[Reload("Runtime/Data/PostProcessData.asset", ReloadAttribute.Package.Root)]
		public PostProcessData postProcessData;

		[Reload("Runtime/Data/XRSystemData.asset", ReloadAttribute.Package.Root)]
		public XRSystemData xrSystemData;

		public ShaderResources shaders;

		[SerializeField]
		private LayerMask m_OpaqueLayerMask = -1;

		[SerializeField]
		private LayerMask m_TransparentLayerMask = -1;

		[SerializeField]
		private StencilStateData m_DefaultStencilState = new StencilStateData
		{
			passOperation = StencilOp.Replace
		};

		[SerializeField]
		private bool m_ShadowTransparentReceive = true;

		[SerializeField]
		private RenderingMode m_RenderingMode;

		[SerializeField]
		private bool m_AccurateGbufferNormals;

		public LayerMask opaqueLayerMask
		{
			get
			{
				return m_OpaqueLayerMask;
			}
			set
			{
				SetDirty();
				m_OpaqueLayerMask = value;
			}
		}

		public LayerMask transparentLayerMask
		{
			get
			{
				return m_TransparentLayerMask;
			}
			set
			{
				SetDirty();
				m_TransparentLayerMask = value;
			}
		}

		public StencilStateData defaultStencilState
		{
			get
			{
				return m_DefaultStencilState;
			}
			set
			{
				SetDirty();
				m_DefaultStencilState = value;
			}
		}

		public bool shadowTransparentReceive
		{
			get
			{
				return m_ShadowTransparentReceive;
			}
			set
			{
				SetDirty();
				m_ShadowTransparentReceive = value;
			}
		}

		public RenderingMode renderingMode
		{
			get
			{
				return m_RenderingMode;
			}
			set
			{
				SetDirty();
				m_RenderingMode = value;
			}
		}

		public bool accurateGbufferNormals
		{
			get
			{
				return m_AccurateGbufferNormals;
			}
			set
			{
				SetDirty();
				m_AccurateGbufferNormals = value;
			}
		}

		protected override ScriptableRenderer Create()
		{
			return new ForwardRenderer(this);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			_ = shaders;
		}
	}
}
