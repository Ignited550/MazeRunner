namespace UnityEngine.Rendering.Universal
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Light))]
	public class UniversalAdditionalLightData : MonoBehaviour
	{
		[Tooltip("Controls the usage of pipeline settings.")]
		[SerializeField]
		private bool m_UsePipelineSettings = true;

		public bool usePipelineSettings
		{
			get
			{
				return m_UsePipelineSettings;
			}
			set
			{
				m_UsePipelineSettings = value;
			}
		}
	}
}
