using System;

namespace UnityEngine.Rendering.Universal
{
	[Serializable]
	public class XRSystemData : ScriptableObject
	{
		[Serializable]
		[ReloadGroup]
		public sealed class ShaderResources
		{
			[Reload("Shaders/XR/XROcclusionMesh.shader", ReloadAttribute.Package.Root)]
			public Shader xrOcclusionMeshPS;

			[Reload("Shaders/XR/XRMirrorView.shader", ReloadAttribute.Package.Root)]
			public Shader xrMirrorViewPS;
		}

		public ShaderResources shaders;
	}
}
