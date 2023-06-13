using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
	[MovedFrom("UnityEngine.Rendering.LWRP")]
	public struct RenderingData
	{
		public CullingResults cullResults;

		public CameraData cameraData;

		public LightData lightData;

		public ShadowData shadowData;

		public PostProcessingData postProcessingData;

		public bool supportsDynamicBatching;

		public PerObjectData perObjectData;

		public bool postProcessingEnabled;
	}
}
