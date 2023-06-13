using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
	[ExcludeFromPreset]
	[MovedFrom("UnityEngine.Rendering.LWRP")]
	public abstract class ScriptableRendererFeature : ScriptableObject, IDisposable
	{
		[SerializeField]
		[HideInInspector]
		private bool m_Active = true;

		public bool isActive => m_Active;

		public abstract void Create();

		public abstract void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData);

		private void OnEnable()
		{
			Create();
		}

		private void OnValidate()
		{
			Create();
		}

		public void SetActive(bool active)
		{
			m_Active = active;
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
