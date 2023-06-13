using System;

namespace UnityEngine.Rendering.Universal
{
	[Serializable]
	[VolumeComponentMenu("Post-processing/Tonemapping")]
	public sealed class Tonemapping : VolumeComponent, IPostProcessComponent
	{
		[Tooltip("Select a tonemapping algorithm to use for the color grading process.")]
		public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None);

		public bool IsActive()
		{
			return mode.value != TonemappingMode.None;
		}

		public bool IsTileCompatible()
		{
			return true;
		}
	}
}
