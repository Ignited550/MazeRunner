using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
	[Obsolete("This is obsolete, please use shadowCascadeCount instead.", false)]
	[MovedFrom("UnityEngine.Rendering.LWRP")]
	public enum ShadowCascadesOption
	{
		NoCascades = 0,
		TwoCascades = 1,
		FourCascades = 2
	}
}
